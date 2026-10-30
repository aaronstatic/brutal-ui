using System;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests that a <see cref="SlotContainer"/> is a plain <see cref="Element"/> for the style pass, the layout pass,
/// and the bindings: a stylesheet styles it, the layout places the mounted roots, and a panel takes the style of the
/// slot that holds it.
/// </summary>
/// <remarks>
/// Each test puts the slot first under the layout root, so the slot starts at the Y offset 0 and the expected Y values
/// are the heights of the mounted roots.
/// </remarks>
public sealed class SlotContainerTreeTests
{
    private static readonly Rgba8 Gold = new(255, 204, 0, 255);
    private static readonly Rgba8 Green = new(0, 255, 0, 255);
    private static readonly Rgba8 Red = new(255, 0, 0, 255);

    /// <summary>Creates a context with a measurer and a viewport of 400 by 300.</summary>
    /// <returns>The context.</returns>
    private static UiContext CreateContext()
    {
        return new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer() })
        {
            ViewportSize = new float2(400, 300),
        };
    }

    /// <summary>Creates a layout root of 400 by 300 with a column direction and adds it to a context.</summary>
    /// <param name="context">The context.</param>
    /// <returns>The layout root.</returns>
    private static Element CreateLayoutRoot(UiContext context)
    {
        Element layoutRoot = new();
        layoutRoot.Inline.Width = 400;
        layoutRoot.Inline.Height = 300;
        layoutRoot.Inline.FlexDirection = FlexDirection.Column;
        context.AddRoot(layoutRoot);
        return layoutRoot;
    }

    /// <summary>Creates an element with a fixed height.</summary>
    /// <param name="height">The height in pixels.</param>
    /// <returns>The element.</returns>
    private static Element CreateRoot(float height)
    {
        Element root = new();
        root.Inline.Height = height;
        return root;
    }

    [Fact]
    public void AStylesheetStylesASlotByTypeName()
    {
        UiContext context = CreateContext();
        Element layoutRoot = CreateLayoutRoot(context);
        SlotContainer slot = new("left"u8);
        Element sibling = new();
        layoutRoot.Children.Add(slot);
        layoutRoot.Children.Add(sibling);
        context.AddSheet(new StyleSheet.Builder()
            .Add(Selector.ForType(ElementType.Intern("SlotContainer")), new StyleProperties { BackgroundColor = Gold })
            .Build());

        context.Update(0f);

        Assert.Equal(Gold, slot.Style.BackgroundColor);
        Assert.NotEqual(Gold, sibling.Style.BackgroundColor);
    }

    [Fact]
    public void AStylesheetStylesASlotByClassAndByName()
    {
        UiContext context = CreateContext();
        Element layoutRoot = CreateLayoutRoot(context);
        SlotContainer slot = new("left"u8);
        Element sibling = new();
        ClassId className = ClassName.Intern(nameof(AStylesheetStylesASlotByClassAndByName) + ".slot");
        slot.Classes.Add(className);
        layoutRoot.Children.Add(slot);
        layoutRoot.Children.Add(sibling);
        context.AddSheet(new StyleSheet.Builder()
            .Add(Selector.ForClass(className), new StyleProperties { BackgroundColor = Green })
            .Add(Selector.ForName("left"), new StyleProperties { BorderColor = Red })
            .Build());

        context.Update(0f);

        Assert.Equal(Green, slot.Style.BackgroundColor);
        Assert.Equal(Red, slot.Style.BorderColor);
        Assert.NotEqual(Green, sibling.Style.BackgroundColor);
        Assert.NotEqual(Red, sibling.Style.BorderColor);
    }

    [Fact]
    public void TheLayoutPassPlacesTheMountedRootsInTheirOrder()
    {
        UiContext context = CreateContext();
        Element layoutRoot = CreateLayoutRoot(context);
        SlotContainer slot = new("left"u8);
        slot.Inline.FlexDirection = FlexDirection.Column;
        layoutRoot.Children.Add(slot);
        Element a = CreateRoot(20);
        Element b = CreateRoot(30);
        Element c = CreateRoot(40);
        slot.Mount(a, 0);
        slot.Mount(b, 1);
        slot.Mount(c, 2);

        context.Update(0f);

        Assert.Equal(0f, a.Layout.Rect.Y, 0.01f);
        Assert.Equal(20f, b.Layout.Rect.Y, 0.01f);
        Assert.Equal(50f, c.Layout.Rect.Y, 0.01f);

        Assert.True(slot.Unmount(b));
        context.Update(0f);

        Assert.Equal(0f, a.Layout.Rect.Y, 0.01f);
        Assert.Equal(20f, c.Layout.Rect.Y, 0.01f);

        slot.Mount(b, 0);
        context.Update(0f);

        Assert.Equal(0f, b.Layout.Rect.Y, 0.01f);
        Assert.Equal(30f, a.Layout.Rect.Y, 0.01f);
        Assert.Equal(50f, c.Layout.Rect.Y, 0.01f);
    }

    [Fact]
    public void APanelThatMovesToAnotherSlotTakesTheStyleOfTheNewSlot()
    {
        UiContext context = CreateContext();
        Element layoutRoot = CreateLayoutRoot(context);
        SlotContainer left = new("left"u8);
        SlotContainer right = new("right"u8);
        ClassId leftClass = ClassName.Intern(nameof(APanelThatMovesToAnotherSlotTakesTheStyleOfTheNewSlot) + ".left");
        ClassId rightClass = ClassName.Intern(nameof(APanelThatMovesToAnotherSlotTakesTheStyleOfTheNewSlot) + ".right");
        left.Classes.Add(leftClass);
        right.Classes.Add(rightClass);
        layoutRoot.Children.Add(left);
        layoutRoot.Children.Add(right);

        Element panelRoot = new();
        Label label = new(default(Label.CreateInfo));
        panelRoot.Children.Add(label);
        ElementTypeId labelType = label.TypeId;
        context.AddSheet(new StyleSheet.Builder()
            .Add(
                Selector.Descendant(new CompoundSelector { Class = leftClass }, new CompoundSelector { Type = labelType }),
                new StyleProperties { Color = Gold })
            .Add(
                Selector.Descendant(new CompoundSelector { Class = rightClass }, new CompoundSelector { Type = labelType }),
                new StyleProperties { Color = Green })
            .Build());
        left.Mount(panelRoot, 0);

        context.Update(0f);

        Assert.Equal(Gold, label.Style.Color);

        // The unmount is the leave. The mount in the other slot is the enter. The join marks the style of the subtree.
        Assert.True(left.Unmount(panelRoot));
        right.Mount(panelRoot, 0);
        context.Update(0f);

        Assert.Equal(Green, label.Style.Color);
    }

    [Fact]
    public void AnUnmountedPanelKeepsItsBindingAndAppliesTheNewValueAfterTheMount()
    {
        UiContext context = CreateContext();
        Element layoutRoot = CreateLayoutRoot(context);
        SlotContainer slot = new("left"u8);
        layoutRoot.Children.Add(slot);
        Store<int> store = new(new Store<int>.CreateInfo { Initial = 1 });
        Element panelRoot = new();
        Label label = new Label(default(Label.CreateInfo)).BindText(store, static s => s);
        panelRoot.Children.Add(label);

        slot.Mount(panelRoot, 0);
        context.Update(0f);

        Assert.True(label.Text.SequenceEqual("1"u8));

        Assert.True(slot.Unmount(panelRoot));
        store.Set(2);
        context.Update(0f);

        Assert.True(label.Text.SequenceEqual("1"u8));

        slot.Mount(panelRoot, 0);
        context.Update(0f);

        Assert.True(label.Text.SequenceEqual("2"u8));
        Assert.Equal(1, label.BindingCount);
    }
}
