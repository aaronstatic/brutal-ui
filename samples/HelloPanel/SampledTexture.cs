using System;
using Brutal;
using Brutal.Numerics;
using Brutal.VulkanApi;
using Brutal.VulkanApi.Abstractions;

namespace HelloPanel;

/// <summary>
/// Owns a 2D RGBA texture that a fragment shader samples: the image, its device memory, the image view, and the
/// sampler.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="VulkanContext.CreateTexture"/> creates the texture and fills it. When that method returns, the image is
/// in <see cref="VkImageLayout.ShaderReadOnlyOptimal"/>, and <see cref="View"/> and <see cref="Sampler"/> are valid.
/// </para>
/// <para>
/// Keep the texture alive while a descriptor set names its view or its sampler. Dispose the texture before the device.
/// </para>
/// </remarks>
internal sealed class SampledTexture : IDisposable
{
    /// <summary>
    /// The format of the image. UNORM keeps the bytes as they are, so the texel colors match the <c>Rgba8</c> colors of
    /// the UI on the UNORM swapchain.
    /// </summary>
    public const VkFormat Format = VkFormat.R8G8B8A8UNorm;

    private readonly Device _device;
    private VkImage _image;
    private VkDeviceMemory _memory;
    private VkImageView _view;
    private VkSampler _sampler;
    private bool _disposed;

    /// <summary>
    /// Creates the image in device-local memory. The image has the layout <see cref="VkImageLayout.Undefined"/>, and
    /// the texture has no view and no sampler yet.
    /// </summary>
    /// <remarks>
    /// The image is 2D, with 1 mip level, 1 layer, 1 sample, and optimal tiling. Its usage is transfer destination and
    /// sampled, with exclusive sharing. On a failure, the constructor frees each object that it created.
    /// </remarks>
    /// <param name="device">The logical device that owns the texture.</param>
    /// <param name="size">The size of the image in texels. Each component is 1 or more.</param>
    /// <exception cref="ArgumentNullException"><paramref name="device"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A component of <paramref name="size"/> is 0 or less.</exception>
    /// <exception cref="VkResultError">A Vulkan call fails.</exception>
    public SampledTexture(Device device, int2 size)
    {
        ArgumentNullException.ThrowIfNull(device);
        if (size.X <= 0 || size.Y <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "Each component of the size must be 1 or more.");
        }

        _device = device;
        Size = size;
        try
        {
            _image = device.CreateImage(
                new VkImageCreateInfo
                {
                    ImageType = VkImageType._2D,
                    Format = Format,
                    Extent = new VkExtent3D(size.X, size.Y, 1),
                    MipLevels = 1,
                    ArrayLayers = 1,
                    Samples = VkSampleCountFlags._1Bit,
                    Tiling = VkImageTiling.Optimal,
                    Usage = VkImageUsageFlags.TransferDstBit | VkImageUsageFlags.SampledBit,
                    SharingMode = VkSharingMode.Exclusive,
                    InitialLayout = VkImageLayout.Undefined,
                },
                null);
            _memory = device.AllocateMemory(device.PhysicalDevice, _image, VkMemoryPropertyFlags.DeviceLocalBit, null);
            device.BindImageMemory(_image, _memory, new ByteSize64(0));
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <summary>Gets the image. The upload records its copy and its layout changes on this image.</summary>
    public VkImage Image => _image;

    /// <summary>Gets the 2D color view of the image, or a null handle before <see cref="CreateViewAndSampler"/>.</summary>
    public VkImageView View => _view;

    /// <summary>Gets the sampler, or a null handle before <see cref="CreateViewAndSampler"/>.</summary>
    /// <remarks>The sampler uses the nearest filter and clamps to the edge on each axis, so each texel has sharp edges.</remarks>
    public VkSampler Sampler => _sampler;

    /// <summary>Gets the size of the image in texels.</summary>
    public int2 Size { get; }

    /// <summary>
    /// Creates the image view and the sampler. Call this method one time, after the upload.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The texture is disposed.</exception>
    /// <exception cref="InvalidOperationException">The texture already has a view.</exception>
    /// <exception cref="VkResultError">A Vulkan call fails.</exception>
    public void CreateViewAndSampler()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_view.IsNull())
        {
            throw new InvalidOperationException("The texture already has a view and a sampler.");
        }

        _view = _device.CreateImageView(
            new VkImageViewCreateInfo
            {
                Image = _image,
                ViewType = VkImageViewType._2D,
                Format = Format,
                SubresourceRange = Subresources.Single(VkImageAspectFlags.ColorBit),
            },
            null);

        // The preset uses the nearest filter, clamp to edge on U, V, and W, and no anisotropy.
        _sampler = _device.CreateSampler(Presets.Sampler.SamplerPointClamped, null);
    }

    /// <summary>
    /// Destroys the sampler, the view, and the image, then frees the memory. A second call does nothing. A partly built
    /// texture is also safe to dispose.
    /// </summary>
    /// <remarks>
    /// The method does not wait for the device. Make sure that no submitted work uses the texture, for example with
    /// <see cref="VulkanContext.WaitIdle"/>.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // The constructor calls Dispose on a failure, so each handle can still be a null handle here.
        if (!_sampler.IsNull())
        {
            _device.DestroySampler(_sampler, null);
            _sampler = default;
        }

        if (!_view.IsNull())
        {
            _device.DestroyImageView(_view, null);
            _view = default;
        }

        if (!_image.IsNull())
        {
            _device.DestroyImage(_image, null);
            _image = default;
        }

        if (!_memory.IsNull())
        {
            _device.FreeMemory(_memory, null);
            _memory = default;
        }
    }
}
