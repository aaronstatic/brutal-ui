using System;
using System.Text;
using BrutalUi.Core;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of <see cref="UiActionMap.WriteBindings"/>: the version line, the one line per binding, the needed count
/// of a short span, the omit-the-default rule, the nine <c>device</c> numbers, and the reserved scheme token <c>-</c>.
/// </summary>
/// <remarks>
/// <para>
/// The grammar: one version line <c>version=1</c>, then one line per binding in the order of the binding array. A line
/// holds its fields in the order <c>action</c>, <c>scheme</c>, <c>device</c>, <c>code</c>, <c>code1</c>, <c>code2</c>,
/// <c>code3</c>, <c>mods</c>, and <c>scale</c>. A semicolon separates two fields, and one name and one value make each
/// field. A field appears only when its value is not its default.
/// </para>
/// <para>
/// The nine <c>device</c> numbers come from <c>InputKind</c>: 0 a key, 1 a mouse button, 2 a gamepad button, 3 a key
/// vector, 4 a gamepad stick, 5 a gamepad axis, 6 a key axis, 7 a pointer delta, and 8 a scroll. The pinned enum values
/// make those numbers a stored contract, so each one is an assert here.
/// </para>
/// <para>
/// Each test builds one map on a new <see cref="CommandRegistry"/> and calls no frame read: the writer is a load-time
/// member. The tests build strings and arrays, so none of them is a zero-byte gate. The binding handle and its index
/// read are internal (A10), so each test reads the text that the public writer gives. GLFW codes: Space 32, W 87,
/// A 65, S 83, D 68.
/// </para>
/// </remarks>
public sealed class BindingTextWriterTests
{
    /// <summary>The GLFW key code of the Space key.</summary>
    private const int SpaceKey = 32;

    /// <summary>The GLFW key code of the W key.</summary>
    private const int WKey = 87;

    /// <summary>The GLFW key code of the A key.</summary>
    private const int AKey = 65;

    /// <summary>The GLFW key code of the S key.</summary>
    private const int SKey = 83;

    /// <summary>The GLFW key code of the D key.</summary>
    private const int DKey = 68;

    /// <summary>The byte that fills an unwritten buffer, so a test can see a write.</summary>
    private const byte Sentinel = 0xAA;

    /// <summary>The version line and its line feed: ten bytes.</summary>
    private static ReadOnlySpan<byte> VersionText => "version=1\n"u8;

    /// <summary>The name of the Button action that the tests fire with.</summary>
    private static ReadOnlySpan<byte> FireAction => "Fire"u8;

    /// <summary>The name of the Vector2 action that the tests move with.</summary>
    private static ReadOnlySpan<byte> MoveAction => "Move"u8;

    /// <summary>The name of the Axis action that the tests zoom with.</summary>
    private static ReadOnlySpan<byte> ZoomAction => "Zoom"u8;

    /// <summary>The stored token of the default scheme: reserved, so no scheme may take it as a name.</summary>
    private static ReadOnlySpan<byte> DashSchemeName => "-"u8;

    /// <summary>A plain scheme name, so a test proves that the reserve hits only the token.</summary>
    private static ReadOnlySpan<byte> DefaultSchemeName => "Default"u8;

    /// <summary>The field order of one line, from the grammar of the format.</summary>
    private static readonly string[] s_fieldOrder =
        ["action", "scheme", "device", "code", "code1", "code2", "code3", "mods", "scale"];

    /// <summary>
    /// The text starts with the version line, holds one line per binding, starts each binding line with <c>action=</c>,
    /// and keeps the field order. An empty map gives the version line alone, and ten bytes. The dash token names scheme 0
    /// in a stored line, so <see cref="UiActionMap.AddScheme"/> rejects a scheme with that name, and a plain name still
    /// intern.
    /// </summary>
    [Fact]
    public void TheWriterStartsWithTheVersionLineAndWritesOneLinePerBinding()
    {
        UiActionMap map = CreateMap();
        map.AddAction(FireAction, UiActionType.Button);
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.AddAction(ZoomAction, UiActionType.Axis);
        int keyboard = map.AddScheme(UiScheme.KeyboardMouse);
        int gamepad = map.AddScheme(UiScheme.Gamepad);
        map.BindKey(FireAction, SpaceKey);
        map.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey, keyboard);
        map.BindScroll(ZoomAction, 2f, gamepad);

        byte[] text = WriteAll(map);
        Assert.True(text.AsSpan(0, VersionText.Length).SequenceEqual(VersionText));

        string[] lines = SplitLines(text);
        Assert.Equal(map.BindingCount, lines.Length - 1);
        Assert.Equal("version=1", lines[0]);
        foreach (string line in lines[1..])
        {
            Assert.StartsWith("action=", line);
            Assert.True(IsInFieldOrder(line));
        }

        Assert.Equal("action=Fire;scheme=-;device=0;code=32", lines[1]);
        Assert.Equal(
            "action=Move;scheme=KeyboardMouse;device=3;code=87;code1=65;code2=83;code3=68",
            lines[2]);
        Assert.Equal("action=Zoom;scheme=Gamepad;device=8;scale=2", lines[3]);

        UiActionMap empty = CreateMap();
        int needed = empty.WriteBindings([]);
        Assert.Equal(10, needed);
        byte[] buffer = new byte[needed];
        Assert.Equal(10, empty.WriteBindings(buffer));
        Assert.True(buffer.AsSpan().SequenceEqual(VersionText));

        // The reserved scheme name: "-" is the stored token for scheme 0, so AddScheme rejects it.
        ArgumentException dash = Assert.Throws<ArgumentException>(() => map.AddScheme(DashSchemeName));
        Assert.Equal("name", dash.ParamName);

        // A plain name is not the token, so it intern as before.
        int named = map.AddScheme(DefaultSchemeName);
        Assert.True(named > 0);
        Assert.Equal(named, map.AddScheme(DefaultSchemeName));
    }

    /// <summary>
    /// An empty span gives the needed count. A span of one byte less gives the same count and writes no byte. A span of
    /// exactly that count writes every byte, so the two counts agree.
    /// </summary>
    [Fact]
    public void AShortSpanGivesTheNeededCountAndWritesNothing()
    {
        UiActionMap map = CreateMap();
        map.AddAction(FireAction, UiActionType.Button);
        map.AddAction(ZoomAction, UiActionType.Axis);
        map.BindKey(FireAction, SpaceKey, UiModifiers.Shift | UiModifiers.Control);
        map.BindScroll(ZoomAction, -1f);

        int needed = map.WriteBindings([]);
        Assert.True(needed > 0);

        byte[] shortBuffer = new byte[needed - 1];
        Array.Fill(shortBuffer, Sentinel);
        Assert.Equal(needed, map.WriteBindings(shortBuffer));
        Assert.Equal(needed - 1, CountSentinels(shortBuffer));

        byte[] exactBuffer = new byte[needed];
        Array.Fill(exactBuffer, Sentinel);
        int written = map.WriteBindings(exactBuffer);
        Assert.Equal(needed, written);
        Assert.Equal(0, CountSentinels(exactBuffer));

        byte[] again = new byte[needed];
        Assert.Equal(needed, map.WriteBindings(again));
        Assert.True(exactBuffer.SequenceEqual(again));
    }

    /// <summary>
    /// A field appears only when its value is not its default, so a plain key line holds no <c>mods</c>, no
    /// <c>scale</c>, and no <c>code1</c>, and a delta or a scroll line holds no <c>code</c>.
    /// </summary>
    [Fact]
    public void TheWriterOmitsEachFieldAtItsDefault()
    {
        UiActionMap plain = CreateMap();
        plain.AddAction(FireAction, UiActionType.Button);
        plain.BindKey(FireAction, SpaceKey);
        string[] plainLines = SplitLines(WriteAll(plain));
        Assert.Equal("action=Fire;scheme=-;device=0;code=32", plainLines[1]);
        Assert.DoesNotContain("mods", Names(plainLines[1]));
        Assert.DoesNotContain("scale", Names(plainLines[1]));
        Assert.DoesNotContain("code1", Names(plainLines[1]));
        Assert.Equal("0", Value(plainLines[1], "device"));

        UiActionMap modified = CreateMap();
        modified.AddAction(FireAction, UiActionType.Button);
        modified.BindKey(FireAction, SpaceKey, UiModifiers.Control);
        string[] modifiedLines = SplitLines(WriteAll(modified));
        Assert.Equal("action=Fire;scheme=-;device=0;code=32;mods=2", modifiedLines[1]);
        Assert.Equal("2", Value(modifiedLines[1], "mods"));

        UiActionMap scrolled = CreateMap();
        scrolled.AddAction(ZoomAction, UiActionType.Axis);
        scrolled.BindScroll(ZoomAction, 2f);
        string[] scrollLines = SplitLines(WriteAll(scrolled));
        Assert.Equal("action=Zoom;scheme=-;device=8;scale=2", scrollLines[1]);
        Assert.DoesNotContain("code", Names(scrollLines[1]));

        UiActionMap stick = CreateMap();
        stick.AddAction(MoveAction, UiActionType.Vector2);
        int gamepad = stick.AddScheme(UiScheme.Gamepad);
        stick.BindGamepadStick(MoveAction, UiGamepadStick.Right, gamepad);
        string[] stickLines = SplitLines(WriteAll(stick));
        Assert.Equal("action=Move;scheme=Gamepad;device=4;code=1", stickLines[1]);
    }

    /// <summary>
    /// One map with all nine shapes writes the pinned <c>device</c> numbers 0 to 8, one per line, in the order of the
    /// bind. The key vector line holds four codes, and the key axis line holds two.
    /// </summary>
    [Fact]
    public void AllNineKindsWriteTheirDeviceNumbers()
    {
        UiActionMap map = CreateMap();
        map.AddAction(FireAction, UiActionType.Button);
        map.AddAction(MoveAction, UiActionType.Vector2);
        map.AddAction(ZoomAction, UiActionType.Axis);
        map.BindKey(FireAction, SpaceKey);
        map.BindMouseButton(FireAction, PointerButton.Right);
        map.BindGamepadButton(FireAction, UiGamepadButtons.A);
        map.BindKeyVector(MoveAction, WKey, AKey, SKey, DKey);
        map.BindGamepadStick(MoveAction, UiGamepadStick.Left);
        map.BindGamepadAxis(ZoomAction, UiGamepadAxis.LeftTrigger);
        map.BindKeyAxis(ZoomAction, AKey, DKey);
        map.BindMouseDelta(MoveAction);
        map.BindScroll(ZoomAction, 2f);

        Assert.Equal(9, map.BindingCount);
        string[] lines = SplitLines(WriteAll(map));
        Assert.Equal(map.BindingCount, lines.Length - 1);

        for (int i = 0; i < map.BindingCount; i++)
        {
            Assert.Equal(i.ToString(), Value(lines[i + 1], "device"));
        }

        string[] vectorNames = Names(lines[4]);
        Assert.Contains("code", vectorNames);
        Assert.Contains("code1", vectorNames);
        Assert.Contains("code2", vectorNames);
        Assert.Contains("code3", vectorNames);

        string[] axisNames = Names(lines[7]);
        Assert.Contains("code", axisNames);
        Assert.Contains("code1", axisNames);
        Assert.DoesNotContain("code2", axisNames);
        Assert.DoesNotContain("code3", axisNames);

        Assert.DoesNotContain("code", Names(lines[8]));
        Assert.DoesNotContain("code", Names(lines[9]));
    }

    /// <summary>
    /// Measures the text, writes it into a buffer of that size, and gives the bytes. The two counts must agree, so the
    /// helper asserts them.
    /// </summary>
    /// <param name="map">The map to write.</param>
    /// <returns>The buffer that holds the whole text.</returns>
    private static byte[] WriteAll(UiActionMap map)
    {
        int needed = map.WriteBindings([]);
        byte[] buffer = new byte[needed];
        int written = map.WriteBindings(buffer);
        Assert.Equal(needed, written);
        return buffer;
    }

    /// <summary>Splits the text on each line feed, and drops the empty tail.</summary>
    /// <param name="text">The bytes of one text.</param>
    /// <returns>The version line, then one entry per binding line.</returns>
    private static string[] SplitLines(ReadOnlySpan<byte> text)
    {
        return Encoding.UTF8.GetString(text).Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>Reads the field names of one line, in the order that they appear.</summary>
    /// <param name="line">One binding line.</param>
    /// <returns>The names, without their equals signs and values.</returns>
    private static string[] Names(string line)
    {
        string[] fields = line.Split(';');
        string[] names = new string[fields.Length];
        for (int i = 0; i < fields.Length; i++)
        {
            int equals = fields[i].IndexOf('=');
            names[i] = equals < 0 ? fields[i] : fields[i][..equals];
        }

        return names;
    }

    /// <summary>Reads the value of one field of one line, or an empty string when the line holds no such field.</summary>
    /// <param name="line">One binding line.</param>
    /// <param name="name">The field name.</param>
    /// <returns>The value bytes as a string.</returns>
    private static string Value(string line, string name)
    {
        foreach (string field in line.Split(';'))
        {
            int equals = field.IndexOf('=');
            if (equals >= 0 && field[..equals] == name)
            {
                return field[(equals + 1)..];
            }
        }

        return string.Empty;
    }

    /// <summary>Checks that each field name of one line sits later than the one before it in the grammar order.</summary>
    /// <param name="line">One binding line.</param>
    /// <returns><see langword="true"/> when the line keeps the field order.</returns>
    private static bool IsInFieldOrder(string line)
    {
        int last = -1;
        foreach (string name in Names(line))
        {
            int now = Array.IndexOf(s_fieldOrder, name);
            if (now <= last)
            {
                return false;
            }

            last = now;
        }

        return true;
    }

    /// <summary>Counts the bytes that still hold the sentinel, so a test can see that nothing was written.</summary>
    /// <param name="buffer">The buffer to read.</param>
    /// <returns>The count of sentinel bytes.</returns>
    private static int CountSentinels(byte[] buffer)
    {
        int count = 0;
        foreach (byte b in buffer)
        {
            if (b == Sentinel)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Creates a map on a new registry with no command.</summary>
    /// <returns>The map with no action.</returns>
    private static UiActionMap CreateMap()
    {
        return new UiActionMap(new UiActionMap.CreateInfo { Commands = new CommandRegistry() });
    }
}
