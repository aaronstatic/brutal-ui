using System;
using BrutalUi.Core;

namespace BrutalUi.Framework;

/// <summary>
/// The owner of the panels, the layouts, and the modes: it applies the mode that the game sets at the start of the
/// next update.
/// </summary>
/// <remarks>
/// <para>
/// The defer rule (Q1): <see cref="SetMode(ReadOnlySpan{byte})"/> records the target and changes no tree. The next
/// <see cref="UiContext.Update(float)"/> applies it before the input pass, so a <c>SetMode</c> from a command
/// applies at the start of the next update.
/// </para>
/// <para>
/// The register order: register the layouts and the panels before the modes. <see cref="Register(ModeDefinition)"/>
/// resolves each layout name, each slot name, and each panel name of the mode against the names registered so far.
/// A name that it cannot resolve throws <see cref="ArgumentException"/>. Each <c>Register</c> call checks its input
/// before a change, so a rejected call leaves the tables and the names as they were.
/// </para>
/// <para>
/// The slot rule: the constructor writes <see cref="UiContext.OnBeforeUpdate"/> with <c>=</c> and replaces a
/// delegate of the game. The write is one time, at construction, and the manager creates the delegate one time.
/// </para>
/// <para>
/// The root rule: the manager adds and removes its layout roots only. The game does not add a layout root itself.
/// A root that the game adds stays.
/// </para>
/// <para>
/// The layout-root rule of the action map: the layout factory sets <see cref="Element.IsPickable"/> to
/// <see langword="false"/> on a root that fills the window. Otherwise the root would take each press that hits no
/// element, and no press would reach the action map or the game.
/// </para>
/// <para>
/// The thread rule: the manager runs on the UI thread and has no thread guard, as <see cref="UiActionMap"/> has
/// none.
/// </para>
/// <para>
/// The diff rules (A4, A5, Q3): a mode change runs the mark, the leave, the layout, the enter, and the map step, in
/// that order. The leave pass reads the real slot of each panel, not the placements of the old mode. A panel that
/// only the old mode holds unmounts and keeps its root, its state, and its bindings. A panel that only the new mode
/// holds mounts, and its controller builds the root at the first mount. A panel in the same slot element stays with
/// no join: it keeps its style, its tweens, and its bindings. A panel that moves to another slot leaves, enters, and
/// joins the new slot. Each slot takes the order of the new mode. The stale rules of Core clear the hover and the
/// focus of a leaving panel in the input pass of the same <see cref="UiContext.Update(float)"/>. A target that
/// equals the current mode changes nothing.
/// </para>
/// <para>
/// The layout rule (A4): the layout root is the one root of the manager in the context. When the new mode names
/// another layout, the diff removes the root of the old layout and adds the root of the new layout. The swap
/// unmounts each panel from the slots of the old layout, because the new slots are other elements. A layout root
/// lives for the life of the manager, and a swap back adds the same root again.
/// <see cref="UiContext.AddRoot(Element)"/> marks the full layout subtree for the style pass (the join rule of
/// Core), so a swap restyles the layout.
/// </para>
/// <para>
/// The map rule (A6): the diff calls <see cref="UiActionMap.Activate(UiContext)"/> on the map of the new mode, also
/// when the old mode holds the same map. It calls <see cref="UiActionMap.Deactivate"/> on the old map only when the
/// new mode has no map. Two modes with no map leave <see cref="UiContext.OnUnhandledInput"/> as the game set it. The
/// map switch comes before the input pass, so a key in the frame of the change reaches the map of the new mode.
/// </para>
/// <para>
/// The error rule: an exception of a factory or of <see cref="SlotContainer.Mount(Element, int)"/> leaves the handler
/// and <see cref="UiContext.Update(float)"/> before the input pass. The target stays, and the next update runs the
/// diff again. The leave pass reads the real slot of each panel, so the next diff, also to another target, leaves no
/// stale panel. The applying guard covers the whole diff: a <see cref="SetMode(ReadOnlySpan{byte})"/> call, a
/// <see cref="ModeHistory.Back"/> call, and a <c>Register</c> call from inside the diff throw
/// <see cref="InvalidOperationException"/>.
/// </para>
/// <para>
/// The history rule (A3): each apply that replaces a mode pushes the old mode into <see cref="History"/>. The
/// first apply adds nothing. A target that equals the current mode adds nothing.
/// <see cref="ModeHistory.Back"/> sets the newest entry as the target and drops it, and the apply of a back step
/// adds no entry, so there is no forward step. A <see cref="SetMode(ReadOnlySpan{byte})"/> call after a back step
/// in one frame wins, and the dropped entry stays dropped. A full ring drops the oldest entry. The push,
/// <see cref="ModeHistory.Back"/>, and <see cref="ModeHistory.Clear"/> allocate nothing.
/// </para>
/// <para>
/// The allocation rule: a <c>Register</c> call allocates, so use it at build time.
/// <see cref="SetMode(ReadOnlySpan{byte})"/>, the mode diff, and a <see cref="UiContext.Update(float)"/> call with no
/// target allocate 0 bytes after warm-up. The two 0-byte tests sit in <c>ModeDiffTests</c>.
/// </para>
/// </remarks>
public sealed class UiManager
{
    /// <summary>The count of history entries when <see cref="CreateInfo.HistoryCapacity"/> is 0.</summary>
    public const int DefaultHistoryCapacity = 16;

    /// <summary>The message of a null <see cref="CreateInfo.Context"/>.</summary>
    private const string NullContextMessage = "The Context of the UiManager.CreateInfo must not be null.";

    /// <summary>The message of a null <see cref="CreateInfo.Panels"/>.</summary>
    private const string NullPanelsMessage = "The Panels of the UiManager.CreateInfo must not be null.";

    /// <summary>The message of a layout name of a mode that the manager cannot resolve.</summary>
    private const string UnknownLayoutMessage = "A mode names a layout that the manager has not registered.";

    /// <summary>The message of a slot name of a mode that the manager cannot resolve.</summary>
    private const string UnknownSlotMessage = "A mode names a slot that its layout holds not.";

    /// <summary>The message of a panel name of a mode that the manager cannot resolve.</summary>
    private const string UnknownPanelMessage = "A mode names a panel that the manager has not registered.";

    /// <summary>The message of a mode name that the manager cannot resolve.</summary>
    private const string UnknownModeMessage = "No registered mode holds the name.";

    /// <summary>The message of a negative <see cref="CreateInfo.HistoryCapacity"/>.</summary>
    private const string NegativeHistoryCapacityMessage =
        "The HistoryCapacity of the UiManager.CreateInfo must not be negative. Use 0 for the default of 16.";

    /// <summary>The message of a <see langword="null"/> panel definition.</summary>
    private const string NullPanelMessage = "The panel definition of Register must not be null.";

    /// <summary>The message of a <see langword="null"/> layout definition.</summary>
    private const string NullLayoutMessage = "The layout definition of Register must not be null.";

    /// <summary>The message of a <see langword="null"/> mode definition.</summary>
    private const string NullModeMessage = "The mode definition of Register must not be null.";

    /// <summary>The message of a panel name that the manager holds.</summary>
    private const string DuplicatePanelMessage = "The manager already holds a panel with this name.";

    /// <summary>The message of a layout name that the manager holds.</summary>
    private const string DuplicateLayoutMessage = "The manager already holds a layout with this name.";

    /// <summary>The message of a mode name that the manager holds.</summary>
    private const string DuplicateModeMessage = "The manager already holds a mode with this name.";

    /// <summary>The message of a layout factory that returns <see langword="null"/>.</summary>
    private const string NullLayoutRootMessage = "The factory of a layout must not return null.";

    /// <summary>The message of a layout root that already has a parent.</summary>
    private const string AttachedLayoutRootMessage = "The root that a layout factory returns must have no parent.";

    /// <summary>The message of a layout root with two slots of one name.</summary>
    private const string DuplicateSlotMessage = "A layout root holds two slots with one name.";

    /// <summary>The message of a register call or a <see cref="SetMode(ReadOnlySpan{byte})"/> call during the diff.</summary>
    private const string ApplyingMessage = "A register call and a SetMode call must not run during the mode diff.";

    /// <summary>One registered panel: its controller, the slot that holds its root now, and the slot of the next mode.</summary>
    private struct PanelEntry
    {
        /// <summary>The controller of the panel.</summary>
        public PanelController Controller;

        /// <summary>The slot that holds the root now, or <see langword="null"/> when no slot holds it.</summary>
        public SlotContainer? Slot;

        /// <summary>The slot of the next mode, or <see langword="null"/> when the next mode holds no panel.</summary>
        public SlotContainer? Next;
    }

    /// <summary>One registered layout: its definition and the root that its factory built at register time.</summary>
    private struct LayoutEntry
    {
        /// <summary>The definition of the layout.</summary>
        public LayoutDefinition Definition;

        /// <summary>The built root.</summary>
        public Element Root;
    }

    /// <summary>One placement of a mode: the panel entry, the cached slot, and the index in the slot.</summary>
    private struct Placement
    {
        /// <summary>The index of the panel entry.</summary>
        public int Panel;

        /// <summary>The cached slot container of the placement.</summary>
        public SlotContainer Slot;

        /// <summary>The index of the panel in the slot.</summary>
        public int Index;
    }

    /// <summary>One registered mode: its definition, its layout entry, and its placements.</summary>
    private struct ModeEntry
    {
        /// <summary>The definition of the mode.</summary>
        public ModeDefinition Definition;

        /// <summary>The index of the layout entry.</summary>
        public int Layout;

        /// <summary>The placements in the slot order of the mode, and in the panel order inside a slot.</summary>
        public Placement[] Placements;
    }

    /// <summary>The handler delegate that the constructor creates one time.</summary>
    private readonly UiBeforeUpdateHandler _onBeforeUpdate;

    /// <summary>The panels in the order of register.</summary>
    private PanelEntry[] _panels = [];

    /// <summary>The count of used panel entries.</summary>
    private int _panelCount;

    /// <summary>The layouts in the order of register.</summary>
    private LayoutEntry[] _layouts = [];

    /// <summary>The count of used layout entries.</summary>
    private int _layoutCount;

    /// <summary>The modes in the order of register.</summary>
    private ModeEntry[] _modes = [];

    /// <summary>The count of used mode entries.</summary>
    private int _modeCount;

    /// <summary>The index of the applied mode, or -1 before the first apply.</summary>
    private int _current = -1;

    /// <summary>The index of the target mode, or -1 when no change waits.</summary>
    private int _target = -1;

    /// <summary>The index of the layout entry that stands in the context now, or -1 when none.</summary>
    private int _shown = -1;

    /// <summary><see langword="true"/> when the last <see cref="SetMode(ReadOnlySpan{byte})"/> record came from <see cref="ModeHistory.Back"/>.</summary>
    private bool _targetFromBack;

    /// <summary><see langword="true"/> while <see cref="Apply"/> runs the diff. A register call, a <see cref="SetMode(ReadOnlySpan{byte})"/> call, and a back step then throw.</summary>
    private bool _applying;

    /// <summary>
    /// Creates a manager that writes the before-update slot of a context.
    /// </summary>
    /// <param name="info">The create-info with the context and the panel context.</param>
    /// <exception cref="ArgumentNullException">
    /// <see cref="CreateInfo.Context"/> or <see cref="CreateInfo.Panels"/> of <paramref name="info"/> is
    /// <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The <see cref="CreateInfo.HistoryCapacity"/> of <paramref name="info"/> is negative.
    /// </exception>
    public UiManager(in CreateInfo info)
    {
        if (info.Context is null)
        {
            throw new ArgumentNullException(nameof(info), NullContextMessage);
        }

        if (info.Panels is null)
        {
            throw new ArgumentNullException(nameof(info), NullPanelsMessage);
        }

        if (info.HistoryCapacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(info), info.HistoryCapacity, NegativeHistoryCapacityMessage);
        }

        Context = info.Context;
        Panels = info.Panels;

        // The ring rejects a capacity of 0, so map 0 to the default first, as UiContext maps it for its queue.
        int capacity = info.HistoryCapacity == 0 ? DefaultHistoryCapacity : info.HistoryCapacity;
        History = new ModeHistory(this, capacity);
        _onBeforeUpdate = OnBeforeUpdate;
        Context.OnBeforeUpdate = _onBeforeUpdate;
    }

    /// <summary>Gets the context of the tree.</summary>
    public UiContext Context { get; }

    /// <summary>Gets the context that the factories of the panels and the layouts read.</summary>
    public PanelContext Panels { get; }

    /// <summary>Gets the ring of the modes that each apply replaced, with the back step.</summary>
    public ModeHistory History { get; }

    /// <summary>Gets the applied mode, or <see langword="null"/> before the first apply.</summary>
    public ModeDefinition? CurrentMode => _current < 0 ? null : _modes[_current].Definition;

    /// <summary>Gets the mode that the next apply takes, or <see langword="null"/> when no change waits.</summary>
    public ModeDefinition? TargetMode => _target < 0 ? null : _modes[_target].Definition;

    /// <summary>
    /// Registers a panel. The manager creates the one controller of the panel.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The manager adds no root and builds no root. The first apply mounts the root of the controller at the first
    /// <see cref="PanelController.EnsureRoot"/> call.
    /// </para>
    /// <para>
    /// The check rule: each check comes before a change. A <see langword="null"/> definition and a name that the
    /// manager holds change nothing, so the game can fix the definition and call <c>Register</c> again. A call during
    /// the mode diff throws.
    /// </para>
    /// <para>The call allocates. Use it at build time.</para>
    /// </remarks>
    /// <param name="panel">The definition of the panel. It must not be <see langword="null"/>.</param>
    /// <returns>The controller of the panel.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="panel"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// The manager holds a panel with the name of <paramref name="panel"/>. The
    /// <see cref="ArgumentException.ParamName"/> is <c>panel</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">The manager runs the mode diff. See the error rule in the remarks.</exception>
    public PanelController Register(PanelDefinition panel)
    {
        if (panel is null)
        {
            throw new ArgumentNullException(nameof(panel), NullPanelMessage);
        }

        if (_applying)
        {
            throw new InvalidOperationException(ApplyingMessage);
        }

        if (FindPanel(panel.Name) >= 0)
        {
            throw new ArgumentException(DuplicatePanelMessage, nameof(panel));
        }

        if (_panelCount == _panels.Length)
        {
            _panels = Grow(_panels);
        }

        PanelController controller = new(panel, Panels);
        _panels[_panelCount] = new PanelEntry { Controller = controller };
        _panelCount++;
        return controller;
    }

    /// <summary>
    /// Registers a layout. The manager calls the factory of the layout one time and keeps the root.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The add does not put the root in the context. The apply adds the root when the mode layout is not the shown
    /// layout, and the layout rule swaps the shown root when the mode changes to another layout.
    /// </para>
    /// <para>
    /// The check rule: the manager checks the definition and the name before it calls the factory. It then checks the
    /// built root: the root must not be <see langword="null"/>, it must have no parent, and no two of its slots may
    /// hold one name. Each check comes before a change, so a rejected layout leaves the name free. The slot check
    /// walks the root in depth-first pre-order, does not enter a slot, and reads
    /// <see cref="SlotContainer.Find(Element, ReadOnlySpan{byte})"/> for each slot name. An exception of the factory
    /// leaves the call with no wrap. A call during the mode diff throws.
    /// </para>
    /// <para>The call allocates. Use it at build time.</para>
    /// </remarks>
    /// <param name="layout">The definition of the layout. It must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="layout"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// The manager holds a layout with the name of <paramref name="layout"/>, or the built root holds two slots of one
    /// name. The <see cref="ArgumentException.ParamName"/> is <c>layout</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The factory returned <see langword="null"/>, the factory returned an element with a parent, or the manager runs
    /// the mode diff. See the error rule in the remarks.
    /// </exception>
    public void Register(LayoutDefinition layout)
    {
        if (layout is null)
        {
            throw new ArgumentNullException(nameof(layout), NullLayoutMessage);
        }

        if (_applying)
        {
            throw new InvalidOperationException(ApplyingMessage);
        }

        if (FindLayout(layout.Name) >= 0)
        {
            throw new ArgumentException(DuplicateLayoutMessage, nameof(layout));
        }

        Element? root = layout.CreateRoot(Panels);
        if (root is null)
        {
            throw new InvalidOperationException(NullLayoutRootMessage);
        }

        if (root.Parent is not null)
        {
            throw new InvalidOperationException(AttachedLayoutRootMessage);
        }

        if (HoldsDuplicateSlot(root, root))
        {
            throw new ArgumentException(DuplicateSlotMessage, nameof(layout));
        }

        if (_layoutCount == _layouts.Length)
        {
            _layouts = Grow(_layouts);
        }

        _layouts[_layoutCount] = new LayoutEntry { Definition = layout, Root = root };
        _layoutCount++;
    }

    /// <summary>
    /// Registers a mode. The manager resolves the layout name, finds and caches each slot, and resolves the panel
    /// name of each placement.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Register the layout and the panels of the mode before the mode. The placements follow the slot order of the
    /// mode and the panel order inside a slot.
    /// </para>
    /// <para>
    /// The check rule: each check comes before a change. A <see langword="null"/> definition, a name that the manager
    /// holds, and a name of a layout, a slot, or a panel that the manager cannot resolve change nothing, so the game
    /// can fix the definition and call <c>Register</c> again. A call during the mode diff throws.
    /// </para>
    /// <para>The call allocates. Use it at build time.</para>
    /// </remarks>
    /// <param name="mode">The definition of the mode. It must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="mode"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// The manager holds a mode with the name of <paramref name="mode"/>, or it cannot resolve the layout name, a slot
    /// name, or a panel name of <paramref name="mode"/>. The <see cref="ArgumentException.ParamName"/> is <c>mode</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">The manager runs the mode diff. See the error rule in the remarks.</exception>
    public void Register(ModeDefinition mode)
    {
        if (mode is null)
        {
            throw new ArgumentNullException(nameof(mode), NullModeMessage);
        }

        if (_applying)
        {
            throw new InvalidOperationException(ApplyingMessage);
        }

        if (FindMode(mode.Name) >= 0)
        {
            throw new ArgumentException(DuplicateModeMessage, nameof(mode));
        }

        int layout = FindLayout(mode.Layout);
        if (layout < 0)
        {
            throw new ArgumentException(UnknownLayoutMessage, nameof(mode));
        }

        Element layoutRoot = _layouts[layout].Root;
        Placement[] placements = new Placement[CountModePanels(mode)];
        int at = 0;
        for (int s = 0; s < mode.SlotCount; s++)
        {
            SlotContainer? slot = SlotContainer.Find(layoutRoot, mode.GetSlotName(s));
            if (slot is null)
            {
                throw new ArgumentException(UnknownSlotMessage, nameof(mode));
            }

            for (int i = 0; i < mode.GetPanelCount(s); i++)
            {
                int panel = FindPanel(mode.GetPanelName(s, i));
                if (panel < 0)
                {
                    throw new ArgumentException(UnknownPanelMessage, nameof(mode));
                }

                placements[at] = new Placement { Panel = panel, Slot = slot, Index = i };
                at++;
            }
        }

        if (_modeCount == _modes.Length)
        {
            _modes = Grow(_modes);
        }

        _modes[_modeCount] = new ModeEntry { Definition = mode, Layout = layout, Placements = placements };
        _modeCount++;
    }

    /// <summary>
    /// Records the mode that the next apply takes. The call changes no tree (Q1).
    /// </summary>
    /// <remarks>
    /// <para>
    /// A second call in one frame replaces the target, so the last call wins. The next
    /// <see cref="UiContext.Update(float)"/> applies the target before the input pass and clears it.
    /// </para>
    /// <para>
    /// A call after a <see cref="ModeHistory.Back"/> step in one frame wins. It clears the back flag, so the apply
    /// of the new target pushes the old mode, and the entry that <see cref="ModeHistory.Back"/> dropped stays
    /// dropped.
    /// </para>
    /// <para>
    /// The check rule: an unknown name and a call during the mode diff change nothing. The throw of an unknown name
    /// comes before the record of the target.
    /// </para>
    /// <para>The call allocates nothing.</para>
    /// </remarks>
    /// <param name="name">The UTF-8 name of a registered mode.</param>
    /// <exception cref="ArgumentException">
    /// No registered mode holds <paramref name="name"/>. The <see cref="ArgumentException.ParamName"/> is
    /// <c>name</c>. The throw comes before a change.
    /// </exception>
    /// <exception cref="InvalidOperationException">The manager runs the mode diff. See the error rule in the remarks.</exception>
    public void SetMode(ReadOnlySpan<byte> name)
    {
        if (_applying)
        {
            throw new InvalidOperationException(ApplyingMessage);
        }

        int target = FindMode(name);
        if (target < 0)
        {
            throw new ArgumentException(UnknownModeMessage, nameof(name));
        }

        _target = target;
        _targetFromBack = false;
    }

    /// <summary>
    /// Sets the back-step target: the mode index that <see cref="ModeHistory.Back"/> popped. The history calls it.
    /// </summary>
    /// <param name="mode">The index of the mode entry to apply next.</param>
    internal void SetBackTarget(int mode)
    {
        _target = mode;
        _targetFromBack = true;
    }

    /// <summary>
    /// Gets a value that tells whether the manager runs the mode diff now. <see cref="ModeHistory"/> reads it for the
    /// applying guard.
    /// </summary>
    internal bool IsApplying => _applying;

    /// <summary>
    /// Counts the panels of a mode over all its slots.
    /// </summary>
    /// <param name="mode">The definition of the mode.</param>
    /// <returns>The count of placements.</returns>
    private static int CountModePanels(ModeDefinition mode)
    {
        int count = 0;
        for (int s = 0; s < mode.SlotCount; s++)
        {
            count += mode.GetPanelCount(s);
        }

        return count;
    }

    /// <summary>
    /// Tests whether a layout root holds two slots of one name. The walk is depth-first pre-order and does not enter
    /// a slot. A slot is a duplicate when <see cref="SlotContainer.Find(Element, ReadOnlySpan{byte})"/> gives another
    /// slot with the same name.
    /// </summary>
    /// <param name="root">The layout root for the name lookup.</param>
    /// <param name="element">The element to check.</param>
    /// <returns><see langword="true"/> when the tree holds two slots of one name.</returns>
    private static bool HoldsDuplicateSlot(Element root, Element element)
    {
        if (element is SlotContainer slot)
        {
            return !ReferenceEquals(SlotContainer.Find(root, slot.SlotName), slot);
        }

        foreach (Element child in element.Children)
        {
            if (HoldsDuplicateSlot(root, child))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Grows an entry array by doubling and keeps its entries.</summary>
    /// <param name="array">The old array.</param>
    /// <returns>The larger array with the entries of the old array.</returns>
    private static T[] Grow<T>(T[] array)
    {
        T[] bigger = new T[array.Length == 0 ? 4 : array.Length * 2];
        Array.Copy(array, bigger, array.Length);
        return bigger;
    }

    /// <summary>Finds the index of a layout by name.</summary>
    /// <param name="name">The UTF-8 name of the layout.</param>
    /// <returns>The index of the layout entry, or -1.</returns>
    private int FindLayout(ReadOnlySpan<byte> name)
    {
        for (int i = 0; i < _layoutCount; i++)
        {
            if (_layouts[i].Definition.Name.SequenceEqual(name))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Finds the index of a panel by name.</summary>
    /// <param name="name">The UTF-8 name of the panel.</param>
    /// <returns>The index of the panel entry, or -1.</returns>
    private int FindPanel(ReadOnlySpan<byte> name)
    {
        for (int i = 0; i < _panelCount; i++)
        {
            if (_panels[i].Controller.Definition.Name.SequenceEqual(name))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Finds the index of a mode by name.</summary>
    /// <param name="name">The UTF-8 name of the mode.</param>
    /// <returns>The index of the mode entry, or -1.</returns>
    private int FindMode(ReadOnlySpan<byte> name)
    {
        for (int i = 0; i < _modeCount; i++)
        {
            if (_modes[i].Definition.Name.SequenceEqual(name))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Runs at the start of <see cref="UiContext.Update(float)"/>. Returns at once when no target waits.</summary>
    /// <param name="deltaTime">The time since the last update, in seconds. The apply reads nothing from it.</param>
    private void OnBeforeUpdate(float deltaTime)
    {
        if (_target < 0)
        {
            return;
        }

        Apply();
    }

    /// <summary>
    /// Applies the target: the same-mode return, the mark, the leave, the layout swap, the enter, the map switch,
    /// and the history push.
    /// </summary>
    /// <remarks>
    /// The applying rule: the guard <see langword="true"/> covers the whole diff, so a
    /// <see cref="SetMode(ReadOnlySpan{byte})"/> call, a <see cref="ModeHistory.Back"/> call, and a register call from
    /// a factory throw. The <see langword="finally"/> block clears the guard, also when a factory or
    /// <see cref="SlotContainer.Mount(Element, int)"/> throws, so the next update can run the diff again.
    /// </remarks>
    private void Apply()
    {
        if (_target == _current)
        {
            _target = -1;
            _targetFromBack = false;
            return;
        }

        ref readonly ModeEntry target = ref _modes[_target];

        _applying = true;
        try
        {
            // 1. Mark. Clear Next, then set the slot of the new mode for each panel of that mode.
            for (int i = 0; i < _panelCount; i++)
            {
                _panels[i].Next = null;
            }

            foreach (Placement p in target.Placements)
            {
                _panels[p.Panel].Next = p.Slot;
            }

            // 2. Leave. Unmount each panel whose real slot is not the slot of the new mode. A panel that stays in its
            // slot takes no join. A root keeps its subtree, its dirty bits, and its bindings.
            for (int i = 0; i < _panelCount; i++)
            {
                ref PanelEntry e = ref _panels[i];
                if (e.Slot is { } slot && !ReferenceEquals(slot, e.Next))
                {
                    slot.Unmount(e.Controller.Instance.Root!);
                    e.Slot = null;
                }
            }

            // 3. Layout. Remove the shown root and add the root of the new mode layout when the layout changes. The
            // leave pass emptied the slots of the old layout, so the removed root holds no panel.
            if (_shown != target.Layout)
            {
                if (_shown >= 0)
                {
                    Context.RemoveRoot(_layouts[_shown].Root);
                }

                Context.AddRoot(_layouts[target.Layout].Root);
                _shown = target.Layout;
            }

            // 4. Enter. Each controller builds its root at the first mount only. The loop order gives the order of the
            // mode inside each slot. A held root moves with no join; a detached root joins.
            foreach (Placement p in target.Placements)
            {
                Element root = _panels[p.Panel].Controller.EnsureRoot();
                p.Slot.Mount(root, p.Index);
                _panels[p.Panel].Slot = p.Slot;
            }

            // 5. Map. Activate the map of the new mode, also when the old mode holds the same map. A mode with no map
            // deactivates the map of the old mode. Two modes with no map leave the slot of the game alone.
            if (target.Definition.ActionMap is { } map)
            {
                map.Activate(Context);
            }
            else if (_current >= 0)
            {
                _modes[_current].Definition.ActionMap?.Deactivate();
            }

            // 6. History. The apply records the mode that it replaces. The first apply adds nothing, and the apply of
            // a back step adds nothing, so there is no forward step (A3).
            if (_current >= 0 && !_targetFromBack)
            {
                History.Push(_current);
            }

            _current = _target;
            _target = -1;
            _targetFromBack = false;
        }
        finally
        {
            _applying = false;
        }
    }

    /// <summary>
    /// The create-info of a <see cref="UiManager"/>.
    /// </summary>
    public struct CreateInfo
    {
        /// <summary>
        /// The context of the tree. It must not be <see langword="null"/>. The constructor writes its
        /// <see cref="UiContext.OnBeforeUpdate"/> slot.
        /// </summary>
        public required UiContext Context;

        /// <summary>
        /// The context that the factories of the panels and the layouts read. It must not be
        /// <see langword="null"/>. The manager does not own it.
        /// </summary>
        public required PanelContext Panels;

        /// <summary>
        /// The count of entries of the <see cref="UiManager.History"/> ring. The default is 0, which gives
        /// <see cref="DefaultHistoryCapacity"/>.
        /// </summary>
        /// <remarks>
        /// The constructor creates the ring with this capacity, so the push and the back step never allocate. A
        /// full ring drops the oldest entry. A negative value throws <see cref="ArgumentOutOfRangeException"/>.
        /// </remarks>
        public int HistoryCapacity;
    }
}
