using System;
using Brutal.Numerics;

namespace BrutalUi.Core;

/// <summary>
/// A handler for a file drop on the window. <see cref="UiContext.RaiseFileDrop(ReadOnlySpan{string})"/> calls it.
/// </summary>
/// <param name="paths">The paths of the dropped files, in the order of the platform. The span is valid during the call only.</param>
/// <remarks>
/// <para>
/// A4: a window event gets no per-element routing. The context calls the handler at once. It does not queue the
/// event, it does not hit-test the drop position, and it walks no element.
/// </para>
/// <para>Copy a path to keep it after the call. The strings stay valid, but the span does not.</para>
/// </remarks>
public delegate void UiFileDropHandler(ReadOnlySpan<string> paths);

/// <summary>
/// A handler for a change of the content scale of the window.
/// <see cref="UiContext.RaiseContentScale(float2)"/> calls it.
/// </summary>
/// <param name="scale">The new content scale: the ratio of the pixel size to the logical size on each axis.</param>
/// <remarks>
/// A4: a window event gets no per-element routing. The context calls the handler at once. It does not queue the
/// event and it walks no element. The context does not apply the scale. The handler applies it, for example to
/// <see cref="UiContext.ViewportSize"/> or to the font size.
/// </remarks>
public delegate void UiContentScaleHandler(float2 scale);
