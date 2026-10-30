using System;
using System.Text;

namespace BrutalUi.Framework;

/// <summary>
/// One placement of a panel in a slot. It is an input of the <see cref="ModeDefinition"/> constructor.
/// </summary>
/// <remarks>
/// A UTF-8 literal is a span and cannot sit in an array, so the struct copies the two names. A
/// <see langword="default"/> value holds no names, and the <see cref="ModeDefinition"/> constructor rejects it.
/// </remarks>
public readonly struct ModePanel
{
    /// <summary>The copy of the slot name.</summary>
    private readonly byte[]? _slot;

    /// <summary>The copy of the panel name.</summary>
    private readonly byte[]? _panel;

    /// <summary>
    /// Creates a placement. The constructor copies both names.
    /// </summary>
    /// <param name="slot">The UTF-8 name of the slot.</param>
    /// <param name="panel">The UTF-8 name of the panel.</param>
    /// <exception cref="ArgumentException"><paramref name="slot"/> or <paramref name="panel"/> is empty.</exception>
    public ModePanel(ReadOnlySpan<byte> slot, ReadOnlySpan<byte> panel)
    {
        if (slot.IsEmpty)
        {
            throw new ArgumentException("The slot name of a mode panel must not be empty.", nameof(slot));
        }

        if (panel.IsEmpty)
        {
            throw new ArgumentException("The panel name of a mode panel must not be empty.", nameof(panel));
        }

        _slot = slot.ToArray();
        _panel = panel.ToArray();
    }

    /// <summary>Gets the UTF-8 name of the slot. The span is empty for a <see langword="default"/> value.</summary>
    public ReadOnlySpan<byte> Slot => _slot;

    /// <summary>Gets the UTF-8 name of the panel. The span is empty for a <see langword="default"/> value.</summary>
    public ReadOnlySpan<byte> Panel => _panel;

    /// <summary>Gets the copy of the slot name for the mode record. No code writes the array.</summary>
    internal byte[]? SlotBytes => _slot;

    /// <summary>Gets the copy of the panel name for the mode record. No code writes the array.</summary>
    internal byte[]? PanelBytes => _panel;
}

/// <summary>
/// The definition of a mode: a UTF-8 name, a layout name, the panels grouped by slot, and an optional action map.
/// </summary>
/// <remarks>
/// <para>
/// The slot order is the order of the first entry of each slot. The panel order of a slot is the order of the list
/// (A4). A slot name in two entries is valid, and the panels of both entries join the one slot. A panel is in one
/// slot of a mode at most.
/// </para>
/// <para>
/// <see cref="ActionMap"/> is optional, and the mode-set feature activates it (A6 of the epic). The record checks no
/// name against a registered panel or layout. The mode-set feature checks the names at register time.
/// </para>
/// <para>
/// The record copies each name at build time. Each getter allocates nothing. The record keeps the equality that the
/// compiler generates. It compares the name copies by reference, so two records from two constructor calls are not
/// equal. Compare the names with <c>Name.SequenceEqual(other)</c>. The record has no <see langword="init"/> member,
/// so a <see langword="with"/> expression cannot skip the validation.
/// </para>
/// <para>
/// <see cref="object.ToString"/> shows the names and the slot count as text. It allocates, so use it in a debugger
/// and a log, not in a frame. The record runs on the UI thread.
/// </para>
/// </remarks>
public sealed record ModeDefinition
{
    /// <summary>One slot: its name and the names of its panels in the order of the list.</summary>
    private readonly struct SlotEntry
    {
        /// <summary>The name of the slot.</summary>
        public readonly byte[] Name;

        /// <summary>The names of the panels.</summary>
        public readonly byte[][] Panels;

        /// <summary>Creates an entry.</summary>
        /// <param name="name">The name of the slot.</param>
        /// <param name="panels">The names of the panels.</param>
        public SlotEntry(byte[] name, byte[][] panels)
        {
            Name = name;
            Panels = panels;
        }
    }

    /// <summary>The copy of the name.</summary>
    private readonly byte[] _name;

    /// <summary>The copy of the layout name.</summary>
    private readonly byte[] _layout;

    /// <summary>The optional action map.</summary>
    private readonly UiActionMap? _actionMap;

    /// <summary>The slots in the order of their first entry.</summary>
    private readonly SlotEntry[] _slots;

    /// <summary>
    /// Creates a definition. The constructor copies the names and groups the panels by slot.
    /// </summary>
    /// <param name="name">The UTF-8 name of the mode.</param>
    /// <param name="layout">The UTF-8 name of the layout of the mode.</param>
    /// <param name="panels">The placements of the panels, in the order of the list.</param>
    /// <param name="actionMap">The optional action map. The mode-set feature activates it.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> or <paramref name="layout"/> is empty, an entry of <paramref name="panels"/> is empty,
    /// or the list names a panel two times.
    /// </exception>
    public ModeDefinition(
        ReadOnlySpan<byte> name,
        ReadOnlySpan<byte> layout,
        ReadOnlySpan<ModePanel> panels,
        UiActionMap? actionMap = null)
    {
        if (name.IsEmpty)
        {
            throw new ArgumentException("The name of a mode must not be empty.", nameof(name));
        }

        if (layout.IsEmpty)
        {
            throw new ArgumentException("The layout of a mode must not be empty.", nameof(layout));
        }

        // First pass: check each entry and count the slots and the panels of each slot.
        int slotCount = 0;
        int[] counts = new int[panels.Length];
        int[] slotOfEntry = new int[panels.Length];
        for (int i = 0; i < panels.Length; i++)
        {
            byte[]? slotBytes = panels[i].SlotBytes;
            byte[]? panelBytes = panels[i].PanelBytes;
            if (slotBytes is null || panelBytes is null)
            {
                throw new ArgumentException("An entry of the panel list must not be empty.", nameof(panels));
            }

            for (int j = 0; j < i; j++)
            {
                if (panelBytes.AsSpan().SequenceEqual(panels[j].PanelBytes))
                {
                    throw new ArgumentException("A mode must not list a panel two times.", nameof(panels));
                }
            }

            int slot = -1;
            for (int j = 0; j < i; j++)
            {
                if (slotBytes.AsSpan().SequenceEqual(panels[j].SlotBytes))
                {
                    slot = slotOfEntry[j];
                    break;
                }
            }

            if (slot < 0)
            {
                slot = slotCount;
                slotCount++;
            }

            slotOfEntry[i] = slot;
            counts[slot]++;
        }

        // Second pass: build the arrays with exact sizes.
        SlotEntry[] slots = new SlotEntry[slotCount];
        int[] filled = new int[slotCount];
        for (int i = 0; i < panels.Length; i++)
        {
            int slot = slotOfEntry[i];
            if (filled[slot] == 0)
            {
                slots[slot] = new SlotEntry(panels[i].SlotBytes!, new byte[counts[slot]][]);
            }

            slots[slot].Panels[filled[slot]] = panels[i].PanelBytes!;
            filled[slot]++;
        }

        _name = name.ToArray();
        _layout = layout.ToArray();
        _actionMap = actionMap;
        _slots = slots;
    }

    /// <summary>Gets the UTF-8 name of the mode.</summary>
    public ReadOnlySpan<byte> Name => _name;

    /// <summary>Gets the UTF-8 name of the layout of the mode.</summary>
    public ReadOnlySpan<byte> Layout => _layout;

    /// <summary>Gets the action map of the mode, or <see langword="null"/> when the mode has none.</summary>
    public UiActionMap? ActionMap => _actionMap;

    /// <summary>Gets the count of slots. The slots keep the order of their first entry.</summary>
    public int SlotCount => _slots.Length;

    /// <summary>Gets the UTF-8 name of a slot.</summary>
    /// <param name="slot">The index of the slot, from 0 to <see cref="SlotCount"/> minus 1.</param>
    /// <returns>The name of the slot.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="slot"/> is outside the range.</exception>
    public ReadOnlySpan<byte> GetSlotName(int slot)
    {
        CheckSlot(slot);
        return _slots[slot].Name;
    }

    /// <summary>Gets the count of panels in a slot.</summary>
    /// <param name="slot">The index of the slot, from 0 to <see cref="SlotCount"/> minus 1.</param>
    /// <returns>The count of panels.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="slot"/> is outside the range.</exception>
    public int GetPanelCount(int slot)
    {
        CheckSlot(slot);
        return _slots[slot].Panels.Length;
    }

    /// <summary>Gets the UTF-8 name of a panel in a slot.</summary>
    /// <param name="slot">The index of the slot, from 0 to <see cref="SlotCount"/> minus 1.</param>
    /// <param name="index">The index of the panel in the slot, in the order of the list.</param>
    /// <returns>The name of the panel.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="slot"/> or <paramref name="index"/> is outside the range.</exception>
    public ReadOnlySpan<byte> GetPanelName(int slot, int index)
    {
        CheckSlot(slot);
        byte[][] slotPanels = _slots[slot].Panels;
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, slotPanels.Length);
        return slotPanels[index];
    }

    /// <summary>Checks a slot index.</summary>
    /// <param name="slot">The index of the slot.</param>
    private void CheckSlot(int slot)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slot, _slots.Length);
    }

    /// <summary>Writes the names and the slot count as text for <see cref="object.ToString"/>.</summary>
    /// <param name="builder">The builder of the text.</param>
    /// <returns><see langword="true"/> always, because the method writes members.</returns>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Name = ");
        builder.Append(Encoding.UTF8.GetString(_name));
        builder.Append(", Layout = ");
        builder.Append(Encoding.UTF8.GetString(_layout));
        builder.Append(", SlotCount = ");
        builder.Append(_slots.Length);
        return true;
    }
}
