using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xunit;

namespace BrutalUi.Markup.Tests.Style;

/// <summary>
/// The robustness tests of the parser: no input makes <see cref="StyleSheetParser.Parse(ReadOnlySpan{byte}, string)"/>
/// throw, and each result holds a sheet or one error or more.
/// </summary>
/// <remarks>
/// A failure prints the index of the text and the text with each byte outside printable ASCII as <c>\xHH</c>, so a
/// person can repeat it. The random texts use few distinct names, so the global class table grows only a little.
/// </remarks>
public sealed class ParserRobustnessTests
{
    private const int Seed = 1234;
    private const int TextCount = 2000;
    private const string SourceName = "robust.css";

    /// <summary>The pieces of the format that the token mixes join.</summary>
    private static readonly string[] Pieces =
    [
        ".a", "#b", "Label", ":hover", ":focus", ":checked", ">", " ", ",", "{", "}", ":", ";", "color", "width",
        "padding", "margin", "opacity", "font", "default", "border-radius", "flex-direction", "row", "#fff", "#1e222c80",
        "#ff", "12px", "-1px", "50%", "0", "1.5", "12em", "auto", "var(", "--x", "--", ")", "(", ":root", "/*", "*/",
        "@media", "@import", "@", "!important", "!", "calc(", "rgb(", "::", ".", "#", "*", "[", "]", "+", "~", "\"",
        "'", "\\", "/", "-", "1e3", "\n", "\r\n", "\r", "\t", "é", "\u0001", "\u007f", "﻿",
        "100000000000000000000000000000000000000000000000000px",
    ];

    [Fact]
    public void NoRandomTextMakesTheParserThrow()
    {
        Random random = new(Seed);
        StyleSheetParser parser = new();
        int sheets = 0;

        for (int i = 0; i < TextCount; i++)
        {
            byte[] text = (i % 3) switch
            {
                0 => RandomBytes(random),
                1 => Encoding.UTF8.GetBytes(RandomMix(random)),
                _ => SpliceByte(random, Encoding.UTF8.GetBytes(RandomMix(random))),
            };

            string? fault = Check(parser, text, out bool hasSheet);
            if (fault is not null)
            {
                string report = "seed " + Seed.ToString(CultureInfo.InvariantCulture) + ", text "
                    + i.ToString(CultureInfo.InvariantCulture) + ": " + fault + " Text: \"" + Escape(text) + "\"";
                TestContext.Current.TestOutputHelper?.WriteLine(report);
                Assert.Fail(report);
            }

            if (hasSheet)
            {
                sheets++;
            }
        }

        TestContext.Current.TestOutputHelper?.WriteLine(
            "texts " + TextCount.ToString(CultureInfo.InvariantCulture) + ", sheets " + sheets.ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void EachCutOfAValidSheetGivesAnErrorOrASheet()
    {
        const string P = nameof(EachCutOfAValidSheetGivesAnErrorOrASheet) + "-";

        // Each unit is a statement or a comment, with the number of rules that it adds. A cut strictly inside a unit
        // gives an error. A cut at the start of a unit, at its end, or in the whitespace between two units gives a sheet.
        (string Text, int Rules)[] units =
        [
            (":root { --ink: #3060c0; --gap: 4px 8px; }", 0),
            ("/* the panel: é */", 0),
            ("Label { color: var(--ink); font: default; font-size: 14px; }", 1),
            ("." + P + "panel { background-color: #1e222c; padding: var(--gap); margin: 1px 2px 3px 4px; border-width: 1px; border-radius: 2px 4px; }", 1),
            ("#title { width: 50%; height: auto; min-width: 0; opacity: 0.5; flex-grow: 1; flex-shrink: 0; }", 1),
            ("." + P + "row:hover { flex-direction: row; justify-content: space-between; align-items: center; /* c */ flex-wrap: wrap; }", 1),
            ("Label." + P + "cell#score:focus:checked { display: flex; position: absolute; overflow: hidden; left: 2px; top: -3px; }", 1),
            ("." + P + "panel ." + P + "cell { border-color: #ffcc00ff; }", 1),
            ("." + P + "panel > Label:disabled { color: #fff; }", 1),
            ("." + P + "a, ." + P + "b:active, #c { align-self: auto; align-content: stretch; flex-basis: 10px; max-width: 100%; max-height: 50px; min-height: 0; right: 0; bottom: 1px; }", 3),
        ];

        List<byte> bytes = [];
        int[] starts = new int[units.Length];
        int[] ends = new int[units.Length];
        for (int u = 0; u < units.Length; u++)
        {
            bytes.AddRange(Encoding.UTF8.GetBytes(u == 0 ? string.Empty : "\n"));
            starts[u] = bytes.Count;
            bytes.AddRange(Encoding.UTF8.GetBytes(units[u].Text));
            ends[u] = bytes.Count;
        }

        bytes.Add((byte)'\n');
        byte[] text = bytes.ToArray();

        StyleSheetParser parser = new();
        int errorCuts = 0;
        int sheetCuts = 0;
        for (int cut = 0; cut <= text.Length; cut++)
        {
            byte[] part = text.AsSpan(0, cut).ToArray();
            string? fault = Check(parser, part, out _);
            Assert.True(fault is null, "cut " + cut.ToString(CultureInfo.InvariantCulture) + ": " + fault + " Text: \"" + Escape(part) + "\"");

            StyleParseResult result = parser.Parse(part, SourceName);
            int inside = -1;
            int rules = 0;
            for (int u = 0; u < units.Length; u++)
            {
                if (starts[u] < cut && cut < ends[u])
                {
                    inside = u;
                }
                else if (cut >= ends[u])
                {
                    rules += units[u].Rules;
                }
            }

            string where = "cut " + cut.ToString(CultureInfo.InvariantCulture) + " Text: \"" + Escape(part) + "\"";
            if (inside >= 0)
            {
                Assert.True(result.Sheet is null, "A cut inside unit " + inside.ToString(CultureInfo.InvariantCulture) + " gives a sheet. " + where);
                Assert.True(result.Errors.Count > 0, where);
                errorCuts++;
            }
            else
            {
                Assert.True(result.Sheet is not null, "A cut between two units gives an error: " + FirstError(result) + " " + where);
                Assert.Equal(rules, result.Sheet.Rules.Length);
                sheetCuts++;
            }
        }

        TestContext.Current.TestOutputHelper?.WriteLine(
            "cuts " + (text.Length + 1).ToString(CultureInfo.InvariantCulture)
            + ", errors " + errorCuts.ToString(CultureInfo.InvariantCulture)
            + ", sheets " + sheetCuts.ToString(CultureInfo.InvariantCulture));
        Assert.Equal(units.Length * 2 + 1, sheetCuts);
    }

    /// <summary>
    /// A text with many token errors and many rule errors gives one error for each copy (CR-1). The drop check of
    /// DS5.5 finds the token error of each rule with a binary search, so the time grows with the text, not with the
    /// square of the text. The test asserts no time bound, because a time bound fails on a slow machine.
    /// <c>context.md</c> records the time of the copies before and after the fix (DR2.1).
    /// </summary>
    /// <param name="copy">The text of one copy: a rule with a token error inside a declaration or inside a selector.</param>
    [Theory]
    [InlineData(".a{cé:1px;}")]
    [InlineData(".a:hé{}")]
    public void ManyTokenErrorsParseInLinearTime(string copy)
    {
        const int Copies = 50000;
        byte[] one = Encoding.UTF8.GetBytes(copy);
        byte[] text = new byte[one.Length * Copies];
        for (int i = 0; i < Copies; i++)
        {
            one.CopyTo(text, i * one.Length);
        }

        StyleSheetParser parser = new();
        long start = System.Diagnostics.Stopwatch.GetTimestamp();
        StyleParseResult result = parser.Parse(text, SourceName);
        TimeSpan elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(start);
        TestContext.Current.TestOutputHelper?.WriteLine(
            "copies " + Copies.ToString(CultureInfo.InvariantCulture) + ", bytes " + text.Length.ToString(CultureInfo.InvariantCulture)
            + ", ms " + elapsed.TotalMilliseconds.ToString("F0", CultureInfo.InvariantCulture));

        Assert.Null(result.Sheet);
        Assert.Equal(Copies, result.Errors.Count);
        Assert.All(result.Errors, e => Assert.Equal(StyleErrorKind.UnexpectedCharacter, e.Kind));
    }

    /// <summary>Parses one text and checks the result shape.</summary>
    /// <param name="parser">The parser.</param>
    /// <param name="text">The UTF-8 text.</param>
    /// <param name="hasSheet">Whether the result holds a sheet.</param>
    /// <returns>The fault, or <see langword="null"/> when the result is correct.</returns>
    private static string? Check(StyleSheetParser parser, byte[] text, out bool hasSheet)
    {
        hasSheet = false;
        StyleParseResult result;
        try
        {
            result = parser.Parse(text, SourceName);
        }
        catch (Exception exception)
        {
            return "Parse threw: " + exception;
        }

        hasSheet = result.Sheet is not null;
        if (result.Sheet is null && result.Errors.Count == 0)
        {
            return "The result holds no sheet and no error.";
        }

        if (result.Sheet is not null && result.Errors.Count > 0)
        {
            return "The result holds a sheet and an error.";
        }

        for (int e = 0; e < result.Errors.Count; e++)
        {
            StyleParseError error = result.Errors[e];
            if (error.Line < 1 || error.Column < 1 || error.Message.Length == 0 || error.SourceName != SourceName
                || error.Kind > StyleErrorKind.UnsupportedSyntax)
            {
                return "Error " + e.ToString(CultureInfo.InvariantCulture) + " is not complete: " + error;
            }

            if (e > 0)
            {
                StyleParseError before = result.Errors[e - 1];
                bool ordered = before.Line < error.Line || (before.Line == error.Line && before.Column <= error.Column);
                if (!ordered)
                {
                    return "Error " + e.ToString(CultureInfo.InvariantCulture) + " comes before the error in front of it: " + before + " / " + error;
                }
            }
        }

        return null;
    }

    private static byte[] RandomBytes(Random random)
    {
        byte[] text = new byte[random.Next(0, 96)];
        random.NextBytes(text);
        return text;
    }

    private static string RandomMix(Random random)
    {
        int count = random.Next(0, 48);
        StringBuilder builder = new();
        for (int i = 0; i < count; i++)
        {
            builder.Append(Pieces[random.Next(Pieces.Length)]);
        }

        return builder.ToString();
    }

    /// <summary>Puts one random byte (0 to 255) at a random place of the text.</summary>
    private static byte[] SpliceByte(Random random, byte[] text)
    {
        int at = random.Next(0, text.Length + 1);
        byte[] result = new byte[text.Length + 1];
        text.AsSpan(0, at).CopyTo(result);
        result[at] = (byte)random.Next(0, 256);
        text.AsSpan(at).CopyTo(result.AsSpan(at + 1));
        return result;
    }

    /// <summary>Gives the text with each byte outside printable ASCII as <c>\xHH</c>.</summary>
    private static string Escape(byte[] text)
    {
        StringBuilder builder = new(text.Length);
        foreach (byte b in text)
        {
            if (b is >= 0x20 and < 0x7F && b != (byte)'\\')
            {
                builder.Append((char)b);
            }
            else
            {
                builder.Append("\\x").Append(b.ToString("X2", CultureInfo.InvariantCulture));
            }
        }

        return builder.ToString();
    }

    private static string FirstError(StyleParseResult result)
    {
        return result.Errors.Count > 0 ? result.Errors[0].ToString() : "(none)";
    }
}
