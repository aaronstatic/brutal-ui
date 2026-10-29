using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The tests of <see cref="LabelBindings.BindText{T, TValue}(Label, Store{T}, Func{T, TValue}, string?, IFormatProvider?)"/>:
/// the first apply, the apply after a change, the slice rule, and the argument checks.
/// </summary>
/// <remarks>
/// Each test uses a context with a <see cref="RecordingRenderer"/> measurer, so the update can measure the label. Each
/// selector is a <see langword="static"/> lambda, as a game uses it.
/// </remarks>
public sealed class LabelBindingTests
{
    /// <summary>The state of the test HUD: two number slices.</summary>
    /// <param name="Score">The score.</param>
    /// <param name="Lives">The count of lives.</param>
    private readonly record struct HudState(int Score, int Lives);

    /// <summary>The state of the format tests: one float slice.</summary>
    /// <param name="Ratio">The ratio.</param>
    private readonly record struct RatioState(float Ratio);

    /// <summary>The state of the string tests: one string slice.</summary>
    /// <param name="Title">The title. It can be <see langword="null"/>.</param>
    private readonly record struct TitleState(string? Title);

    /// <summary>The state of the reference slice test: one slice of a reference type.</summary>
    /// <param name="Score">
    /// The score. The type is not nullable, because the constraint of the bind method rejects a nullable type. The test
    /// stores <see langword="null"/> with the null-forgiving operator.
    /// </param>
    private readonly record struct RefState(RefScore Score);

    /// <summary>A reference type that formats as the text <c>R</c>.</summary>
    private sealed class RefScore : IUtf8SpanFormattable
    {
        /// <inheritdoc />
        public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            if (utf8Destination.IsEmpty)
            {
                bytesWritten = 0;
                return false;
            }

            utf8Destination[0] = (byte)'R';
            bytesWritten = 1;
            return true;
        }
    }

    /// <summary>The demo: a bound label shows the score of the store after the first update.</summary>
    [Fact]
    public void ABoundLabelShowsTheScoreAfterTheFirstUpdate()
    {
        UiContext context = CreateContext(new RecordingRenderer());
        Element root = AddRoot(context);
        Store<HudState> store = CreateStore(42, 3);
        Label label = new(default(Label.CreateInfo));
        root.Children.Add(label);

        Label returned = label.BindText(store, static s => s.Score);

        Assert.Same(label, returned);
        Assert.Equal(1, label.BindingCount);
        Assert.True(label.Text.IsEmpty);

        context.Update(0f);

        AssertText("42"u8, label);
    }

    /// <summary>
    /// A set of a new score changes the text at the next update, not before it. A score of 0 shows <c>0</c>.
    /// </summary>
    [Fact]
    public void ASetOfANewScoreChangesTheTextAtTheNextUpdate()
    {
        UiContext context = CreateContext(new RecordingRenderer());
        Element root = AddRoot(context);
        Store<HudState> store = CreateStore(42, 3);
        Label label = new Label(default(Label.CreateInfo)).BindText(store, static s => s.Score);
        root.Children.Add(label);
        context.Update(0f);

        store.Set(store.Value with { Score = 1250 });

        AssertText("42"u8, label);

        context.Update(0f);

        AssertText("1250"u8, label);

        // A score of 0 is the default of int, not a null slice. It shows "0", not an empty text.
        store.Set(store.Value with { Score = 0 });
        context.Update(0f);

        AssertText("0"u8, label);
    }

    /// <summary>Two sets before one update show the last score.</summary>
    [Fact]
    public void TwoSetsBeforeOneUpdateShowTheLastScore()
    {
        UiContext context = CreateContext(new RecordingRenderer());
        Element root = AddRoot(context);
        Store<HudState> store = CreateStore(42, 3);
        Label label = new Label(default(Label.CreateInfo)).BindText(store, static s => s.Score);
        root.Children.Add(label);
        context.Update(0f);

        store.Set(store.Value with { Score = 7 });
        store.Set(store.Value with { Score = 98765 });
        context.Update(0f);

        AssertText("98765"u8, label);
    }

    /// <summary>
    /// A change of another slice raises the version, and the binding formats the same bytes. The label keeps its text
    /// and marks nothing, so the update measures no text again. A change of the bound slice then measures again.
    /// </summary>
    [Fact]
    public void AChangeOfAnotherSliceKeepsTheText()
    {
        RecordingRenderer measurer = new();
        UiContext context = CreateContext(measurer);
        Element root = AddRoot(context);
        Store<HudState> store = CreateStore(42, 3);
        Label label = new Label(default(Label.CreateInfo)).BindText(store, static s => s.Score);
        root.Children.Add(label);
        context.Update(0f);
        context.Update(0f);
        int measureCount = measurer.MeasureCount;
        ulong version = store.Version;

        store.Set(store.Value with { Lives = 2 });
        context.Update(0f);

        Assert.Equal(version + 1, store.Version);
        AssertText("42"u8, label);
        Assert.Equal(measureCount, measurer.MeasureCount);

        // The control: a change of the bound slice changes the text, and the update measures the label again.
        store.Set(store.Value with { Score = 43 });
        context.Update(0f);

        AssertText("43"u8, label);
        Assert.True(measurer.MeasureCount > measureCount);
    }

    /// <summary>Two labels on one store show their own slices, and a change of one slice changes one label.</summary>
    [Fact]
    public void TwoLabelsOnOneStoreShowTheirOwnSlices()
    {
        UiContext context = CreateContext(new RecordingRenderer());
        Element root = AddRoot(context);
        Store<HudState> store = CreateStore(42, 3);
        Label score = new Label(default(Label.CreateInfo)).BindText(store, static s => s.Score);
        Label lives = new Label(default(Label.CreateInfo)).BindText(store, static s => s.Lives);
        root.Children.Add(score);
        root.Children.Add(lives);

        context.Update(0f);

        AssertText("42"u8, score);
        AssertText("3"u8, lives);

        store.Set(store.Value with { Score = 100 });
        context.Update(0f);

        AssertText("100"u8, score);
        AssertText("3"u8, lives);
    }

    /// <summary>The bind method rejects a null store or a null selector, and the label then holds no binding.</summary>
    [Fact]
    public void BindTextRejectsANullStoreOrSelector()
    {
        Store<HudState> store = CreateStore(42, 3);
        Label label = new(default(Label.CreateInfo));

        Assert.Throws<ArgumentNullException>(
            "store",
            () => label.BindText<HudState, int>(null!, static s => s.Score));
        Assert.Throws<ArgumentNullException>(
            "selector",
            () => label.BindText<HudState, int>(store, null!));
        Assert.Equal(0, label.BindingCount);
    }

    /// <summary>
    /// A label bound to a string slice shows the string after the first update and the new string after a set. The
    /// <c>string?</c> selector picks the string overload, and the <c>int</c> selector of the other tests still picks the
    /// number overload.
    /// </summary>
    [Fact]
    public void ALabelShowsAStringSlice()
    {
        UiContext context = CreateContext(new RecordingRenderer());
        Element root = AddRoot(context);
        Store<TitleState> store = CreateTitleStore("Level 1");
        Label label = new(default(Label.CreateInfo));
        root.Children.Add(label);

        Label returned = label.BindText(store, static s => s.Title);

        Assert.Same(label, returned);
        Assert.Equal(1, label.BindingCount);
        Assert.True(label.Text.IsEmpty);

        context.Update(0f);

        AssertText("Level 1"u8, label);

        store.Set(store.Value with { Title = "Level 2" });
        context.Update(0f);

        AssertText("Level 2"u8, label);
    }

    /// <summary>
    /// A null string slice gives an empty text, also after a text. A second step binds a number handler to a reference
    /// slice. A null slice of that type also gives an empty text.
    /// </summary>
    [Fact]
    public void ANullStringSliceGivesAnEmptyText()
    {
        UiContext context = CreateContext(new RecordingRenderer());
        Element root = AddRoot(context);
        Store<TitleState> store = CreateTitleStore(null);
        Label label = new Label(default(Label.CreateInfo)).BindText(store, static s => s.Title);
        root.Children.Add(label);

        context.Update(0f);

        Assert.True(label.Text.IsEmpty);

        store.Set(store.Value with { Title = "Menu" });
        context.Update(0f);

        AssertText("Menu"u8, label);

        store.Set(store.Value with { Title = null });
        context.Update(0f);

        Assert.True(label.Text.IsEmpty);

        // A null slice of a reference type that implements IUtf8SpanFormattable gives an empty text, not a throw.
        UiContext refContext = CreateContext(new RecordingRenderer());
        Element refRoot = AddRoot(refContext);
        Store<RefState> refStore = new(new Store<RefState>.CreateInfo { Initial = new RefState(new RefScore()) });
        Label refLabel = new Label(default(Label.CreateInfo)).BindText(refStore, static s => s.Score);
        refRoot.Children.Add(refLabel);

        refContext.Update(0f);

        AssertText("R"u8, refLabel);

        refStore.Set(new RefState(null!));
        refContext.Update(0f);

        Assert.True(refLabel.Text.IsEmpty);

        refStore.Set(new RefState(new RefScore()));
        refContext.Update(0f);

        AssertText("R"u8, refLabel);
    }

    /// <summary>A format string and the invariant culture format a float slice with two decimals.</summary>
    [Fact]
    public void AFormatStringFormatsTheNumber()
    {
        UiContext context = CreateContext(new RecordingRenderer());
        Element root = AddRoot(context);
        Store<RatioState> store = new(new Store<RatioState>.CreateInfo { Initial = new RatioState(3.14159f) });
        Label label = new Label(default(Label.CreateInfo))
            .BindText(store, static s => s.Ratio, "F2", CultureInfo.InvariantCulture);
        root.Children.Add(label);

        context.Update(0f);

        AssertText("3.14"u8, label);
    }

    /// <summary>
    /// A provider with its own decimal separator formats the number. A second step binds a label with no provider while
    /// the current culture of the thread has the decimal separator <c>,</c>. The label then shows <c>3,14</c>.
    /// </summary>
    [Fact]
    public void AProviderFormatsTheNumber()
    {
        UiContext context = CreateContext(new RecordingRenderer());
        Element root = AddRoot(context);
        NumberFormatInfo provider = (NumberFormatInfo)NumberFormatInfo.InvariantInfo.Clone();
        provider.NumberDecimalSeparator = ",";
        Store<RatioState> store = new(new Store<RatioState>.CreateInfo { Initial = new RatioState(3.14159f) });
        Label label = new Label(default(Label.CreateInfo)).BindText(store, static s => s.Ratio, "F2", provider);
        root.Children.Add(label);

        context.Update(0f);

        AssertText("3,14"u8, label);

        // A null provider gives the current culture. The culture is local to the thread, and the finally block restores it.
        CultureInfo oldCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo comma = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            comma.NumberFormat.NumberDecimalSeparator = ",";
            CultureInfo.CurrentCulture = comma;

            UiContext cultureContext = CreateContext(new RecordingRenderer());
            Element cultureRoot = AddRoot(cultureContext);
            Store<RatioState> cultureStore = new(new Store<RatioState>.CreateInfo { Initial = new RatioState(3.14159f) });
            Label cultureLabel = new Label(default(Label.CreateInfo)).BindText(cultureStore, static s => s.Ratio, "F2");
            cultureRoot.Children.Add(cultureLabel);

            cultureContext.Update(0f);

            AssertText("3,14"u8, cultureLabel);
        }
        finally
        {
            CultureInfo.CurrentCulture = oldCulture;
        }
    }

    /// <summary>
    /// A text above the 32 bytes of the start scratch grows the buffer. A second step uses a new context and the format
    /// <c>D5000</c>. The text needs more than 4096 bytes, so the update throws <see cref="InvalidOperationException"/>.
    /// </summary>
    [Fact]
    public void ALongTextGrowsTheBuffer()
    {
        UiContext context = CreateContext(new RecordingRenderer());
        Element root = AddRoot(context);
        Store<HudState> store = CreateStore(42, 3);
        Label label = new Label(default(Label.CreateInfo))
            .BindText(store, static s => s.Score, "D40", CultureInfo.InvariantCulture);
        root.Children.Add(label);

        context.Update(0f);

        AssertText("0000000000000000000000000000000000000042"u8, label);
        Assert.Equal(40, label.Text.Length);

        // The limit: a text over 4096 bytes throws in the update. A new context keeps the throw away from the step above.
        UiContext limitContext = CreateContext(new RecordingRenderer());
        Element limitRoot = AddRoot(limitContext);
        Store<HudState> limitStore = CreateStore(42, 3);
        Label limitLabel = new Label(default(Label.CreateInfo))
            .BindText(limitStore, static s => s.Score, "D5000", CultureInfo.InvariantCulture);
        limitRoot.Children.Add(limitLabel);

        Assert.Throws<InvalidOperationException>(() => limitContext.Update(0f));
    }

    /// <summary>
    /// A store keeps no reference to a bound label. After the label leaves its context and the collector runs, the
    /// weak reference is dead while the store is alive.
    /// </summary>
    [Fact]
    public void AStoreKeepsNoReferenceToADroppedLabel()
    {
        Store<HudState> store = CreateStore(42, 3);
        WeakReference weak = BuildDroppedLabel(store);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(weak.IsAlive);
        GC.KeepAlive(store);
    }

    /// <summary>
    /// Builds a context with a bound label, runs one update, removes the root, and returns a weak reference to the
    /// label. The method does not inline, so no local keeps the label alive.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <returns>The weak reference to the label.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference BuildDroppedLabel(Store<HudState> store)
    {
        UiContext context = CreateContext(new RecordingRenderer());
        Element root = AddRoot(context);
        Label label = new Label(default(Label.CreateInfo)).BindText(store, static s => s.Score);
        root.Children.Add(label);
        context.Update(0f);
        AssertText("42"u8, label);

        context.RemoveRoot(root);
        return new WeakReference(label);
    }

    /// <summary>Creates a store with a title.</summary>
    /// <param name="title">The start title.</param>
    /// <returns>The store at version 1.</returns>
    private static Store<TitleState> CreateTitleStore(string? title)
    {
        return new Store<TitleState>(new Store<TitleState>.CreateInfo { Initial = new TitleState(title) });
    }

    /// <summary>Creates a store with a score and a count of lives.</summary>
    /// <param name="score">The start score.</param>
    /// <param name="lives">The start count of lives.</param>
    /// <returns>The store at version 1.</returns>
    private static Store<HudState> CreateStore(int score, int lives)
    {
        return new Store<HudState>(new Store<HudState>.CreateInfo { Initial = new HudState(score, lives) });
    }

    /// <summary>Creates a context with a measurer and a viewport of 400 by 300.</summary>
    /// <param name="measurer">The measurer.</param>
    /// <returns>The context.</returns>
    private static UiContext CreateContext(RecordingRenderer measurer)
    {
        return new UiContext(new UiContext.CreateInfo { Measurer = measurer }) { ViewportSize = new float2(400, 300) };
    }

    /// <summary>Adds a root with <see cref="Align.FlexStart"/> to a context. A child then takes the width of its content.</summary>
    /// <param name="context">The context.</param>
    /// <returns>The root.</returns>
    private static Element AddRoot(UiContext context)
    {
        Element root = new();
        root.Inline.AlignItems = Align.FlexStart;
        context.AddRoot(root);
        return root;
    }

    /// <summary>Checks the UTF-8 text of a label.</summary>
    /// <param name="expected">The expected bytes.</param>
    /// <param name="label">The label.</param>
    private static void AssertText(ReadOnlySpan<byte> expected, Label label)
    {
        Assert.Equal(expected.ToArray(), label.Text.ToArray());
    }
}
