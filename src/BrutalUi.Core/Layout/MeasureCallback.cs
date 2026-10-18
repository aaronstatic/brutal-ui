using Brutal.Numerics;

namespace BrutalUi.Core;

/// <summary>
/// Measures the content of a leaf element for the layout pass. Code sets it in <see cref="LayoutNode.Measure"/>.
/// </summary>
/// <remarks>
/// <para>
/// The engine calls the callback for an element with no children only. A callback on an element with children
/// has no effect. The engine does not call the callback when the available sizes already give the size, for
/// example when both modes are <see cref="MeasureMode.Exactly"/>.
/// </para>
/// <para>
/// The width and the height are the inner size: the available size less the margin, the padding, and the border
/// of the element. The size is never negative. An undefined size is <c>NaN</c>, and its mode is
/// <see cref="MeasureMode.Undefined"/>. The returned size is the size of the content only. The engine adds the
/// padding and the border, and it applies the minimum and the maximum of the element. An invalid component
/// (<c>NaN</c> or negative) counts as 0. An infinite component gives an infinite size. Return a finite size.
/// </para>
/// <para>
/// The callback must not change the tree, must not mark a dirty bit, and must not allocate. It runs often in one
/// layout pass. Assign a <see langword="static"/> lambda or a method group one time, for example in a
/// constructor, and not in the frame loop.
/// </para>
/// <para>
/// The callback gives the size of its content. With <see cref="MeasureMode.AtMost"/>, it gives the same size as with
/// <see cref="MeasureMode.Undefined"/> when that size fits the limit. A callback that fills the available space
/// breaks this contract. The layout cache can then return a stale size for a constraint that the callback never
/// saw.
/// </para>
/// </remarks>
/// <param name="element">The element to measure.</param>
/// <param name="width">The available inner width in pixels. <c>NaN</c> when <paramref name="widthMode"/> is <see cref="MeasureMode.Undefined"/>.</param>
/// <param name="widthMode">The rule for <paramref name="width"/>.</param>
/// <param name="height">The available inner height in pixels. <c>NaN</c> when <paramref name="heightMode"/> is <see cref="MeasureMode.Undefined"/>.</param>
/// <param name="heightMode">The rule for <paramref name="height"/>.</param>
/// <returns>The width and the height of the content in pixels.</returns>
public delegate float2 MeasureCallback(
    Element element,
    float width,
    MeasureMode widthMode,
    float height,
    MeasureMode heightMode);
