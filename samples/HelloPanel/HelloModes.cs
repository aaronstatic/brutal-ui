using System;
using Brutal.GlfwApi;
using BrutalUi.Core;
using BrutalUi.Framework;

namespace HelloPanel;

/// <summary>
/// The mode model of the sample: one layout with the slots <c>top</c> and <c>bottom</c>, the panels
/// <c>hello.widgets</c>, <c>hello.state</c>, and <c>hello.info</c>, and the modes <c>hello.main</c> and
/// <c>hello.detail</c>. The class also owns the two commands, the two actions, the two keys, and the mode button.
/// </summary>
/// <remarks>
/// <para>
/// The register order: the constructor registers the three panels, then the layout, then the two modes.
/// <see cref="UiManager.Register(ModeDefinition)"/> resolves each layout name, each slot name, and each panel name
/// against the names registered before it, so this order is a must. The frame loop calls
/// <see cref="UiManager.SetMode(ReadOnlySpan{byte})"/> with <c>hello.main</c> one time before the first update.
/// </para>
/// <para>
/// The build rule: the constructor calls <see cref="PanelController.EnsureRoot"/> on each panel controller, so every
/// root exists at build time and no factory runs in a frame. The manager mounts the built roots at the first apply.
/// <see cref="HelloScreen"/> builds the trees.
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

    /// <summary>The state of the sample. Its map serves both modes, and its row fills the state panel.</summary>
    private readonly HelloState _state;

    /// <summary>The checker texture for the widget panel.</summary>
    private readonly TextureHandle _checker;

    /// <summary>The gate label that the widget factory writes. The constructor copies it into <see cref="GateLabel"/>.</summary>
    private Label? _gateLabel;

    /// <summary>The widget section that the widget factory writes. The constructor copies it into <see cref="Widgets"/>.</summary>
    private HelloWidgets? _widgets;

    /// <summary>
    /// Creates the mode model: the commands, the actions, the keys, the mode button, the panel context, the manager,
    /// the three panel registers, the built roots, the layout register, and the two mode registers.
    /// </summary>
    /// <remarks>
    /// The constructor does all the allocation, at build time. Each register call allocates, and
    /// <see cref="UiManager"/> writes the <see cref="UiContext.OnBeforeUpdate"/> slot of
    /// <paramref name="context"/> one time.
    /// </remarks>
    /// <param name="context">The context of the tree. The manager takes its before-update slot.</param>
    /// <param name="state">The state of the sample. Its command registry and its action map serve the modes.</param>
    /// <param name="checker">The checker texture for the image of the widget panel.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="state"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// The widget factory left <see cref="GateLabel"/> or <see cref="Widgets"/> null. The build rule cannot fail in
    /// this sample; the check documents the contract of <see cref="HelloScreen.BuildWidgetPanel"/>.
    /// </exception>
    public HelloModes(UiContext context, HelloState state, TextureHandle checker)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(state);

        _state = state;
        _checker = checker;

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

        // Register the panels first, then the layout, then the modes (the register order rule).
        PanelController widgets = Manager.Register(
            new PanelDefinition("hello.widgets"u8, "top"u8, CreateWidgetPanel));
        PanelController statePanel = Manager.Register(
            new PanelDefinition("hello.state"u8, "bottom"u8, CreateStatePanel));
        PanelController infoPanel = Manager.Register(
            new PanelDefinition("hello.info"u8, "bottom"u8, CreateInfoPanel));

        // The build rule: each root exists before the first frame, so no factory runs in a frame (Decision 3).
        widgets.EnsureRoot();
        statePanel.EnsureRoot();
        infoPanel.EnsureRoot();

        GateLabel = _gateLabel ?? throw new InvalidOperationException("The widget factory did not give the gate label.");
        Widgets = _widgets ?? throw new InvalidOperationException("The widget factory did not give the widget section.");
        WidgetPanel = widgets.Instance.Root!;

        Manager.Register(new LayoutDefinition("hello.layout"u8, CreateLayout));

        Main = new ModeDefinition(
            "hello.main"u8,
            "hello.layout"u8,
            [new ModePanel("top"u8, "hello.widgets"u8), new ModePanel("bottom"u8, "hello.state"u8)],
            state.Map);
        Detail = new ModeDefinition(
            "hello.detail"u8,
            "hello.layout"u8,
            [new ModePanel("top"u8, "hello.widgets"u8), new ModePanel("bottom"u8, "hello.info"u8)],
            state.Map);
        Manager.Register(Main);
        Manager.Register(Detail);
    }

    /// <summary>Gets the manager that applies the modes.</summary>
    public UiManager Manager { get; }

    /// <summary>Gets the definition of the main mode: the widget panel in <c>top</c> and the state panel in <c>bottom</c>.</summary>
    public ModeDefinition Main { get; }

    /// <summary>Gets the definition of the detail mode: the widget panel in <c>top</c> and the info panel in <c>bottom</c>.</summary>
    public ModeDefinition Detail { get; }

    /// <summary>Gets the root of the widget panel. The smoke run aims its pointer move at it.</summary>
    public Element WidgetPanel { get; }

    /// <summary>Gets the gate label of the widget panel. The frame loop sets its text after each draw.</summary>
    public Label GateLabel { get; }

    /// <summary>Gets the widget section. The smoke run aims its widget events at the widgets and reads the counts.</summary>
    public HelloWidgets Widgets { get; }

    /// <summary>Gets the <c>Mode (F1)</c> button. A click runs <c>hello.mode</c>.</summary>
    public Button ModeButton { get; }

    /// <summary>Gets the count of runs of <c>hello.mode</c>.</summary>
    public int Switches { get; private set; }

    /// <summary>Gets the count of runs of <c>hello.back</c> where <see cref="ModeHistory.Back"/> returned <see langword="true"/>.</summary>
    public int Backs { get; private set; }

    /// <summary>The factory of <c>hello.widgets</c>. It builds the widget panel and writes the two out fields.</summary>
    /// <param name="context">The panel context. The factory reads nothing from it.</param>
    /// <returns>The root of the widget panel.</returns>
    private Element CreateWidgetPanel(PanelContext context) =>
        HelloScreen.BuildWidgetPanel(_checker, ModeButton, out _gateLabel, out _widgets);

    /// <summary>The factory of <c>hello.state</c>.</summary>
    /// <param name="context">The panel context. The factory reads nothing from it.</param>
    /// <returns>The root of the state panel.</returns>
    private Element CreateStatePanel(PanelContext context) => HelloScreen.BuildStatePanel(_state);

    /// <summary>The factory of <c>hello.info</c>.</summary>
    /// <param name="context">The panel context. The factory reads nothing from it.</param>
    /// <returns>The root of the info panel.</returns>
    private Element CreateInfoPanel(PanelContext context) => HelloScreen.BuildInfoPanel();

    /// <summary>The factory of <c>hello.layout</c>.</summary>
    /// <param name="context">The panel context. The factory reads nothing from it.</param>
    /// <returns>The root of the layout.</returns>
    private Element CreateLayout(PanelContext context) => HelloScreen.BuildLayout();
}
