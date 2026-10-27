using System;
using Brutal.Numerics;

namespace BrutalUi.Core;

/// <summary>
/// An element that shows a part of its children and moves them by a scroll offset. A wheel scroll over the view or
/// over a child moves <see cref="ScrollOffset"/> and calls <see cref="ScrollChanged"/> one time when the offset changes.
/// </summary>
/// <remarks>
/// <para>
/// The offset rule of the walks: the view itself uses the origin of its parent plus its rect position. Its own rect,
/// its clip, and its paint hook use this origin with no offset. The children use the origin of the view minus
/// <see cref="ScrollOffset"/>, so a child adds its rect position to that origin. In a view in a view, the offsets add
/// up and the clips intersect. The draw walk, the hit walk, and <c>WidgetInput.ViewportOrigin</c> apply this rule.
/// </para>
/// <para>
/// The clip: the constructor sets the inline overflow to <see cref="Overflow.Scroll"/>. The draw walk thus clips the
/// children to the padding box of the view. The layout gives the children no maximum on the main axis, so the content
/// can be larger than the view. An inline value wins over each rule, so a rule with <c>overflow: visible</c> does not
/// remove the clip. A write of <c>Inline.Overflow</c> from code replaces the value of the constructor. The children then
/// draw and hit outside the view.
/// </para>
/// <para>
/// The content size (Decision 4): on each axis, the far edge of the content is the largest far edge of the layout rects
/// of the direct children (their border boxes), plus the trailing padding and the trailing border of the view. No margin
/// counts. An absolute child counts, because it has a layout rect. A hidden child (<see cref="Display.None"/>) has a rect
/// of 0 and adds nothing. The view reads the layout of the last <see cref="UiContext.Update(float)"/>.
/// </para>
/// <para>
/// The leading edge rule: the view shows content from its leading edge only, because the offset stays at 0 or more.
/// Content before the leading edge of the view is out of reach. A <c>*-reverse</c> direction (for example
/// <c>column-reverse</c>) on the scroll axis puts content out of reach. A <c>justify-content</c> other than
/// <c>flex-start</c> on the scroll axis does the same. Start-aligned content is the supported case.
/// </para>
/// <para>
/// The clamp: on each axis, the offset is from 0 to the content size minus the size of the view, and never below 0. A
/// view with content that fits has the maximum offset 0 on that axis. Each path that writes the offset clamps it.
/// </para>
/// <para>
/// The clamp at use (Decision 5): the stored offset is the last value that a path wrote. The walks and the get of
/// <see cref="ScrollOffset"/> clamp it again with the layout of the last update. A content shrink thus shows the end of
/// the new content at once, and it calls <see cref="ScrollChanged"/> never. The stored offset stays. When the content
/// grows before the next input, the old offset comes back. A user input that changes the offset, or a set from code,
/// stores the clamped result, so the old offset is then gone. A wheel or a drag move that changes nothing stores
/// nothing.
/// </para>
/// <para>
/// The NaN rule: a NaN offset component gives 0 on its axis. Positive infinity gives the maximum, and negative infinity
/// gives 0. A delta component of a scroll that is not finite counts as 0.
/// </para>
/// <para>
/// The scroll rule: the next offset is the offset minus the delta of the event times <see cref="ScrollSpeed"/>, then
/// clamped. A negative y delta (the wheel moves toward the user) moves the content up and the offset down the content.
/// A positive delta scrolls toward the start. This is the GLFW sign convention.
/// </para>
/// <para>
/// The stop rule: a scroll that moves the offset stops the walk of the event. The stop also skips a later delegate of
/// the same slot. A scroll that does not move the offset, at the start or at the end, does not stop the walk. The
/// scroll then bubbles on to the ancestors, so an outer view can take it.
/// </para>
/// <para>
/// The sender: the view calls <see cref="ScrollChanged"/> after the stop and after the change, and it passes itself as
/// the sender. The delegate reads the new offset.
/// </para>
/// <para>
/// Read <see cref="ScrollChanged"/>, not the slot. A handler that a game adds with <c>+=</c> to
/// <see cref="Element.OnScroll"/> of a view gets no call for a scroll that moves the view. Read
/// <see cref="ScrollChanged"/>, or use a capture handler on an ancestor.
/// </para>
/// <para>
/// A write of <c>=</c> to <see cref="Element.OnScroll"/> removes the wheel. The view then ignores the wheel.
/// </para>
/// <para>
/// The hit walk and <c>WidgetInput.ViewportOrigin</c> read the same <see cref="AppliedOffset"/> as the draw walk, so a
/// hit and a widget map follow the content that the draw shows.
/// </para>
/// <para>
/// The drag rule (Decision 9): a left press on the view itself and a pointer move while the view holds the press drag
/// the content with the pointer. The press writes the pointer position and no offset. Each move adds the difference of
/// the last pointer position and the new position to the applied offset, and then writes the new position. The content
/// thus follows the pointer. A wheel scroll or a code set during the drag composes with the drag. At the end of the
/// content, a move back moves the content at once, because the drag never stores a position outside the clamp.
/// </para>
/// <para>
/// The press on the view itself (Q2 of the plan): only a press that hits the view starts a drag. The view itself is its
/// padding, a gap between the children, or a child with <see cref="Element.IsPickable"/> set to
/// <see langword="false"/>. A press on a pickable child gives no drag, so a button in the view still clicks. Set
/// <see cref="Element.IsPickable"/> to <see langword="false"/> on a decorative child to let a press fall through to the
/// view.
/// </para>
/// <para>
/// The stop rule of the drag: each drag move stops the walk, also when the offset does not change, because the view uses
/// the move. The press that starts a drag stops the walk too. A move with no press, a move after the release, and a move
/// after a press on another element give no drag, and the move walks on.
/// </para>
/// <para>
/// The anchor rule: <c>_dragPointer</c> holds the pointer position of the last drag press or drag move, or NaN when the
/// view has no anchor. The constructor writes NaN. A move on the view that is not a drag move writes NaN and walks on.
/// A hover move bubbles to the view before each press, so a stale anchor does not live to the next gesture. A drag
/// move with a NaN anchor stops the walk, stores the position, and changes no offset and calls nothing. An ancestor
/// can stop a press with a capture handler. The view then gets no press and no anchor, so the first move sets the
/// anchor and the later moves drag.
/// </para>
/// <para>
/// The press button rule: only a left press with no other button down starts a drag. A right press and a chord give no
/// drag, and the move walks on.
/// </para>
/// <para>
/// The capture rule: the view implements <see cref="IDragCapture"/>. While the view holds the left press with no chord,
/// each pointer move with no target walks to the view, also when the pointer leaves the view. The release of the last
/// button ends the capture. A disabled view captures no move.
/// </para>
/// <para>
/// A write of <c>=</c> to <see cref="Element.OnPointerDown"/> removes the drag. The view then ignores the press, and the
/// moves walk on.
/// </para>
/// <para>
/// The nested rule: a view can hold another view. Each level subtracts its own offset from the origin that it passes
/// down, so the offsets of the views add up for a child of the inner view. The clips intersect: a child of the inner
/// view shows only inside the clip of the inner view and inside the clip of the outer view. The draw walk, the hit
/// walk, and <c>WidgetInput.ViewportOrigin</c> use the same sum. <c>ViewportOrigin</c> subtracts the offset of each
/// ancestor view, and never the offset of the element itself.
/// </para>
/// <para>
/// The wheel in a nested view: the wheel walks from the hit element up to the root, so the inner view takes it first.
/// An inner view that moves stops the walk, and the outer view gets nothing. An inner view at the end of the content,
/// or with content that fits, does not move and does not stop the walk (the stop rule). The wheel then passes to the
/// outer view, which moves and calls its own <see cref="ScrollChanged"/>. The inner view calls its delegate never in
/// this case. A drag on the inner view moves the inner view only, because the press and each move stop the walk there.
/// </para>
/// <para>
/// The disabled rule (Decision 11): a view that holds <see cref="PseudoStates.Disabled"/> takes no wheel, no drag, and no
/// capture. It does not stop the walk, so the event goes on to the next ancestor. A disabled inner view thus passes
/// the wheel to the outer view. A disable during a drag ends the drag: the next move walks to the hit and changes
/// nothing. The walks still apply the offset of a disabled view, so its content does not jump. A set of
/// <see cref="ScrollOffset"/> from code still works, because <see cref="PseudoStates.Disabled"/> blocks user input only.
/// </para>
/// <para>
/// Only user input calls <see cref="ScrollChanged"/> (Decision 10). A set of <see cref="ScrollOffset"/> from code clamps
/// the offset and calls nothing.
/// </para>
/// <para>
/// The view has no own size (Decision 13). It has no measure callback and no inline size. Give it a width and a height,
/// or a maximum size, with a style rule or with <see cref="Element.Inline"/>. Else the view grows to its content and
/// has nothing to scroll. <c>flex-grow</c> alone does not limit the view, because the view grows to its content. Use a
/// size, a maximum size, or a percent size.
/// </para>
/// <para>
/// A set of <see cref="ScrollOffset"/> before the first <see cref="UiContext.Update(float)"/> gives (0, 0), because the
/// view has no layout yet and the maximum offset is (0, 0). The set stores (0, 0), so a later layout does not bring the
/// value back.
/// </para>
/// <para>
/// The view is not focusable and takes no key.
/// </para>
/// </remarks>
public sealed class ScrollView : Element, IDragCapture
{
    /// <summary>The scroll speed for a create-info speed of 0: 40 pixels for one unit of the delta.</summary>
    public const float DefaultScrollSpeed = 40f;

    /// <summary>The type id of each scroll view.</summary>
    private static readonly ElementTypeId s_typeId = ElementType.Intern("ScrollView");

    /// <summary>The name of the constructor parameter. A bad create-info speed throws with this name.</summary>
    private const string InfoName = "info";

    /// <summary>
    /// The scroll handler of each view. The delegate has no target, so each view shares it, and the constructor allocates
    /// no delegate.
    /// </summary>
    private static readonly UiEventHandler<ScrollEvent> s_scroll = HandleScroll;

    /// <summary>
    /// The press handler of each view. The delegate has no target, so each view shares it, and the constructor allocates
    /// no delegate.
    /// </summary>
    private static readonly UiEventHandler<PointerEvent> s_pointerDown = HandlePointerDown;

    /// <summary>
    /// The move handler of each view. The delegate has no target, so each view shares it, and the constructor allocates
    /// no delegate.
    /// </summary>
    private static readonly UiEventHandler<PointerEvent> s_pointerMove = HandlePointerMove;

    /// <summary>The stored scroll offset. Each write clamps it, and <see cref="AppliedOffset"/> clamps it again at use.</summary>
    private float2 _offset;

    /// <summary>The value of <see cref="_dragPointer"/> that means no anchor: both components are NaN.</summary>
    private static readonly float2 NoAnchor = new(float.NaN, float.NaN);

    /// <summary>
    /// The pointer position of the last drag press or drag move, or <see cref="NoAnchor"/>. It is data only: the press
    /// target of the context tells if a drag runs.
    /// </summary>
    private float2 _dragPointer;

    /// <summary>The scroll speed in pixels for one unit of the delta. It is finite and above 0.</summary>
    private readonly float _scrollSpeed;

    /// <summary>Creates a scroll view.</summary>
    /// <remarks>
    /// The constructor checks the speed first (Decision 8). A speed of 0 gives <see cref="DefaultScrollSpeed"/>. The
    /// constructor sets the inline overflow to <see cref="Overflow.Scroll"/> (Decision 6) and writes the scroll handler.
    /// It calls <see cref="ScrollChanged"/> never.
    /// </remarks>
    /// <param name="info">The create-info.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The speed is negative, NaN, or infinite. The <see cref="ArgumentException.ParamName"/> is <c>info</c>, and the
    /// message names the bad field of the create-info.
    /// </exception>
    public ScrollView(in CreateInfo info)
    {
        ValidateSpeed(info.ScrollSpeed);
        _scrollSpeed = info.ScrollSpeed == 0f ? DefaultScrollSpeed : info.ScrollSpeed;
        Inline.Overflow = Overflow.Scroll;
        OnScroll = s_scroll;
        OnPointerDown = s_pointerDown;
        OnPointerMove = s_pointerMove;
        _dragPointer = NoAnchor;
    }

    /// <summary>Gets the type id of the scroll view: <c>ScrollView</c>.</summary>
    public override ElementTypeId TypeId => s_typeId;

    /// <summary>Gets the scroll speed in pixels for one unit of the delta of a scroll event.</summary>
    public float ScrollSpeed => _scrollSpeed;

    /// <summary>Gets or sets the scroll offset in pixels: how far the content moves left and up.</summary>
    /// <remarks>
    /// The get gives the shown offset: the stored offset clamped with the layout of the last update. A set clamps each
    /// axis from 0 to the maximum offset of the last layout. A NaN gives 0. A set to a new shown offset marks
    /// <see cref="DirtyFlags.Paint"/>. A set to the shown offset marks nothing. A set from code calls no
    /// <see cref="ScrollChanged"/> (Decision 10).
    /// </remarks>
    public float2 ScrollOffset
    {
        get => AppliedOffset();
        set => Apply(value);
    }

    /// <summary>
    /// Gets or sets the delegate that a change by the user calls. The delegate gets the view as the sender.
    /// </summary>
    /// <remarks>
    /// Only user input calls the delegate, and only when the offset changes. The view stops the walk of the event,
    /// writes the offset, and then calls the delegate. A view with no delegate still scrolls and stops the walk.
    /// </remarks>
    public Action<ScrollView>? ScrollChanged { get; set; }

    /// <summary>
    /// Gives the maximum offset on each axis: the content size minus the size of the view, never below 0 (Decision 4).
    /// </summary>
    /// <remarks>
    /// On each axis, the far edge is the largest <c>X + width</c> (or <c>Y + height</c>) of the layout rects of the direct
    /// children, or 0 with no child. The method adds the trailing inset (the trailing padding plus the trailing border)
    /// and subtracts the size of the view. It reads the layout of the last update. The loop reads one span and allocates
    /// nothing. A NaN edge does not count.
    /// </remarks>
    /// <returns>The maximum offset in pixels.</returns>
    internal float2 MaxOffset()
    {
        float farX = 0f;
        float farY = 0f;
        ReadOnlySpan<Element> children = ChildStorage.AsSpan();
        for (int i = 0; i < children.Length; i++)
        {
            float4 r = children[i].Layout.Rect;
            float right = r.X + r.Z;
            float bottom = r.Y + r.W;
            if (right > farX)
            {
                farX = right;
            }

            if (bottom > farY)
            {
                farY = bottom;
            }
        }

        float4 rect = Layout.Rect;
        float4 insets = Layout.ContentInsets;
        float maxX = farX + insets.Z - rect.Z;
        float maxY = farY + insets.W - rect.W;
        return new float2(maxX > 0f ? maxX : 0f, maxY > 0f ? maxY : 0f);
    }

    /// <summary>Gives the offset that the walks apply to the children (Decision 5).</summary>
    /// <remarks>
    /// The method clamps the stored offset with <see cref="MaxOffset"/> of the last layout. The draw walk, the hit walk,
    /// <c>WidgetInput.ViewportOrigin</c>, and the get of <see cref="ScrollOffset"/> read it. The method writes nothing, so
    /// a clamp that a content change causes calls no <see cref="ScrollChanged"/>.
    /// </remarks>
    /// <returns>The applied offset in pixels.</returns>
    internal float2 AppliedOffset()
    {
        return Clamp(_offset);
    }

    /// <summary>
    /// The scroll handler. It moves the offset by the delta times the speed, and it stops the walk only when the offset
    /// changes (Decision 7).
    /// </summary>
    /// <param name="e">The scroll event.</param>
    private static void HandleScroll(ref ScrollEvent e)
    {
        if (e.Header.CurrentTarget is not ScrollView view || !WidgetInput.IsScroll(view, in e))
        {
            return;
        }

        float2 applied = view.AppliedOffset();
        float speed = view._scrollSpeed;
        float deltaX = float.IsFinite(e.Delta.X) ? e.Delta.X : 0f;
        float deltaY = float.IsFinite(e.Delta.Y) ? e.Delta.Y : 0f;
        float2 next = view.Clamp(new float2(applied.X - (deltaX * speed), applied.Y - (deltaY * speed)));
        if (next.X == applied.X && next.Y == applied.Y)
        {
            return;
        }

        e.Header.StopPropagation();
        view.UserSet(next);
    }

    /// <summary>Gets a value that tells if the view takes the moves of its drag: <see langword="true"/> unless disabled.</summary>
    /// <remarks>
    /// A view that holds <see cref="PseudoStates.Disabled"/> captures no move. The move then walks to the hit.
    /// </remarks>
    bool IDragCapture.CapturesMoves => (States & PseudoStates.Disabled) == 0;

    /// <summary>
    /// The press handler. It stores the pointer position when the press starts a drag on the view itself. The offset does
    /// not change at the press.
    /// </summary>
    /// <param name="e">The press event.</param>
    private static void HandlePointerDown(ref PointerEvent e)
    {
        if (e.Header.CurrentTarget is not ScrollView view || !WidgetInput.IsDragStart(view, in e))
        {
            return;
        }

        e.Header.StopPropagation();
        view._dragPointer = e.Position;
    }

    /// <summary>
    /// The move handler. It moves the offset by the difference of the last pointer position and the new position when the
    /// view holds the left press. It stops the walk also when the offset does not change.
    /// </summary>
    /// <param name="e">The move event.</param>
    private static void HandlePointerMove(ref PointerEvent e)
    {
        if (e.Header.CurrentTarget is not ScrollView view)
        {
            return;
        }

        if (!WidgetInput.IsDragMove(view, in e))
        {
            view._dragPointer = NoAnchor;
            return;
        }

        e.Header.StopPropagation();
        float2 last = view._dragPointer;
        view._dragPointer = e.Position;
        if (float.IsNaN(last.X) || float.IsNaN(last.Y))
        {
            return;
        }

        float2 applied = view.AppliedOffset();
        float2 next = view.Clamp(new float2(applied.X + (last.X - e.Position.X), applied.Y + (last.Y - e.Position.Y)));
        if (next.X != applied.X || next.Y != applied.Y)
        {
            view.UserSet(next);
        }
    }

    /// <summary>Checks the speed of the create-info (Decision 8).</summary>
    /// <param name="speed">The speed of the create-info.</param>
    /// <exception cref="ArgumentOutOfRangeException">The speed is negative, NaN, or infinite.</exception>
    private static void ValidateSpeed(float speed)
    {
        if (!float.IsFinite(speed) || speed < 0f)
        {
            throw new ArgumentOutOfRangeException(InfoName, speed, "CreateInfo.ScrollSpeed must be finite and 0 or more.");
        }
    }

    /// <summary>Clamps an offset on each axis from 0 to <see cref="MaxOffset"/>. A NaN gives 0.</summary>
    /// <param name="value">The offset.</param>
    /// <returns>The clamped offset.</returns>
    private float2 Clamp(float2 value)
    {
        float2 max = MaxOffset();
        return new float2(ClampAxis(value.X, max.X), ClampAxis(value.Y, max.Y));
    }

    /// <summary>Clamps one axis from 0 to a maximum. A NaN gives 0.</summary>
    /// <param name="value">The value.</param>
    /// <param name="max">The maximum. It is 0 or more.</param>
    /// <returns>The clamped value.</returns>
    private static float ClampAxis(float value, float max)
    {
        if (!(value > 0f))
        {
            return 0f;
        }

        return value < max ? value : max;
    }

    /// <summary>
    /// Clamps an offset, compares it with the applied offset, writes the clamped value, and marks the paint when the
    /// shown offset changes.
    /// </summary>
    /// <remarks>
    /// The method writes the clamped value also when it equals the applied offset. A set to the shown offset thus replaces
    /// a stored offset that the last layout clamped.
    /// </remarks>
    /// <param name="value">The new offset.</param>
    /// <returns><see langword="true"/> when the shown offset changed.</returns>
    private bool Apply(float2 value)
    {
        float2 next = Clamp(value);
        float2 applied = AppliedOffset();
        _offset = next;
        if (next.X == applied.X && next.Y == applied.Y)
        {
            return false;
        }

        MarkDirty(DirtyFlags.Paint);
        return true;
    }

    /// <summary>Writes an offset from user input and calls <see cref="ScrollChanged"/> when the offset changes.</summary>
    /// <param name="value">The new offset.</param>
    private void UserSet(float2 value)
    {
        if (Apply(value))
        {
            ScrollChanged?.Invoke(this);
        }
    }

    /// <summary>The create-info of a <see cref="ScrollView"/>.</summary>
    /// <remarks>The create-info has no required member: each member has a safe default.</remarks>
    public struct CreateInfo
    {
        /// <summary>
        /// The scroll speed in pixels for one unit of the delta of a scroll event. 0 gives
        /// <see cref="DefaultScrollSpeed"/>. A negative, NaN, or infinite value throws. The default is 0.
        /// </summary>
        public float ScrollSpeed;
    }
}
