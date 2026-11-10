using System;
using BrutalUi.Core;

namespace BrutalUi.Framework;

// The scheme part of the map: the name table, the intern, the two standard names, and the active scheme.
// A scheme is one int id that resolves to a UTF-8 name. Id 0 is the default scheme: it holds no name,
// and a binding with it belongs to each scheme. The constructor seeds the table with the two standard
// names of UiScheme, so AddScheme gives their id with no add. The build-time rule: AddScheme copies the
// name and can grow an array, so add the scheme names at build time, not in the frame loop. SchemeName
// reads the name of any id, and the read allocates nothing. ActiveScheme reports the device of the last
// binding that gave a value, and the report carries from frame to frame. OnSchemeChanged is one slot that the
// frame read calls when the reported scheme changes. SetScheme locks the map to one scheme, and the lock filters the
// frame read, the press path, and the gamepad invoke.
public sealed partial class UiActionMap
{
    /// <summary>
    /// The public parameter name of every scheme id: <c>scheme</c>. Each throw names it through this constant, so a
    /// rename of a parameter cannot leave a wrong name in a throw.
    /// </summary>
    private const string SchemeParameter = "scheme";

    /// <summary>The scheme names, indexed by id. Index 0 holds no name: id 0 is the default scheme.</summary>
    private byte[][] _schemes = [];

    /// <summary>The count of valid ids, from 0 to <c>_schemeCount - 1</c>. The new map holds the default id only.</summary>
    private int _schemeCount = 1;

    /// <summary>The id of the seeded <see cref="UiScheme.KeyboardMouse"/> name.</summary>
    private int _keyboardScheme;

    /// <summary>The id of the seeded <see cref="UiScheme.Gamepad"/> name.</summary>
    private int _gamepadScheme;

    /// <summary>The scheme id that the last frame read reported.</summary>
    private int _activeScheme;

    /// <summary>The scheme id of the lock, or 0 when the map holds no lock. <see cref="SetScheme(int)"/> writes it.</summary>
    private int _lockedScheme;

    /// <summary>
    /// The scheme id that the map last reported through <see cref="OnSchemeChanged"/>. The frame read compares it, and
    /// <see cref="ResetFrame"/> sets it with the active scheme.
    /// </summary>
    private int _reportedScheme;

    /// <summary>
    /// Appends the two standard names, keeps their ids, and seeds the active scheme with KeyboardMouse. The
    /// constructor calls it one time.
    /// </summary>
    private void SeedSchemes()
    {
        _keyboardScheme = AddScheme(UiScheme.KeyboardMouse);
        _gamepadScheme = AddScheme(UiScheme.Gamepad);
        _activeScheme = _keyboardScheme;
        _reportedScheme = _keyboardScheme;
    }

    /// <summary>
    /// Adds a scheme name and gives its id.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The build-time rule: the method copies the name and can grow an array, so add the scheme names at build time,
    /// not in the frame loop.
    /// </para>
    /// <para>
    /// The intern rule: the map compares the name by its bytes, so a call for a name the table holds gives the id of
    /// that name and adds nothing. Two names that differ in case are two names.
    /// </para>
    /// <para>
    /// The map holds <see cref="UiScheme.KeyboardMouse"/> and <see cref="UiScheme.Gamepad"/> in the table from
    /// construction, so a call for one of the two standard names gives its seeded id with no add.
    /// </para>
    /// </remarks>
    /// <param name="name">The UTF-8 name of the scheme, for example <see cref="UiScheme.Gamepad"/>.</param>
    /// <returns>The id of the name. Id 0 is the default scheme, and this method never gives it.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is empty. The parameter name is <c>name</c>.
    /// </exception>
    public int AddScheme(ReadOnlySpan<byte> name)
    {
        if (name.IsEmpty)
        {
            throw new ArgumentException("The name of a scheme must not be empty.", nameof(name));
        }

        int index = IndexOfScheme(name);
        if (index >= 0)
        {
            return index;
        }

        if (_schemeCount >= _schemes.Length)
        {
            Array.Resize(ref _schemes, _schemes.Length == 0 ? InitialCapacity : _schemes.Length * 2);
        }

        _schemes[_schemeCount] = name.ToArray();
        _schemeCount++;
        return _schemeCount - 1;
    }

    /// <summary>
    /// Reads the UTF-8 name of a scheme id.
    /// </summary>
    /// <remarks>
    /// Id 0 gives an empty span: the default scheme has no name, and it means each scheme. The read copies no array
    /// and allocates nothing, so a game can read the name of <see cref="ActiveScheme"/> in each frame.
    /// </remarks>
    /// <param name="scheme">The scheme id.</param>
    /// <returns>The name bytes that <see cref="AddScheme(ReadOnlySpan{byte})"/> stored for the id, or an empty span for id 0.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="scheme"/> is negative, or it is at or above the count of valid ids. The parameter name is
    /// <c>scheme</c>.
    /// </exception>
    public ReadOnlySpan<byte> SchemeName(int scheme)
    {
        if (scheme == 0)
        {
            return [];
        }

        if ((uint)scheme >= (uint)_schemeCount)
        {
            throw new ArgumentOutOfRangeException(
                SchemeParameter,
                "The scheme id must be 0 or the id of a name that AddScheme gave.");
        }

        return _schemes[scheme];
    }

    /// <summary>
    /// Gets the id of the scheme that the map reports.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The device rule: the frame read reports the device of the last binding that gave a value. A key, a mouse
    /// button, a key composite, the pointer delta, and a scroll give <see cref="UiScheme.KeyboardMouse"/>. A stick,
    /// an axis, and a gamepad button give <see cref="UiScheme.Gamepad"/>. A stored value of exactly 0 gives no value.
    /// The tag of a binding never sets the device, so a custom scheme name groups bindings only.
    /// </para>
    /// <para>
    /// The carry rule: the report keeps its value from frame to frame, so a frame with no input keeps the last
    /// report. The map seeds <see cref="UiScheme.KeyboardMouse"/> at construction and in <see cref="Activate"/>, so a
    /// fresh map and a fresh activation report KeyboardMouse.
    /// </para>
    /// <para>
    /// The locked rule: while a lock stands, the report is the locked id, whatever the frame read reads. A game sets
    /// the lock with <see cref="SetScheme(int)"/>, and reads it with <see cref="LockedScheme"/>.
    /// </para>
    /// <para>
    /// A game reads the name with <see cref="SchemeName(int)"/>. The read copies nothing and allocates nothing.
    /// </para>
    /// </remarks>
    public int ActiveScheme => _lockedScheme == 0 ? _activeScheme : _lockedScheme;

    /// <summary>
    /// Gets the id of the scheme that the map is locked to, or 0 when the map holds no lock.
    /// </summary>
    /// <remarks>
    /// <see cref="SetScheme(int)"/> writes the value. The property allocates nothing.
    /// </remarks>
    public int LockedScheme => _lockedScheme;

    /// <summary>
    /// Locks the map to one scheme, or clears the lock.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The filter rule: under a lock, a binding of another scheme gives no value on the frame read, on the press path
    /// of <see cref="PressKey"/> and <see cref="PressMouseButton"/>, and on the gamepad invoke of the frame read and of
    /// <see cref="PushGamepad"/>. No binding of another scheme runs a command.
    /// </para>
    /// <para>
    /// The held rule: a lock that stands over a held binding of another scheme ends that action, and the map reports
    /// one release for it in that frame. The player never released the input, so a game that reacts to a release must
    /// read this case. The latch clears, so the map reports the release one time and not in each frame under the lock.
    /// A press of another scheme under a lock invokes no command and is not kept: the map reads the edge and drops it,
    /// so the command does not run after a clear of the lock.
    /// </para>
    /// <para>
    /// The default rule: a binding of the default scheme reads under every lock, because it belongs to each scheme.
    /// </para>
    /// <para>
    /// The unlocked rule: the lock is the only filter. With no lock, the map reads every binding, whatever its tag, so a
    /// bind of one device can still report that device and move <see cref="ActiveScheme"/>.
    /// </para>
    /// <para>
    /// The report rule: <see cref="ActiveScheme"/> reads the locked id while the lock stands, and the map calls
    /// <see cref="OnSchemeChanged"/> at the next <see cref="UiContext.Update(float)"/> with it. A clear of the lock
    /// returns the report to the device of the frame read at the next update.
    /// </para>
    /// <para>
    /// The reset rule: <see cref="Activate"/> and <see cref="Deactivate"/> keep the lock, because the lock is a build
    /// setting of the map and not a frame value.
    /// </para>
    /// <para>
    /// The cost rule: the method allocates nothing. A change handler that calls it takes effect at the next frame,
    /// because the frame read copied its bindings to a local before it scanned.
    /// </para>
    /// </remarks>
    /// <param name="scheme">
    /// The id that <see cref="AddScheme(ReadOnlySpan{byte})"/> gave, or 0 to clear the lock.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="scheme"/> is negative, or it is at or above the count of valid ids. The method keeps the lock that
    /// it held. The parameter name is <c>scheme</c>.
    /// </exception>
    public void SetScheme(int scheme)
    {
        if ((uint)scheme >= (uint)_schemeCount)
        {
            throw new ArgumentOutOfRangeException(
                SchemeParameter,
                "The scheme id must be 0 or the id of a name that AddScheme gave.");
        }

        _lockedScheme = scheme;
    }

    /// <summary>
    /// Gets or sets the handler that the map calls when the scheme that it reports changes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The slot rule: write the property with <c>=</c>. The map holds one handler, not a list, so a write with <c>+=</c>
    /// is a mistake: it combines delegates, and no call then names one handler. A null slot calls nothing. See
    /// <see cref="UiSchemeChangedHandler"/> for the place rule, the change rule, the re-entry rule, the exception rule,
    /// and the build-time rule.
    /// </para>
    /// <para>
    /// The map calls the slot at the end of the frame read, inside <see cref="UiContext.Update(float)"/>, so a game
    /// needs no extra call in its own frame loop. The handler reads the finished values of the frame, the invoked
    /// commands, and the new <see cref="ActiveScheme"/>.
    /// </para>
    /// </remarks>
    public UiSchemeChangedHandler? OnSchemeChanged { get; set; }

    /// <summary>
    /// Reports the scheme that the map now stands on, through <see cref="OnSchemeChanged"/>, and calls the slot only
    /// when that scheme changed. The frame read calls the method as its last statement.
    /// </summary>
    /// <remarks>
    /// One compare and one call. The method allocates nothing: it reads two <see cref="int"/> fields and calls one
    /// delegate, and it holds no list. A locked map reports the locked id, whatever the frame read read.
    /// </remarks>
    private void ReportSchemeChange()
    {
        int reported = _lockedScheme == 0 ? _activeScheme : _lockedScheme;
        if (reported != _reportedScheme)
        {
            _reportedScheme = reported;
            OnSchemeChanged?.Invoke(this, reported);
        }
    }

    /// <summary>
    /// Reports whether two scheme ids share one scheme, so the duplicate check of a bind rejects the new bind.
    /// </summary>
    /// <remarks>
    /// The clash rule: the default scheme belongs to each scheme, so a default bind clashes with a tagged bind of the
    /// same input, in either order. Two tags that differ do not clash, so one key can bind two actions in two schemes.
    /// </remarks>
    /// <param name="existingScheme">The scheme id of the binding that the map holds.</param>
    /// <param name="newScheme">The scheme id of the new bind.</param>
    /// <returns><see langword="true"/> when the two ids share one scheme.</returns>
    private static bool SchemeClashes(int existingScheme, int newScheme)
    {
        return newScheme == 0 || existingScheme == 0 || newScheme == existingScheme;
    }

    /// <summary>
    /// Reports whether the lock of the map lets one binding read.
    /// </summary>
    /// <remarks>
    /// The allows rule: the lock is the only filter. An unlocked map reads every binding, whatever its tag, so a bind
    /// that reports another device can still report it. Under a lock, a binding of the default scheme reads, and a
    /// binding of the locked scheme reads. The frame read calls it in its binding loop and in its invoke pass,
    /// <see cref="PushGamepad"/> calls it in its loop, and <see cref="Find"/> calls it in lock mode for the press path.
    /// </remarks>
    /// <param name="binding">The binding to read.</param>
    /// <returns>
    /// <see langword="true"/> when the map holds no lock, or the binding holds the default scheme or the locked scheme.
    /// </returns>
    private bool SchemeAllows(in InputBinding binding)
    {
        return _lockedScheme == 0 || binding.Scheme == 0 || binding.Scheme == _lockedScheme;
    }

    /// <summary>Finds a scheme name in the table.</summary>
    /// <param name="name">The UTF-8 name.</param>
    /// <returns>The id of the name, or -1 when the table does not hold it.</returns>
    private int IndexOfScheme(ReadOnlySpan<byte> name)
    {
        byte[][] schemes = _schemes;
        int count = _schemeCount;
        for (int i = 1; i < count; i++)
        {
            if (schemes[i].AsSpan().SequenceEqual(name))
            {
                return i;
            }
        }

        return -1;
    }
}
