using System;
using System.Text;
using BrutalUi.Core;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Compares two element trees and gives the first difference.
/// </summary>
/// <remarks>
/// <para>
/// The helper uses public members of <c>BrutalUi.Core</c>, <c>System</c>, and <c>System.Text</c>. It uses no test
/// framework type, so each test project can link it as <c>AllocationGate.cs</c> is linked today. A test writes
/// <c>Assert.Null(TreeCompare.FirstDifference(expected, actual));</c>.
/// </para>
/// <para>
/// <c>BrutalUi.Framework.Tests</c> links this file. xml-element-loader links it into <c>BrutalUi.Markup.Tests</c>
/// with the line <c>&lt;Compile Include="../BrutalUi.Core.Tests/Shared/TreeCompare.cs" LinkBase="Shared" /&gt;</c>.
/// </para>
/// </remarks>
internal static class TreeCompare
{
    /// <summary>
    /// Compares two trees and returns a message for the first difference, or <see langword="null"/> when the trees
    /// are equal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The message names the path and the member, for example <c>root/1/0: Name differs (expected 'score', actual
    /// 'scor')</c>. The path starts at <c>root</c> and adds one child index for each step down.
    /// </para>
    /// <para>
    /// Per element, the helper compares the runtime type, <c>TypeId</c>, <c>Name</c> (ordinal), <c>IsPickable</c>,
    /// <c>IsFocusable</c>, <c>States</c>, the class count and each <c>ClassId</c> in order, the child count, and the
    /// invocation count of the 14 event slots (0 for a <see langword="null"/> slot). Per widget, it compares the
    /// members that the plan names. It compares delegate invocation counts, not delegate identity.
    /// </para>
    /// <para>
    /// The helper does not compare <c>Inline</c>, <c>Layout</c>, <c>Style</c>, <c>Dirty</c>, or <c>BindingCount</c>.
    /// The builder sets none of them.
    /// </para>
    /// </remarks>
    /// <param name="expected">The root of the expected tree.</param>
    /// <param name="actual">The root of the actual tree.</param>
    /// <returns>A message for the first difference, or <see langword="null"/> when the trees are equal.</returns>
    internal static string? FirstDifference(Element expected, Element actual)
    {
        return CompareElement(expected, actual, "root");
    }

    private static string? CompareElement(Element expected, Element actual, string path)
    {
        Type expectedType = expected.GetType();
        Type actualType = actual.GetType();
        if (expectedType != actualType)
        {
            return Diff(path, "Type", Show(expectedType.Name), Show(actualType.Name));
        }

        if (expected.TypeId != actual.TypeId)
        {
            return Diff(path, "TypeId", Show(expected.TypeId), Show(actual.TypeId));
        }

        if (!string.Equals(expected.Name, actual.Name, StringComparison.Ordinal))
        {
            return Diff(path, "Name", Show(expected.Name), Show(actual.Name));
        }

        if (expected.IsPickable != actual.IsPickable)
        {
            return Diff(path, "IsPickable", Show(expected.IsPickable), Show(actual.IsPickable));
        }

        if (expected.IsFocusable != actual.IsFocusable)
        {
            return Diff(path, "IsFocusable", Show(expected.IsFocusable), Show(actual.IsFocusable));
        }

        if (expected.States != actual.States)
        {
            return Diff(path, "States", Show(expected.States), Show(actual.States));
        }

        string? difference = CompareClasses(expected, actual, path);
        if (difference is not null)
        {
            return difference;
        }

        ElementList expectedChildren = expected.Children;
        ElementList actualChildren = actual.Children;
        if (expectedChildren.Count != actualChildren.Count)
        {
            return Diff(path, "Child count", Show(expectedChildren.Count), Show(actualChildren.Count));
        }

        difference = CompareSlots(expected, actual, path);
        if (difference is not null)
        {
            return difference;
        }

        difference = CompareWidget(expected, actual, path);
        if (difference is not null)
        {
            return difference;
        }

        for (int i = 0; i < expectedChildren.Count; i++)
        {
            difference = CompareElement(expectedChildren[i], actualChildren[i], $"{path}/{i}");
            if (difference is not null)
            {
                return difference;
            }
        }

        return null;
    }

    private static string? CompareClasses(Element expected, Element actual, string path)
    {
        ClassList expectedClasses = expected.Classes;
        ClassList actualClasses = actual.Classes;
        if (expectedClasses.Count != actualClasses.Count)
        {
            return Diff(path, "Class count", Show(expectedClasses.Count), Show(actualClasses.Count));
        }

        for (int i = 0; i < expectedClasses.Count; i++)
        {
            if (expectedClasses[i] != actualClasses[i])
            {
                return Diff(path, $"Class {i}", Show(expectedClasses[i]), Show(actualClasses[i]));
            }
        }

        return null;
    }

    private static string? CompareSlots(Element expected, Element actual, string path)
    {
        string? difference = SlotDiff(path, "OnPointerMove", expected.OnPointerMove, actual.OnPointerMove);
        difference ??= SlotDiff(path, "OnPointerMoveCapture", expected.OnPointerMoveCapture, actual.OnPointerMoveCapture);
        difference ??= SlotDiff(path, "OnPointerDown", expected.OnPointerDown, actual.OnPointerDown);
        difference ??= SlotDiff(path, "OnPointerDownCapture", expected.OnPointerDownCapture, actual.OnPointerDownCapture);
        difference ??= SlotDiff(path, "OnPointerUp", expected.OnPointerUp, actual.OnPointerUp);
        difference ??= SlotDiff(path, "OnPointerUpCapture", expected.OnPointerUpCapture, actual.OnPointerUpCapture);
        difference ??= SlotDiff(path, "OnScroll", expected.OnScroll, actual.OnScroll);
        difference ??= SlotDiff(path, "OnScrollCapture", expected.OnScrollCapture, actual.OnScrollCapture);
        difference ??= SlotDiff(path, "OnKeyDown", expected.OnKeyDown, actual.OnKeyDown);
        difference ??= SlotDiff(path, "OnKeyDownCapture", expected.OnKeyDownCapture, actual.OnKeyDownCapture);
        difference ??= SlotDiff(path, "OnKeyUp", expected.OnKeyUp, actual.OnKeyUp);
        difference ??= SlotDiff(path, "OnKeyUpCapture", expected.OnKeyUpCapture, actual.OnKeyUpCapture);
        difference ??= SlotDiff(path, "OnChar", expected.OnChar, actual.OnChar);
        difference ??= SlotDiff(path, "OnCharCapture", expected.OnCharCapture, actual.OnCharCapture);
        return difference;
    }

    private static string? CompareWidget(Element expected, Element actual, string path)
    {
        if (expected is Label expectedLabel)
        {
            Label actualLabel = (Label)actual;
            string? difference = TextDiff(path, expectedLabel.Text, actualLabel.Text);
            difference ??= LocalizedDiff(path, expectedLabel.LocalizedText, actualLabel.LocalizedText);
            return difference;
        }

        if (expected is Button expectedButton)
        {
            Button actualButton = (Button)actual;
            string? difference = TextDiff(path, expectedButton.Text, actualButton.Text);
            difference ??= LocalizedDiff(path, expectedButton.LocalizedText, actualButton.LocalizedText);
            difference ??= SlotDiff(path, "Clicked", expectedButton.Clicked, actualButton.Clicked);
            return difference;
        }

        if (expected is Toggle expectedToggle)
        {
            return SlotDiff(path, "CheckedChanged", expectedToggle.CheckedChanged, ((Toggle)actual).CheckedChanged);
        }

        if (expected is Slider expectedSlider)
        {
            Slider actualSlider = (Slider)actual;
            string? difference = FloatDiff(path, "Minimum", expectedSlider.Minimum, actualSlider.Minimum);
            difference ??= FloatDiff(path, "Maximum", expectedSlider.Maximum, actualSlider.Maximum);
            difference ??= FloatDiff(path, "Step", expectedSlider.Step, actualSlider.Step);
            difference ??= FloatDiff(path, "Value", expectedSlider.Value, actualSlider.Value);
            difference ??= SlotDiff(path, "ValueChanged", expectedSlider.ValueChanged, actualSlider.ValueChanged);
            return difference;
        }

        if (expected is TextField expectedField)
        {
            TextField actualField = (TextField)actual;
            string? difference = DiffUnless(
                expectedField.Capacity == actualField.Capacity,
                Diff(path, "Capacity", Show(expectedField.Capacity), Show(actualField.Capacity)));
            difference ??= TextDiff(path, expectedField.Text, actualField.Text);
            difference ??= SlotDiff(path, "TextChanged", expectedField.TextChanged, actualField.TextChanged);
            difference ??= SlotDiff(path, "Submitted", expectedField.Submitted, actualField.Submitted);
            return difference;
        }

        if (expected is Image expectedImage)
        {
            Image actualImage = (Image)actual;
            string? difference = DiffUnless(
                expectedImage.Texture.Id == actualImage.Texture.Id,
                Diff(path, "Texture.Id", Show(expectedImage.Texture.Id), Show(actualImage.Texture.Id)));
            difference ??= DiffUnless(
                expectedImage.Texture.Size.Equals(actualImage.Texture.Size),
                Diff(path, "Texture.Size", Show(expectedImage.Texture.Size), Show(actualImage.Texture.Size)));
            difference ??= DiffUnless(
                expectedImage.Uv.Equals(actualImage.Uv),
                Diff(path, "Uv", Show(expectedImage.Uv), Show(actualImage.Uv)));
            difference ??= DiffUnless(
                expectedImage.Tint.Equals(actualImage.Tint),
                Diff(path, "Tint", Show(expectedImage.Tint), Show(actualImage.Tint)));
            difference ??= DiffUnless(
                expectedImage.ScaleMode == actualImage.ScaleMode,
                Diff(path, "ScaleMode", Show(expectedImage.ScaleMode), Show(actualImage.ScaleMode)));
            return difference;
        }

        if (expected is ScrollView expectedView)
        {
            ScrollView actualView = (ScrollView)actual;
            string? difference = DiffUnless(
                expectedView.ScrollOffset.Equals(actualView.ScrollOffset),
                Diff(path, "ScrollOffset", Show(expectedView.ScrollOffset), Show(actualView.ScrollOffset)));
            difference ??= FloatDiff(path, "ScrollSpeed", expectedView.ScrollSpeed, actualView.ScrollSpeed);
            difference ??= SlotDiff(path, "ScrollChanged", expectedView.ScrollChanged, actualView.ScrollChanged);
            return difference;
        }

        return null;
    }

    private static string? SlotDiff(string path, string member, Delegate? expected, Delegate? actual)
    {
        int expectedCount = SlotCount(expected);
        int actualCount = SlotCount(actual);
        if (expectedCount == actualCount)
        {
            return null;
        }

        return Diff(path, $"{member} count", Show(expectedCount), Show(actualCount));
    }

    private static string? TextDiff(string path, ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual)
    {
        if (expected.SequenceEqual(actual))
        {
            return null;
        }

        return Diff(path, "Text", Show(Decode(expected)), Show(Decode(actual)));
    }

    private static string? LocalizedDiff(string path, object? expected, object? actual)
    {
        if (ReferenceEquals(expected, actual))
        {
            return null;
        }

        return Diff(path, "LocalizedText reference", Show(expected is null ? "none" : "set"), Show(actual is null ? "none" : "set"));
    }

    private static string? FloatDiff(string path, string member, float expected, float actual)
    {
        if (expected.Equals(actual))
        {
            return null;
        }

        return Diff(path, member, Show(expected), Show(actual));
    }

    private static string? DiffUnless(bool equal, string? message)
    {
        return equal ? null : message;
    }

    private static int SlotCount(Delegate? slot)
    {
        return slot?.GetInvocationList().Length ?? 0;
    }

    private static string Decode(ReadOnlySpan<byte> utf8)
    {
        return Encoding.UTF8.GetString(utf8.ToArray());
    }

    private static string Diff(string path, string member, string expected, string actual)
    {
        return $"{path}: {member} differs (expected {expected}, actual {actual})";
    }

    private static string Show(object? value)
    {
        if (value is null)
        {
            return "null";
        }

        if (value is string text)
        {
            return $"'{text}'";
        }

        return value.ToString() ?? "null";
    }
}
