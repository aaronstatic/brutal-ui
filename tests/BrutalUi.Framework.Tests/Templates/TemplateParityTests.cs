using System;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The parity of a loaded tree with the C# tree: the template of <see cref="BoundTemplate"/> and its C# twin make the
/// same tree, and one click on each <c>pause</c> button runs the command one time for each tree.
/// </summary>
/// <remarks>
/// <para>
/// Both trees bind the same <see cref="Store{T}"/> and the same <see cref="CommandRegistry"/>, so one <c>Set</c> of
/// <see cref="BoundTree.Next(int)"/> drives both trees. Each context applies its own tree in its own
/// <see cref="UiContext.Update(float)"/> call.
/// </para>
/// <para>
/// <see cref="TreeCompare"/> compares no <see cref="Element.BindingCount"/> and no checked state of a toggle, so the
/// test compares those two members by hand. A <c>bind-text</c> addition is invisible to a tree compare, and a
/// <c>command</c> handler counts like <see cref="Button.Clicked"/>. Both trees therefore use
/// <see cref="CommandBindings.BindCommand"/> for <c>game.pause</c>.
/// </para>
/// <para>
/// Each context uses a <see cref="RecordingRenderer"/> measurer and a default font size of 16, so a loaded leaf gets a
/// real height and a click hits its button. Each root lays out as a column of 400 by 300, so each child keeps its own
/// row.
/// </para>
/// </remarks>
public sealed class TemplateParityTests
{
    /// <summary>The handler that adds one to the counter target.</summary>
    private static readonly CommandHandler s_count = static target => ((Counter)target!).Calls++;

    /// <summary>
    /// After each of 10 store versions the two trees compare equal, each bound name holds the same binding count, and
    /// the two toggles show the checked state of the store.
    /// </summary>
    [Fact]
    public void TheTemplateTreeEqualsTheCodeTreeAfterEachStoreVersion()
    {
        CommandRegistry registry = new();
        Store<BoundTree.HudState> store = CreateStore();

        UiContext codeContext = CreateContext();
        Element code = AddRoot(codeContext, BoundTemplate.BuildCode(store, registry));
        UiContext loadedContext = CreateContext();
        Element loaded = AddRoot(loadedContext, BoundTemplate.Load(store, registry));

        for (int round = 1; round <= 10; round++)
        {
            BoundTree.HudState state = BoundTree.Next(round);
            store.Set(in state);
            codeContext.Update(0.016f);
            loadedContext.Update(0.016f);

            Assert.Null(TreeCompare.FirstDifference(code, loaded));
            AssertSameBindingCounts(code, loaded);
            AssertSameCheckedState(code, loaded, state);
        }
    }

    /// <summary>One click on each <c>pause</c> button calls <c>game.pause</c> two times in total.</summary>
    [Fact]
    public void AClickOnEachTreeRunsTheCommandOneTime()
    {
        CommandRegistry registry = new();
        Counter counter = new();
        registry.Register("game.pause"u8, s_count, counter);
        Store<BoundTree.HudState> store = CreateStore();

        UiContext codeContext = CreateContext();
        Element code = AddRoot(codeContext, BoundTemplate.BuildCode(store, registry));
        UiContext loadedContext = CreateContext();
        Element loaded = AddRoot(loadedContext, BoundTemplate.Load(store, registry));

        codeContext.Update(0.016f);
        loadedContext.Update(0.016f);

        ClickPause(codeContext, code);
        Assert.Equal(1, counter.Calls);

        ClickPause(loadedContext, loaded);
        Assert.Equal(2, counter.Calls);
    }

    /// <summary>Creates the store of the HUD state at round 0.</summary>
    /// <returns>The store.</returns>
    private static Store<BoundTree.HudState> CreateStore()
    {
        return new Store<BoundTree.HudState>(new Store<BoundTree.HudState>.CreateInfo
        {
            Initial = BoundTree.Next(0),
        });
    }

    /// <summary>Checks that each bound name holds the same <see cref="Element.BindingCount"/> in both trees.</summary>
    /// <param name="code">The root of the C# tree.</param>
    /// <param name="loaded">The root of the loaded tree.</param>
    private static void AssertSameBindingCounts(Element code, Element loaded)
    {
        AssertBindingCount(code, loaded, "score"u8, 1);
        AssertBindingCount(code, loaded, "volume"u8, 1);
        AssertBindingCount(code, loaded, "player"u8, 1);
        AssertBindingCount(code, loaded, "name-field"u8, 1);
        AssertBindingCount(code, loaded, "mute"u8, 1);
        AssertBindingCount(code, loaded, "volume-slider"u8, 1);
        AssertBindingCount(code, loaded, "pause"u8, 0);
    }

    /// <summary>Compares the binding count of one name in the two trees.</summary>
    /// <param name="code">The root of the C# tree.</param>
    /// <param name="loaded">The root of the loaded tree.</param>
    /// <param name="name">The UTF-8 name of the element.</param>
    /// <param name="count">The binding count that both elements must hold.</param>
    private static void AssertBindingCount(Element code, Element loaded, ReadOnlySpan<byte> name, int count)
    {
        Element? codeElement = code.Find<Element>(name);
        Element? loadedElement = loaded.Find<Element>(name);
        Assert.NotNull(codeElement);
        Assert.NotNull(loadedElement);

        Assert.Equal(count, codeElement.BindingCount);
        Assert.Equal(count, loadedElement.BindingCount);
    }

    /// <summary>
    /// Checks the checked state by hand. <see cref="TreeCompare"/> reads no member of a toggle but its slots, so a
    /// wrong <c>bind-checked</c> selector is invisible to a tree compare.
    /// </summary>
    /// <param name="code">The root of the C# tree.</param>
    /// <param name="loaded">The root of the loaded tree.</param>
    /// <param name="state">The state of the store for this round.</param>
    private static void AssertSameCheckedState(Element code, Element loaded, in BoundTree.HudState state)
    {
        Toggle? codeMute = code.Find<Toggle>("mute"u8);
        Toggle? loadedMute = loaded.Find<Toggle>("mute"u8);
        Assert.NotNull(codeMute);
        Assert.NotNull(loadedMute);

        Assert.Equal(state.Muted, codeMute.Checked);
        Assert.Equal(codeMute.Checked, loadedMute.Checked);
    }

    /// <summary>Clicks the <c>pause</c> button of one tree and runs one update.</summary>
    /// <param name="context">The context that holds the tree.</param>
    /// <param name="root">The root of the tree.</param>
    private static void ClickPause(UiContext context, Element root)
    {
        Button? pause = root.Find<Button>("pause"u8);
        Assert.NotNull(pause);
        Assert.True(pause.Layout.Rect.Z > 0f);

        float2 center = ThousandWidgetTree.ViewportCenter(pause);
        context.PushEvent(Pointer(UiEventType.PointerDown, center));
        context.PushEvent(Pointer(UiEventType.PointerUp, center));
        context.Update(0.016f);
    }

    /// <summary>Creates a context with a measurer, a default font size of 16, and a viewport of 400 by 300.</summary>
    /// <returns>The context.</returns>
    private static UiContext CreateContext()
    {
        return new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer(), DefaultFontSize = 16f })
        {
            ViewportSize = new float2(400, 300),
        };
    }

    /// <summary>Adds a root, sized to the viewport and laid out as a column, to a context.</summary>
    /// <param name="context">The context.</param>
    /// <param name="root">The root of the tree.</param>
    /// <returns>The same root.</returns>
    private static Element AddRoot(UiContext context, Element root)
    {
        root.Inline.FlexDirection = FlexDirection.Column;
        root.Inline.Width = 400f;
        root.Inline.Height = 300f;
        context.AddRoot(root);
        return root;
    }

    /// <summary>Creates a left-button pointer event with no target.</summary>
    /// <param name="type"><see cref="UiEventType.PointerDown"/> or <see cref="UiEventType.PointerUp"/>.</param>
    /// <param name="position">The position in viewport pixels.</param>
    /// <returns>The event.</returns>
    private static PointerEvent Pointer(UiEventType type, float2 position)
    {
        return new PointerEvent
        {
            Header = new UiEventHeader(type, 0d, null),
            Position = position,
            Button = PointerButton.Left,
            Modifiers = UiModifiers.None,
        };
    }

    /// <summary>The target of the command handler: a call counter.</summary>
    private sealed class Counter
    {
        /// <summary>The count of calls.</summary>
        public int Calls;
    }
}
