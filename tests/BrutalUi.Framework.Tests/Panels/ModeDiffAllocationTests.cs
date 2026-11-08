using System;
using System.Text;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The eighteenth allocation gate: the toggle cycle on a HelloPanel shape with real widgets allocates 0 bytes, and an
/// idle frame allocates 0 bytes. A third test reads the tree and proves that the measured body does real work.
/// </summary>
/// <remarks>
/// <para>
/// The scene builds in the process with a <see cref="RecordingRenderer"/> measurer and a viewport of 800 by 600. The
/// gate opens no window. The layout root is a column of 800 by 600 with the slots <c>top</c> and <c>bottom</c>. The
/// modes <c>mda.main</c> and <c>mda.detail</c> sit on the one layout and name the one action map. The widget panel
/// holds a label, a bound button, and a focused text field. The state panel holds a bound button and a label bound to
/// a store. The info panel holds one label. Each panel uses inline styles only.
/// </para>
/// <para>
/// The rule of the measured body (Decision 9): it holds no lambda, no string literal, no boxed value, no new array,
/// and no <c>Assert</c>. The command handler and the store selector live in static fields, so each one builds one
/// time at warm-up. Each 0-byte test asserts 0 bytes and a fixed sum, so an empty body cannot pass. Plants 3 to 5
/// guard the three tests.
/// </para>
/// </remarks>
public sealed class ModeDiffAllocationTests
{
    /// <summary>The count of measured steps in each 0-byte test.</summary>
    private const int Iterations = 100;

    /// <summary>The count of steps that warm up the tree, the arrays, and the JIT before a measure.</summary>
    private const int WarmUpRounds = 2;

    /// <summary>The GLFW code of the Escape key, which the map binds to the cancel action.</summary>
    private const int EscapeKey = 256;

    /// <summary>The handler of the three commands. The field builds it one time, so the measured scene holds no per-call delegate.</summary>
    private static readonly CommandHandler s_count = static target => ((Counter)target!).Calls++;

    /// <summary>The selector of the score label: the identity on the store value. The field builds it one time.</summary>
    private static readonly Func<int, int> s_score = static value => value;

    /// <summary>The target of a command handler. It counts the calls.</summary>
    private sealed class Counter
    {
        /// <summary>The count of calls.</summary>
        public int Calls;
    }

    /// <summary>A panel or layout factory: it records its calls and returns one built root.</summary>
    private sealed class Factory
    {
        private readonly Element _root;

        /// <summary>The count of calls.</summary>
        public int Calls;

        /// <summary>Creates a factory for one built root.</summary>
        /// <param name="root">The root. The factory never builds another one.</param>
        public Factory(Element root)
        {
            _root = root;
        }

        /// <summary>Records one call and returns the built root.</summary>
        /// <param name="context">The context of the controller. The factory reads nothing from it.</param>
        /// <returns>The built root.</returns>
        public Element Create(PanelContext context)
        {
            Calls++;
            return _root;
        }
    }

    /// <summary>The scene of the gate: the context, the manager, the modes, the map, the store, the tree parts, and the factories.</summary>
    private sealed class Scene
    {
        /// <summary>The context of the tree.</summary>
        public UiContext Context = null!;

        /// <summary>The renderer: the measurer of the context and the target of each draw.</summary>
        public RecordingRenderer Renderer = null!;

        /// <summary>The manager under test.</summary>
        public UiManager Ui = null!;

        /// <summary>The action map that both modes name.</summary>
        public UiActionMap Map = null!;

        /// <summary>The store that the score label shows.</summary>
        public Store<int> Score = null!;

        /// <summary>The definition of the main mode.</summary>
        public ModeDefinition Main = null!;

        /// <summary>The definition of the detail mode.</summary>
        public ModeDefinition Detail = null!;

        /// <summary>The slot <c>top</c> of the layout root.</summary>
        public SlotContainer Top = null!;

        /// <summary>The slot <c>bottom</c> of the layout root.</summary>
        public SlotContainer Bottom = null!;

        /// <summary>The root of the widget panel.</summary>
        public Element WidgetRoot = null!;

        /// <summary>The root of the state panel.</summary>
        public Element StateRoot = null!;

        /// <summary>The root of the info panel.</summary>
        public Element InfoRoot = null!;

        /// <summary>The label bound to the store.</summary>
        public Label ScoreLabel = null!;

        /// <summary>The focused text field.</summary>
        public TextField Field = null!;

        /// <summary>The factory of the layout root.</summary>
        public Factory LayoutFactory = null!;

        /// <summary>The factory of the widget root.</summary>
        public Factory WidgetFactory = null!;

        /// <summary>The factory of the state root.</summary>
        public Factory StateFactory = null!;

        /// <summary>The factory of the info root.</summary>
        public Factory InfoFactory = null!;
    }

    /// <summary>
    /// Builds the scene: the context, the registry, the map, the store, the manager, the three panels with the roots
    /// built at build time, the layout, and the two modes. The build applies <c>mda.main</c>, gives the focus to the
    /// field, runs one update and one draw, and runs one toggle step, so each root is built, styled, and laid out.
    /// </summary>
    /// <returns>The scene, ready for warm-up and a measure.</returns>
    private static Scene BuildScene()
    {
        Scene scene = new();
        scene.Renderer = new RecordingRenderer();
        scene.Context = new UiContext(new UiContext.CreateInfo { Measurer = scene.Renderer })
        {
            ViewportSize = new float2(800f, 600f),
        };

        CommandRegistry commands = new();
        commands.Register("mda.mode"u8, s_count, new Counter());
        commands.Register("mda.add"u8, s_count, new Counter());
        commands.Register("mda.cancel"u8, s_count, new Counter());

        scene.Map = new UiActionMap(new UiActionMap.CreateInfo { Commands = commands });
        scene.Map.AddAction("mda.cancel"u8, "mda.cancel"u8);
        scene.Map.BindKey("mda.cancel"u8, EscapeKey);

        scene.Score = new Store<int>(new Store<int>.CreateInfo { Initial = 0 });

        PanelContext panels = new()
        {
            Commands = commands,
            Events = new EventBus(),
            Services = new ServiceRegistry(),
        };

        scene.Ui = new UiManager(new UiManager.CreateInfo { Context = scene.Context, Panels = panels });

        // The widget panel: a label, a bound button, and a text field of 120 by 24.
        Label titleLabel = new(new Label.CreateInfo { Text = "Widgets" });
        Button modeButton = new(new Button.CreateInfo { Text = "Mode" });
        modeButton.BindCommand(commands, "mda.mode"u8);
        scene.Field = new TextField(new TextField.CreateInfo { Capacity = 16 });
        scene.Field.Inline.Width = 120;
        scene.Field.Inline.Height = 24;
        scene.WidgetRoot = new Element();
        scene.WidgetRoot.Children.Add(titleLabel);
        scene.WidgetRoot.Children.Add(modeButton);
        scene.WidgetRoot.Children.Add(scene.Field);

        // The state panel: a bound button of 80 by 24 and the score label.
        Button addButton = new(new Button.CreateInfo { Text = "Add" });
        addButton.Inline.Width = 80;
        addButton.Inline.Height = 24;
        addButton.BindCommand(commands, "mda.add"u8);
        scene.ScoreLabel = new Label(default(Label.CreateInfo)).BindText(scene.Score, s_score);
        scene.StateRoot = new Element();
        scene.StateRoot.Children.Add(addButton);
        scene.StateRoot.Children.Add(scene.ScoreLabel);

        // The info panel: one label.
        scene.InfoRoot = new Element();
        scene.InfoRoot.Children.Add(new Label(new Label.CreateInfo { Text = "Info" }));

        // The layout root: a column of 800 by 600 with the slots top and bottom.
        scene.Top = new SlotContainer("top"u8);
        scene.Bottom = new SlotContainer("bottom"u8);
        Element layout = new();
        layout.Inline.Width = 800;
        layout.Inline.Height = 600;
        layout.IsPickable = false;
        layout.Children.Add(scene.Top);
        layout.Children.Add(scene.Bottom);

        // Build each root at build time (Decision 3), so no factory runs in a frame.
        scene.LayoutFactory = new Factory(layout);
        scene.WidgetFactory = new Factory(scene.WidgetRoot);
        scene.StateFactory = new Factory(scene.StateRoot);
        scene.InfoFactory = new Factory(scene.InfoRoot);

        PanelController widgetPanel = scene.Ui.Register(new PanelDefinition("mda.widgets"u8, "top"u8, scene.WidgetFactory.Create));
        PanelController statePanel = scene.Ui.Register(new PanelDefinition("mda.state"u8, "bottom"u8, scene.StateFactory.Create));
        PanelController infoPanel = scene.Ui.Register(new PanelDefinition("mda.info"u8, "bottom"u8, scene.InfoFactory.Create));
        widgetPanel.EnsureRoot();
        statePanel.EnsureRoot();
        infoPanel.EnsureRoot();
        scene.Ui.Register(new LayoutDefinition("mda.layout"u8, scene.LayoutFactory.Create));

        ModePanel[] entriesMain =
        [
            new ModePanel("top"u8, "mda.widgets"u8),
            new ModePanel("bottom"u8, "mda.state"u8),
        ];
        ModePanel[] entriesDetail =
        [
            new ModePanel("top"u8, "mda.widgets"u8),
            new ModePanel("bottom"u8, "mda.info"u8),
        ];
        scene.Main = new ModeDefinition("mda.main"u8, "mda.layout"u8, entriesMain, scene.Map);
        scene.Detail = new ModeDefinition("mda.detail"u8, "mda.layout"u8, entriesDetail, scene.Map);
        scene.Ui.Register(scene.Main);
        scene.Ui.Register(scene.Detail);

        // Apply Main, give the focus to the field, run one update and one draw, then run one toggle step.
        scene.Ui.SetMode("mda.main"u8);
        scene.Context.Update(0.016f);
        Assert.True(scene.Context.SetFocus(scene.Field));
        scene.Context.Update(0.016f);
        scene.Context.Draw(scene.Renderer);
        ToggleStep(scene);
        return scene;
    }

    /// <summary>
    /// The measured body of the toggle gate (Decision 9): <c>SetMode(Detail)</c>, one update, a store set,
    /// <c>SetMode(Main)</c>, one update, a back step, one update, <c>SetMode(Main)</c>, one update, and one draw. The
    /// store set while the state panel is unmounted makes the label binding do real work at the next mount. The
    /// focused field makes the caret pass run in each update. Both modes name the one map, so each apply runs the
    /// same-map <see cref="UiActionMap.Activate(UiContext)"/> call. After each update the body adds 1 for the expected
    /// mode and the child count of each slot. The body holds no lambda, no string literal, no boxed value, no new
    /// array, and no <c>Assert</c>.
    /// </summary>
    /// <param name="scene">The scene.</param>
    /// <returns>The count of the work that the step did: 12 for one full step.</returns>
    private static int ToggleStep(Scene scene)
    {
        int sum = 0;

        scene.Ui.SetMode(scene.Detail.Name);
        scene.Context.Update(0.016f);
        if (ReferenceEquals(scene.Ui.CurrentMode, scene.Detail))
        {
            sum++;
        }

        sum += scene.Top.Children.Count + scene.Bottom.Children.Count;

        scene.Score.Set(scene.Score.Value + 1);
        scene.Ui.SetMode(scene.Main.Name);
        scene.Context.Update(0.016f);
        if (ReferenceEquals(scene.Ui.CurrentMode, scene.Main))
        {
            sum++;
        }

        sum += scene.Top.Children.Count + scene.Bottom.Children.Count;

        scene.Ui.History.Back();
        scene.Context.Update(0.016f);
        if (ReferenceEquals(scene.Ui.CurrentMode, scene.Detail))
        {
            sum++;
        }

        sum += scene.Top.Children.Count + scene.Bottom.Children.Count;

        scene.Ui.SetMode(scene.Main.Name);
        scene.Context.Update(0.016f);
        if (ReferenceEquals(scene.Ui.CurrentMode, scene.Main))
        {
            sum++;
        }

        sum += scene.Top.Children.Count + scene.Bottom.Children.Count;

        scene.Context.Draw(scene.Renderer);
        return sum;
    }

    /// <summary>
    /// The measured body of the idle gate: one update with no target and one draw. The body adds 1 for the main mode
    /// and the child count of each slot: 3. It holds no lambda, no string literal, no boxed value, no new array, and
    /// no <c>Assert</c>.
    /// </summary>
    /// <param name="scene">The scene, applied on <c>mda.main</c> with no target.</param>
    /// <returns>The count of the work that the frame did: 3 for one frame.</returns>
    private static int IdleStep(Scene scene)
    {
        scene.Context.Update(0.016f);

        int sum = 0;
        if (ReferenceEquals(scene.Ui.CurrentMode, scene.Main))
        {
            sum++;
        }

        sum += scene.Top.Children.Count + scene.Bottom.Children.Count;

        scene.Context.Draw(scene.Renderer);
        return sum;
    }

    /// <summary>Runs one body a number of times and adds the results.</summary>
    /// <param name="scene">The scene.</param>
    /// <param name="count">The count of runs.</param>
    /// <param name="toggle"><see langword="true"/> runs the toggle step; <see langword="false"/> runs an idle frame.</param>
    /// <returns>The sum of the counts.</returns>
    private static int RunSteps(Scene scene, int count, bool toggle)
    {
        int sum = 0;
        for (int i = 0; i < count; i++)
        {
            sum += toggle ? ToggleStep(scene) : IdleStep(scene);
        }

        return sum;
    }

    /// <summary>
    /// The 0-byte rule of the toggle cycle: the mode diff, the store binding at the next mount, the caret pass, the
    /// back step, the history ring, the same-map map step, and the draw allocate 0 bytes after warm-up.
    /// </summary>
    [Fact]
    public void TheToggleCycleAllocatesNothingAfterWarmUp()
    {
        Scene scene = BuildScene();

        int warm = RunSteps(scene, WarmUpRounds, toggle: true);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        int sum = RunSteps(scene, Iterations, toggle: true);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);

        // The body does real work: each step adds 4 mode hits and 8 slot children.
        Assert.Equal(WarmUpRounds * 12, warm);
        Assert.Equal(Iterations * 12, sum);
    }

    /// <summary>
    /// The 0-byte rule of an idle frame: <see cref="UiContext.Update(float)"/> and <see cref="UiContext.Draw"/> with
    /// a focused field and no input allocate nothing.
    /// </summary>
    [Fact]
    public void AnIdleFrameAllocatesNothing()
    {
        Scene scene = BuildScene();

        int warm = RunSteps(scene, WarmUpRounds, toggle: false);

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        int sum = RunSteps(scene, Iterations, toggle: false);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);

        // Each idle frame adds 1 mode hit and the 2 children of the two slots.
        Assert.Equal(WarmUpRounds * 3, warm);
        Assert.Equal(Iterations * 3, sum);
    }

    /// <summary>
    /// The real-work check: one toggle step ends on the main mode with the state root in the bottom slot, the info
    /// root detached, the score label showing the store value, and the history grown by 2. One idle frame keeps the
    /// history. An empty step body cannot satisfy this test.
    /// </summary>
    [Fact]
    public void TheMeasuredBodyDoesRealWork()
    {
        Scene scene = BuildScene();

        int historyBefore = scene.Ui.History.Count;
        int step = ToggleStep(scene);

        Assert.Equal(12, step);
        Assert.Same(scene.Main, scene.Ui.CurrentMode);
        Assert.Null(scene.Ui.TargetMode);
        Assert.Equal(1, scene.Bottom.Children.Count);
        Assert.Same(scene.StateRoot, scene.Bottom.Children[0]);
        Assert.Null(scene.InfoRoot.Parent);
        Assert.Same(scene.Top, scene.WidgetRoot.Parent);
        Assert.Equal(scene.Score.Value.ToString(), Encoding.UTF8.GetString(scene.ScoreLabel.Text));
        Assert.Equal(historyBefore + 2, scene.Ui.History.Count);
        Assert.Equal(1, scene.LayoutFactory.Calls);
        Assert.Equal(1, scene.WidgetFactory.Calls);
        Assert.Equal(1, scene.StateFactory.Calls);
        Assert.Equal(1, scene.InfoFactory.Calls);
        Assert.True(scene.Map.IsActive);

        int idle = IdleStep(scene);

        Assert.Equal(3, idle);
        Assert.Equal(historyBefore + 2, scene.Ui.History.Count);
    }
}
