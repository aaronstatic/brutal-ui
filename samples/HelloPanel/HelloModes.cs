using System;
using System.Collections.Generic;
using System.IO;
using Brutal.GlfwApi;
using BrutalUi.Core;
using BrutalUi.Framework;
using BrutalUi.Markup;

namespace HelloPanel;

/// <summary>
/// The mode model of the sample: one layout with the slots <c>top</c> and <c>bottom</c>, the panels
/// <c>hello.widgets</c>, <c>hello.state</c>, and <c>hello.info</c>, and the modes <c>hello.main</c> and
/// <c>hello.detail</c>. The six records come from <c>HelloDefinitions.xml</c>. The class also owns the two
/// commands, the two actions, the two keys, and the mode button.
/// </summary>
/// <remarks>
/// <para>
/// The register order: <see cref="DefinitionLoadResult.RegisterTo(UiManager)"/> registers the three panels, then the
/// layout, then the two modes, in the text order of <c>HelloDefinitions.xml</c>. The file must keep this order,
/// because <see cref="UiManager.Register(ModeDefinition)"/> resolves each layout name, each slot name, and each
/// panel name against the names registered before it. The frame loop calls
/// <see cref="UiManager.SetMode(ReadOnlySpan{byte})"/> with <c>hello.main</c> one time before the first update.
/// </para>
/// <para>
/// The build rule: <see cref="Mount"/> calls <see cref="PanelController.EnsureRoot"/> on each controller that
/// <see cref="DefinitionLoadResult.RegisterTo(UiManager)"/> returns, so every root exists before the first frame and
/// no factory runs in a frame. The manager mounts the built roots at the first apply. <see cref="HelloScreen"/>
/// builds the widget, info, and layout trees, and <c>HelloState.xml</c> builds the state tree.
/// </para>
/// <para>
/// The map rule: both modes name the one action map of <see cref="HelloState"/>, so the switch keeps the map. The
/// diff calls <see cref="UiActionMap.Activate(UiContext)"/> on the map of each apply, and the call changes nothing for
/// the same map. The first apply activates the map before the input pass of frame 0, so the game does not call
/// <see cref="UiActionMap.Activate(UiContext)"/> itself.
/// </para>
/// <para>
/// The toggle rule: <c>hello.mode</c> reads <see cref="UiManager.TargetMode"/> first, then
/// <see cref="UiManager.CurrentMode"/>. Two presses in one frame therefore toggle two times. The command sets the
/// other mode with <see cref="UiManager.SetMode(ReadOnlySpan{byte})"/>.
/// </para>
/// <para>
/// The back rule: <c>hello.back</c> calls <see cref="ModeHistory.Back"/>. The call sets the mode that the last apply
/// replaced as the new target. An empty history gives <see langword="false"/>, and the command counts nothing. There
/// is no forward step.
/// </para>
/// <para>
/// The frame rule: <see cref="UiManager.SetMode(ReadOnlySpan{byte})"/> records the target and changes no tree. A key
/// in frame N applies at the start of the <see cref="UiContext.Update(float)"/> of frame N+1, before the input pass.
/// The hit test of that update reads the layout of frame N, so a press in the frame of the change can hit the old
/// tree.
/// </para>
/// <para>
/// The command rule: the two commands live in the registry of the map (<see cref="UiActionMap.Commands"/>), so one
/// registry serves the map, the buttons, and the <see cref="PanelContext"/>. The handlers are static fields with this
/// class as the target, so a command call allocates nothing. <c>F1</c> and the <c>Mode (F1)</c> button run
/// <c>hello.mode</c>, and <c>F2</c> runs <c>hello.back</c>. A focused <see cref="TextField"/> passes both keys to the
/// map, because neither key is a text key.
/// </para>
/// <para>
/// Copy this file to start a screen with modes of your game. Keep the register order and the build rule.
/// </para>
/// </remarks>
internal sealed class HelloModes
{
    /// <summary>The UTF-8 name of the main mode.</summary>
    public static ReadOnlySpan<byte> MainName => "hello.main"u8;

    /// <summary>The UTF-8 name of the detail mode.</summary>
    public static ReadOnlySpan<byte> DetailName => "hello.detail"u8;

    /// <summary>
    /// The handler of <c>hello.mode</c>. The target is the modes. The handler sets the other mode and counts the run.
    /// </summary>
    private static readonly CommandHandler s_mode = static target =>
    {
        HelloModes modes = (HelloModes)target!;
        ModeDefinition? from = modes.Manager.TargetMode ?? modes.Manager.CurrentMode;
        modes.Manager.SetMode(ReferenceEquals(from, modes.Detail) ? MainName : DetailName);
        modes.Switches++;
    };

    /// <summary>
    /// The handler of <c>hello.back</c>. The target is the modes. The handler counts a back step only when
    /// <see cref="ModeHistory.Back"/> returns <see langword="true"/>.
    /// </summary>
    private static readonly CommandHandler s_back = static target =>
    {
        HelloModes modes = (HelloModes)target!;
        if (modes.Manager.History.Back())
        {
            modes.Backs++;
        }
    };

    /// <summary>The selector of the score binding. It returns the score itself, static, created one time.</summary>
    private static readonly Func<int, int> s_score = static s => s;

    /// <summary>The state of the sample. Its map serves both modes, and its XML row fills the state panel.</summary>
    private readonly HelloState _state;

    /// <summary>The checker texture for the widget panel. <see cref="Mount"/> sets it before the first factory call.</summary>
    private TextureHandle _checker;

    /// <summary>The gate label that the widget factory writes. <see cref="Mount"/> copies it into <see cref="HelloMount.GateLabel"/>.</summary>
    private Label? _gateLabel;

    /// <summary>
    /// The input label that the widget factory writes. <see cref="Mount"/> copies it into
    /// <see cref="HelloMount.InputLabel"/>.
    /// </summary>
    private Label? _inputLabel;

    /// <summary>
    /// The stick label that the widget factory writes. <see cref="Mount"/> copies it into
    /// <see cref="HelloMount.StickLabel"/>.
    /// </summary>
    private Label? _stickLabel;

    /// <summary>
    /// The move label that the widget factory writes. <see cref="Mount"/> copies it into
    /// <see cref="HelloMount.MoveLabel"/>.
    /// </summary>
    private Label? _moveLabel;

    /// <summary>
    /// The scheme label that the widget factory writes. <see cref="Mount"/> copies it into
    /// <see cref="HelloMount.SchemeLabel"/>.
    /// </summary>
    private Label? _schemeLabel;

    /// <summary>
    /// The rebind label that the widget factory writes. <see cref="Mount"/> copies it into
    /// <see cref="HelloMount.RebindLabel"/>.
    /// </summary>
    private Label? _rebindLabel;

    /// <summary>The widget section that the widget factory writes. <see cref="Mount"/> copies it into
    /// <see cref="HelloMount.Widgets"/>.</summary>
    private HelloWidgets? _widgets;

    /// <summary>
    /// Creates the mode model: the commands, the keys, the mode button, the panel context, and the manager. The
    /// records register later, in <see cref="Mount"/>.
    /// </summary>
    /// <remarks>
    /// The constructor needs no GPU. The panel factories run only inside <see cref="Mount"/>, after the texture
    /// handle exists (Decision 5). The <see cref="UiManager"/> constructor writes the
    /// <see cref="UiContext.OnBeforeUpdate"/> slot of <paramref name="context"/> one time.
    /// </remarks>
    /// <param name="context">The context of the tree. The manager takes its before-update slot.</param>
    /// <param name="state">The state of the sample. Its command registry and its action map serve the modes.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="state"/> is
    /// <see langword="null"/>.</exception>
    public HelloModes(UiContext context, HelloState state)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(state);

        _state = state;

        // The commands join the registry of the map, so the F keys, the Add button, and the mode button hit one
        // register (Decision 2). The action names equal the command names.
        state.Map.Commands.Register("hello.mode"u8, s_mode, this);
        state.Map.Commands.Register("hello.back"u8, s_back, this);
        state.Map.AddAction("hello.mode"u8, "hello.mode"u8);
        state.Map.AddAction("hello.back"u8, "hello.back"u8);
        state.Map.BindKey("hello.mode"u8, (int)GlfwKey.F1);
        state.Map.BindKey("hello.back"u8, (int)GlfwKey.F2);
        ModeButton = new Button(new Button.CreateInfo { Text = "Mode (F1)" }).BindCommand(
            state.Map.Commands, "hello.mode"u8);

        PanelContext panels = new()
        {
            Commands = state.Map.Commands,
            Events = new EventBus(),
            Services = new ServiceRegistry(),
        };
        Manager = new UiManager(new UiManager.CreateInfo { Context = context, Panels = panels });
    }

    /// <summary>
    /// Loads the state panel template from <c>HelloState.xml</c> and the six records from
    /// <c>HelloDefinitions.xml</c>, registers the records with the manager, builds every panel root, and returns the
    /// mounted trees.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The flow order: the bindings attach to the template loader, the generated register call adds the marked
    /// types, the check load of <c>HelloState.xml</c> runs first, and the definitions load follows. The load calls
    /// no factory; <see cref="PanelController.EnsureRoot"/> runs the factories (the build rule). Phase 2 moves this
    /// flow and the error print to <c>HelloMarkup.Load</c>, ahead of the window.
    /// </para>
    /// <para>
    /// A failed load throws with one message line per error, in the <see cref="TemplateError.ToString"/> and
    /// <see cref="DefinitionError.ToString"/> form, joined with LF (Notes item 7). The check tree is dropped: the
    /// mounted state root comes from the template panel record (Decision 9).
    /// </para>
    /// </remarks>
    /// <param name="checker">The checker texture for the image of the widget panel.</param>
    /// <returns>The mount: the two roots, the six labels, the widgets, the <c>Add</c> button, and the counts.</returns>
    /// <exception cref="InvalidOperationException">
    /// The check load or the definitions load failed, with one error line each. The widget factory left the gate
    /// label, the input label, the stick label, the move label, the scheme label, the rebind label, or the widget
    /// section null. A panel controller, a mounted element, or a loaded mode is missing. The build rule cannot fail
    /// in this sample; the checks document the contract of <see cref="HelloScreen.BuildWidgetPanel"/>.
    /// </exception>
    public HelloMount Mount(TextureHandle checker)
    {
        _checker = checker;

        byte[] stateText = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "HelloState.xml"));
        byte[] definitionsText = File.ReadAllBytes(
            Path.Combine(AppContext.BaseDirectory, "HelloDefinitions.xml"));

        TemplateLoader templates = new();
        TemplateBindings bindings = new(_state.Map.Commands);
        bindings.AddText("hello.score", _state.Score, s_score);
        bindings.AttachTo(templates);
        templates.RegisterHelloPanelElements();

        TemplateLoadResult check = templates.Load(stateText, "HelloState.xml");
        if (!check.Succeeded)
        {
            throw new InvalidOperationException(JoinErrors(check.Errors));
        }

        DefinitionLoader loader = new(templates);
        loader.AddTemplate("HelloState.xml", stateText);
        loader.AddFactory("HelloModes.CreateWidgetPanel", CreateWidgetPanel);
        loader.AddFactory("HelloModes.CreateInfoPanel", CreateInfoPanel);
        loader.AddFactory("HelloModes.CreateLayout", CreateLayout);
        loader.AddActionMap("hello.actions", _state.Map);

        DefinitionLoadResult result = loader.Load(definitionsText, "HelloDefinitions.xml");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(JoinErrors(result.Errors));
        }

        IReadOnlyList<PanelController> controllers = result.RegisterTo(Manager);

        // The build rule: each root exists before the first frame, so no factory runs in a frame (Decision 3).
        foreach (PanelController controller in controllers)
        {
            controller.EnsureRoot();
        }

        // The widget factory ran inside the EnsureRoot loop and wrote its seven out fields.
        Label gateLabel = _gateLabel ?? throw new InvalidOperationException("The widget factory did not give the gate label.");
        Label inputLabel = _inputLabel
            ?? throw new InvalidOperationException("The widget factory did not give the input label.");
        Label stickLabel = _stickLabel
            ?? throw new InvalidOperationException("The widget factory did not give the stick label.");
        Label moveLabel = _moveLabel
            ?? throw new InvalidOperationException("The widget factory did not give the move label.");
        Label schemeLabel = _schemeLabel
            ?? throw new InvalidOperationException("The widget factory did not give the scheme label.");
        Label rebindLabel = _rebindLabel
            ?? throw new InvalidOperationException("The widget factory did not give the rebind label.");
        HelloWidgets widgetsSection = _widgets
            ?? throw new InvalidOperationException("The widget factory did not give the widget section.");

        Element widgetRoot = FindRoot(controllers, "hello.widgets"u8, "hello.widgets");
        Element stateRoot = FindRoot(controllers, "hello.state"u8, "hello.state");
        Button addButton = stateRoot.Find<Button>("add"u8)
            ?? throw new InvalidOperationException("The state panel has no 'add' button.");

        Main = FindMode(result.Modes, MainName, "hello.main");
        Detail = FindMode(result.Modes, DetailName, "hello.detail");

        // Decision 9: line 8 counts the mounted state root, not the dropped check tree.
        int elements = 0;
        int bindingCount = 0;
        int commandCount = 0;
        CountTree(stateRoot, ref elements, ref bindingCount, ref commandCount);
        MarkupCounts markup = new(
            elements, bindingCount, commandCount, result.Panels.Count, result.Layouts.Count, result.Modes.Count);

        return new HelloMount
        {
            WidgetPanel = widgetRoot,
            GateLabel = gateLabel,
            InputLabel = inputLabel,
            StickLabel = stickLabel,
            MoveLabel = moveLabel,
            SchemeLabel = schemeLabel,
            RebindLabel = rebindLabel,
            Widgets = widgetsSection,
            AddButton = addButton,
            Markup = markup,
        };
    }

    /// <summary>Gets the manager that applies the modes.</summary>
    public UiManager Manager { get; }

    /// <summary>
    /// Gets the definition of the main mode: the widget panel in <c>top</c> and the state panel in <c>bottom</c>.
    /// <see cref="Mount"/> sets it from the loaded records. The <c>hello.mode</c> handler reads it.
    /// </summary>
    public ModeDefinition? Main { get; private set; }

    /// <summary>
    /// Gets the definition of the detail mode: the widget panel in <c>top</c> and the info panel in <c>bottom</c>.
    /// <see cref="Mount"/> sets it from the loaded records. The <c>hello.mode</c> handler reads it.
    /// </summary>
    public ModeDefinition? Detail { get; private set; }

    /// <summary>Gets the <c>Mode (F1)</c> button. A click runs <c>hello.mode</c>.</summary>
    public Button ModeButton { get; }

    /// <summary>Gets the count of runs of <c>hello.mode</c>.</summary>
    public int Switches { get; private set; }

    /// <summary>Gets the count of runs of <c>hello.back</c> where <see cref="ModeHistory.Back"/> returned <see langword="true"/>.</summary>
    public int Backs { get; private set; }

    /// <summary>
    /// Finds the built root of the controller whose definition name equals the given name. A missing controller or a
    /// missing root throws <see cref="InvalidOperationException"/> with a message that names the panel.
    /// </summary>
    private static Element FindRoot(IReadOnlyList<PanelController> controllers, ReadOnlySpan<byte> name, string displayName)
    {
        foreach (PanelController controller in controllers)
        {
            if (controller.Definition.Name.SequenceEqual(name))
            {
                return controller.Instance.Root
                    ?? throw new InvalidOperationException($"The panel '{displayName}' has no built root.");
            }
        }

        throw new InvalidOperationException($"RegisterTo gave no controller for the panel '{displayName}'.");
    }

    /// <summary>
    /// Finds the loaded mode whose name equals the given name. A missing mode throws
    /// <see cref="InvalidOperationException"/> with a message that names the mode.
    /// </summary>
    private static ModeDefinition FindMode(IReadOnlyList<ModeDefinition> modes, ReadOnlySpan<byte> name, string displayName)
    {
        foreach (ModeDefinition mode in modes)
        {
            if (mode.Name.SequenceEqual(name))
            {
                return mode;
            }
        }

        throw new InvalidOperationException($"The definitions load gave no mode '{displayName}'.");
    }

    /// <summary>
    /// Adds one subtree to the three markup counts: the element count, the binding sum, and the command count. The
    /// command count counts each <see cref="Button"/> with a <see cref="Button.Clicked"/> slot that is not null.
    /// </summary>
    private static void CountTree(Element element, ref int elements, ref int bindings, ref int commands)
    {
        elements++;
        bindings += element.BindingCount;
        if (element is Button button && button.Clicked is not null)
        {
            commands++;
        }

        foreach (Element child in element.Children)
        {
            CountTree(child, ref elements, ref bindings, ref commands);
        }
    }

    /// <summary>Joins the <see cref="object.ToString"/> line of each error with LF.</summary>
    private static string JoinErrors<T>(IReadOnlyList<T> errors)
        where T : struct
    {
        List<string> lines = new(errors.Count);
        foreach (T error in errors)
        {
            lines.Add(error.ToString() ?? string.Empty);
        }

        return string.Join("\n", lines);
    }

    /// <summary>The factory of <c>hello.widgets</c>. It builds the widget panel and writes seven out fields.</summary>
    /// <param name="context">The panel context. The factory reads nothing from it.</param>
    /// <returns>The root of the widget panel.</returns>
    internal Element CreateWidgetPanel(PanelContext context) =>
        HelloScreen.BuildWidgetPanel(
            _checker, ModeButton, out _gateLabel, out _inputLabel, out _stickLabel, out _moveLabel, out _schemeLabel,
            out _rebindLabel, out _widgets);

    /// <summary>The factory of <c>hello.info</c>.</summary>
    /// <param name="context">The panel context. The factory reads nothing from it.</param>
    /// <returns>The root of the info panel.</returns>
    internal Element CreateInfoPanel(PanelContext context) => HelloScreen.BuildInfoPanel();

    /// <summary>The factory of <c>hello.layout</c>.</summary>
    /// <param name="context">The panel context. The factory reads nothing from it.</param>
    /// <returns>The root of the layout.</returns>
    internal Element CreateLayout(PanelContext context) => HelloScreen.BuildLayout();
}

/// <summary>
/// The mount of the sample: the trees, the labels, the widgets, the <c>Add</c> button, and the markup counts that
/// <see cref="HelloModes.Mount"/> builds. The frame loop reads the members in place of the eight tree arguments of
/// the old constructor path. The <see cref="required"/> init properties keep each member non-null.
/// </summary>
internal sealed class HelloMount
{
    /// <summary>Gets or sets the root of the widget panel. The smoke run aims its pointer move at it.</summary>
    public required Element WidgetPanel { get; init; }

    /// <summary>Gets or sets the gate label of the widget panel. The frame loop sets its text after each draw.</summary>
    public required Label GateLabel { get; init; }

    /// <summary>Gets or sets the input label of the widget panel. The frame loop sets its text after each draw.</summary>
    public required Label InputLabel { get; init; }

    /// <summary>Gets or sets the stick label of the widget panel. The frame loop sets its text after each draw.</summary>
    public required Label StickLabel { get; init; }

    /// <summary>Gets or sets the move label of the widget panel. The frame loop sets its text after each draw.</summary>
    public required Label MoveLabel { get; init; }

    /// <summary>Gets or sets the scheme label of the widget panel. The frame loop sets its text after each draw.</summary>
    public required Label SchemeLabel { get; init; }

    /// <summary>Gets or sets the rebind label of the widget panel. The frame loop sets its text after each draw.</summary>
    public required Label RebindLabel { get; init; }

    /// <summary>Gets or sets the widget section. The smoke run aims its widget events at the widgets and reads the counts.</summary>
    public required HelloWidgets Widgets { get; init; }

    /// <summary>Gets or sets the <c>Add</c> button of the XML state panel. The smoke input aims its Add click at
    /// this button (Q2).</summary>
    public required Button AddButton { get; init; }

    /// <summary>Gets or sets the markup counts. <see cref="HelloModes.Mount"/> computes them one time after the
    /// build rule.</summary>
    public required MarkupCounts Markup { get; init; }
}

/// <summary>
/// The markup counts of smoke line 8. The first three counts cover the mounted state root after the build rule
/// (Decision 9); the last three are the record counts of the loaded definitions.
/// </summary>
/// <param name="Elements">The element count of the mounted state root, the root included.</param>
/// <param name="Bindings">The sum of <see cref="Element.BindingCount"/> over the mounted state tree.</param>
/// <param name="Commands">The count of buttons in the mounted state tree with a click slot that is not null.</param>
/// <param name="Panels">The count of the loaded panel records.</param>
/// <param name="Layouts">The count of the loaded layout records.</param>
/// <param name="Modes">The count of the loaded mode records.</param>
internal readonly record struct MarkupCounts(
    int Elements,
    int Bindings,
    int Commands,
    int Panels,
    int Layouts,
    int Modes);
