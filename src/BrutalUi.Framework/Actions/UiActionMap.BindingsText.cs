using System;
using System.Globalization;
using System.Numerics;
using System.Text;
using BrutalUi.Core;

namespace BrutalUi.Framework;

// The binding text part of the map: the writer, the reader, and the internal binding handle of review CR-5 of
// input-actions.
// One version line, then one line per binding, in the order of the binding array. A line holds its fields in this
// order: action, scheme, device, code, code1, code2, code3, mods, and scale. A semicolon separates two fields, and
// one name and one value make each field, with an equals sign between them. Each line ends with a line feed, and the
// text is UTF-8 with no space padding. A field appears only when its value is not its default: code, code1, code2,
// and code3 when they are not -1, mods when it is not 0, and scale when it is not 1. The scheme field holds the name
// bytes, or a dash for the default scheme id 0, because SchemeName(0) is empty. The device field is always present,
// and it holds the InputKind number: 0 for a key, 1 for a mouse button, 2 for a gamepad button, 3 for a key vector,
// 4 for a gamepad stick, 5 for a gamepad axis, 6 for a key axis, 7 for a pointer delta, and 8 for a scroll. The
// explicit values of InputKind pin those nine numbers, so they are a stored contract. WriteBindings returns the byte
// count, and a span that is too short returns the needed count with nothing written. Both counts come from one line
// writer, so they agree exactly. A game owns the file and the buffer: the map reads no file and allocates nothing in
// the write. ReadBindings reads the same grammar back: the first non-empty line must name version 1, and each later
// line is one binding, or one bad line that the read skips and counts. The read merges per action and scheme pair, so
// a pair that no line names keeps its bindings. The read grows two arrays and reads each scale through one string, so it
// runs at load time.
public sealed partial class UiActionMap
{
    /// <summary>
    /// The version that <see cref="WriteBindings"/> writes and that the reader accepts: <c>1</c>. The version line is
    /// the first non-empty line of a text.
    /// </summary>
    private const int BindingsVersion = 1;

    /// <summary>The version line of a text, with no line feed: <c>version=1</c>.</summary>
    private static ReadOnlySpan<byte> VersionLine => "version=1"u8;

    /// <summary>The name of the version field and its equals sign: <c>version=</c>.</summary>
    private static ReadOnlySpan<byte> VersionField => "version="u8;

    /// <summary>The scheme token of the default scheme: a dash, because id 0 holds no name.</summary>
    private static ReadOnlySpan<byte> DefaultSchemeToken => "-"u8;

    /// <summary>The field that separates two fields of one line: a semicolon.</summary>
    private static ReadOnlySpan<byte> FieldSeparator => ";"u8;

    /// <summary>The field that ends one line: a line feed.</summary>
    private static ReadOnlySpan<byte> LineFeed => "\n"u8;

    /// <summary>The name of the action field and its equals sign: <c>action=</c>.</summary>
    private static ReadOnlySpan<byte> ActionField => "action="u8;

    /// <summary>The name of the scheme field and its equals sign: <c>scheme=</c>.</summary>
    private static ReadOnlySpan<byte> SchemeField => "scheme="u8;

    /// <summary>The name of the device field and its equals sign: <c>device=</c>.</summary>
    private static ReadOnlySpan<byte> DeviceField => "device="u8;

    /// <summary>The name of the first code field and its equals sign: <c>code=</c>.</summary>
    private static ReadOnlySpan<byte> CodeField => "code="u8;

    /// <summary>The name of the second code field and its equals sign: <c>code1=</c>.</summary>
    private static ReadOnlySpan<byte> Code1Field => "code1="u8;

    /// <summary>The name of the third code field and its equals sign: <c>code2=</c>.</summary>
    private static ReadOnlySpan<byte> Code2Field => "code2="u8;

    /// <summary>The name of the fourth code field and its equals sign: <c>code3=</c>.</summary>
    private static ReadOnlySpan<byte> Code3Field => "code3="u8;

    /// <summary>The name of the modifier field and its equals sign: <c>mods=</c>.</summary>
    private static ReadOnlySpan<byte> ModsField => "mods="u8;

    /// <summary>The name of the scale field and its equals sign: <c>scale=</c>.</summary>
    private static ReadOnlySpan<byte> ScaleField => "scale="u8;

    /// <summary>
    /// The most bytes that one number writes. A signed <see cref="int"/> holds eleven, and an invariant
    /// <see cref="float"/> holds fewer than this.
    /// </summary>
    private const int MaxNumberBytes = 32;

    /// <summary>
    /// The 15-button cap that the text parser adds for <c>device=2</c>, on top of the checks that the matching bind
    /// runs, because the gamepad state holds buttons 0 to 14. A <see cref="InputKind.GamepadButton"/> code with its one
    /// bit at or above this limit is out of range: a file that holds a wider bound bit skips that line.
    /// </summary>
    private const int GamepadButtonBitLimit = 1 << 15;

    /// <summary>
    /// The handle that names one binding: the small struct and the index read that review CR-5 of input-actions asks
    /// for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The type is <see langword="internal"/>. No public per-binding handle ships, because
    /// <c>BrutalUi.Framework.Tests</c> holds no <c>InternalsVisibleTo</c> for <c>BrutalUi.Framework</c> (A10). Every
    /// test reaches this feature through <see cref="WriteBindings"/>.
    /// </para>
    /// <para>
    /// The nine fields copy the nine fields of one <see cref="InputBinding"/>, with two casts: <see cref="Device"/>
    /// holds the <see cref="InputKind"/> number, and <see cref="Modifiers"/> holds the <see cref="UiModifiers"/> mask.
    /// A copy of this struct copies no array.
    /// </para>
    /// </remarks>
    internal struct BindingText
    {
        /// <summary>The <see cref="InputKind"/> number of the binding, the <c>device</c> field of a line.</summary>
        public int Device;

        /// <summary>The first code, or -1 when the kind holds no code.</summary>
        public int Code;

        /// <summary>The second code of a composite, or -1.</summary>
        public int Code1;

        /// <summary>The third code of a composite, or -1.</summary>
        public int Code2;

        /// <summary>The fourth code of a composite, or -1.</summary>
        public int Code3;

        /// <summary>The <see cref="UiModifiers"/> mask, inside <see cref="MatchMask"/>.</summary>
        public int Modifiers;

        /// <summary>The scheme id. Id 0 is the default scheme, and it writes the dash token.</summary>
        public int Scheme;

        /// <summary>The scale that the frame read multiplies into the value of the binding.</summary>
        public float Scale;

        /// <summary>The index of the action in the action table.</summary>
        public int Action;
    }

    /// <summary>
    /// Reads the nine fields of one binding into a <see cref="BindingText"/>.
    /// </summary>
    /// <remarks>
    /// The method runs one bounds check and one small struct copy, and it allocates nothing. The index runs from 0 to
    /// <see cref="BindingCount"/> - 1, the order of the bind, so a caller that walks the array reads each binding once.
    /// </remarks>
    /// <param name="index">The index of the binding.</param>
    /// <param name="text">
    /// When this method returns <see langword="true"/>, the nine fields of the binding. Otherwise a zeroed struct.
    /// </param>
    /// <returns>
    /// <see langword="false"/> when <paramref name="index"/> is negative or at or above the binding count; else
    /// <see langword="true"/>.
    /// </returns>
    internal bool TryReadBinding(int index, out BindingText text)
    {
        if ((uint)index >= (uint)_bindingCount)
        {
            text = default;
            return false;
        }

        ref readonly InputBinding binding = ref _bindings[index];
        text = new BindingText
        {
            Device = (int)binding.Kind,
            Code = binding.Code,
            Code1 = binding.Code1,
            Code2 = binding.Code2,
            Code3 = binding.Code3,
            Modifiers = (int)binding.Modifiers,
            Scheme = binding.Scheme,
            Scale = binding.Scale,
            Action = binding.Action,
        };

        return true;
    }

    /// <summary>
    /// Writes the whole binding list as binding text, and gives the byte count.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The byte-count rule: the method writes the version line, then one line per binding, and it returns the count of
    /// bytes it wrote. An empty list gives the version line alone, and the count is 10.
    /// </para>
    /// <para>
    /// The needed-count rule: a <paramref name="destination"/> that is shorter than the text gives the needed count, and
    /// the method writes no byte of the text. A caller sizes its buffer with that return, then calls again with a span
    /// of that size. Both counts come from one line writer over the same bindings, so they agree exactly.
    /// </para>
    /// <para>
    /// The order rule: the lines follow the order of the binding array, and each line holds its fields in the order
    /// <c>action</c>, <c>scheme</c>, <c>device</c>, <c>code</c>, <c>code1</c>, <c>code2</c>, <c>code3</c>, <c>mods</c>,
    /// and <c>scale</c>.
    /// </para>
    /// <para>
    /// The omit-the-default rule: a field appears only when its value is not its default. <c>code</c> to <c>code3</c>
    /// appear when they are not -1, <c>mods</c> appears when it is not 0, and <c>scale</c> appears when it is not 1. The
    /// <c>action</c>, <c>scheme</c>, and <c>device</c> fields always appear.
    /// </para>
    /// <para>
    /// The name rule: an action name and a scheme name hold no semicolon and no line feed, because the writer emits the
    /// name bytes unchanged. A name that holds a separator gives a line that the reader counts in <c>Skipped</c>.
    /// </para>
    /// <para>
    /// The version rule: the first line holds <c>version=1</c>, the <see cref="BindingsVersion"/> of this writer. A
    /// reader that does not know that number applies nothing.
    /// </para>
    /// <para>
    /// The load-time rule: a game stores the text in its own file, and it owns the buffer. The method allocates nothing:
    /// it writes into <paramref name="destination"/> and formats each number into a stack buffer.
    /// </para>
    /// </remarks>
    /// <param name="destination">The buffer that takes the text. An empty span measures the text.</param>
    /// <returns>
    /// The count of bytes written, or the count of bytes that the text needs when <paramref name="destination"/> is too
    /// short.
    /// </returns>
    public int WriteBindings(Span<byte> destination)
    {
        int needed = 0;
        WriteText(destination, ref needed, measure: true);
        if (destination.Length < needed)
        {
            return needed;
        }

        int written = 0;
        WriteText(destination, ref written, measure: false);
        return written;
    }

    /// <summary>
    /// Reads one binding text, merges it into the map, and gives the counts and the version.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The version rule: the first non-empty line must be <c>version=1</c>. A later <c>version=</c> line, a missing
    /// version line, and an unreadable version each give <c>Version</c> 0, and an unknown number gives that number.
    /// Each of those reads applies no binding (A8).
    /// </para>
    /// <para>
    /// The merge rule: for each action and scheme pair that the lines name, the read replaces that pair. A pair that no
    /// line names keeps its bindings (Q6). Two lines of one pair give two bindings.
    /// </para>
    /// <para>
    /// The line rule: one line feed ends one line. A carriage return before it is not part of a value, and a line with
    /// no bytes gives no binding and no count. A game may keep the text in its own file and edit it by hand.
    /// </para>
    /// <para>
    /// The skip rule: one bad line is counted in the <c>Skipped</c> value and never thrown (A8). A bad line is an unknown
    /// action, an unknown field name, a repeated field, a missing <c>action</c>, <c>scheme</c>, or <c>device</c> field, a
    /// bad number, an out-of-range code, an unknown <c>device</c> number, a <c>mods</c> value outside
    /// <see cref="MatchMask"/>, a non-finite <c>scale</c>, a scheme name that the map does not hold, and a composite line
    /// that repeats one code. The three fields <c>action</c>, <c>scheme</c>, and <c>device</c> always appear in a
    /// written line, so a line that misses one of them is a bad line.
    /// </para>
    /// <para>
    /// The no-throw rule: a game hands this method a file that a person edited. The method throws nothing of its own:
    /// every problem lands in the skip count.
    /// </para>
    /// <para>
    /// The name rule: an action name and a scheme name hold no semicolon and no line feed, because the writer emits the
    /// name bytes unchanged. From the reader side, a line whose <c>action</c> or <c>scheme</c> value holds a separator
    /// is a bad line, and the read counts it in <c>Skipped</c>.
    /// </para>
    /// <para>
    /// The range rule: the parser runs at least the checks that the matching bind runs, so a bad number cannot land a
    /// dead binding (review CR-6 of input-actions), and it adds the 15-button cap for <c>device=2</c>, because the
    /// gamepad state holds buttons 0 to 14, so a file that holds a wider bound bit skips that line. A code field that
    /// the kind does not read lands as -1, and a scroll or a pointer delta keeps no code, so a re-write of the same
    /// list gives the same text.
    /// </para>
    /// <para>
    /// The distinct-key rule: a key vector line needs four different codes, and a key axis line needs two different
    /// codes. The two composite binds run the same rule, so a repeated code makes a bad line.
    /// </para>
    /// <para>
    /// The snapshot rule: the read does not move the defaults snapshot, so <see cref="ResetBindings"/> after a read still
    /// gives the bindings of the build (A6).
    /// </para>
    /// <para>
    /// The load-time rule: the method grows one local array, can grow the binding array through <see cref="Append"/>, and
    /// reads each <c>scale</c> field through one string, so call it at load, not in the frame loop. The frame path is not
    /// involved.
    /// </para>
    /// </remarks>
    /// <param name="text">The UTF-8 bytes of one binding text.</param>
    /// <returns>
    /// The count of bindings that the read applied, the count of lines that it skipped, and the version that the first
    /// line named.
    /// </returns>
    public (int Applied, int Skipped, int Version) ReadBindings(ReadOnlySpan<byte> text)
    {
        BindingText[] lines = [];
        int lineCount = 0;
        int skipped = 0;
        bool sawVersion = false;
        int at = 0;
        while (at < text.Length)
        {
            int feed = text[at..].IndexOf((byte)'\n');
            int cut = feed < 0 ? -1 : at + feed;
            int end = cut < 0 ? text.Length : cut;
            ReadOnlySpan<byte> line = text[at..end].TrimEnd((byte)'\r');
            at = cut < 0 ? text.Length : cut + 1;
            if (line.IsEmpty)
            {
                continue;
            }

            if (!sawVersion)
            {
                if (!TryReadVersion(line, out int version))
                {
                    return (0, 0, version);
                }

                sawVersion = true;
                continue;
            }

            if (!TryReadLine(line, out BindingText binding))
            {
                skipped++;
                continue;
            }

            if (lineCount == lines.Length)
            {
                Array.Resize(ref lines, lineCount == 0 ? InitialCapacity : lines.Length * 2);
            }

            lines[lineCount++] = binding;
        }

        if (!sawVersion)
        {
            return (0, 0, 0);
        }

        for (int i = 0; i < lineCount; i++)
        {
            if (FirstOfPair(lines, i))
            {
                _ = RemoveBindings(lines[i].Action, lines[i].Scheme);
            }
        }

        for (int i = 0; i < lineCount; i++)
        {
            Append(ToBinding(in lines[i]));
        }

        return (lineCount, skipped, BindingsVersion);
    }

    /// <summary>
    /// Writes the version line and each binding line, or counts their bytes. Both passes call it, so the two counts
    /// agree.
    /// </summary>
    /// <param name="destination">The buffer of the write pass.</param>
    /// <param name="at">The running byte count, or the write position.</param>
    /// <param name="measure"><see langword="true"/> to count only; <see langword="false"/> to write.</param>
    private void WriteText(Span<byte> destination, ref int at, bool measure)
    {
        Emit(VersionLine, destination, ref at, measure);
        Emit(LineFeed, destination, ref at, measure);

        for (int i = 0; i < _bindingCount; i++)
        {
            _ = TryReadBinding(i, out BindingText text);
            WriteLine(in text, destination, ref at, measure);
        }
    }

    /// <summary>
    /// Writes one binding line, or counts its bytes. The two passes share it (the needed-count rule).
    /// </summary>
    /// <param name="b">The binding to write.</param>
    /// <param name="destination">The buffer of the write pass.</param>
    /// <param name="at">The running byte count, or the write position.</param>
    /// <param name="measure"><see langword="true"/> to count only; <see langword="false"/> to write.</param>
    private void WriteLine(in BindingText b, Span<byte> destination, ref int at, bool measure)
    {
        Emit(ActionField, destination, ref at, measure);
        Emit(_actions[b.Action].Name, destination, ref at, measure);

        Emit(FieldSeparator, destination, ref at, measure);
        Emit(SchemeField, destination, ref at, measure);
        Emit(b.Scheme == 0 ? DefaultSchemeToken : SchemeName(b.Scheme), destination, ref at, measure);

        Emit(FieldSeparator, destination, ref at, measure);
        Emit(DeviceField, destination, ref at, measure);
        EmitNumber(b.Device, destination, ref at, measure);

        if (b.Code != -1)
        {
            Emit(FieldSeparator, destination, ref at, measure);
            Emit(CodeField, destination, ref at, measure);
            EmitNumber(b.Code, destination, ref at, measure);
        }

        if (b.Code1 != -1)
        {
            Emit(FieldSeparator, destination, ref at, measure);
            Emit(Code1Field, destination, ref at, measure);
            EmitNumber(b.Code1, destination, ref at, measure);
        }

        if (b.Code2 != -1)
        {
            Emit(FieldSeparator, destination, ref at, measure);
            Emit(Code2Field, destination, ref at, measure);
            EmitNumber(b.Code2, destination, ref at, measure);
        }

        if (b.Code3 != -1)
        {
            Emit(FieldSeparator, destination, ref at, measure);
            Emit(Code3Field, destination, ref at, measure);
            EmitNumber(b.Code3, destination, ref at, measure);
        }

        if (b.Modifiers != 0)
        {
            Emit(FieldSeparator, destination, ref at, measure);
            Emit(ModsField, destination, ref at, measure);
            EmitNumber(b.Modifiers, destination, ref at, measure);
        }

        if (b.Scale != 1f)
        {
            Emit(FieldSeparator, destination, ref at, measure);
            Emit(ScaleField, destination, ref at, measure);
            EmitScale(b.Scale, destination, ref at, measure);
        }

        Emit(LineFeed, destination, ref at, measure);
    }

    /// <summary>
    /// Adds the bytes of one piece to the running count, or copies them into the buffer.
    /// </summary>
    /// <param name="bytes">The bytes of the piece.</param>
    /// <param name="destination">The buffer of the write pass.</param>
    /// <param name="at">The running byte count, or the write position.</param>
    /// <param name="measure"><see langword="true"/> to count only; <see langword="false"/> to write.</param>
    private static void Emit(ReadOnlySpan<byte> bytes, Span<byte> destination, ref int at, bool measure)
    {
        if (measure)
        {
            at += bytes.Length;
            return;
        }

        bytes.CopyTo(destination[at..]);
        at += bytes.Length;
    }

    /// <summary>
    /// Adds the bytes of one number to the running count, or copies them into the buffer. The two passes format the
    /// number the same way, so the counts agree.
    /// </summary>
    /// <param name="value">The number to write.</param>
    /// <param name="destination">The buffer of the write pass.</param>
    /// <param name="at">The running byte count, or the write position.</param>
    /// <param name="measure"><see langword="true"/> to count only; <see langword="false"/> to write.</param>
    private static void EmitNumber(int value, Span<byte> destination, ref int at, bool measure)
    {
        Span<byte> scratch = stackalloc byte[MaxNumberBytes];
        _ = value.TryFormat(scratch, out int written, default, CultureInfo.InvariantCulture);
        Emit(scratch[..written], destination, ref at, measure);
    }

    /// <summary>
    /// Adds the bytes of the scale to the running count, or copies them into the buffer. The method writes no trailing
    /// zero rule of its own: the invariant format gives the shortest round-trip text.
    /// </summary>
    /// <param name="value">The scale to write.</param>
    /// <param name="destination">The buffer of the write pass.</param>
    /// <param name="at">The running byte count, or the write position.</param>
    /// <param name="measure"><see langword="true"/> to count only; <see langword="false"/> to write.</param>
    private static void EmitScale(float value, Span<byte> destination, ref int at, bool measure)
    {
        Span<byte> scratch = stackalloc byte[MaxNumberBytes];
        _ = value.TryFormat(scratch, out int written, default, CultureInfo.InvariantCulture);
        Emit(scratch[..written], destination, ref at, measure);
    }

    /// <summary>
    /// Reads one line of the text and gives the version number. The method is true only for the version that this
    /// reader accepts.
    /// </summary>
    /// <param name="line">The first non-empty line of a text.</param>
    /// <param name="version">The number after <c>version=</c>, or 0 when the line holds no readable number.</param>
    /// <returns>
    /// <see langword="true"/> when <paramref name="line"/> is <c>version=1</c>. Else <see langword="false"/>, and
    /// <paramref name="version"/> names the number that the read found, or 0.
    /// </returns>
    private static bool TryReadVersion(ReadOnlySpan<byte> line, out int version)
    {
        version = 0;
        if (!line.StartsWith(VersionField) || !TryParseSignedInt(line[VersionField.Length..], out int read))
        {
            return false;
        }

        version = read;
        return read == BindingsVersion;
    }

    /// <summary>
    /// Reads one binding line into a <see cref="BindingText"/>. The method walks the fields, resolves each name, and
    /// runs the range checks of the kind. The method throws nothing and allocates nothing for a good line.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One walk splits the line on each semicolon, and one split on the first equals sign makes each field. A field
    /// spans its name and its equals sign, so a prefix test names the field and the rest is its value. The read rejects
    /// a repeated field name, an unknown field name, and a line that misses one of the three always-present fields:
    /// <c>action</c>, <c>scheme</c>, and <c>device</c>.
    /// </para>
    /// <para>
    /// The absent default: a <c>code</c> field that the line omits reads as -1, <c>mods</c> reads as 0, and
    /// <c>scale</c> reads as 1f. These are the defaults that the writer omits.
    /// </para>
    /// <para>
    /// The kind shape: the read sets a code slot that the kind does not read to -1, so one line reads back the same nine
    /// fields that the matching bind writes.
    /// </para>
    /// </remarks>
    /// <param name="line">One binding line.</param>
    /// <param name="text">When this method returns <see langword="true"/>, the nine fields of the binding.</param>
    /// <returns><see langword="true"/> when the line is a good binding. Else <see langword="false"/>.</returns>
    private bool TryReadLine(ReadOnlySpan<byte> line, out BindingText text)
    {
        text = default;
        const int ActionBit = 1 << 0;
        const int SchemeBit = 1 << 1;
        const int DeviceBit = 1 << 2;
        const int CodeBit = 1 << 3;
        const int Code1Bit = 1 << 4;
        const int Code2Bit = 1 << 5;
        const int Code3Bit = 1 << 6;
        const int ModsBit = 1 << 7;
        const int ScaleBit = 1 << 8;
        const int RequiredBits = ActionBit | SchemeBit | DeviceBit;

        int seen = 0;
        int action = -1;
        int scheme = -1;
        int device = -1;
        int code = -1;
        int code1 = -1;
        int code2 = -1;
        int code3 = -1;
        int mods = 0;
        float scale = 1f;

        ReadOnlySpan<byte> rest = line;
        while (true)
        {
            int cut = rest.IndexOf((byte)';');
            ReadOnlySpan<byte> field = cut < 0 ? rest : rest[..cut];
            int bit;
            if (field.StartsWith(ActionField))
            {
                bit = ActionBit;
                action = IndexOfAction(field[ActionField.Length..]);
                if (action < 0)
                {
                    return false;
                }
            }
            else if (field.StartsWith(SchemeField))
            {
                bit = SchemeBit;
                ReadOnlySpan<byte> name = field[SchemeField.Length..];
                scheme = name.SequenceEqual(DefaultSchemeToken) ? 0 : IndexOfScheme(name);
                if (scheme < 0)
                {
                    return false;
                }
            }
            else if (field.StartsWith(DeviceField))
            {
                bit = DeviceBit;
                if (!TryParseSignedInt(field[DeviceField.Length..], out device)
                    || (uint)device > (uint)InputKind.Scroll)
                {
                    return false;
                }
            }
            else if (field.StartsWith(CodeField))
            {
                bit = CodeBit;
                if (!TryParseSignedInt(field[CodeField.Length..], out code))
                {
                    return false;
                }
            }
            else if (field.StartsWith(Code1Field))
            {
                bit = Code1Bit;
                if (!TryParseSignedInt(field[Code1Field.Length..], out code1))
                {
                    return false;
                }
            }
            else if (field.StartsWith(Code2Field))
            {
                bit = Code2Bit;
                if (!TryParseSignedInt(field[Code2Field.Length..], out code2))
                {
                    return false;
                }
            }
            else if (field.StartsWith(Code3Field))
            {
                bit = Code3Bit;
                if (!TryParseSignedInt(field[Code3Field.Length..], out code3))
                {
                    return false;
                }
            }
            else if (field.StartsWith(ModsField))
            {
                bit = ModsBit;
                if (!TryParseSignedInt(field[ModsField.Length..], out int value)
                    || (value & ~(int)MatchMask) != 0)
                {
                    return false;
                }

                mods = value;
            }
            else if (field.StartsWith(ScaleField))
            {
                bit = ScaleBit;
                if (!float.TryParse(
                        Encoding.UTF8.GetString(field[ScaleField.Length..]),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out scale)
                    || !float.IsFinite(scale))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            if ((seen & bit) != 0)
            {
                return false;
            }

            seen |= bit;
            if (cut < 0)
            {
                break;
            }

            rest = rest[(cut + 1)..];
        }

        if ((seen & RequiredBits) != RequiredBits || !FitsKind(device, ref code, ref code1, ref code2, ref code3))
        {
            return false;
        }

        text = new BindingText
        {
            Device = device,
            Code = code,
            Code1 = code1,
            Code2 = code2,
            Code3 = code3,
            Modifiers = mods,
            Scheme = scheme,
            Scale = scale,
            Action = action,
        };
        return true;
    }

    /// <summary>
    /// Runs the code checks of one kind and sets the code slots that the kind does not read to -1. The checks equal the
    /// checks of the matching bind (review CR-6 of input-actions), and they include the distinct-key rule.
    /// </summary>
    /// <param name="device">The <see cref="InputKind"/> number of the line.</param>
    /// <param name="code">The first code, or -1.</param>
    /// <param name="code1">The second code, or -1.</param>
    /// <param name="code2">The third code, or -1.</param>
    /// <param name="code3">The fourth code, or -1.</param>
    /// <returns><see langword="true"/> when every read code fits the kind.</returns>
    private static bool FitsKind(int device, ref int code, ref int code1, ref int code2, ref int code3)
    {
        switch ((InputKind)device)
        {
            case InputKind.Key:
            {
                if ((uint)code >= UiKeyState.KeyCount)
                {
                    return false;
                }

                code1 = code2 = code3 = -1;
                return true;
            }

            case InputKind.MouseButton:
            {
                if ((uint)code >= UiPointerButtonState.ButtonCount)
                {
                    return false;
                }

                code1 = code2 = code3 = -1;
                return true;
            }

            case InputKind.GamepadButton:
            {
                if ((uint)code >= GamepadButtonBitLimit || BitOperations.PopCount((uint)code) != 1)
                {
                    return false;
                }

                code1 = code2 = code3 = -1;
                return true;
            }

            case InputKind.KeyVector:
            {
                return (uint)code < UiKeyState.KeyCount
                    && (uint)code1 < UiKeyState.KeyCount
                    && (uint)code2 < UiKeyState.KeyCount
                    && (uint)code3 < UiKeyState.KeyCount
                    && code != code1
                    && code != code2
                    && code != code3
                    && code1 != code2
                    && code1 != code3
                    && code2 != code3;
            }

            case InputKind.GamepadStick:
            {
                if ((uint)code > (uint)UiGamepadStick.Right)
                {
                    return false;
                }

                code1 = code2 = code3 = -1;
                return true;
            }

            case InputKind.GamepadAxis:
            {
                if ((uint)code >= UiGamepadState.AxisCount)
                {
                    return false;
                }

                code1 = code2 = code3 = -1;
                return true;
            }

            case InputKind.KeyAxis:
            {
                if ((uint)code >= UiKeyState.KeyCount || (uint)code1 >= UiKeyState.KeyCount || code == code1)
                {
                    return false;
                }

                code2 = code3 = -1;
                return true;
            }

            default:
            {
                code = code1 = code2 = code3 = -1;
                return true;
            }
        }
    }

    /// <summary>
    /// Reads a signed integer from bytes. The method walks a sign and then digits, with no culture and no allocation.
    /// </summary>
    /// <param name="source">The digits of the number.</param>
    /// <param name="value">The value, or 0 when the read fails.</param>
    /// <returns>
    /// <see langword="true"/> when <paramref name="source"/> is a sign, which is optional, and one or more digits, and
    /// the value fits an <see cref="int"/>. Else <see langword="false"/>.
    /// </returns>
    private static bool TryParseSignedInt(ReadOnlySpan<byte> source, out int value)
    {
        value = 0;
        int at = 0;
        bool negative = false;
        if (source.Length > 0 && (source[0] == (byte)'-' || source[0] == (byte)'+'))
        {
            negative = source[0] == (byte)'-';
            at = 1;
        }

        if (at == source.Length)
        {
            return false;
        }

        long sum = 0;
        for (; at < source.Length; at++)
        {
            byte digit = source[at];
            if (digit < (byte)'0' || digit > (byte)'9')
            {
                return false;
            }

            sum = (sum * 10) + (digit - (byte)'0');
            if (sum > int.MaxValue)
            {
                return false;
            }
        }

        value = negative ? -(int)sum : (int)sum;
        return true;
    }

    /// <summary>
    /// Reports whether one good line is the first line of its action and scheme pair. The merge removes each named pair
    /// once, before it appends.
    /// </summary>
    /// <param name="lines">The good lines, in the order of the text.</param>
    /// <param name="at">The index of the line to check.</param>
    /// <returns><see langword="true"/> when no earlier line names the same pair.</returns>
    private static bool FirstOfPair(BindingText[] lines, int at)
    {
        for (int i = 0; i < at; i++)
        {
            if (lines[i].Action == lines[at].Action && lines[i].Scheme == lines[at].Scheme)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Turns one read line into one binding, with the same field copy that the binds run.</summary>
    /// <param name="text">The nine fields of one good line.</param>
    /// <returns>The binding to append.</returns>
    private static InputBinding ToBinding(in BindingText text)
    {
        return new InputBinding
        {
            Kind = (InputKind)text.Device,
            Code = text.Code,
            Code1 = text.Code1,
            Code2 = text.Code2,
            Code3 = text.Code3,
            Modifiers = (UiModifiers)text.Modifiers,
            Scheme = text.Scheme,
            Scale = text.Scale,
            Action = text.Action,
        };
    }
}
