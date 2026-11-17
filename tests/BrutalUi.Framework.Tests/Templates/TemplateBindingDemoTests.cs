using System;
using System.Globalization;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using BrutalUi.Markup;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The phase 1 demo: the section 11 template with <c>bind-text="hud.score"</c> loads through a
/// <see cref="TemplateBindings"/>, a click on <c>pause</c> runs <c>game.pause</c>, and a store write changes the
/// label text.
/// </summary>
/// <remarks>
/// <para>
/// The template equals the template of <strong>The binding attributes.</strong> in section 11 of
/// <c>docs/technical-design.md</c>, word for word, with LF line ends. It is a <c>u8</c> literal, because the BRUTAL
/// analyzer rejects a string literal in <see cref="TemplateLoader.Load(string, string)"/>.
/// </para>
/// <para>
/// Each test builds a context with a <see cref="RecordingRenderer"/> measurer, so the text gives the button a size.
/// The tests pass the provider to <see cref="TemplateBindings.AddText{T, TValue}"/> and never change
/// <see cref="CultureInfo.CurrentCulture"/>.
/// </para>
/// </remarks>
public sealed class TemplateBindingDemoTests
{
    /// <summary>The handler that adds one to the counter target.</summary>
    private static readonly CommandHandler s_count = static target => ((Counter)target!).Calls++;

    /// <summary>Gets the section 11 template of <strong>The binding attributes.</strong>, word for word.</summary>
    private static ReadOnlySpan<byte> SectionElevenTemplate => """
        <ui:Element class="hud" xmlns:ui="brutal-ui">
          <ui:Label name="score" class="hud-value" text="0" bind-text="hud.score" />
          <ui:Button name="pause" class="hud-button" text="Pause" command="game.pause" />
        </ui:Element>
        """u8;

    /// <summary>
    /// The demo. The template loads with attached bindings. After the first update the label shows <c>0</c>. A press
    /// and a release at the center of <c>pause</c> call <c>game.pause</c> one time. A store write of the score 1500
    /// changes the label to <c>1,500</c> with the <c>N0</c> format and the invariant culture.
    /// </summary>
    [Fact]
    public void TheSectionElevenTemplateRunsTheCommandAndShowsTheStore()
    {
        UiContext context = CreateContext();
        CommandRegistry registry = new();
        Counter counter = new();
        registry.Register("game.pause"u8, s_count, counter);
        Store<BoundTree.HudState> store = new(new Store<BoundTree.HudState>.CreateInfo
        {
            Initial = new BoundTree.HudState(0, 0, 0f, false, string.Empty),
        });

        TemplateBindings bindings = new(registry);
        bindings.AddText("hud.score", store, static s => s.Score, "N0", CultureInfo.InvariantCulture);
        TemplateLoader loader = new();
        bindings.AttachTo(loader);
        Element root = AddLoadedRoot(context, loader);

        context.Update(0f);
        Label? score = root.Find<Label>("score"u8);
        Assert.NotNull(score);
        Assert.True(score.Text.SequenceEqual("0"u8));
        Assert.Equal(1, score.BindingCount);

        Button? pause = root.Find<Button>("pause"u8);
        Assert.NotNull(pause);
        Assert.True(pause.Layout.Rect.Z > 0f);
        float2 center = ThousandWidgetTree.ViewportCenter(pause);
        context.PushEvent(Pointer(UiEventType.PointerDown, center));
        context.PushEvent(Pointer(UiEventType.PointerUp, center));
        context.Update(0f);
        Assert.Equal(1, counter.Calls);

        BoundTree.HudState state = store.Value with { Score = 1500 };
        store.Set(in state);
        context.Update(0f);
        Assert.True(score.Text.SequenceEqual("1,500"u8));
    }

    /// <summary>
    /// The format and the provider come from the register call: the same key with <c>N0</c> and <c>de-DE</c> gives
    /// <c>1.500</c> for 1500.
    /// </summary>
    [Fact]
    public void TheFormatAndTheProviderComeFromTheRegisterCall()
    {
        UiContext context = CreateContext();
        Store<BoundTree.HudState> store = new(new Store<BoundTree.HudState>.CreateInfo
        {
            Initial = new BoundTree.HudState(0, 0, 0f, false, string.Empty),
        });

        TemplateBindings bindings = new(new CommandRegistry());
        bindings.AddText("hud.score", store, static s => s.Score, "N0", CultureInfo.GetCultureInfo("de-DE"));
        TemplateLoader loader = new();
        bindings.AttachTo(loader);
        Element root = AddLoadedRoot(context, loader);

        context.Update(0f);
        BoundTree.HudState state = store.Value with { Score = 1500 };
        store.Set(in state);
        context.Update(0f);

        Label? score = root.Find<Label>("score"u8);
        Assert.NotNull(score);
        Assert.True(score.Text.SequenceEqual("1.500"u8));
    }

    /// <summary>
    /// <c>command</c> checks nothing at load: the load runs with no <c>game.pause</c> in the registry, the test
    /// registers it after the load, and a click runs it.
    /// </summary>
    [Fact]
    public void ACommandThatTheGameRegistersAfterTheLoadRuns()
    {
        UiContext context = CreateContext();
        CommandRegistry registry = new();
        Counter counter = new();
        Store<BoundTree.HudState> store = new(new Store<BoundTree.HudState>.CreateInfo
        {
            Initial = new BoundTree.HudState(0, 0, 0f, false, string.Empty),
        });

        TemplateBindings bindings = new(registry);
        bindings.AddText("hud.score", store, static s => s.Score, "N0", CultureInfo.InvariantCulture);
        TemplateLoader loader = new();
        bindings.AttachTo(loader);
        Element root = AddLoadedRoot(context, loader);

        context.Update(0f);
        registry.Register("game.pause"u8, s_count, counter);

        Button? pause = root.Find<Button>("pause"u8);
        Assert.NotNull(pause);
        float2 center = ThousandWidgetTree.ViewportCenter(pause);
        context.PushEvent(Pointer(UiEventType.PointerDown, center));
        context.PushEvent(Pointer(UiEventType.PointerUp, center));
        context.Update(0f);

        Assert.Equal(1, counter.Calls);
    }

    /// <summary>
    /// An add after <see cref="TemplateBindings.AttachTo"/> serves the next load: <c>AttachTo</c> runs first,
    /// <c>AddText</c> second, and the load binds the label. The store starts at 7, so the shown <c>7</c> proves the
    /// binding, not the template text <c>0</c>.
    /// </summary>
    [Fact]
    public void AnAddAfterAttachToServesTheNextLoad()
    {
        UiContext context = CreateContext();
        Store<BoundTree.HudState> store = new(new Store<BoundTree.HudState>.CreateInfo
        {
            Initial = new BoundTree.HudState(7, 0, 0f, false, string.Empty),
        });

        TemplateBindings bindings = new(new CommandRegistry());
        TemplateLoader loader = new();
        bindings.AttachTo(loader);
        bindings.AddText("hud.score", store, static s => s.Score, "N0", CultureInfo.InvariantCulture);
        Element root = AddLoadedRoot(context, loader);

        context.Update(0f);
        Label? score = root.Find<Label>("score"u8);
        Assert.NotNull(score);
        Assert.Equal(1, score.BindingCount);
        Assert.True(score.Text.SequenceEqual("7"u8));
    }

    /// <summary>Loads the template and adds its root, sized to the viewport, to a context.</summary>
    /// <param name="context">The context.</param>
    /// <param name="loader">The loader with the attached bindings.</param>
    /// <returns>The loaded root.</returns>
    private static Element AddLoadedRoot(UiContext context, TemplateLoader loader)
    {
        Element root = loader.Load(SectionElevenTemplate, "hud.xml").GetRootOrThrow();
        root.Inline.Width = 400f;
        root.Inline.Height = 300f;
        context.AddRoot(root);
        return root;
    }

    /// <summary>Creates a context with a measurer, a default font size of 16, and a viewport of 400 by 300.</summary>
    /// <remarks>
    /// <see cref="UiContext.CreateInfo.DefaultFontSize"/> of 16 gives the text a size with no stylesheet, so the
    /// button gets its size from the text (a byte is 8 pixels wide in the measurer).
    /// </remarks>
    /// <returns>The context.</returns>
    private static UiContext CreateContext()
    {
        return new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer(), DefaultFontSize = 16f })
        {
            ViewportSize = new float2(400, 300),
        };
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
