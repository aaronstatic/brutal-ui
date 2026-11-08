using System;
using System.Globalization;
using Brutal.GlfwApi;
using Brutal.ImGuiApi;
using Brutal.ImGuiApi.Backends;
using Brutal.Numerics;
using Brutal.VulkanApi;
using BrutalUi.Core;
using BrutalUi.Framework;
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
/// After a run, the sample writes seven lines to standard output:
/// <c>HelloPanel: &lt;count&gt; frames rendered.</c>,
/// <c>HelloPanel: last frame: draw commands &lt;C&gt;, vertices &lt;V&gt;.</c>,
/// <c>HelloPanel: bytes after warm-up: Update and Draw &lt;B&gt;, frame &lt;F&gt;, in &lt;M&gt; frames.</c>,
/// <c>HelloPanel: gate: pointer over UI in &lt;P&gt; frames, keyboard focus in UI in &lt;K&gt; frames, of
/// &lt;M&gt; measured frames.</c>,
/// <c>HelloPanel: widgets: clicks &lt;C&gt;, flips &lt;T&gt;, values &lt;V&gt;, scrolls &lt;S&gt;, edits &lt;E&gt;,
/// submits &lt;U&gt;.</c>, and
/// <c>HelloPanel: state: adds &lt;A&gt;, resets &lt;R&gt;, versions &lt;V&gt;.</c>, and
/// <c>HelloPanel: modes: switches &lt;S&gt;, backs &lt;B&gt;, main frames &lt;M&gt;, detail frames &lt;D&gt;.</c> The
/// counts of line 2 come from the ImGui background draw list of the last frame. The counts of line 5 come from
/// <see cref="HelloWidgets"/> and cover the full run. The counts of line 6 come from <see cref="HelloState"/> and cover
/// the full run. The counts of line 7 cover the full run for the switches and the backs, and the measured frames for
/// the two mode frame counts. The exit code is 0 for success,
/// 1 for a runtime failure, for an allocation of the UI region after warm-up, or for a smoke run in which the
/// synthetic input did not reach the UI, the gate read the pointer over the UI in each measured frame, a widget got no
/// change, the state row got no command, no action, or no store change, or a mode count is 0, and 2 for a usage error.
/// The command line contract is in <c>docs/features/dev-ops/solution-skeleton/context.md</c>.
/// </para>
/// <para>
/// The sample reads the real input of the window through <see cref="GlfwUiInput"/>. The screen reacts to the pointer
/// and to Tab. A run with <c>--frames</c> (the smoke run) also pushes synthetic events in each frame: the widget
/// events first (a left press and a left release on the <c>Click me</c> button, on the toggle, and on the slider, and
/// a scroll at the center of the scroll view), then a Tab, then an Escape key (each fourth frame), then one F1 key or
/// one F2 key, then the text field events (a char or a Backspace, an Enter, and a left press and a left release on
/// the field), then a pointer move.
/// The widget events include a click on the <c>Add</c> button of <see cref="HelloState"/>, after the toggle click and
/// before the slider click. The frame loop also polls the gamepad and pushes the buttons into the action map. The first measured frame also fills
/// the text field to its capacity and empties it again, so the two text labels get their longest text in a measured
/// frame. Thus the run proves with no user that the input reaches the UI and each widget.
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

    /// <summary>The code point that the smoke run types into the text field in an even frame: <c>a</c>.</summary>
    private const uint SmokeChar = 'a';

    /// <summary>
    /// The code point of the longest text burst: <c>W</c>. The longest text of each text label of
    /// <see cref="HelloWidgets"/> uses the same char.
    /// </summary>
    private const uint SmokeLongestChar = 'W';

    /// <summary>
    /// The GLFW key code of the Backspace key: 259, the value of <c>WidgetInput.BackspaceKey</c> of
    /// <c>BrutalUi.Core</c>. That constant is internal, so the sample writes the raw value.
    /// </summary>
    private const int SmokeBackspaceKey = 259;

    /// <summary>
    /// The GLFW key code of the main Enter key: 257, the value of <c>WidgetInput.EnterKey</c> of <c>BrutalUi.Core</c>.
    /// That constant is internal, so the sample writes the raw value. The keypad Enter key has another code.
    /// </summary>
    private const int SmokeEnterKey = 257;

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
    /// <param name="CaretFrames">
    /// The number of measured frames in which the text field holds the focus after
    /// <see cref="UiContext.Update(float)"/>. The caret pass of the field runs in these frames. The widget check reads
    /// this count. Line 5 does not show it.
    /// </param>
    /// <param name="Clicks">
    /// The number of clicks of the <c>Click me</c> button in the full run: <see cref="HelloWidgets.Clicks"/> after the
    /// last frame.
    /// </param>
    /// <param name="Flips">
    /// The number of flips of the toggle in the full run: <see cref="HelloWidgets.Flips"/> after the last frame.
    /// </param>
    /// <param name="Values">
    /// The number of value changes of the slider in the full run: <see cref="HelloWidgets.Values"/> after the last
    /// frame.
    /// </param>
    /// <param name="Scrolls">
    /// The number of offset changes of the scroll view in the full run: <see cref="HelloWidgets.Scrolls"/> after the
    /// last frame.
    /// </param>
    /// <param name="Edits">
    /// The number of text changes of the text field in the full run: <see cref="HelloWidgets.Edits"/> after the last
    /// frame.
    /// </param>
    /// <param name="Submits">
    /// The number of submits of the text field in the full run: <see cref="HelloWidgets.Submits"/> after the last
    /// frame.
    /// </param>
    /// <param name="Adds">
    /// The number of runs of the command <c>hello.add</c> in the full run: <see cref="HelloState.Adds"/> after the last
    /// frame.
    /// </param>
    /// <param name="Resets">
    /// The number of runs of the command <c>hello.reset</c> in the full run: <see cref="HelloState.Resets"/> after the
    /// last frame.
    /// </param>
    /// <param name="Versions">
    /// The number of changes of the score store in the full run: <c>Score.Version - 1</c>, because the store starts at
    /// version 1.
    /// </param>
    /// <param name="Switches">
    /// The number of runs of the command <c>hello.mode</c> in the full run: <see cref="HelloModes.Switches"/> after the
    /// last frame.
    /// </param>
    /// <param name="Backs">
    /// The number of runs of the command <c>hello.back</c> with a true back step in the full run:
    /// <see cref="HelloModes.Backs"/> after the last frame.
    /// </param>
    /// <param name="MainFrames">
    /// The number of measured frames with <c>hello.main</c> as the current mode after
    /// <see cref="UiContext.Update(float)"/>.
    /// </param>
    /// <param name="DetailFrames">
    /// The number of measured frames with <c>hello.detail</c> as the current mode after
    /// <see cref="UiContext.Update(float)"/>.
    /// </param>
    private readonly record struct RunResult(
        int FramesRendered,
        int DrawCommandCount,
        int VertexCount,
        long UiAllocatedBytes,
        long FrameAllocatedBytes,
        int MeasuredFrames,
        int PointerOverUiFrames,
        int KeyboardFocusInUiFrames,
        int CaretFrames,
        int Clicks,
        int Flips,
        int Values,
        int Scrolls,
        int Edits,
        int Submits,
        int Adds,
        int Resets,
        int Versions,
        int Switches,
        int Backs,
        int MainFrames,
        int DetailFrames);

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
    /// (line 3), the gate counts of the measured frames (line 4), the widget counts of the full run (line 5), the
    /// state counts of the full run (line 6), and the mode counts (line 7) to standard output. When the UI region allocates after warm-up, writes the allocation line to standard error and
    /// returns <see cref="ExitFailure"/>. When the synthetic input of a smoke run did not reach the UI, or when the
    /// gate read the pointer over the UI in each measured frame, writes the input line to standard error and returns
    /// <see cref="ExitFailure"/>. When a widget count of a smoke run is 0, or when the text field holds the focus after
    /// the update in no measured frame, writes the widget line to standard error and returns <see cref="ExitFailure"/>.
    /// When a mode count of a smoke run is 0, writes the mode line to standard error and returns
    /// <see cref="ExitFailure"/>.
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
    /// <para>
    /// The widget check runs after the input check, with the same condition. In each frame, the smoke run pushes a
    /// press and a release on the <c>Click me</c> button, on the toggle, and on the slider, a scroll on the scroll
    /// view, the char <c>a</c> or a Backspace and a main Enter on the text field, and a press and a release on the
    /// field. Thus a click count, a flip count, a value count, a scroll count, an edit count, or a submit count of 0
    /// means that the synthetic input did not reach each widget.
    /// </para>
    /// <para>
    /// The widget check also reads the caret count: the number of measured frames in which the text field holds the
    /// focus after <see cref="UiContext.Update(float)"/>. The caret pass and the caret draw run for the focus owner
    /// only. Thus a caret count of 0 means that the caret pass and the caret draw did not run under the allocation
    /// check, for example after a change of the push order or of the focus rule. Line 5 does not show the caret count.
    /// </para>
    /// <para>
    /// For each failure, the widget check writes the widget line to standard error and returns
    /// <see cref="ExitFailure"/>. The allocation check comes first, so a run that allocates and gets no click still
    /// writes the allocation line.
    /// </para>
    /// <para>
    /// The state check runs after the widget check, with the same condition. The smoke run pushes a click on the
    /// <c>Add</c> button in each frame and an Escape key in each fourth frame. Thus an add count, a reset count, or a
    /// store version count of 0 means that the synthetic input did not reach the state row. The method writes the state
    /// line to standard error and returns <see cref="ExitFailure"/>.
    /// </para>
    /// <para>
    /// The mode check runs after the state check, with the same condition. The smoke run pushes the F1 key or the F2
    /// key in each frame (Decision 5). F1 runs <c>hello.mode</c> and switches the mode. F2 runs <c>hello.back</c> and
    /// steps back through the mode history. The switch and the back step move the panels, so the measured frames land
    /// in the two modes. A count of 0 among the switches, the backs, the main frames, and the detail frames means that
    /// the synthetic input did not reach the mode switch. The method writes the mode line to standard error and
    /// returns <see cref="ExitFailure"/>.
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
        Console.WriteLine(
            $"HelloPanel: widgets: clicks {result.Clicks}, flips {result.Flips}, values {result.Values}, " +
            $"scrolls {result.Scrolls}, edits {result.Edits}, submits {result.Submits}.");
        Console.WriteLine(
            $"HelloPanel: state: adds {result.Adds}, resets {result.Resets}, versions {result.Versions}.");
        Console.WriteLine(
            $"HelloPanel: modes: switches {result.Switches}, backs {result.Backs}, " +
            $"main frames {result.MainFrames}, detail frames {result.DetailFrames}.");

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

            // The widget check. Each frame pushes the events of each widget, so each count rises. The field press is
            // the last focus change of each drain, so the field holds the focus at the caret pass and the caret count
            // rises.
            if (result.Clicks == 0 || result.Flips == 0 || result.Values == 0 || result.Scrolls == 0
                || result.Edits == 0 || result.Submits == 0 || result.CaretFrames == 0)
            {
                Console.Error.WriteLine("HelloPanel: the synthetic input did not reach each widget.");
                return ExitFailure;
            }

            // The state check. The Add click runs hello.add, the Escape key runs hello.reset through the action map,
            // and both change the score store. A count of 0 means that the command, the action, or the store did no work.
            if (result.Adds == 0 || result.Resets == 0 || result.Versions == 0)
            {
                Console.Error.WriteLine("HelloPanel: the synthetic input did not reach the state row.");
                return ExitFailure;
            }

            // The mode check. The F1 key runs hello.mode, and the F2 key runs hello.back. The switch and the back
            // step move the panels, so the measured frames land in the two modes. A count of 0 means that the
            // synthetic input did not reach the mode switch.
            if (result.Switches == 0 || result.Backs == 0 || result.MainFrames == 0 || result.DetailFrames == 0)
            {
                Console.Error.WriteLine("HelloPanel: the synthetic input did not reach the mode switch.");
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

        // The state builds the store, the commands, the action map, and the state row one time, at build time.
        HelloState state = new(ui);

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
            // The sheet gives each size and each color of the screen. The trees set only the classes.
            ui.AddSheet(sheet);

            // The modes own the panel context, the manager, the commands, the keys, and the mode button. The
            // constructor builds each panel root at build time, and SetMode records hello.main. The first Update
            // adds the layout root, mounts the panels, and activates the map before the input pass of frame 0, so
            // the sample does not call UiActionMap.Activate.
            HelloModes modes = new HelloModes(ui, state, handle);
            modes.Manager.SetMode(HelloModes.MainName);

            return RunFrames(
                window,
                vulkan,
                glfwBackend,
                vulkanBackend,
                input,
                ui,
                uiRenderer,
                modes.WidgetPanel,
                modes.GateLabel,
                modes.InputLabel,
                modes.Widgets,
                state,
                modes,
                frameLimit);
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
    /// the events, poll the gamepad, wait while the window is minimized, and check the close flag. The poll gives each
    /// window event to ImGui first and then to the adapter, and the adapter pushes it into the ring of the context. The
    /// gamepad poll (<see cref="GlfwUiInput.PollGamepad"/>) sits outside the UI region, so an allocation of the BRUTAL
    /// binding cannot fail the allocation gate. (2) Start the frame of the GLFW backend, the Vulkan backend, and ImGui.
    /// (3) The UI region: push the gamepad buttons into the action map (<c>UiActionMap.PushGamepad</c>, the first line,
    /// so the gate measures the map), push the smoke input (a run with a frame limit only), set the viewport size from <c>io.DisplaySize</c>, update the tree, read the focus owner, draw the
    /// tree, read
    /// <see cref="UiContext.RoutingGate"/>, and set the text of the gate label and the input label. The input pass of
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
    /// The smoke input: a run with a frame limit pushes, at the start of the UI region of each frame, the widget events
    /// (a left press and a left release at the center of the <c>Click me</c> button, at the center of the toggle, at the
    /// cached center of the <c>Add</c> button, and at a point of the slider, and a scroll at the center of the scroll view), then one Tab key down and one Tab key up
    /// with no target, then in each fourth frame one Escape key down and one Escape key up with no target, then one F1
    /// key down and one F1 key up or one F2 key down and one F2 key up with no target (the mode script, Decision 5),
    /// then the
    /// text field events (a char or a Backspace key down and a main Enter key down with the
    /// field as the target, and a left press and a left release at the center of the field), then one pointer move.
    /// The move goes to the center of the panel in an even frame and outside the panel in an odd frame. The widget
    /// events come before the move, because the gate reads the hit of the last pointer event of the drain. The first
    /// measured frame also pushes the longest text burst before the char or the Backspace of the frame (see
    /// <see cref="PushLongestTextBurst"/>). A run with no frame limit pushes no synthetic event and uses the real input
    /// of the window only.
    /// </para>
    /// <para>
    /// The delegates of the widgets run in the input pass of <see cref="UiContext.Update(float)"/>, so the allocation
    /// gate measures each delegate and each write of a status label.
    /// </para>
    /// <para>
    /// The gate counts: after the draw, the loop reads the gate. A measured frame adds 1 to the pointer count when
    /// <see cref="UiRoutingGate.PointerOverUi"/> is <see langword="true"/>, and 1 to the focus count when
    /// <see cref="UiRoutingGate.KeyboardFocusInUi"/> is <see langword="true"/>. The gate read and the label update are
    /// inside the UI region, so the allocation gate measures them. The loop also sets the input label from
    /// <see cref="UiContext.GameInput"/>. The read of the delta copies a struct part through a
    /// <see langword="ref readonly"/> member, and the write formats into the fixed scratch buffer, so the update of the
    /// input label allocates nothing. A measured frame adds 1 to the caret count when
    /// <see cref="UiContext.FocusedElement"/> is the text field after <see cref="UiContext.Update(float)"/>. The read
    /// is a reference compare inside the UI region, so the allocation gate measures it, and it allocates nothing. The
    /// loop also reads the current mode right after <see cref="UiContext.Update(float)"/>. A measured frame adds 1 to
    /// the main count when the current mode is <c>hello.main</c>, and 1 to the detail count when the current mode is
    /// <c>hello.detail</c>. The read is a reference compare inside the UI region, so it allocates nothing.
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
    /// and the measured frame count as line 3, the two gate counts and the measured frame count as line 4, the
    /// widget counts as line 5, the state counts as line 6, and the mode counts as line 7. The loop reads the widget
    /// counts, the state counts, and the switch and back counts after the last frame, so they cover the full run. The
    /// main count and the detail count cover the measured frames.
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
    /// <param name="panel">The root of the widget panel. The smoke input aims its pointer move at the panel. The
    /// manager keeps this panel mounted in the <c>top</c> slot through both modes.</param>
    /// <param name="gateLabel">The gate label of the screen. The loop sets its text after each draw.</param>
    /// <param name="inputLabel">
    /// The input label of the screen. The loop sets its text after each draw, from <see cref="UiContext.GameInput"/>.
    /// </param>
    /// <param name="widgets">
    /// The widget section of the screen. The smoke input aims its widget events at the widgets, and the loop reads the
    /// widget counts after the last frame.
    /// </param>
    /// <param name="state">
    /// The state of the sample. The loop pushes the gamepad buttons into its action map, the smoke input aims a click at
    /// its <c>Add</c> button, and the loop reads its counts after the last frame.
    /// </param>
    /// <param name="modes">
    /// The modes of the sample. The loop reads the current mode after each update, caches the center of the
    /// <c>Add</c> button in the frames that end in <c>hello.main</c> (Decision 6), counts the measured frames in each
    /// mode, and reads the switch and back counts after the last frame.
    /// </param>
    /// <param name="frameLimit">The number of frames to render, or <see cref="NoFrameLimit"/>.</param>
    /// <returns>
    /// The result of the run: the frame count, the draw list counts of the last frame, the allocation sums of the
    /// measured frames, the gate counts of the measured frames, the widget counts of the full run, the state counts
    /// of the full run, and the mode counts.
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
        Label inputLabel,
        HelloWidgets widgets,
        HelloState state,
        HelloModes modes,
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
        int caretFrames = 0;

        // The cached Add point (Decision 6) and the mode frame counts. A float2 local allocates nothing.
        float2 addPoint = default;
        int mainFrames = 0;
        int detailFrames = 0;

        // The scratch buffer of the input label. The longest input text needs 48 bytes, so the buffer never needs a
        // change of its size. The loop allocates nothing in it.
        byte[] inputScratch = new byte[64];

        // The counter value after the last release of the allocation context, or -1 before the first release.
        long releasedBytes = -1;
        while (frameLimit == NoFrameLimit || framesRendered < frameLimit)
        {
            // ImGui and the adapter both get each key event. The adapter pushes no key while ImGui wants the keyboard.
            // The flag of the last ImGui frame is the newest value before the poll.
            input.KeyboardEnabled = !io.WantCaptureKeyboard;
            Glfw.PollEvents();

            // The gamepad poll stays outside the UI region, so an allocation of the BRUTAL binding cannot fail the
            // allocation gate. The push into the map is the first line of the UI region (A7).
            UiGamepadButtons pad = input.PollGamepad();

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
            state.Map.PushGamepad(pad);

            // The smoke input (Decision 8). The events are structs, so the pushes allocate nothing.
            if (frameLimit != NoFrameLimit)
            {
                PushSmokeInput(ui, panel, widgets, addPoint, framesRendered);
            }

            float2 displaySize = io.DisplaySize;
            ui.ViewportSize = new float2(MathF.Max(displaySize.X, 0f), MathF.Max(displaySize.Y, 0f));

            // The input pass of Update drains the events of the poll and of the smoke input first.
            ui.Update(io.DeltaTime);

            // The mode diff applies at the start of Update. The read gives the current mode of this frame after the
            // diff. The read is a reference compare inside the UI region, so it allocates nothing.
            ModeDefinition? currentMode = modes.Manager.CurrentMode;

            // Decision 6: the smoke run aims the Add click at a point cached in a frame with hello.main as the
            // current mode. The hit test of the next frame reads the layout of this frame, so the point stays valid.
            if (ReferenceEquals(currentMode, modes.Main))
            {
                addPoint = ViewportCenter(state.AddButton);
            }

            // The caret pass of Update runs for the focus owner only (CR-3). The compare allocates nothing.
            bool fieldFocused = ui.FocusedElement == widgets.TextField;
            ui.Draw(uiRenderer);

            // The game handles the world input here, after Update and with the gate of this frame. With an ImGui
            // window over the game, the game uses gate.PointerOverUi || io.WantCaptureMouse for the pointer
            // (Decision 5). The keyboard side is the KeyboardEnabled line before the poll.
            UiRoutingGate gate = ui.RoutingGate;

            // The label shows the gate of this frame in the next frame. Each gate text fits the label buffer.
            gateLabel.SetText(HelloScreen.GateText(gate));

            // The label shows the game view of this frame in the next frame. The reads allocate nothing.
            inputLabel.SetText(HelloScreen.InputText(in ui.GameInput, inputScratch));
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

                if (fieldFocused)
                {
                    caretFrames++;
                }

                if (ReferenceEquals(currentMode, modes.Main))
                {
                    mainFrames++;
                }

                if (ReferenceEquals(currentMode, modes.Detail))
                {
                    detailFrames++;
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
            keyboardFocusInUiFrames,
            caretFrames,
            widgets.Clicks,
            widgets.Flips,
            widgets.Values,
            widgets.Scrolls,
            widgets.Edits,
            widgets.Submits,
            state.Adds,
            state.Resets,
            checked((int)(state.Score.Version - 1)),
            modes.Switches,
            modes.Backs,
            mainFrames,
            detailFrames);
    }

    /// <summary>
    /// Pushes the synthetic input of one frame of the smoke run: the widget events, one Tab key down and one Tab key up,
    /// one Escape key down and one Escape key up in each fourth frame, one F1 key down and one F1 key up or one F2 key
    /// down and one F2 key up, the longest text burst in the first measured frame only, the text field events, and then
    /// one pointer move.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The state script: a left press and a left release at <paramref name="addPoint"/>, the cached center of the
    /// <c>Add</c> button of <see cref="HelloState"/> (Decision 6). The pair comes after the toggle pair and before the
    /// slider pair. The click runs <c>hello.add</c> and raises the score. The slider press then keeps the focus, so the
    /// Tab still moves the focus from the slider to the text field. In each frame with <c>frameIndex % 4 == 3</c>, an
    /// Escape key down and key up with no target follow the Tab. The text field holds the focus then, and a focused
    /// field passes a non-text key to the action map. The map runs <c>hello.reset</c>, so the score text changes and the
    /// label binding does real work.
    /// </para>
    /// <para>
    /// The mode script (Decision 5): after the Escape block and before the longest text burst, one key down and one key
    /// up with no target. The key is <c>F1</c> when <c>(frameIndex - WarmUpFrames) % 2 == 0</c>, and <c>F2</c>
    /// otherwise. The rule runs in each frame, also in the warm-up frames. The text field holds the focus, because the
    /// field press comes later in the drain, and a focused field passes a non-text key to the action map. <c>F1</c>
    /// runs <c>hello.mode</c> and switches the mode. <c>F2</c> runs <c>hello.back</c> and steps back through the mode
    /// history. The frame rule: a key of frame N applies at the start of the update of frame N+1, so each measured
    /// frame from the first one runs one diff. With <c>--frames 120</c>, the run gives 60 switches, 59 backs, 59
    /// measured <c>hello.main</c> frames, and 58 measured <c>hello.detail</c> frames.
    /// </para>
    /// <para>
    /// The widget script: a left press and a left release at the center of the <c>Click me</c> button. The button
    /// clicks one time, and its delegate adds 1 to <see cref="HelloWidgets.Clicks"/>. The press also gives the focus to
    /// the button. A pointer event with a caller target ends no press, so each event has no target. The same pair
    /// flips the toggle at its center, so <see cref="HelloWidgets.Flips"/> rises. A third pair presses the slider at 25 %
    /// of its width in an even frame, or at 75 % in an odd frame. The press sets the value and the release ends it, so
    /// no drag follows and <see cref="HelloWidgets.Values"/> rises. A scroll event with no target follows at the center of
    /// the scroll view. Its delta is (0, -1) in an even frame and (0, 1) in an odd frame, so the offset moves down 40
    /// pixels and then back up. Each frame after the first even frame changes the offset, and
    /// <see cref="HelloWidgets.Scrolls"/> rises. The view is not inside another scroll view, so
    /// <see cref="ViewportCenter(Element)"/> needs no offset for it.
    /// </para>
    /// <para>
    /// The text field script: the char <c>a</c> in an even frame, or a Backspace key down in an odd frame, with the
    /// field as the caller target (Finding 13). The char adds <c>a</c> at the caret, and the Backspace removes it, so
    /// each frame changes the text and <see cref="HelloWidgets.Edits"/> rises. A main Enter key down with the field as
    /// the caller target follows, so <see cref="HelloWidgets.Submits"/> rises. A left press and a left release with no
    /// target at the center of the field come last. The press stores the pointer x, and the caret pass of
    /// <see cref="UiContext.Update(float)"/> maps it to a caret index with the measure of <see cref="ImGuiRenderer"/>.
    /// </para>
    /// <para>
    /// The longest text burst: in the first measured frame, <see cref="PushLongestTextBurst"/> pushes its events after
    /// the Tab and before the char or the Backspace of the frame. Thus the two text labels of
    /// <see cref="HelloWidgets"/> get their longest text in a measured frame, and the field ends the frame with the
    /// same text as with no burst.
    /// </para>
    /// <para>
    /// The focus rule: the caret pass runs for the focus owner only, and the field draws its caret only while it holds
    /// the focus. The field press is the last focus change of the drain, so the field holds the focus at the caret
    /// pass. A Tab after the field press moves the focus to the panel, and the caret pass and the caret draw then do
    /// not run. The caret count of <see cref="RunFrames"/> then stays at 0, and the widget check of
    /// <see cref="Run"/> fails. The press comes after the edit keys, because a change of the text after the press drops
    /// the stored pointer x, and the caret pass then maps nothing.
    /// </para>
    /// <para>
    /// The order rule: the widget events come before the move. The gate reads the hit of the last pointer event of the
    /// drain, so the move decides <see cref="UiRoutingGate.PointerOverUi"/>. A widget event after the move puts the
    /// pointer over the UI in the off-panel frames too, and the input check of <see cref="Run"/> fails.
    /// </para>
    /// <para>
    /// The move goes to the center of the panel in an even frame, and to a point 8 pixels above and to the left of the
    /// panel in an odd frame. That point is inside the root padding. The root is not pickable, so the move hits no
    /// element. Each position comes from the layout of the last <see cref="UiContext.Update(float)"/>, as the hit test
    /// of the input pass. Before the first layout, each rectangle is 0, and no event hits an element.
    /// </para>
    /// <para>
    /// The Tab down moves the focus to the next focusable element in tree order: from the slider to the text field. The
    /// focus stays in the UI. The Tab up does not move the focus. The presses on the widgets also give the focus to the
    /// UI, so the keyboard count of line 4 does not prove the Tab. <c>FocusAllocationTests</c> and the queue case of
    /// <c>ThousandElementAllocationTests</c> guard the Tab. The method uses the public push methods of
    /// <see cref="UiContext"/>, because the push methods of <see cref="GlfwUiInput"/> are internal. The events are
    /// structs, so the method allocates nothing.
    /// </para>
    /// </remarks>
    /// <param name="ui">The UI context that gets the events.</param>
    /// <param name="panel">The root of the widget panel. The move aims at its center in an even frame and at a point
    /// outside it in an odd frame.</param>
    /// <param name="widgets">The widget section of the screen. The widget events aim at its widgets.</param>
    /// <param name="addPoint">The cached center of the <c>Add</c> button of <see cref="HelloState"/> from the last
    /// frame with <c>hello.main</c> as the current mode (Decision 6). The click aims at it, because that button has
    /// no parent in a <c>hello.detail</c> frame. The click runs the <c>hello.add</c> command.</param>
    /// <param name="frameIndex">The index of the frame: the number of frames that the loop rendered before it.</param>
    private static void PushSmokeInput(
        UiContext ui, Element panel, HelloWidgets widgets, float2 addPoint, int frameIndex)
    {
        double time = Glfw.Time;

        // The widget script. The widget events come before the move (the order rule). The Add click comes before the
        // slider click, so the slider press keeps the focus and the Tab still moves it from the slider to the field.
        // The click aims at the cached point (Decision 6), because the Add button has no parent in a hello.detail
        // frame.
        PushClick(ui, ViewportCenter(widgets.ClickButton), time);
        PushClick(ui, ViewportCenter(widgets.Toggle), time);
        PushClick(ui, addPoint, time);
        PushClick(ui, SliderPoint(widgets.Slider, frameIndex), time);

        // The scroll at the center of the view. The delta is (0, -1) in an even frame and (0, 1) in an odd frame, so
        // the offset moves down in one frame and back up in the next frame.
        ui.PushEvent(new ScrollEvent
        {
            Header = new UiEventHeader(UiEventType.Scroll, time, null),
            Position = ViewportCenter(widgets.ScrollView),
            Delta = new float2(0f, frameIndex % 2 == 0 ? -1f : 1f),
            Modifiers = UiModifiers.None,
        });

        // The Tab moves the focus from the slider to the text field. The field press below gives the field the focus
        // too. The Tab comes before that press, because the field press is the last focus change of the drain (the
        // focus rule).
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

        // The Escape key runs the Cancel action of the map, and the action runs hello.reset. The key has no target and
        // comes after the Tab, so the text field holds the focus. The field passes a non-text key to the map. Each
        // fourth frame pushes it, so the score text changes between resets.
        if (frameIndex % 4 == 3)
        {
            ui.PushEvent(new KeyEvent
            {
                Header = new UiEventHeader(UiEventType.KeyDown, time, null),
                Key = (int)GlfwKey.Escape,
                ScanCode = 0,
                Modifiers = UiModifiers.None,
                IsRepeat = false,
            });
            ui.PushEvent(new KeyEvent
            {
                Header = new UiEventHeader(UiEventType.KeyUp, time, null),
                Key = (int)GlfwKey.Escape,
                ScanCode = 0,
                Modifiers = UiModifiers.None,
                IsRepeat = false,
            });
        }

        // The mode script (Decision 5). F1 runs hello.mode, and F2 runs hello.back. The keys have no target and come
        // after the Escape block, so the text field holds the focus and passes them to the map. The rule runs in each
        // frame, also in the warm-up frames.
        int modeKey = (frameIndex - WarmUpFrames) % 2 == 0 ? (int)GlfwKey.F1 : (int)GlfwKey.F2;
        ui.PushEvent(new KeyEvent
        {
            Header = new UiEventHeader(UiEventType.KeyDown, time, null),
            Key = modeKey,
            ScanCode = 0,
            Modifiers = UiModifiers.None,
            IsRepeat = false,
        });
        ui.PushEvent(new KeyEvent
        {
            Header = new UiEventHeader(UiEventType.KeyUp, time, null),
            Key = modeKey,
            ScanCode = 0,
            Modifiers = UiModifiers.None,
            IsRepeat = false,
        });

        // The longest text burst runs in the first measured frame only, before the char or the Backspace of the frame.
        TextField field = widgets.TextField;
        if (frameIndex == WarmUpFrames)
        {
            PushLongestTextBurst(ui, field, time);
        }

        // The text field script. The char and the keys have the field as the caller target (Finding 13).
        if (frameIndex % 2 == 0)
        {
            ui.PushEvent(new CharEvent
            {
                Header = new UiEventHeader(UiEventType.Char, time, field),
                CodePoint = SmokeChar,
            });
        }
        else
        {
            PushKeyDown(ui, field, SmokeBackspaceKey, time);
        }

        PushKeyDown(ui, field, SmokeEnterKey, time);

        // The press comes after the edit keys, so no text change drops the stored pointer x.
        PushClick(ui, ViewportCenter(field), time);

        // The move. The off-panel point is 8 pixels above and to the left of the top-left corner of the panel.
        float4 panelRect = panel.Layout.Rect;
        float2 panelCenter = ViewportCenter(panel);
        float2 position = frameIndex % 2 == 0
            ? panelCenter
            : new float2(
                panelCenter.X - (panelRect.Z * 0.5f) - SmokeOffPanelOffset,
                panelCenter.Y - (panelRect.W * 0.5f) - SmokeOffPanelOffset);

        ui.PushEvent(new PointerEvent
        {
            Header = new UiEventHeader(UiEventType.PointerMove, time, null),
            Position = position,
            Button = PointerButton.Left,
            Modifiers = UiModifiers.None,
        });
    }

    /// <summary>
    /// Pushes the longest text burst into the text field: <see cref="HelloWidgets.TextCapacity"/> chars of <c>W</c>,
    /// then one main Enter key down, then <see cref="HelloWidgets.TextCapacity"/> Backspace key downs. Each event has
    /// the field as the caller target.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The burst runs in the first measured frame (<see cref="WarmUpFrames"/>), an odd frame. The field starts that
    /// frame with the text <c>a</c> and the caret at the end, because the press of the frame before is past the end of
    /// the text. The field drops a char that does not fit, so 31 chars of <c>W</c> go in and the last one does not.
    /// The text then has <see cref="HelloWidgets.TextCapacity"/> bytes, and the text label shows its longest text (38
    /// bytes). The Enter then shows the longest text of the submit label (43 bytes). Both texts are longer than the
    /// start buffer of a label (32 bytes). Thus a label that does not get its longest text at the build grows its
    /// buffer in a measured frame, and the allocation check of <see cref="Run"/> fails.
    /// </para>
    /// <para>
    /// The Backspace keys remove the bytes before the caret, so the field is empty after the burst. The Backspace of
    /// the frame then changes nothing, and the field ends the frame empty, as in each odd frame. In an even frame, the
    /// burst also ends with an empty field, and the char of the frame then gives <c>a</c>.
    /// </para>
    /// <para>
    /// The frame then pushes about 80 events and the poll events of the window. The ring of the context holds
    /// <see cref="UiContext.DefaultEventQueueCapacity"/> (256) entries, so the frame drops no event. The events are
    /// structs, so the method allocates nothing.
    /// </para>
    /// </remarks>
    /// <param name="ui">The UI context that gets the events.</param>
    /// <param name="field">The text field. It is the caller target of each event.</param>
    /// <param name="time">The time stamp of the events, in seconds.</param>
    private static void PushLongestTextBurst(UiContext ui, TextField field, double time)
    {
        for (int i = 0; i < HelloWidgets.TextCapacity; i++)
        {
            ui.PushEvent(new CharEvent
            {
                Header = new UiEventHeader(UiEventType.Char, time, field),
                CodePoint = SmokeLongestChar,
            });
        }

        PushKeyDown(ui, field, SmokeEnterKey, time);
        for (int i = 0; i < HelloWidgets.TextCapacity; i++)
        {
            PushKeyDown(ui, field, SmokeBackspaceKey, time);
        }
    }

    /// <summary>
    /// Pushes one key down with a caller target, no modifier, the scan code 0, and no repeat. The method pushes no key
    /// up.
    /// </summary>
    /// <remarks>
    /// A key with a caller target keeps its target and changes no focus, so a Tab of the same frame does not move it.
    /// The event is a struct, so the method allocates nothing.
    /// </remarks>
    /// <param name="ui">The UI context that gets the event.</param>
    /// <param name="target">The caller target of the key.</param>
    /// <param name="key">The GLFW key code.</param>
    /// <param name="time">The time stamp of the event, in seconds.</param>
    private static void PushKeyDown(UiContext ui, Element target, int key, double time)
    {
        ui.PushEvent(new KeyEvent
        {
            Header = new UiEventHeader(UiEventType.KeyDown, time, target),
            Key = key,
            ScanCode = 0,
            Modifiers = UiModifiers.None,
            IsRepeat = false,
        });
    }

    /// <summary>
    /// Pushes a left press and a left release at one point, each with no target. On a widget that clicks, the pair
    /// gives one click.
    /// </summary>
    /// <remarks>
    /// The press and the release have no caller target, so the drain sets the press target from the hit test, and the
    /// release can end the press. The events are structs, so the method allocates nothing.
    /// </remarks>
    /// <param name="ui">The UI context that gets the events.</param>
    /// <param name="position">The point of the press and the release, in viewport pixels.</param>
    /// <param name="time">The time stamp of the events, in seconds.</param>
    private static void PushClick(UiContext ui, float2 position, double time)
    {
        ui.PushEvent(new PointerEvent
        {
            Header = new UiEventHeader(UiEventType.PointerDown, time, null),
            Position = position,
            Button = PointerButton.Left,
            Modifiers = UiModifiers.None,
        });
        ui.PushEvent(new PointerEvent
        {
            Header = new UiEventHeader(UiEventType.PointerUp, time, null),
            Position = position,
            Button = PointerButton.Left,
            Modifiers = UiModifiers.None,
        });
    }

    /// <summary>
    /// Gives the press point of the slider in a frame: 25 % of the width of the slider in an even frame, 75 % in an odd
    /// frame, at the middle of the height.
    /// </summary>
    /// <remarks>
    /// The two points map to two different whole values, and both differ from the start value of 50. Thus each frame
    /// changes the value and calls <see cref="Slider.ValueChanged"/>. The point is inside the content box, because the
    /// border of the slider is 2 pixels wide. Before the first layout, the result is (0, 0). The method allocates
    /// nothing.
    /// </remarks>
    /// <param name="slider">The slider. It must be in the tree of the context.</param>
    /// <param name="frameIndex">The index of the frame.</param>
    /// <returns>The press point in viewport pixels.</returns>
    private static float2 SliderPoint(Slider slider, int frameIndex)
    {
        float2 center = ViewportCenter(slider);
        float4 rect = slider.Layout.Rect;
        float fraction = frameIndex % 2 == 0 ? 0.25f : 0.75f;
        return new float2(center.X - (rect.Z * 0.5f) + (rect.Z * fraction), center.Y);
    }

    /// <summary>
    /// Gives the center of an element in viewport pixels, from the layout of the last
    /// <see cref="UiContext.Update(float)"/>.
    /// </summary>
    /// <remarks>
    /// The layout rectangle of an element is relative to its parent. The method adds the position of each ancestor up
    /// to the root, then adds half of the size of the element. Before the first layout, each rectangle is 0, so the
    /// result is (0, 0). The method subtracts no scroll offset. Thus a point of a widget inside a
    /// <see cref="ScrollView"/> needs the offset of the view too. The method allocates nothing.
    /// </remarks>
    /// <param name="element">The element. It must be in the tree of the context.</param>
    /// <returns>The center of the border box of <paramref name="element"/> in viewport pixels.</returns>
    private static float2 ViewportCenter(Element element)
    {
        float4 rect = element.Layout.Rect;
        float2 origin = new(rect.X, rect.Y);
        for (Element? ancestor = element.Parent; ancestor is not null; ancestor = ancestor.Parent)
        {
            float4 ancestorRect = ancestor.Layout.Rect;
            origin = new float2(origin.X + ancestorRect.X, origin.Y + ancestorRect.Y);
        }

        return new float2(origin.X + (rect.Z * 0.5f), origin.Y + (rect.W * 0.5f));
    }
}
