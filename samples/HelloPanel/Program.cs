using System;
using System.Globalization;
using Brutal.GlfwApi;
using Brutal.ImGuiApi;
using Brutal.ImGuiApi.Backends;
using Brutal.Numerics;
using Brutal.VulkanApi;

namespace HelloPanel;

/// <summary>
/// Entry point of the GPU smoke sample. The sample opens a GLFW window and draws one ImGui text label
/// through the Vulkan backend. The command line contract is in
/// <c>docs/features/dev-ops/solution-skeleton/context.md</c>.
/// </summary>
internal static class Program
{
    /// <summary>The exit code for a successful run.</summary>
    private const int ExitSuccess = 0;

    /// <summary>The exit code for a runtime failure.</summary>
    private const int ExitFailure = 1;

    /// <summary>The exit code for a usage error.</summary>
    private const int ExitUsage = 2;

    /// <summary>The usage line that the sample writes on a usage error.</summary>
    private const string UsageLine = "Usage: HelloPanel [--frames <N>]";

    /// <summary>The frame limit that means "run until the user closes the window".</summary>
    private const int NoFrameLimit = 0;

    /// <summary>The descriptor pool size of the ImGui renderer. 8 is the minimum that <see cref="VulkanBackend"/> accepts.</summary>
    private const int ImGuiDescriptorPoolSize = 8;

    private static int Main(string[] args)
    {
        try
        {
            // Parse the arguments before any GLFW call, so a usage check needs no GPU and no display.
            if (!TryParseArguments(args, out int frameLimit, out string? usageError))
            {
                Console.Error.WriteLine(UsageLine);
                Console.Error.WriteLine($"HelloPanel: {usageError}");
                return ExitUsage;
            }

            return Run(frameLimit);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"HelloPanel: {exception}");
            return ExitFailure;
        }
    }

    /// <summary>
    /// Parses the command line. The only argument is <c>--frames &lt;N&gt;</c>, with <c>N</c> from 1 to
    /// <see cref="int.MaxValue"/>. No argument gives <see cref="NoFrameLimit"/>.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <param name="frameLimit">The number of frames to render, or <see cref="NoFrameLimit"/>.</param>
    /// <param name="error">The reason for a usage error, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the arguments are valid.</returns>
    private static bool TryParseArguments(string[] args, out int frameLimit, out string? error)
    {
        frameLimit = NoFrameLimit;
        error = null;
        bool hasFrames = false;

        for (int i = 0; i < args.Length; i++)
        {
            string argument = args[i];
            if (argument != "--frames")
            {
                error = $"Unknown argument '{argument}'.";
                return false;
            }

            if (hasFrames)
            {
                error = "The argument '--frames' occurs more than one time.";
                return false;
            }

            if (i + 1 >= args.Length)
            {
                error = "The argument '--frames' has no value.";
                return false;
            }

            string value = args[++i];
            if (!int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int count))
            {
                error = $"The value '{value}' is not an integer from 1 to {int.MaxValue}.";
                return false;
            }

            if (count < 1)
            {
                error = $"The value '{value}' is below 1.";
                return false;
            }

            frameLimit = count;
            hasFrames = true;
        }

        return true;
    }

    /// <summary>
    /// Starts GLFW, runs the sample, and ends GLFW. On success, writes the frame count to standard output.
    /// </summary>
    /// <param name="frameLimit">The number of frames to render, or <see cref="NoFrameLimit"/>.</param>
    /// <returns>The exit code.</returns>
    private static int Run(int frameLimit)
    {
        if (!Glfw.Init())
        {
            GlfwErrorCode code = Glfw.GetError(out string? description);
            Console.Error.WriteLine($"HelloPanel: GLFW init failed: {code} ({description}).");
            return ExitFailure;
        }

        int framesRendered;
        try
        {
            framesRendered = RunWindow(frameLimit);
        }
        finally
        {
            Glfw.Terminate();
        }

        Console.WriteLine($"HelloPanel: {framesRendered} frames rendered.");
        return ExitSuccess;
    }

    /// <summary>
    /// Opens the window and runs the sample in it. A failure throws, and <see cref="Main"/> returns 1.
    /// </summary>
    /// <param name="frameLimit">The number of frames to render, or <see cref="NoFrameLimit"/>.</param>
    /// <returns>The number of frames that the sample rendered.</returns>
    private static int RunWindow(int frameLimit)
    {
        if (!Glfw.VulkanSupported())
        {
            GlfwErrorCode code = Glfw.GetError(out string? description);
            throw new InvalidOperationException($"GLFW finds no Vulkan loader: {code} ({description}).");
        }

        // Vulkan draws into the window, so GLFW makes no OpenGL context.
        // The window has a fixed size, so the swapchain never needs re-creation.
        Glfw.WindowHint(GlfwWindowHint.ClientApi, Glfw.Constants.GLFW_NO_API);
        Glfw.WindowHint(GlfwWindowHint.Resizable, Glfw.Constants.GLFW_FALSE);

        using GlfwWindow window = Glfw.CreateWindow(new GlfwWindow.CreateInfo
        {
            Title = "HelloPanel",
            Size = new int2(1280, 720),
        });

        using VulkanContext vulkan = new(new VulkanContext.CreateInfo
        {
            Window = window,
            ApplicationName = "HelloPanel",
        });

        ImGuiContextPtr imGuiContext = ImGui.CreateContext();
        try
        {
            return RunImGui(window, vulkan, frameLimit);
        }
        finally
        {
            ImGui.DestroyContext(imGuiContext);
        }
    }

    /// <summary>
    /// Creates the two ImGui backends, runs the frame loop, and disposes the backends.
    /// </summary>
    /// <param name="window">The window.</param>
    /// <param name="vulkan">The Vulkan context of the window.</param>
    /// <param name="frameLimit">The number of frames to render, or <see cref="NoFrameLimit"/>.</param>
    /// <returns>The number of frames that the sample rendered.</returns>
    private static int RunImGui(GlfwWindow window, VulkanContext vulkan, int frameLimit)
    {
        // ImGui writes no imgui.ini file into the working directory.
        ImGuiIOPtr io = ImGui.GetIO();
        io.IniFilename = default;
        ImGui.StyleColorsDark();

        // The platform backend comes first. It sets the surface hook that the renderer backend checks.
        using GlfwBackend platform = new(window, GlfwBackend.ClientApi.Vulkan);
        using VulkanBackend renderer = new(new VulkanBackend.CreateInfo
        {
            Device = vulkan.Device,
            GraphicsQueue = vulkan.GraphicsQueue,
            RenderPass = vulkan.RenderPass,
            MinImageCount = vulkan.MinImageCount,
            ImageCount = vulkan.ImageCount,
            SampleCount = VkSampleCountFlags._1Bit,
            DescriptorPoolSize = ImGuiDescriptorPoolSize,
        });

        try
        {
            return RunFrames(window, vulkan, platform, renderer, frameLimit);
        }
        finally
        {
            // The renderer backend does not wait for the GPU when it releases its objects.
            vulkan.WaitIdle();
        }
    }

    /// <summary>
    /// Runs the frame loop. The loop allocates no managed memory. While the window is minimized, the loop
    /// renders no frame and blocks on events. A skipped frame does not count toward the frame limit.
    /// </summary>
    /// <param name="window">The window.</param>
    /// <param name="vulkan">The Vulkan context of the window.</param>
    /// <param name="platform">The ImGui platform backend.</param>
    /// <param name="renderer">The ImGui renderer backend.</param>
    /// <param name="frameLimit">The number of frames to render, or <see cref="NoFrameLimit"/>.</param>
    /// <returns>The number of frames that the loop rendered.</returns>
    private static int RunFrames(GlfwWindow window, VulkanContext vulkan, GlfwBackend platform, VulkanBackend renderer, int frameLimit)
    {
        int framesRendered = 0;
        while (frameLimit == NoFrameLimit || framesRendered < frameLimit)
        {
            Glfw.PollEvents();

            // A minimized window can have a framebuffer of size zero, and the swapchain then fails.
            // The loop sleeps until an event occurs. The user can close a minimized window from the taskbar,
            // so the wait also checks the close flag. GlfwWindow.WaitForWindow does not check the close flag.
            while (window.IsIconified && !window.ShouldClose)
            {
                Glfw.WaitEvents();
            }

            if (window.ShouldClose)
            {
                break;
            }

            platform.NewFrame();
            renderer.NewFrame();
            ImGui.NewFrame();

            if (ImGui.Begin("HelloPanel"u8, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text("Hello, brutal-ui"u8);
            }

            ImGui.End();
            ImGui.Render();

            CommandBuffer commandBuffer = vulkan.BeginFrame();
            renderer.RenderDrawData(ImGui.GetDrawData(), commandBuffer);
            vulkan.EndFrame();

            framesRendered++;
        }

        return framesRendered;
    }
}
