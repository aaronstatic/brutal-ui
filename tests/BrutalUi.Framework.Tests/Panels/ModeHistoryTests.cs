using System;
using System.Text;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of <see cref="ModeHistory"/> on four modes A, B, C, and D of one layout: the push rule, the back step,
/// the empty ring, the no-forward-step rule, the same-mode rule, the full ring, the back-then-set rule, and the
/// capacity of the create-info with <see cref="ModeHistory.Clear"/>.
/// </summary>
/// <remarks>
/// Each test builds its own context with a <see cref="RecordingRenderer"/> and a viewport of 400 by 300, and its
/// own panel, layout, and mode names in unique UTF-8 literals. Each mode holds one panel in the slot <c>left</c>,
/// so a factory count shows that no diff ran.
/// </remarks>
public sealed class ModeHistoryTests
{
    /// <summary>The factory of a panel. Each call counts and records the root it creates.</summary>
    private sealed class PanelFactory
    {
        /// <summary>The count of calls.</summary>
        public int Calls;

        /// <summary>The root of the last call, or <see langword="null"/> before the first call.</summary>
        public Element? Last;

        /// <summary>Creates a root of 100 by 50.</summary>
        /// <param name="context">The context of the controller. The factory reads nothing from it.</param>
        /// <returns>A new element.</returns>
        public Element Create(PanelContext context)
        {
            Calls++;
            Element root = new();
            root.Inline.Width = 100;
            root.Inline.Height = 50;
            Last = root;
            return root;
        }
    }

    /// <summary>The factory of the layout. Each call counts and holds the slot it builds.</summary>
    private sealed class LayoutFactory
    {
        /// <summary>The count of calls.</summary>
        public int Calls;

        /// <summary>The slot <c>left</c> of the root of the last call.</summary>
        public SlotContainer? Left;

        /// <summary>Creates a root of 400 by 300 with the slots <c>left</c> and <c>right</c>.</summary>
        /// <param name="context">The context of the manager. The factory reads nothing from it.</param>
        /// <returns>A new element.</returns>
        public Element Create(PanelContext context)
        {
            Calls++;
            Left = new SlotContainer("left"u8);
            Element root = new();
            root.Inline.Width = 400;
            root.Inline.Height = 300;
            root.IsPickable = false;
            root.Children.Add(Left);
            return root;
        }
    }

    /// <summary>One scene: a manager, its context, four panel factories, and the four mode definitions.</summary>
    private sealed class Scene
    {
        /// <summary>The context of the tree.</summary>
        public UiContext Context = null!;

        /// <summary>The manager under test.</summary>
        public UiManager Ui = null!;

        /// <summary>The factory of the panel of mode A.</summary>
        public PanelFactory Pa = new();

        /// <summary>The factory of the panel of mode B.</summary>
        public PanelFactory Pb = new();

        /// <summary>The factory of the panel of mode C.</summary>
        public PanelFactory Pc = new();

        /// <summary>The factory of the panel of mode D.</summary>
        public PanelFactory Pd = new();

        /// <summary>The definition of mode A.</summary>
        public ModeDefinition A = null!;

        /// <summary>The definition of mode B.</summary>
        public ModeDefinition B = null!;

        /// <summary>The definition of mode C.</summary>
        public ModeDefinition C = null!;

        /// <summary>The definition of mode D.</summary>
        public ModeDefinition D = null!;

        /// <summary>The UTF-8 names of the modes, in the order A, B, C, D.</summary>
        public byte[][] ModeNames = null!;
    }

    /// <summary>Encodes a name from the test prefix and a suffix.</summary>
    /// <param name="prefix">The unique prefix of the test.</param>
    /// <param name="suffix">The suffix that tells the kind and the letter.</param>
    /// <returns>The UTF-8 bytes of the name.</returns>
    private static byte[] Name(string prefix, string suffix)
    {
        return Encoding.UTF8.GetBytes(prefix + suffix);
    }

    /// <summary>Creates a panel context with three new registries.</summary>
    /// <returns>The context.</returns>
    private static PanelContext NewPanels()
    {
        return new PanelContext
        {
            Commands = new CommandRegistry(),
            Events = new EventBus(),
            Services = new ServiceRegistry(),
        };
    }

    /// <summary>Builds a scene: four panels, one layout, and four modes of one panel each.</summary>
    /// <param name="prefix">The unique prefix of the test.</param>
    /// <param name="capacity">The value for <see cref="UiManager.CreateInfo.HistoryCapacity"/>.</param>
    /// <returns>The scene, with the modes registered but no target set.</returns>
    private static Scene NewScene(string prefix, int capacity = 0)
    {
        Scene s = new();
        s.Context = new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer() })
        {
            ViewportSize = new float2(400f, 300f),
        };
        s.Ui = new UiManager(new UiManager.CreateInfo
        {
            Context = s.Context,
            Panels = NewPanels(),
            HistoryCapacity = capacity,
        });
        s.Ui.Register(new PanelDefinition(Name(prefix, ".pa"), "left"u8, s.Pa.Create));
        s.Ui.Register(new PanelDefinition(Name(prefix, ".pb"), "left"u8, s.Pb.Create));
        s.Ui.Register(new PanelDefinition(Name(prefix, ".pc"), "left"u8, s.Pc.Create));
        s.Ui.Register(new PanelDefinition(Name(prefix, ".pd"), "left"u8, s.Pd.Create));
        s.Ui.Register(new LayoutDefinition(Name(prefix, ".main"), new LayoutFactory().Create));
        s.ModeNames = [Name(prefix, ".a"), Name(prefix, ".b"), Name(prefix, ".c"), Name(prefix, ".d")];
        byte[] layout = Name(prefix, ".main");
        s.A = new ModeDefinition(s.ModeNames[0], layout, [new ModePanel("left"u8, Name(prefix, ".pa"))]);
        s.B = new ModeDefinition(s.ModeNames[1], layout, [new ModePanel("left"u8, Name(prefix, ".pb"))]);
        s.C = new ModeDefinition(s.ModeNames[2], layout, [new ModePanel("left"u8, Name(prefix, ".pc"))]);
        s.D = new ModeDefinition(s.ModeNames[3], layout, [new ModePanel("left"u8, Name(prefix, ".pd"))]);
        s.Ui.Register(s.A);
        s.Ui.Register(s.B);
        s.Ui.Register(s.C);
        s.Ui.Register(s.D);
        return s;
    }

    /// <summary>Sets the mode at the index and runs one update, so the apply records the old mode.</summary>
    /// <param name="s">The scene.</param>
    /// <param name="mode">The index of the mode: 0 for A, 1 for B, 2 for C, 3 for D.</param>
    private static void ApplyMode(Scene s, int mode)
    {
        s.Ui.SetMode(s.ModeNames[mode]);
        s.Context.Update(0f);
    }

    /// <summary>
    /// The push rule: the first apply adds nothing, and each apply after the first adds the old mode. A, B, C give
    /// a count of 2.
    /// </summary>
    [Fact]
    public void EachApplyAfterTheFirstAddsTheOldMode()
    {
        Scene s = NewScene("mh1");

        ApplyMode(s, 0);
        Assert.Equal(0, s.Ui.History.Count);

        ApplyMode(s, 1);
        Assert.Equal(1, s.Ui.History.Count);

        ApplyMode(s, 2);
        Assert.Equal(2, s.Ui.History.Count);
        Assert.Same(s.C, s.Ui.CurrentMode);
    }

    /// <summary>
    /// The back rule: after A, B, C, <see cref="ModeHistory.Back"/> gives <see langword="true"/>, sets B as the
    /// target, and drops the entry. <see cref="UiManager.CurrentMode"/> stays C until the next update.
    /// </summary>
    [Fact]
    public void BackSetsThePreviousModeAsTheTargetAndDropsIt()
    {
        Scene s = NewScene("mh2");
        ApplyMode(s, 0);
        ApplyMode(s, 1);
        ApplyMode(s, 2);

        Assert.True(s.Ui.History.Back());

        Assert.Same(s.B, s.Ui.TargetMode);
        Assert.Equal(1, s.Ui.History.Count);
        Assert.Same(s.C, s.Ui.CurrentMode);
    }

    /// <summary>
    /// The walk rule: two back steps with an update after each give B, then A. A third
    /// <see cref="ModeHistory.Back"/> gives <see langword="false"/>.
    /// </summary>
    [Fact]
    public void BackWalksToTheFirstModeAndThenReturnsFalse()
    {
        Scene s = NewScene("mh3");
        ApplyMode(s, 0);
        ApplyMode(s, 1);
        ApplyMode(s, 2);

        Assert.True(s.Ui.History.Back());
        s.Context.Update(0f);
        Assert.Same(s.B, s.Ui.CurrentMode);
        Assert.Equal(1, s.Ui.History.Count);

        Assert.True(s.Ui.History.Back());
        s.Context.Update(0f);
        Assert.Same(s.A, s.Ui.CurrentMode);
        Assert.Equal(0, s.Ui.History.Count);

        Assert.False(s.Ui.History.Back());
    }

    /// <summary>
    /// The empty rule: <see cref="ModeHistory.Back"/> on an empty history gives
    /// <see langword="false"/>, and a target that <see cref="UiManager.SetMode"/> set stays.
    /// </summary>
    [Fact]
    public void BackOnAnEmptyHistoryReturnsFalseAndKeepsTheTarget()
    {
        Scene s = NewScene("mh4");
        ApplyMode(s, 0);
        s.Ui.SetMode(s.ModeNames[1]);

        Assert.False(s.Ui.History.Back());

        Assert.Same(s.B, s.Ui.TargetMode);
        Assert.Equal(0, s.Ui.History.Count);
    }

    /// <summary>
    /// The no-forward-step rule (A3): after a back step and its apply, the count is one less than before the back
    /// step, and it stays so.
    /// </summary>
    [Fact]
    public void TheApplyOfABackStepAddsNoEntry()
    {
        Scene s = NewScene("mh5");
        ApplyMode(s, 0);
        ApplyMode(s, 1);
        ApplyMode(s, 2);
        Assert.Equal(2, s.Ui.History.Count);

        Assert.True(s.Ui.History.Back());
        Assert.Equal(1, s.Ui.History.Count);

        s.Context.Update(0f);
        Assert.Same(s.B, s.Ui.CurrentMode);
        Assert.Equal(1, s.Ui.History.Count);

        s.Context.Update(0f);
        Assert.Equal(1, s.Ui.History.Count);
    }

    /// <summary>
    /// The same-mode rule: a <see cref="UiManager.SetMode"/> of the current mode changes nothing. The count stays,
    /// and no factory runs.
    /// </summary>
    [Fact]
    public void ASetModeOfTheCurrentModeChangesNothing()
    {
        Scene s = NewScene("mh6");
        ApplyMode(s, 0);
        ApplyMode(s, 1);

        s.Ui.SetMode(s.ModeNames[1]);
        s.Context.Update(0f);

        Assert.Equal(1, s.Ui.History.Count);
        Assert.Same(s.B, s.Ui.CurrentMode);
        Assert.Null(s.Ui.TargetMode);
        Assert.Equal(1, s.Pa.Calls);
        Assert.Equal(1, s.Pb.Calls);
        Assert.Equal(0, s.Pc.Calls);
    }

    /// <summary>
    /// The full-ring rule: with capacity 2, A, B, C, D give a count of 2. The back steps give C, then B, then
    /// <see langword="false"/>.
    /// </summary>
    [Fact]
    public void AFullHistoryDropsTheOldestMode()
    {
        Scene s = NewScene("mh7", 2);
        ApplyMode(s, 0);
        ApplyMode(s, 1);
        ApplyMode(s, 2);
        ApplyMode(s, 3);
        Assert.Equal(2, s.Ui.History.Count);

        Assert.True(s.Ui.History.Back());
        Assert.Same(s.C, s.Ui.TargetMode);
        s.Context.Update(0f);

        Assert.True(s.Ui.History.Back());
        Assert.Same(s.B, s.Ui.TargetMode);
        s.Context.Update(0f);

        Assert.False(s.Ui.History.Back());
    }

    /// <summary>
    /// The back-then-set rule: after C with the history A, B, a back step, then <see cref="UiManager.SetMode"/> of
    /// A, then the update give A. The history holds A and C: B stays dropped, and the apply pushes C, because it is
    /// not a back step. The set-then-back rule: from the same start, a <see cref="UiManager.SetMode"/> of D, then a
    /// back step, then the update give B. The last call wins, so the apply is a back step and adds nothing. The
    /// history holds A: B stays dropped, and C never enters.
    /// </summary>
    [Fact]
    public void ASetModeAfterBackInOneFrameWins()
    {
        Scene s = NewScene("mh8");
        ApplyMode(s, 0);
        ApplyMode(s, 1);
        ApplyMode(s, 2);
        Assert.Equal(2, s.Ui.History.Count);

        Assert.True(s.Ui.History.Back());
        s.Ui.SetMode(s.ModeNames[0]);
        s.Context.Update(0f);

        Assert.Same(s.A, s.Ui.CurrentMode);
        Assert.Null(s.Ui.TargetMode);
        Assert.Equal(2, s.Ui.History.Count);

        Assert.True(s.Ui.History.Back());
        Assert.Same(s.C, s.Ui.TargetMode);
        s.Context.Update(0f);

        Assert.True(s.Ui.History.Back());
        Assert.Same(s.A, s.Ui.TargetMode);
        Assert.Equal(0, s.Ui.History.Count);

        Scene later = NewScene("mh8");
        ApplyMode(later, 0);
        ApplyMode(later, 1);
        ApplyMode(later, 2);

        later.Ui.SetMode(later.ModeNames[3]);
        Assert.True(later.Ui.History.Back());
        later.Context.Update(0f);

        Assert.Same(later.B, later.Ui.CurrentMode);
        Assert.Null(later.Ui.TargetMode);
        Assert.Equal(1, later.Ui.History.Count);
    }

    /// <summary>
    /// The capacity rule: 0 gives 16, 3 gives 3, and -1 throws <see cref="ArgumentOutOfRangeException"/> with the
    /// parameter <c>info</c>. <see cref="ModeHistory.Clear"/> gives a count of 0, and a back step then gives
    /// <see langword="false"/>.
    /// </summary>
    [Fact]
    public void TheCapacityComesFromTheCreateInfoAndClearEmptiesTheHistory()
    {
        Scene zero = NewScene("mh9");
        Assert.Equal(16, zero.Ui.History.Capacity);
        Assert.Equal(UiManager.DefaultHistoryCapacity, zero.Ui.History.Capacity);
        Assert.Equal(0, zero.Ui.History.Count);

        Scene three = NewScene("mh9", 3);
        Assert.Equal(3, three.Ui.History.Capacity);

        UiContext context = NewScene("mh9").Context;
        Assert.Throws<ArgumentOutOfRangeException>(() => new UiManager(new UiManager.CreateInfo
        {
            Context = context,
            Panels = new PanelContext
            {
                Commands = new CommandRegistry(),
                Events = new EventBus(),
                Services = new ServiceRegistry(),
            },
            HistoryCapacity = -1,
        }));
        ArgumentOutOfRangeException e = Assert.Throws<ArgumentOutOfRangeException>(() => new UiManager(
            new UiManager.CreateInfo { Context = context, Panels = new PanelContext
            {
                Commands = new CommandRegistry(),
                Events = new EventBus(),
                Services = new ServiceRegistry(),
            },
            HistoryCapacity = -1,
        }));
        Assert.Equal("info", e.ParamName);

        ApplyMode(three, 0);
        ApplyMode(three, 1);
        Assert.Equal(1, three.Ui.History.Count);

        three.Ui.History.Clear();
        Assert.Equal(0, three.Ui.History.Count);
        Assert.False(three.Ui.History.Back());
    }
}
