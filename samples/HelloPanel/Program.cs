using System;
using System.Globalization;
using Brutal.GlfwApi;
using Brutal.ImGuiApi;
using Brutal.ImGuiApi.Backends;
using Brutal.Numerics;
using Brutal.VulkanApi;
using BrutalUi.Core;
using BrutalUi.Glfw;
using BrutalUi.ImGui;

namespace HelloPanel;

/// <summary>
/// Entry point of the GPU smoke sample. The sample opens a GLFW window and draws a brutal-ui tree (the screen of
/// <see cref="HelloScreen"/>) through <see cref="ImGuiRenderer"/> and the Vulkan backend. The sample makes no ImGui
/// widget call.
/// </summary>
/// <remarks>
/// <para>
/// After a run, the sample writes four lines to standard output:
/// <c>HelloPanel: &lt;count&gt; frames rendered.</c>,
/// <c>HelloPanel: last frame: draw commands &lt;C&gt;, vertices &lt;V&gt;.</c>,
/// <c>HelloPanel: bytes after warm-up: Update and Draw &lt;B&gt;, frame &lt;F&gt;, in &lt;M&gt; frames.</c>, and
/// <c>HelloPanel: gate: pointer over UI in &lt;P&gt; frames, keyboard focus in UI in &lt;K&gt; frames, of
/// &lt;M&gt; measured frames.</c> The counts of line 2 come from the ImGui background draw list of the last frame.
/// The exit code is 0 for success, 1 for a runtime failure, for an allocation of the UI region after warm-up, or for
/// a smoke run in which the synthetic input did not reach the UI or the gate read the pointer over the UI in each
/// measured frame, and 2 for a usage error. The command line contract
/// is in <c>docs/features/dev-ops/solution-skeleton/context.md</c>.
/// </para>
/// <para>
/// The sample reads the real input of the window through <see cref="GlfwUiInput"/>. The screen reacts to the pointer
/// and to Tab. A run with <c>--frames</c> (the smoke run) also pushes a synthetic pointer move and a synthetic Tab in
/// each frame, so the run proves with no user that the input reaches the UI.
/// </para>
/// <para>
/// The sample loads <c>HelloScreen.css</c> from the folder of its assembly before it opens the window. A sheet with
/// an error gives exit code 1 and the error lines on standard error, with no GPU and no display.
/// </para>
/// </remarks>
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

    /// <summary>
    /// The descriptor pool size of the ImGui renderer. 8 is the minimum that <see cref="VulkanBackend"/> accepts. The
    /// sample uses 2 sets: the font atlas and the checker texture.
    /// </summary>
    private const int ImGuiDescriptorPoolSize = 8;

    /// <summary>The width and the height of the checker texture, in texels.</summary>
    private const int CheckerTextureSide = 64;

    /// <summary>The number of bytes of one RGBA texel.</summary>
    private const int BytesPerTexel = 4;

    /// <summary>
    /// The number of rendered frames at the start of the run that the allocation gate does not measure. The paint
    /// stack of <see cref="UiContext"/> and the glyph bake of ImGui settle in these frames.
    /// </summary>
    private const int WarmUpFrames = 3;

    /// <summary>
    /// The distance in pixels to the left of and above the panel of the synthetic move in an odd frame of the smoke
    /// run. The point is inside the root padding, so the move hits no element.
    /// </summary>
    private const float SmokeOffPanelOffset = 8f;

    /// <summary>The result of a run of the sample.</summary>
    /// <param name="FramesRendered">The number of frames that the sample rendered.</param>
    /// <param name="DrawCommandCount">
    /// The number of draw commands of the ImGui background draw list in the last frame, or 0 when the sample rendered
    /// no frame.
    /// </param>
    /// <param name="VertexCount">
    /// The number of vertices of the ImGui background draw list in the last frame, or 0 when the sample rendered no
    /// frame.
    /// </param>
    /// <param name="UiAllocatedBytes">
    /// The sum of the managed bytes that the UI region allocates in the measured frames. The gate reads this sum.
    /// </param>
    /// <param name="FrameAllocatedBytes">
    /// The sum of the managed bytes that the frame region allocates in the measured frames. This sum is a report only.
    /// </param>
    /// <param name="MeasuredFrames">
    /// The number of measured frames: the rendered frames after the <see cref="WarmUpFrames"/> warm-up frames.
    /// </param>
    /// <param name="PointerOverUiFrames">
    /// The number of measured frames with <see cref="UiRoutingGate.PointerOverUi"/> <see langword="true"/> after the
    /// draw.
    /// </param>
    /// <param name="KeyboardFocusInUiFrames">
    /// The number of measured frames with <see cref="UiRoutingGate.KeyboardFocusInUi"/> <see langword="true"/> after
    /// the draw.
    /// </param>
    private readonly record struct RunResult(
        int FramesRendered,
        int DrawCommandCount,
        int VertexCount,
        long UiAllocatedBytes,
        long FrameAllocatedBytes,
        int MeasuredFrames,
        int PointerOverUiFrames,
        int KeyboardFocusInUiFrames);

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
    /// Loads the sheet, starts GLFW, runs the sample, and ends GLFW. After the run, writes the frame count (line 1),
    /// the draw command count and the vertex count of the last frame (line 2), the allocated bytes after warm-up
    /// (line 3), and the gate counts of the measured frames (line 4) to standard output. When the UI region allocates
    /// after warm-up, writes the allocation line to standard error and returns <see cref="ExitFailure"/>. When the
    /// synthetic input of a smoke run did not reach the UI, or when the gate read the pointer over the UI in each
    /// measured frame, writes the input line to standard error and returns <see cref="ExitFailure"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The sheet loads before the first GLFW call, so a bad sheet fails with no GPU and no display. A parse error
    /// throws <see cref="InvalidOperationException"/>. The <c>catch</c> of <see cref="Main"/> writes the exception to
    /// standard error, with one <c>HelloScreen.css(line,column): kind: text</c> line for each error, and returns
    /// <see cref="ExitFailure"/>.
    /// </para>
    /// <para>
    /// The input check runs after the allocation check. It runs only for a run with a frame limit and 2 measured
    /// frames or more. Then the synthetic input puts the pointer over the panel in each even frame and gives the
    /// focus to the panel. It puts the pointer off the panel in each odd frame. A count of 0 for the pointer or for the
    /// focus means that the input did not reach the UI. A pointer count equal to the measured count means that the
    /// gate read the pointer over the UI in the off-panel frames too, so the off-panel frames did not reach the game.
    /// Both cases write a line to standard error and return <see cref="ExitFailure"/>. A run with no frame limit
    /// pushes no synthetic input, so the check does not run.
    /// </para>
    /// </remarks>
    /// <param name="frameLimit">The number of frames to render, or <see cref="NoFrameLimit"/>.</param>
    /// <returns>The exit code.</returns>
    private static int Run(int frameLimit)
    {
        StyleSheet sheet = HelloScreen.LoadSheet();

        if (!Glfw.Init())
        {
            GlfwErrorCode code = Glfw.GetError(out string? description);
            Console.Error.WriteLine($"HelloPanel: GLFW init failed: {code} ({description}).");
            return ExitFailure;
        }

        RunResult result;
        try
        {
            result = RunWindow(sheet, frameLimit);
        }
        finally
        {
            Glfw.Terminate();
        }

        Console.WriteLine($"HelloPanel: {result.FramesRendered} frames rendered.");
        Console.WriteLine($"HelloPanel: last frame: draw commands {result.DrawCommandCount}, vertices {result.VertexCount}.");
        Console.WriteLine(
            $"HelloPanel: bytes after warm-up: Update and Draw {result.UiAllocatedBytes}, " +
            $"frame {result.FrameAllocatedBytes}, in {result.MeasuredFrames} frames.");
        Console.WriteLine(
            $"HelloPanel: gate: pointer over UI in {result.PointerOverUiFrames} frames, " +
            $"keyboard focus in UI in {result.KeyboardFocusInUiFrames} frames, " +
            $"of {result.MeasuredFrames} measured frames.");

        // The gate reads the UI sum only. The frame sum also holds the managed bytes of the backends.
        if (result.UiAllocatedBytes > 0)
        {
            Console.Error.WriteLine(
                $"HelloPanel: Update and Draw allocated {result.UiAllocatedBytes} bytes after warm-up. " +
                "The limit is 0 bytes.");
            return ExitFailure;
        }

        // The smoke run pushes a synthetic move and a synthetic Tab in each frame (Decision 8). With 2 measured frames
        // or more, one measured frame is even and puts the pointer over the panel, and one is odd and puts the pointer
        // off the panel. A count of 0 means that the input pass did no work. A count equal to the measured count means
        // that the gate read the pointer over the UI in the off-panel frames too, for example with a pickable root.
        if (frameLimit != NoFrameLimit && result.MeasuredFrames >= 2)
        {
            if (result.PointerOverUiFrames == 0 || result.KeyboardFocusInUiFrames == 0)
            {
                Console.Error.WriteLine("HelloPanel: the synthetic input did not reach the UI.");
                return ExitFailure;
            }

            if (result.PointerOverUiFrames == result.MeasuredFrames)
            {
                Console.Error.WriteLine(
                    "HelloPanel: the pointer read over the UI in each measured frame. " +
                    "The off-panel frames did not reach the game.");
                return ExitFailure;
            }
        }

        return ExitSuccess;
    }

    /// <summary>
    /// Opens the window, uploads the checker texture, and runs the sample in the window. A failure throws, and
    /// <see cref="Main"/> returns 1.
    /// </summary>
    /// <remarks>
    /// The checker texture is created before the ImGui context and disposed after it. The <c>using</c> declarations
    /// dispose the texture first, then the Vulkan context, then the window. Thus the device is alive when the texture
    /// is destroyed.
    /// </remarks>
    /// <param name="sheet">The sheet of the screen, from <see cref="HelloScreen.LoadSheet"/>.</param>
    /// <param name="frameLimit">The number of frames to render, or <see cref="NoFrameLimit"/>.</param>
    /// <returns>
    /// The result of the run: the frame count, the draw list counts of the last frame, and the allocation sums of the
    /// measured frames.
    /// </returns>
    private static RunResult RunWindow(StyleSheet sheet, int frameLimit)
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

        // The sample fills the texels in code and reads no image file.
        int2 checkerSize = new(CheckerTextureSide, CheckerTextureSide);
        byte[] pixels = new byte[CheckerTextureSide * CheckerTextureSide * BytesPerTexel];
        HelloScreen.FillChecker(pixels, checkerSize);
        using SampledTexture checker = vulkan.CreateTexture(pixels, checkerSize);

        ImGuiContextPtr imGuiContext = ImGui.CreateContext();
        try
        {
            return RunImGui(window, vulkan, checker, sheet, frameLimit);
        }
        finally
        {
            ImGui.DestroyContext(imGuiContext);
        }
    }

    /// <summary>
    /// Creates the brutal-ui renderer, the UI context, the two ImGui backends, and the input adapter. Then registers
    /// the checker texture, adds the sheet and the screen, runs the frame loop, removes the texture, and disposes the
    /// adapter and the backends.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The teardown waits for the device, then removes the texture, then disposes the adapter and the backends. Thus
    /// no frame in flight uses the descriptor set of the texture when the backend frees it.
    /// </para>
    /// <para>
    /// The creation order of the <see cref="GlfwUiInput"/> remarks: the window, then <see cref="GlfwBackend"/>, then
    /// the adapter. Thus ImGui gets each window event first. The adapter comes after both backends, so its
    /// <c>using</c> declaration disposes it first, before <see cref="GlfwBackend"/> and before the window.
    /// </para>
    /// </remarks>
    /// <param name="window">The window.</param>
    /// <param name="vulkan">The Vulkan context of the window.</param>
    /// <param name="checker">The checker texture. It stays alive until after the backends are disposed.</param>
    /// <param name="sheet">The sheet of the screen. It gives each size and each color of the tree.</param>
    /// <param name="frameLimit">The number of frames to render, or <see cref="NoFrameLimit"/>.</param>
    /// <returns>
    /// The result of the run: the frame count, the draw list counts of the last frame, and the allocation sums of the
    /// measured frames.
    /// </returns>
    private static RunResult RunImGui(
        GlfwWindow window, VulkanContext vulkan, SampledTexture checker, StyleSheet sheet, int frameLimit)
    {
        // ImGui writes no imgui.ini file into the working directory.
        ImGuiIOPtr io = ImGui.GetIO();
        io.IniFilename = default;
        ImGui.StyleColorsDark();

        // The sample loads no font, so font 0 is the ImGui default font.
        // The renderer measures the text of the layout pass and draws the tree, so the context and the draw use
        // the same font ids.
        ImGuiFontService fonts = new();
        ImGuiTextureService textures = new();
        ImGuiRenderer uiRenderer = new(new ImGuiRenderer.CreateInfo
        {
            Fonts = fonts,
            Textures = textures,
        });
        UiContext ui = new(new UiContext.CreateInfo { Measurer = uiRenderer });

        // The platform backend comes first. It sets the surface hook that the renderer backend checks.
        using GlfwBackend glfwBackend = new(window, GlfwBackend.ClientApi.Vulkan);
        using VulkanBackend vulkanBackend = new(new VulkanBackend.CreateInfo
        {
            Device = vulkan.Device,
            GraphicsQueue = vulkan.GraphicsQueue,
            RenderPass = vulkan.RenderPass,
            MinImageCount = vulkan.MinImageCount,
            ImageCount = vulkan.ImageCount,
            SampleCount = VkSampleCountFlags._1Bit,
            DescriptorPoolSize = ImGuiDescriptorPoolSize,
        });

        // The adapter comes after GlfwBackend, so ImGui gets each window event first. The using declaration
        // disposes the adapter first, while the window is alive.
        using GlfwUiInput input = new(new GlfwUiInput.CreateInfo { Window = window, Context = ui });

        TextureHandle handle = default;
        try
        {
            // The pool now holds 2 of its sets: the font atlas and the checker texture.
            handle = textures.AddVulkan(
                vulkanBackend, checker.Sampler, checker.View, VkImageLayout.ShaderReadOnlyOptimal, checker.Size);
            // The sheet gives each size and each color of the screen. The tree sets only the classes.
            ui.AddSheet(sheet);
            ui.AddRoot(HelloScreen.Build(handle, out Element panel, out Label gateLabel));

            return RunFrames(
                window, vulkan, glfwBackend, vulkanBackend, input, ui, uiRenderer, panel, gateLabel, frameLimit);
        }
        finally
        {
            // The renderer backend does not wait for the GPU when it releases its objects.
            vulkan.WaitIdle();

            // The backend frees the descriptor set of the texture. No frame in flight uses it after the wait.
            if (!handle.IsNone)
            {
                textures.Remove(handle);
            }
        }
    }

    /// <summary>
    /// Runs the frame loop. Each frame updates and draws the brutal-ui tree, and the Vulkan backend renders the ImGui
    /// draw data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The frame order: (1) set <see cref="GlfwUiInput.KeyboardEnabled"/> from <c>io.WantCaptureKeyboard</c>, poll
    /// the events, wait while the window is minimized, and check the close flag. The poll gives each window event to
    /// ImGui first and then to the adapter, and the adapter pushes it into the ring of the context. (2) Start the frame
    /// of the GLFW backend, the Vulkan backend, and ImGui. (3) The UI region: push the smoke input (a run with a frame
    /// limit only), set the viewport size from <c>io.DisplaySize</c>, update the tree, draw the tree, read
    /// <see cref="UiContext.RoutingGate"/>, and set the text of the gate label. The input pass of
    /// <see cref="UiContext.Update(float)"/> drains the ring before the style pass, so the hover and the focus of this
    /// frame show in this frame. (4) <c>ImGui.Render</c>, then read the draw command count and the vertex count of the
    /// background draw list. (5) Record and present the frame. (6) Count the frame and the gate values.
    /// </para>
    /// <para>
    /// The <see cref="GlfwUiInput.KeyboardEnabled"/> line comes before the poll. The ImGui backend and the adapter
    /// both get each key event, so the adapter pushes no key while ImGui wants the keyboard. The pointer has no switch.
    /// A game with an ImGui window over the game uses <c>gate.PointerOverUi || io.WantCaptureMouse</c> (Decision 5).
    /// </para>
    /// <para>
    /// The smoke input: a run with a frame limit pushes, at the start of the UI region of each frame, one pointer move,
    /// one Tab key down, and one Tab key up, with no target. The move goes to the center of the panel in an even frame
    /// and outside the panel in an odd frame. A run with no frame limit pushes no synthetic event and uses the real
    /// input of the window only.
    /// </para>
    /// <para>
    /// The gate counts: after the draw, the loop reads the gate. A measured frame adds 1 to the pointer count when
    /// <see cref="UiRoutingGate.PointerOverUi"/> is <see langword="true"/>, and 1 to the focus count when
    /// <see cref="UiRoutingGate.KeyboardFocusInUi"/> is <see langword="true"/>. The gate read and the label update are
    /// inside the UI region, so the allocation gate measures them.
    /// </para>
    /// <para>
    /// The UI region runs between <c>ImGui.NewFrame</c> and <c>ImGui.Render</c>, because the renderer draws on the
    /// background draw list of the frame. The viewport size is clamped to 0 or more, because ImGui gives (-1, -1)
    /// before the first platform frame and the setter throws for a negative value.
    /// </para>
    /// <para>
    /// The loop measures two regions with <see cref="GC.GetAllocatedBytesForCurrentThread"/>. The UI region is step
    /// (3). The frame region is steps (2) to (6). Each region reads the counter right before and right after, and adds
    /// the difference to a <see langword="long"/> sum. The first <see cref="WarmUpFrames"/> rendered frames are not
    /// measured. After warm-up, the UI region must allocate 0 managed bytes. <see cref="Run"/> fails the run when the
    /// UI sum is above 0. The frame sum also holds the managed bytes of the backends, so it is a report only.
    /// </para>
    /// <para>
    /// A background GC can add the free part of the allocation context of the thread to the counter. The UI region
    /// then reads bytes that it did not allocate. Thus a measured frame releases the allocation context with a
    /// blocking GC of generation 0 after <c>ImGui.NewFrame</c> and before the UI region, when the thread allocated
    /// since the last release. The test helper <c>AllocationGate</c> uses the same release.
    /// </para>
    /// <para>
    /// While the window is minimized, the loop renders no frame and blocks on events. A skipped frame does not count
    /// toward the frame limit, and it is not measured.
    /// </para>
    /// <para>
    /// <see cref="Run"/> writes the frame count as line 1, the two counts of the last frame as line 2, the two sums
    /// and the measured frame count as line 3, and the two gate counts and the measured frame count as line 4.
    /// </para>
    /// </remarks>
    /// <param name="window">The window.</param>
    /// <param name="vulkan">The Vulkan context of the window.</param>
    /// <param name="glfwBackend">The ImGui platform backend.</param>
    /// <param name="vulkanBackend">The ImGui renderer backend.</param>
    /// <param name="input">The input adapter of the window. It pushes the real events of the window into
    /// <paramref name="ui"/>.</param>
    /// <param name="ui">The UI context that holds the screen.</param>
    /// <param name="uiRenderer">The brutal-ui renderer. It is also the measurer of <paramref name="ui"/>.</param>
    /// <param name="panel">The panel of the screen. The smoke input aims its pointer move at the panel.</param>
    /// <param name="gateLabel">The gate label of the screen. The loop sets its text after each draw.</param>
    /// <param name="frameLimit">The number of frames to render, or <see cref="NoFrameLimit"/>.</param>
    /// <returns>
    /// The result of the run: the frame count, the draw list counts of the last frame, the allocation sums of the
    /// measured frames, and the gate counts of the measured frames.
    /// </returns>
    private static RunResult RunFrames(
        GlfwWindow window,
        VulkanContext vulkan,
        GlfwBackend glfwBackend,
        VulkanBackend vulkanBackend,
        GlfwUiInput input,
        UiContext ui,
        ImGuiRenderer uiRenderer,
        Element panel,
        Label gateLabel,
        int frameLimit)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        int framesRendered = 0;
        int drawCommandCount = 0;
        int vertexCount = 0;

        // The allocation gate uses only locals, so the measurement adds no managed allocation.
        long uiAllocatedBytes = 0;
        long frameAllocatedBytes = 0;
        int measuredFrames = 0;
        int pointerOverUiFrames = 0;
        int keyboardFocusInUiFrames = 0;

        // The counter value after the last release of the allocation context, or -1 before the first release.
        long releasedBytes = -1;
        while (frameLimit == NoFrameLimit || framesRendered < frameLimit)
        {
            // ImGui and the adapter both get each key event. The adapter pushes no key while ImGui wants the keyboard.
            // The flag of the last ImGui frame is the newest value before the poll.
            input.KeyboardEnabled = !io.WantCaptureKeyboard;
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

            // The frame region starts here. The first WarmUpFrames rendered frames are not measured.
            bool measured = framesRendered >= WarmUpFrames;
            long frameStartBytes = GC.GetAllocatedBytesForCurrentThread();

            glfwBackend.NewFrame();
            vulkanBackend.NewFrame();
            ImGui.NewFrame();

            // Release the allocation context when the thread allocated since the last release.
            // A background GC can add the free part of the allocation context to the counter.
            // The test helper AllocationGate uses the same release. The release stays outside the UI region.
            if (measured && GC.GetAllocatedBytesForCurrentThread() != releasedBytes)
            {
                GC.Collect(0, GCCollectionMode.Forced, blocking: true);
                releasedBytes = GC.GetAllocatedBytesForCurrentThread();
            }

            // The UI region. Put no other call inside it.
            long uiStartBytes = GC.GetAllocatedBytesForCurrentThread();

            // The smoke input (Decision 8). The events are structs, so the pushes allocate nothing.
            if (frameLimit != NoFrameLimit)
            {
                PushSmokeInput(ui, panel, framesRendered);
            }

            float2 displaySize = io.DisplaySize;
            ui.ViewportSize = new float2(MathF.Max(displaySize.X, 0f), MathF.Max(displaySize.Y, 0f));

            // The input pass of Update drains the events of the poll and of the smoke input first.
            ui.Update(io.DeltaTime);
            ui.Draw(uiRenderer);

            // The game handles the world input here, after Update and with the gate of this frame. With an ImGui
            // window over the game, the game uses gate.PointerOverUi || io.WantCaptureMouse for the pointer
            // (Decision 5). The keyboard side is the KeyboardEnabled line before the poll.
            UiRoutingGate gate = ui.RoutingGate;

            // The label shows the gate of this frame in the next frame. Each gate text fits the label buffer.
            gateLabel.SetText(HelloScreen.GateText(gate));
            long uiEndBytes = GC.GetAllocatedBytesForCurrentThread();

            ImGui.Render();

            ImDrawListPtr background = ImGui.GetBackgroundDrawList(ImGui.GetMainViewport());
            drawCommandCount = background.CmdBuffer.Count;
            vertexCount = background.VtxBuffer.Count;

            CommandBuffer commandBuffer = vulkan.BeginFrame();
            vulkanBackend.RenderDrawData(ImGui.GetDrawData(), commandBuffer);
            vulkan.EndFrame();

            framesRendered++;

            // The frame region ends here.
            long frameEndBytes = GC.GetAllocatedBytesForCurrentThread();
            if (measured)
            {
                uiAllocatedBytes += uiEndBytes - uiStartBytes;
                frameAllocatedBytes += frameEndBytes - frameStartBytes;
                measuredFrames++;
                if (gate.PointerOverUi)
                {
                    pointerOverUiFrames++;
                }

                if (gate.KeyboardFocusInUi)
                {
                    keyboardFocusInUiFrames++;
                }
            }
        }

        return new RunResult(
            framesRendered,
            drawCommandCount,
            vertexCount,
            uiAllocatedBytes,
            frameAllocatedBytes,
            measuredFrames,
            pointerOverUiFrames,
            keyboardFocusInUiFrames);
    }

    /// <summary>
    /// Pushes the synthetic input of one frame of the smoke run: one pointer move, one Tab key down, and one Tab key
    /// up, each with no target.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The move goes to the center of the panel in an even frame, and to a point 8 pixels above and to the left of the
    /// panel in an odd frame. That point is inside the root padding. The root is not pickable, so the move hits no
    /// element. The position comes from the layout of the last <see cref="UiContext.Update(float)"/>, as the hit test
    /// of the input pass. Before the first layout, the panel rectangle is 0, and the move hits no element.
    /// </para>
    /// <para>
    /// The Tab down moves the focus to the next focusable element. The panel is the only focusable element, so the
    /// focus stays on the panel. The Tab up does not move the focus. The method uses the public
    /// <see cref="UiContext.PushEvent(in PointerEvent)"/> and <see cref="UiContext.PushEvent(in KeyEvent)"/>,
    /// because the push methods of <see cref="GlfwUiInput"/> are internal. The events are structs, so the method
    /// allocates nothing.
    /// </para>
    /// </remarks>
    /// <param name="ui">The UI context that gets the events.</param>
    /// <param name="panel">The panel of the screen.</param>
    /// <param name="frameIndex">The index of the frame: the number of frames that the loop rendered before it.</param>
    private static void PushSmokeInput(UiContext ui, Element panel, int frameIndex)
    {
        // The viewport rectangle of the panel: the position of the panel plus the position of the root.
        float4 panelRect = panel.Layout.Rect;
        float2 origin = new(panelRect.X, panelRect.Y);
        for (Element? ancestor = panel.Parent; ancestor is not null; ancestor = ancestor.Parent)
        {
            float4 ancestorRect = ancestor.Layout.Rect;
            origin = new float2(origin.X + ancestorRect.X, origin.Y + ancestorRect.Y);
        }

        float2 position = frameIndex % 2 == 0
            ? new float2(origin.X + (panelRect.Z * 0.5f), origin.Y + (panelRect.W * 0.5f))
            : new float2(origin.X - SmokeOffPanelOffset, origin.Y - SmokeOffPanelOffset);

        double time = Glfw.Time;
        ui.PushEvent(new PointerEvent
        {
            Header = new UiEventHeader(UiEventType.PointerMove, time, null),
            Position = position,
            Button = PointerButton.Left,
            Modifiers = UiModifiers.None,
        });
        ui.PushEvent(new KeyEvent
        {
            Header = new UiEventHeader(UiEventType.KeyDown, time, null),
            Key = (int)GlfwKey.Tab,
            ScanCode = 0,
            Modifiers = UiModifiers.None,
            IsRepeat = false,
        });
        ui.PushEvent(new KeyEvent
        {
            Header = new UiEventHeader(UiEventType.KeyUp, time, null),
            Key = (int)GlfwKey.Tab,
            ScanCode = 0,
            Modifiers = UiModifiers.None,
            IsRepeat = false,
        });
    }
}
