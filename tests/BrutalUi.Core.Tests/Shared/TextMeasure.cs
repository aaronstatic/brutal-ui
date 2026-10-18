using System;
using Brutal.Numerics;

namespace BrutalUi.Core.Tests;

/// <summary>
/// A measure callback for a text leaf. It counts its calls.
/// </summary>
/// <remarks>
/// <para>
/// The text has a fixed content width and wraps into lines of a fixed height. The line width is the content width
/// for <see cref="MeasureMode.Undefined"/>, the available width for <see cref="MeasureMode.Exactly"/>, and the smaller
/// of both for <see cref="MeasureMode.AtMost"/>. The height is the line height times the line count
/// (<c>ceil(contentWidth / max(lineWidth, 1))</c>), the available height for <see cref="MeasureMode.Exactly"/>, and
/// not more than the available height for <see cref="MeasureMode.AtMost"/>.
/// </para>
/// <para>
/// The callback keeps the cache contract of section 6 of the technical design. It gives the size of its content
/// only. With an <see cref="MeasureMode.AtMost"/> limit, it gives the result of an undefined size when that result
/// fits. It does not change the tree, it marks no bit, and it allocates nothing.
/// </para>
/// <para>
/// The constructor creates the delegate one time. Many leaves can share one instance. The benchmark project links
/// this file, so the file uses no test framework type and no internal member of <c>BrutalUi.Core</c>.
/// </para>
/// </remarks>
internal sealed class TextMeasure
{
    private readonly float _contentWidth;
    private readonly float _lineHeight;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextMeasure"/> class.
    /// </summary>
    /// <param name="contentWidth">The width of the text on one line, in pixels.</param>
    /// <param name="lineHeight">The height of one line, in pixels.</param>
    public TextMeasure(float contentWidth, float lineHeight)
    {
        _contentWidth = contentWidth;
        _lineHeight = lineHeight;
        Callback = Measure;
    }

    /// <summary>Gets the measure function to give to <c>Layout.Measure</c>. It counts each call.</summary>
    public MeasureCallback Callback { get; }

    /// <summary>Gets the number of calls since the creation of the instance or the last <see cref="Reset"/>.</summary>
    public int Count { get; private set; }

    /// <summary>Sets <see cref="Count"/> to 0.</summary>
    public void Reset()
    {
        Count = 0;
    }

    private float2 Measure(Element element, float width, MeasureMode widthMode, float height, MeasureMode heightMode)
    {
        Count++;

        float lineWidth = widthMode switch
        {
            MeasureMode.Undefined => _contentWidth,
            MeasureMode.Exactly => width,
            _ => MathF.Min(_contentWidth, width),
        };

        float lines = MathF.Ceiling(_contentWidth / MathF.Max(lineWidth, 1f));
        float textHeight = heightMode switch
        {
            MeasureMode.Undefined => _lineHeight * lines,
            MeasureMode.Exactly => height,
            _ => MathF.Min(_lineHeight * lines, height),
        };

        return new float2(lineWidth, textHeight);
    }
}
