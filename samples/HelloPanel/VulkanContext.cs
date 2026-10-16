using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Brutal.GlfwApi;
using Brutal.Numerics;
using Brutal.VulkanApi;
using Brutal.VulkanApi.Abstractions;

namespace HelloPanel;

/// <summary>
/// Owns the Vulkan objects that draw into one GLFW window: the instance, the surface, the device, the
/// swapchain, the render pass, the framebuffers, the command buffers, and the synchronization objects.
/// </summary>
/// <remarks>
/// All swapchain code stays in this class. A later offscreen target can replace it with no change to
/// <see cref="Program"/>. The window has a fixed size, so this class does not re-create the swapchain.
/// The BRUTAL wrappers throw <see cref="VkResultError"/> on <c>VK_ERROR_OUT_OF_DATE_KHR</c>, so an
/// out-of-date swapchain stops the sample with exit code 1.
/// </remarks>
internal sealed unsafe class VulkanContext : IDisposable
{
    /// <summary>The settings for a new <see cref="VulkanContext"/>.</summary>
    public struct CreateInfo
    {
        /// <summary>The window to draw into. The window must not be resizable.</summary>
        public required GlfwWindow Window;

        /// <summary>The application name that the Vulkan instance reports to the driver.</summary>
        public required string ApplicationName;
    }

    /// <summary>The number of frames that the CPU records while the GPU draws earlier frames.</summary>
    private const int FramesInFlight = 2;

    /// <summary>The Vulkan API version 1.1, in the <c>VK_MAKE_API_VERSION</c> encoding.</summary>
    private const int ApiVersion11 = (1 << 22) | (1 << 12);

    /// <summary>The timeout value that means "wait with no limit".</summary>
    private const nint NoTimeout = -1;

    /// <summary>The value of <c>currentExtent.width</c> when the window sets the swapchain size.</summary>
    private const int ExtentFromSwapchain = -1;

    private readonly Instance _instance;
    private readonly VkSurfaceKHR _surface;
    private readonly PhysicalDevice _physicalDevice;
    private readonly int _graphicsFamily;
    private readonly int _presentFamily;
    private readonly DeviceEx _device;
    private readonly Queue _presentQueue;
    private readonly VkSurfaceFormatKHR _surfaceFormat;
    private readonly VkExtent2D _extent;
    private readonly VkSwapchainKHR _swapchain;
    private readonly VkImage[] _images = [];
    private readonly VkImageView[] _imageViews = [];
    private readonly VkFramebuffer[] _framebuffers = [];
    private readonly VkCommandPool _commandPool;
    private readonly CommandBuffer[] _commandBuffers = [];
    private readonly VkSemaphore[] _imageAcquired = [];
    private readonly VkFence[] _inFlight = [];
    private readonly VkSemaphore[] _renderComplete = [];

    private int _frameSlot;
    private int _imageIndex;
    private bool _frameOpen;
    private bool _disposed;

    /// <summary>
    /// Creates the Vulkan instance, the window surface, the device, and the frame resources.
    /// </summary>
    /// <param name="info">The settings.</param>
    /// <exception cref="InvalidOperationException">No Vulkan device can draw to the window.</exception>
    /// <exception cref="VkResultError">A Vulkan call fails.</exception>
    public VulkanContext(in CreateInfo info)
    {
        try
        {
            _instance = CreateInstance(info.ApplicationName);
            _surface = CreateSurface(_instance, info.Window);
            (_physicalDevice, _graphicsFamily, _presentFamily) = SelectPhysicalDevice(_instance, _surface);
            _device = CreateDevice(_physicalDevice, _graphicsFamily, _presentFamily);
            GraphicsQueue = _device.GetQueue(_graphicsFamily, 0);
            _presentQueue = _device.GetQueue(_presentFamily, 0);

            VkSurfaceCapabilitiesKHR capabilities = _physicalDevice.GetSurfaceCapabilitiesKHR(_surface);
            _surfaceFormat = SelectSurfaceFormat(_physicalDevice, _surface);
            _extent = SelectExtent(capabilities, info.Window.FramebufferSize);
            MinImageCount = SelectMinImageCount(capabilities);
            _swapchain = CreateSwapchain(capabilities);

            int imageCount = _device.GetSwapchainImagesKHR_Count(_swapchain);
            _images = new VkImage[imageCount];
            CheckResult(_device.GetSwapchainImagesKHR_Write(_swapchain, _images), "vkGetSwapchainImagesKHR");

            RenderPass = CreateRenderPass(_surfaceFormat.Format);

            _imageViews = new VkImageView[imageCount];
            _framebuffers = new VkFramebuffer[imageCount];
            _renderComplete = new VkSemaphore[imageCount];
            for (int i = 0; i < imageCount; i++)
            {
                _imageViews[i] = CreateImageView(_images[i]);
                _framebuffers[i] = CreateFramebuffer(_imageViews[i]);
                _renderComplete[i] = _device.CreateSemaphore(new VkSemaphoreCreateInfo(), null);
            }

            _commandPool = _device.CreateCommandPool(
                new VkCommandPoolCreateInfo
                {
                    Flags = VkCommandPoolCreateFlags.ResetCommandBufferBit,
                    QueueFamilyIndex = _graphicsFamily,
                },
                null);

            _commandBuffers = new CommandBuffer[FramesInFlight];
            _device.AllocateCommandBuffers(
                new VkCommandBufferAllocateInfo
                {
                    CommandPool = _commandPool,
                    Level = VkCommandBufferLevel.Primary,
                },
                _commandBuffers);

            _imageAcquired = new VkSemaphore[FramesInFlight];
            _inFlight = new VkFence[FramesInFlight];
            for (int i = 0; i < FramesInFlight; i++)
            {
                _imageAcquired[i] = _device.CreateSemaphore(new VkSemaphoreCreateInfo(), null);
                _inFlight[i] = _device.CreateFence(new VkFenceCreateInfo { Flags = VkFenceCreateFlags.SignaledBit }, null);
            }
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <summary>Gets the logical device. The ImGui Vulkan backend needs it.</summary>
    public DeviceEx Device => _device;

    /// <summary>Gets the queue that records the draw commands and uploads the textures.</summary>
    public Queue GraphicsQueue { get; }

    /// <summary>Gets the render pass that draws into a swapchain image.</summary>
    public VkRenderPass RenderPass { get; }

    /// <summary>Gets the minimum swapchain image count that this context requests. The value is 2 or more.</summary>
    public int MinImageCount { get; }

    /// <summary>Gets the number of swapchain images.</summary>
    public int ImageCount => _images.Length;

    /// <summary>
    /// Waits for the frame slot, acquires a swapchain image, and opens the render pass.
    /// </summary>
    /// <returns>The command buffer in the open render pass. Record the draw commands into it.</returns>
    public CommandBuffer BeginFrame()
    {
        if (_frameOpen)
        {
            throw new InvalidOperationException("BeginFrame occurs two times with no EndFrame between them.");
        }

        VkFence fence = _inFlight[_frameSlot];
        CheckResult(_device.WaitForFence(fence, NoTimeout), "vkWaitForFences");

        VkResult acquire = _device.AcquireNextImageKHR(_swapchain, NoTimeout, _imageAcquired[_frameSlot], default, out _imageIndex);
        if (acquire != VkResult.Success && acquire != VkResult.SuboptimalKHR)
        {
            throw new InvalidOperationException($"vkAcquireNextImageKHR returns {acquire}.");
        }

        // Reset the fence only after the acquire succeeds. A failed acquire then leaves the fence signaled.
        _device.ResetFences(new ReadOnlySpan<VkFence>(in fence));

        CommandBuffer commandBuffer = _commandBuffers[_frameSlot];
        commandBuffer.Reset(VkCommandBufferResetFlags.None);
        commandBuffer.Begin(new VkCommandBufferBeginInfo { Flags = VkCommandBufferUsageFlags.OneTimeSubmitBit });

        VkClearValue clearValue = new()
        {
            Color = new VkClearColorValue { Float32 = new float4(0.10f, 0.10f, 0.12f, 1.0f) },
        };
        commandBuffer.BeginRenderPass(
            new VkRenderPassBeginInfo
            {
                RenderPass = RenderPass,
                Framebuffer = _framebuffers[_imageIndex],
                RenderArea = new VkRect2D { Extent = _extent },
                ClearValueCount = 1,
                ClearValues = &clearValue,
            },
            VkSubpassContents.Inline);

        _frameOpen = true;
        return commandBuffer;
    }

    /// <summary>
    /// Closes the render pass, submits the command buffer, and presents the swapchain image.
    /// </summary>
    public void EndFrame()
    {
        if (!_frameOpen)
        {
            throw new InvalidOperationException("EndFrame occurs with no BeginFrame before it.");
        }

        _frameOpen = false;

        CommandBuffer commandBuffer = _commandBuffers[_frameSlot];
        commandBuffer.EndRenderPass();
        commandBuffer.End();

        VkSemaphore waitSemaphore = _imageAcquired[_frameSlot];
        VkPipelineStageFlags waitStage = VkPipelineStageFlags.ColorAttachmentOutputBit;
        VkSemaphore signalSemaphore = _renderComplete[_imageIndex];
        GraphicsQueue.Submit(
            new Span<VkSemaphore>(ref waitSemaphore),
            new Span<VkPipelineStageFlags>(ref waitStage),
            new Span<CommandBuffer>(ref commandBuffer),
            new Span<VkSemaphore>(ref signalSemaphore),
            _inFlight[_frameSlot]);

        VkSwapchainKHR swapchain = _swapchain;
        int imageIndex = _imageIndex;
        VkResult present = _presentQueue.PresentKHR(new VkPresentInfoKHR
        {
            WaitSemaphoreCount = 1,
            WaitSemaphores = &signalSemaphore,
            SwapchainCount = 1,
            Swapchains = &swapchain,
            ImageIndices = &imageIndex,
        });
        if (present != VkResult.Success && present != VkResult.SuboptimalKHR)
        {
            throw new InvalidOperationException($"vkQueuePresentKHR returns {present}.");
        }

        _frameSlot = (_frameSlot + 1) % FramesInFlight;
    }

    /// <summary>Waits until the device finishes all submitted work.</summary>
    public void WaitIdle()
    {
        _device.WaitIdle();
    }

    /// <summary>
    /// Waits for the device, then destroys all objects in the reverse order of creation.
    /// A partly constructed context is also safe to dispose.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // The constructor calls Dispose on a failure, so each field can still be null or empty here.
        if (_device is not null)
        {
            _device.WaitIdle();

            foreach (VkFence fence in _inFlight)
            {
                _device.DestroyFence(fence, null);
            }

            foreach (VkSemaphore semaphore in _imageAcquired)
            {
                _device.DestroySemaphore(semaphore, null);
            }

            // Destroying the pool also frees its command buffers.
            _device.DestroyCommandPool(_commandPool, null);

            foreach (VkSemaphore semaphore in _renderComplete)
            {
                _device.DestroySemaphore(semaphore, null);
            }

            foreach (VkFramebuffer framebuffer in _framebuffers)
            {
                _device.DestroyFramebuffer(framebuffer, null);
            }

            foreach (VkImageView imageView in _imageViews)
            {
                _device.DestroyImageView(imageView, null);
            }

            _device.DestroyRenderPass(RenderPass, null);
            _device.DestroySwapchainKHR(_swapchain, null);
            _device.Dispose();
        }

        if (_instance is not null)
        {
            _instance.DestroySurfaceKHR(_surface, null);
            _instance.Dispose();
        }
    }

    /// <summary>Creates the Vulkan instance with the extensions that GLFW needs for a window surface.</summary>
    private static Instance CreateInstance(string applicationName)
    {
        byte** requiredNames = Glfw.GetRequiredInstanceExtensions(out uint requiredCount);
        if (requiredNames == null)
        {
            GlfwErrorCode code = Glfw.GetError(out string? description);
            throw new InvalidOperationException($"GLFW finds no Vulkan surface extensions: {code} ({description}).");
        }

        HashSet<string> extensions = [];
        for (uint i = 0; i < requiredCount; i++)
        {
            string name = Marshal.PtrToStringUTF8((nint)requiredNames[i])
                ?? throw new InvalidOperationException("GLFW returns a null extension name.");
            extensions.Add(name);
        }

        VulkanHelpers.AddSurfaceExtensions(extensions);
        VulkanHelpers.AddPlatformRequiredExtensions(extensions);

        // Vulkan needs a null-terminated UTF-8 name.
        byte[] nameBytes = Encoding.UTF8.GetBytes(applicationName + "\0");
        fixed (byte* namePointer = nameBytes)
        {
            return Vulkan.CreateInstance(new Instance.CreateInfo
            {
                AppInfo = new VkApplicationInfo
                {
                    ApplicationName = namePointer,
                    ApplicationVersion = 1,
                    ApiVersion = ApiVersion11,
                },
                Flags = VulkanHelpers.GetPlatformRequiredInstanceCreateFlags(),
                EnabledExtensions = extensions,
                EnabledLayers = [],
            });
        }
    }

    /// <summary>Creates the Vulkan surface of the window through GLFW.</summary>
    private static VkSurfaceKHR CreateSurface(Instance instance, GlfwWindow window)
    {
        int result = Glfw.CreateWindowSurface(instance.VkHandle, window, out ulong surface);
        if (result != (int)VkResult.Success)
        {
            throw new InvalidOperationException($"glfwCreateWindowSurface returns {(VkResult)result}.");
        }

        return new VkSurfaceKHR((nint)surface);
    }

    /// <summary>
    /// Selects a device that supports the swapchain extension and has a graphics queue and a present queue.
    /// A discrete GPU comes first. One family with both capabilities comes before two families.
    /// </summary>
    private static (PhysicalDevice Device, int GraphicsFamily, int PresentFamily) SelectPhysicalDevice(Instance instance, VkSurfaceKHR surface)
    {
        PhysicalDevice? best = null;
        int bestScore = -1;
        int bestGraphics = -1;
        int bestPresent = -1;

        foreach (PhysicalDevice candidate in instance.PhysicalDevices)
        {
            if (!candidate.SupportsExtension("VK_KHR_swapchain"u8))
            {
                continue;
            }

            if (candidate.GetSurfaceFormatsKHR_Count(surface) == 0 || candidate.GetSurfacePresentModesKHR_Count(surface) == 0)
            {
                continue;
            }

            VkQueueFamilyProperties[] families = new VkQueueFamilyProperties[candidate.GetQueueFamilyProperties_Count()];
            candidate.GetQueueFamilyProperties_Write(families);

            int graphics = -1;
            int present = -1;
            for (int i = 0; i < families.Length; i++)
            {
                bool hasGraphics = (families[i].QueueFlags & VkQueueFlags.GraphicsBit) != 0;
                bool hasPresent = candidate.GetSurfaceSupportKHR(i, surface);
                if (hasGraphics && hasPresent)
                {
                    graphics = i;
                    present = i;
                    break;
                }

                if (hasGraphics && graphics < 0)
                {
                    graphics = i;
                }

                if (hasPresent && present < 0)
                {
                    present = i;
                }
            }

            if (graphics < 0 || present < 0)
            {
                continue;
            }

            int score = candidate.GetProperties().DeviceType switch
            {
                VkPhysicalDeviceType.DiscreteGpu => 3,
                VkPhysicalDeviceType.IntegratedGpu => 2,
                VkPhysicalDeviceType.VirtualGpu => 1,
                _ => 0,
            };

            if (score > bestScore)
            {
                best = candidate;
                bestScore = score;
                bestGraphics = graphics;
                bestPresent = present;
            }
        }

        if (best is null)
        {
            throw new InvalidOperationException("No Vulkan device can draw to the window surface and present it.");
        }

        return (best, bestGraphics, bestPresent);
    }

    /// <summary>Creates the logical device with one queue per family and the swapchain extension.</summary>
    private static DeviceEx CreateDevice(PhysicalDevice physicalDevice, int graphicsFamily, int presentFamily)
    {
        float priority = 1.0f;
        Span<VkDeviceQueueCreateInfo> queues = stackalloc VkDeviceQueueCreateInfo[2];
        queues[0] = new VkDeviceQueueCreateInfo
        {
            QueueFamilyIndex = graphicsFamily,
            QueueCount = 1,
            QueuePriorities = &priority,
        };
        int queueCount = 1;
        if (presentFamily != graphicsFamily)
        {
            queues[1] = new VkDeviceQueueCreateInfo
            {
                QueueFamilyIndex = presentFamily,
                QueueCount = 1,
                QueuePriorities = &priority,
            };
            queueCount = 2;
        }

        DeviceEx.CreateInfo createInfo = new()
        {
            QueueCreateInfos = queues[..queueCount],
        };
        VulkanHelpers.AddSwapchainExtensions(createInfo.Extensions);
        VulkanHelpers.AddMandatoryIfSupportedDeviceExtensions(createInfo.Extensions, physicalDevice);
        return physicalDevice.CreateDevice(createInfo);
    }

    /// <summary>Selects an 8-bit UNORM format with the sRGB color space, or the first format of the surface.</summary>
    private static VkSurfaceFormatKHR SelectSurfaceFormat(PhysicalDevice physicalDevice, VkSurfaceKHR surface)
    {
        VkSurfaceFormatKHR[] formats = new VkSurfaceFormatKHR[physicalDevice.GetSurfaceFormatsKHR_Count(surface)];
        CheckResult(physicalDevice.GetSurfaceFormatsKHR_Write(surface, formats), "vkGetPhysicalDeviceSurfaceFormatsKHR");

        if (formats.Length == 1 && formats[0].Format == VkFormat.Undefined)
        {
            return new VkSurfaceFormatKHR { Format = VkFormat.B8G8R8A8UNorm, ColorSpace = VkColorSpaceKHR.SRGBNonlinearKHR };
        }

        // ImGui gives its colors in sRGB space already, so a UNORM target shows them as ImGui expects.
        ReadOnlySpan<VkFormat> preferred = [VkFormat.B8G8R8A8UNorm, VkFormat.R8G8B8A8UNorm];
        foreach (VkFormat format in preferred)
        {
            foreach (VkSurfaceFormatKHR candidate in formats)
            {
                if (candidate.Format == format && candidate.ColorSpace == VkColorSpaceKHR.SRGBNonlinearKHR)
                {
                    return candidate;
                }
            }
        }

        return formats[0];
    }

    /// <summary>Selects the swapchain size. The surface size wins. Otherwise the framebuffer size of the window applies.</summary>
    private static VkExtent2D SelectExtent(in VkSurfaceCapabilitiesKHR capabilities, int2 framebufferSize)
    {
        VkExtent2D extent = capabilities.CurrentExtent;
        if (extent.Width == ExtentFromSwapchain)
        {
            extent = new VkExtent2D
            {
                Width = Math.Clamp(framebufferSize.X, capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width),
                Height = Math.Clamp(framebufferSize.Y, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height),
            };
        }

        if (extent.Width <= 0 || extent.Height <= 0)
        {
            throw new InvalidOperationException($"The window surface has no area ({extent.Width} x {extent.Height}).");
        }

        return extent;
    }

    /// <summary>Selects one image more than the surface minimum, and at least 2, as the ImGui backend needs.</summary>
    private static int SelectMinImageCount(in VkSurfaceCapabilitiesKHR capabilities)
    {
        int count = Math.Max(capabilities.MinImageCount + 1, 2);
        if (capabilities.MaxImageCount > 0)
        {
            count = Math.Min(count, capabilities.MaxImageCount);
        }

        if (count < 2)
        {
            throw new InvalidOperationException($"The surface allows {capabilities.MaxImageCount} swapchain image. The sample needs 2 or more.");
        }

        return count;
    }

    /// <summary>Selects the opaque composite mode, or the first mode that the surface supports.</summary>
    private static VkCompositeAlphaFlagsKHR SelectCompositeAlpha(VkCompositeAlphaFlagsKHR supported)
    {
        ReadOnlySpan<VkCompositeAlphaFlagsKHR> preferred =
        [
            VkCompositeAlphaFlagsKHR.OpaqueBitKHR,
            VkCompositeAlphaFlagsKHR.InheritBitKHR,
            VkCompositeAlphaFlagsKHR.PreMultipliedBitKHR,
            VkCompositeAlphaFlagsKHR.PostMultipliedBitKHR,
        ];
        foreach (VkCompositeAlphaFlagsKHR mode in preferred)
        {
            if ((supported & mode) != 0)
            {
                return mode;
            }
        }

        throw new InvalidOperationException("The surface supports no composite alpha mode.");
    }

    /// <summary>Checks that a Vulkan call returns <see cref="VkResult.Success"/>.</summary>
    private static void CheckResult(VkResult result, string call)
    {
        if (result != VkResult.Success)
        {
            throw new InvalidOperationException($"{call} returns {result}.");
        }
    }

    /// <summary>Creates the swapchain with the FIFO present mode, which every Vulkan driver supports.</summary>
    private VkSwapchainKHR CreateSwapchain(in VkSurfaceCapabilitiesKHR capabilities)
    {
        bool concurrent = _graphicsFamily != _presentFamily;
        int* families = stackalloc int[2] { _graphicsFamily, _presentFamily };

        return _device.CreateSwapchainKHR(
            new VkSwapchainCreateInfoKHR
            {
                Surface = _surface,
                MinImageCount = MinImageCount,
                ImageFormat = _surfaceFormat.Format,
                ImageColorSpace = _surfaceFormat.ColorSpace,
                ImageExtent = _extent,
                ImageArrayLayers = 1,
                ImageUsage = VkImageUsageFlags.ColorAttachmentBit,
                ImageSharingMode = concurrent ? VkSharingMode.Concurrent : VkSharingMode.Exclusive,
                QueueFamilyIndexCount = concurrent ? 2 : 0,
                QueueFamilyIndices = concurrent ? families : null,
                PreTransform = capabilities.CurrentTransform,
                CompositeAlpha = SelectCompositeAlpha(capabilities.SupportedCompositeAlpha),
                PresentMode = VkPresentModeKHR.FifoKHR,
                Clipped = true,
            },
            null);
    }

    /// <summary>Creates a color view of one swapchain image.</summary>
    private VkImageView CreateImageView(VkImage image)
    {
        return _device.CreateImageView(
            new VkImageViewCreateInfo
            {
                Image = image,
                ViewType = VkImageViewType._2D,
                Format = _surfaceFormat.Format,
                SubresourceRange = new VkImageSubresourceRange
                {
                    AspectMask = VkImageAspectFlags.ColorBit,
                    LevelCount = 1,
                    LayerCount = 1,
                },
            },
            null);
    }

    /// <summary>
    /// Creates a render pass with one color attachment. The pass clears the image and leaves it ready to present.
    /// </summary>
    private VkRenderPass CreateRenderPass(VkFormat format)
    {
        VkAttachmentDescription color = new()
        {
            Format = format,
            Samples = VkSampleCountFlags._1Bit,
            LoadOp = VkAttachmentLoadOp.Clear,
            StoreOp = VkAttachmentStoreOp.Store,
            StencilLoadOp = VkAttachmentLoadOp.DontCare,
            StencilStoreOp = VkAttachmentStoreOp.DontCare,
            InitialLayout = VkImageLayout.Undefined,
            FinalLayout = VkImageLayout.PresentSrcKHR,
        };
        VkAttachmentReference colorReference = new()
        {
            Attachment = 0,
            Layout = VkImageLayout.ColorAttachmentOptimal,
        };
        VkSubpassDescription subpass = new()
        {
            PipelineBindPoint = VkPipelineBindPoint.Graphics,
            ColorAttachmentCount = 1,
            ColorAttachments = &colorReference,
        };

        // The image layout change waits until the acquire semaphore signals.
        VkSubpassDependency dependency = new()
        {
            SrcSubpass = VK.SUBPASS_EXTERNAL,
            DstSubpass = 0,
            SrcStageMask = VkPipelineStageFlags.ColorAttachmentOutputBit,
            DstStageMask = VkPipelineStageFlags.ColorAttachmentOutputBit,
            SrcAccessMask = VkAccessFlags.None,
            DstAccessMask = VkAccessFlags.ColorAttachmentWriteBit,
        };

        return _device.CreateRenderPass(
            new VkRenderPassCreateInfo
            {
                AttachmentCount = 1,
                Attachments = &color,
                SubpassCount = 1,
                Subpasses = &subpass,
                DependencyCount = 1,
                Dependencies = &dependency,
            },
            null);
    }

    /// <summary>Creates a framebuffer for one swapchain image view.</summary>
    private VkFramebuffer CreateFramebuffer(VkImageView imageView)
    {
        return _device.CreateFramebuffer(
            new VkFramebufferCreateInfo
            {
                RenderPass = RenderPass,
                AttachmentCount = 1,
                Attachments = &imageView,
                Width = _extent.Width,
                Height = _extent.Height,
                Layers = 1,
            },
            null);
    }
}
