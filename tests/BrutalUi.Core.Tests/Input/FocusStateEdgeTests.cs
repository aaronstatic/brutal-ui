using System.Collections.Generic;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the edge rules of the keyboard focus: an owner that cannot hold the focus loses it at the start of the next
/// drain, a disabled element takes no focus, a set on the owner marks nothing, and a set from a handler routes the
/// next key of the same drain.
/// </summary>
/// <remarks>
/// The class table is global and xUnit runs test classes in parallel. Each test gives the tree a unique name prefix.
/// </remarks>
public sealed class FocusStateEdgeTests
{
    /// <summary>A point inside <see cref="PointerTree.Right"/> and outside <see cref="PointerTree.Inner"/>.</summary>
    private static readonly float2 OverRight = new(175f, 75f);

    /// <summary>A point inside <see cref="PointerTree.Inner"/>.</summary>
    private static readonly float2 OverInner = new(125f, 25f);

    /// <summary>The GLFW key code of the A key.</summary>
    private const int KeyA = 65;

    /// <summary>The GLFW key code of the B key.</summary>
    private const int KeyB = 66;

    /// <summary>
    /// The demo of phase 5. A focused element that leaves the tree keeps the focus until the next drain. An empty
    /// drain then clears <see cref="PseudoStates.Focus"/> on it and sets <see cref="UiContext.FocusedElement"/> to
    /// <see langword="null"/>.
    /// </summary>
    [Fact]
    public void AFocusedElementThatLeavesTheTreeLosesTheFocus()
    {
        PointerTree tree = new(nameof(FocusStateEdgeTests) + "." + nameof(AFocusedElementThatLeavesTheTreeLosesTheFocus));
        tree.Right.IsFocusable = true;
        tree.Settle();

        Assert.True(tree.Context.SetFocus(tree.Right));
        Assert.Equal(PseudoStates.Focus, tree.Right.States);

        Assert.True(tree.Root.Children.Remove(tree.Right));

        // The removal itself does not move the focus. The stale rule runs at the start of the next drain.
        Assert.Same(tree.Right, tree.Context.FocusedElement);

        Assert.Equal(0, tree.Context.DrainEvents());

        TestContext.Current.TestOutputHelper?.WriteLine(
            tree.Right.Name + " " + tree.Right.States + " " + (tree.Context.FocusedElement?.Name ?? "none"));

        Assert.Equal(PseudoStates.None, tree.Right.States);
        Assert.Null(tree.Context.FocusedElement);

        // The element is outside the roots, so a code call cannot focus it again.
        Assert.False(tree.Context.SetFocus(tree.Right));
        Assert.Null(tree.Context.FocusedElement);
    }

    /// <summary>A root that the caller removes from the context takes the focus of its descendants away.</summary>
    [Fact]
    public void ARemovedRootLosesTheFocus()
    {
        PointerTree tree = new(nameof(FocusStateEdgeTests) + "." + nameof(ARemovedRootLosesTheFocus));
        tree.Inner.IsFocusable = true;
        tree.Settle();

        Assert.True(tree.Context.SetFocus(tree.Inner));
        Assert.True(tree.Context.RemoveRoot(tree.Root));
        Assert.Same(tree.Inner, tree.Context.FocusedElement);

        // A key of the same drain gets no walk: the stale rule clears the owner before the target step.
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, KeyA, UiModifiers.None));
        Assert.Equal(0, tree.Context.DrainEvents());

        Assert.Equal(PseudoStates.None, tree.Inner.States);
        Assert.Null(tree.Context.FocusedElement);
        Assert.False(tree.Context.SetFocus(tree.Inner));

        // The root comes back. The focus does not come back by itself, but a code call can set it again.
        tree.Context.AddRoot(tree.Root);
        Assert.Null(tree.Context.FocusedElement);
        Assert.True(tree.Context.SetFocus(tree.Inner));
        Assert.Equal(PseudoStates.Focus, tree.Inner.States);
    }

    /// <summary>
    /// An owner that turns unfocusable keeps the focus until the next drain. The drain then clears it before the target
    /// step, so a char of that drain gets no walk.
    /// </summary>
    [Fact]
    public void AnOwnerThatTurnsUnfocusableLosesTheFocus()
    {
        PointerTree tree = new(nameof(FocusStateEdgeTests) + "." + nameof(AnOwnerThatTurnsUnfocusableLosesTheFocus));
        tree.Right.IsFocusable = true;
        tree.Settle();

        Assert.True(tree.Context.SetFocus(tree.Right));
        tree.Right.IsFocusable = false;
        Assert.Same(tree.Right, tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.Focus, tree.Right.States);

        int chars = 0;
        tree.Right.OnChar = (ref CharEvent e) => chars++;

        tree.Context.PushEvent(PointerTree.Char('a'));
        Assert.Equal(0, tree.Context.DrainEvents());

        Assert.Equal(0, chars);
        Assert.Equal(PseudoStates.None, tree.Right.States);
        Assert.Null(tree.Context.FocusedElement);
        Assert.False(tree.Context.SetFocus(tree.Right));

        // The element turns focusable again. It does not get the focus back by itself.
        tree.Right.IsFocusable = true;
        Assert.Equal(0, tree.Context.DrainEvents());
        Assert.Null(tree.Context.FocusedElement);
        Assert.True(tree.Context.SetFocus(tree.Right));
    }

    /// <summary>
    /// An owner that gets <see cref="PseudoStates.Disabled"/> keeps the focus until the next drain. The drain then
    /// clears <see cref="PseudoStates.Focus"/> and keeps <see cref="PseudoStates.Disabled"/>. A failed set on the
    /// disabled owner clears the focus at once (CR-4).
    /// </summary>
    [Fact]
    public void AnOwnerThatGetsDisabledLosesTheFocus()
    {
        PointerTree tree = new(nameof(FocusStateEdgeTests) + "." + nameof(AnOwnerThatGetsDisabledLosesTheFocus));
        tree.Right.IsFocusable = true;
        tree.Settle();

        Assert.True(tree.Context.SetFocus(tree.Right));
        tree.Right.States |= PseudoStates.Disabled;
        Assert.Same(tree.Right, tree.Context.FocusedElement);

        // CR-4: a failed SetFocus on the owner clears the focus at once, so the result and the state agree.
        Assert.False(tree.Context.SetFocus(tree.Right));
        Assert.Null(tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.Disabled, tree.Right.States);

        // The owner comes back for the drain check below.
        tree.Right.States &= ~PseudoStates.Disabled;
        Assert.True(tree.Context.SetFocus(tree.Right));
        tree.Right.States |= PseudoStates.Disabled;
        Assert.Same(tree.Right, tree.Context.FocusedElement);

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, KeyA, UiModifiers.None));
        Assert.Equal(0, tree.Context.DrainEvents());

        Assert.Equal(PseudoStates.Disabled, tree.Right.States);
        Assert.Null(tree.Context.FocusedElement);
        Assert.False(tree.Context.SetFocus(tree.Right));

        // The element gets enabled again. A code call can focus it now.
        tree.Right.States &= ~PseudoStates.Disabled;
        Assert.True(tree.Context.SetFocus(tree.Right));
        Assert.Equal(PseudoStates.Focus, tree.Right.States);
    }

    /// <summary>
    /// An owner under a hidden ancestor loses the focus at the next drain. The test writes the style after
    /// <see cref="PointerTree.Settle"/>, so no style pass overwrites it before the drain. <see cref="Display.None"/>
    /// on the parent hides the owner, and so does a clamped opacity product of 0. A product above 0 keeps the owner.
    /// </summary>
    [Fact]
    public void AnOwnerUnderAHiddenAncestorLosesTheFocus()
    {
        PointerTree tree = new(nameof(FocusStateEdgeTests) + "." + nameof(AnOwnerUnderAHiddenAncestorLosesTheFocus));
        tree.Inner.IsFocusable = true;
        tree.Settle();

        // Display.None on the parent.
        Assert.True(tree.Context.SetFocus(tree.Inner));
        tree.Right.StyleStorage.Display = Display.None;
        Assert.Same(tree.Inner, tree.Context.FocusedElement);
        Assert.Equal(0, tree.Context.DrainEvents());
        Assert.Null(tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.None, tree.Inner.States);
        Assert.False(tree.Context.SetFocus(tree.Inner));

        // An opacity of 0 on the parent.
        tree.Right.StyleStorage.Display = Display.Flex;
        Assert.True(tree.Context.SetFocus(tree.Inner));
        tree.Right.StyleStorage.Opacity = 0f;
        Assert.Equal(0, tree.Context.DrainEvents());
        Assert.Null(tree.Context.FocusedElement);
        Assert.False(tree.Context.SetFocus(tree.Inner));

        // A value below 0 counts as 0.
        tree.Right.StyleStorage.Opacity = 1f;
        Assert.True(tree.Context.SetFocus(tree.Inner));
        tree.Right.StyleStorage.Opacity = -3f;
        Assert.Equal(0, tree.Context.DrainEvents());
        Assert.Null(tree.Context.FocusedElement);

        // NaN and a value above 1 count as 1, and a product of 0.25 is above 0. The owner keeps the focus.
        tree.Right.StyleStorage.Opacity = float.NaN;
        Assert.True(tree.Context.SetFocus(tree.Inner));
        Assert.Equal(0, tree.Context.DrainEvents());
        Assert.Same(tree.Inner, tree.Context.FocusedElement);

        tree.Right.StyleStorage.Opacity = 7f;
        Assert.Equal(0, tree.Context.DrainEvents());
        Assert.Same(tree.Inner, tree.Context.FocusedElement);

        tree.Right.StyleStorage.Opacity = 0.5f;
        tree.Inner.StyleStorage.Opacity = 0.5f;
        Assert.Equal(0, tree.Context.DrainEvents());
        Assert.Same(tree.Inner, tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.Focus, tree.Inner.States);

        // Display.None on the root hides each element of the tree.
        tree.Root.StyleStorage.Display = Display.None;
        Assert.Equal(0, tree.Context.DrainEvents());
        Assert.Null(tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.None, tree.Inner.States);
    }

    /// <summary>
    /// A disabled element takes no focus by code, by click, or by Tab. A press on it clears the focus, and the press
    /// does not give the focus to a focusable ancestor. A Tab skips it.
    /// </summary>
    [Fact]
    public void ADisabledElementTakesNoFocus()
    {
        PointerTree tree = new(nameof(FocusStateEdgeTests) + "." + nameof(ADisabledElementTakesNoFocus));
        tree.Root.IsFocusable = true;
        tree.Left.IsFocusable = true;
        tree.Right.IsFocusable = true;
        tree.Right.States = PseudoStates.Disabled;
        tree.Inner.IsFocusable = true;
        tree.Settle();

        // By code.
        Assert.False(tree.Context.SetFocus(tree.Right));
        Assert.Null(tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.Disabled, tree.Right.States);

        // By Tab: the order is Root, Left, Right, and Inner. Tab and Shift+Tab skip Right.
        Assert.True(tree.Context.SetFocus(tree.Left));
        tree.Context.PushEvent(PointerTree.Tab(false));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Same(tree.Inner, tree.Context.FocusedElement);

        tree.Context.PushEvent(PointerTree.Tab(true));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Same(tree.Left, tree.Context.FocusedElement);

        // By click: the press on Right clears the focus. Root is focusable, but it does not get the focus.
        tree.Context.PushEvent(PointerTree.Down(OverRight, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Null(tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.None, tree.Right.States & PseudoStates.Focus);
        Assert.Equal(PseudoStates.None, tree.Root.States & PseudoStates.Focus);
        Assert.Equal(PseudoStates.None, tree.Left.States & PseudoStates.Focus);

        tree.Context.PushEvent(PointerTree.Up(OverRight, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());

        // A press on Inner focuses Inner. The disabled parent does not block a focusable child.
        tree.Context.PushEvent(PointerTree.Down(OverInner, PointerButton.Left));
        Assert.Equal(1, tree.Context.DrainEvents());
        Assert.Same(tree.Inner, tree.Context.FocusedElement);
    }

    /// <summary>
    /// A <see cref="UiContext.SetFocus(Element)"/> call for the current owner after <see cref="PointerTree.Settle"/>
    /// marks no element for <see cref="DirtyFlags.Style"/>. An empty drain with a valid owner marks nothing too. When
    /// code clears <see cref="PseudoStates.Focus"/> through <see cref="Element.States"/>, a drain and a set on the
    /// owner set the bit again (CR-3).
    /// </summary>
    [Fact]
    public void SetFocusOnTheOwnerMarksNothing()
    {
        PointerTree tree = new(nameof(FocusStateEdgeTests) + "." + nameof(SetFocusOnTheOwnerMarksNothing));
        tree.Right.IsFocusable = true;
        tree.AddFocusSheet();
        tree.Settle();

        Assert.True(tree.Context.SetFocus(tree.Right));
        tree.Settle();
        Assert.Equal(PointerTree.FocusFill, tree.Right.Style.BackgroundColor);

        Assert.True(tree.Context.SetFocus(tree.Right));
        Assert.Equal(0, tree.Context.DrainEvents());

        Assert.Equal(DirtyFlags.None, tree.Root.Dirty & DirtyFlags.Style);
        Assert.Equal(DirtyFlags.None, tree.Left.Dirty & DirtyFlags.Style);
        Assert.Equal(DirtyFlags.None, tree.Right.Dirty & DirtyFlags.Style);
        Assert.Equal(DirtyFlags.None, tree.Inner.Dirty & DirtyFlags.Style);
        Assert.Same(tree.Right, tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.Focus, tree.Right.States);

        // The repair rule (CR-3): code clears the bit through States. An empty drain sets it again on the owner.
        tree.Right.States = PseudoStates.None;
        Assert.Same(tree.Right, tree.Context.FocusedElement);
        Assert.Equal(0, tree.Context.DrainEvents());
        Assert.Same(tree.Right, tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.Focus, tree.Right.States);
        tree.Settle();
        Assert.Equal(PointerTree.FocusFill, tree.Right.Style.BackgroundColor);

        // A SetFocus call on the owner sets the bit again too.
        tree.Right.States = PseudoStates.None;
        Assert.True(tree.Context.SetFocus(tree.Right));
        Assert.Equal(PseudoStates.Focus, tree.Right.States);
        Assert.Same(tree.Right, tree.Context.FocusedElement);
    }

    /// <summary>
    /// A <see cref="UiEventType.KeyDown"/> handler on <see cref="PointerTree.Left"/> calls
    /// <see cref="UiContext.SetFocus(Element)"/> for <see cref="PointerTree.Right"/>. The next key of the same drain
    /// walks to <see cref="PointerTree.Right"/>. A handler that disables the owner or clears
    /// <see cref="Element.IsFocusable"/> drops the owner at the next key or char entry of the same drain (CR-5).
    /// </summary>
    [Fact]
    public void ASetFocusFromAHandlerRoutesTheNextKey()
    {
        PointerTree tree = new(nameof(FocusStateEdgeTests) + "." + nameof(ASetFocusFromAHandlerRoutesTheNextKey));
        tree.Left.IsFocusable = true;
        tree.Right.IsFocusable = true;
        tree.Settle();

        List<string> seen = new();
        bool moved = false;
        tree.Left.OnKeyDown = (ref KeyEvent e) =>
        {
            seen.Add("left " + e.Key + " " + e.Header.Target?.Name);
            moved = tree.Context.SetFocus(tree.Right);
        };
        tree.Right.OnKeyDown = (ref KeyEvent e) => seen.Add("right " + e.Key + " " + e.Header.Target?.Name);

        Assert.True(tree.Context.SetFocus(tree.Left));
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, KeyA, UiModifiers.None));
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, KeyB, UiModifiers.None));
        Assert.Equal(2, tree.Context.DrainEvents());

        Assert.True(moved);
        Assert.Equal(new[] { "left 65 left", "right 66 right" }, seen.ToArray());
        Assert.Same(tree.Right, tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.None, tree.Left.States);
        Assert.Equal(PseudoStates.Focus, tree.Right.States);

        // CR-5: a KeyDown handler disables the owner. The first key walks to the owner. The second key of the same
        // drain gets no walk, and the drain drops the owner.
        seen.Clear();
        tree.Right.OnKeyDown = (ref KeyEvent e) =>
        {
            seen.Add("right " + e.Key + " " + e.Header.Target?.Name);
            tree.Right.States |= PseudoStates.Disabled;
        };

        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, KeyA, UiModifiers.None));
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, KeyB, UiModifiers.None));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(new[] { "right 65 right" }, seen.ToArray());
        Assert.Null(tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.Disabled, tree.Right.States);

        // A handler that clears IsFocusable drops the owner at the next char entry of the same drain.
        seen.Clear();
        int chars = 0;
        tree.Left.OnKeyDown = (ref KeyEvent e) =>
        {
            seen.Add("left " + e.Key + " " + e.Header.Target?.Name);
            tree.Left.IsFocusable = false;
        };
        tree.Left.OnChar = (ref CharEvent e) => chars++;

        Assert.True(tree.Context.SetFocus(tree.Left));
        tree.Context.PushEvent(PointerTree.Key(UiEventType.KeyDown, KeyA, UiModifiers.None));
        tree.Context.PushEvent(PointerTree.Char('a'));
        Assert.Equal(1, tree.Context.DrainEvents());

        Assert.Equal(new[] { "left 65 left" }, seen.ToArray());
        Assert.Equal(0, chars);
        Assert.Null(tree.Context.FocusedElement);
        Assert.Equal(PseudoStates.None, tree.Left.States);
    }
}
