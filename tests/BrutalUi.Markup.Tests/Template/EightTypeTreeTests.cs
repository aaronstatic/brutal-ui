using System;
using System.Collections.Generic;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Markup.Tests.Template;

/// <summary>
/// The phase 2 proof: one template with each built-in type and each row equals the builder tree. The order of the
/// attributes in the source does not change the tree.
/// </summary>
/// <remarks>
/// <para>
/// The builder tree uses the fluent methods of the Builder API and the create-info fields for
/// <c>Capacity</c> and <c>ScrollSpeed</c>. The <see cref="Slider.Range"/> call runs before
/// <see cref="Slider.Value"/>, as the loader runs the factory before the set rows.
/// </para>
/// <para>
/// The rows of <c>text-key</c> come in phase 5, so no template here holds that attribute.
/// </para>
/// </remarks>
public sealed class EightTypeTreeTests
{
    /// <summary>The icon handle that both trees read under the name <c>icon</c>.</summary>
    private static readonly TextureHandle Icon = new(7, new int2(16, 16));

    /// <summary>The tint that both trees read.</summary>
    private static readonly Rgba8 Tint = new(0xff, 0x00, 0x00, 0x80);

    /// <summary>Gets a template with each built-in type and each of its rows, in table order.</summary>
    private static ReadOnlySpan<byte> EightTypeTemplate => """
        <ui:Element xmlns:ui="brutal-ui" name="root" class="hud panel" pickable="false" focusable="true">
          <ui:Label name="score" text="Score" />
          <ui:Button name="pause" text="Pause" />
          <ui:Toggle name="sound" checked="true" />
          <ui:Slider name="level" min="0" max="10" step="1" value="7" />
          <ui:TextField name="player" capacity="8" text="abc" />
          <ui:ScrollView name="list" scroll-speed="60" scroll-offset="0 30" />
          <ui:Image name="icon" texture="icon" uv="0 0 0.5 0.5" tint="#ff000080" scale-mode="scale-to-fit" />
        </ui:Element>
        """u8;

    /// <summary>
    /// Gets the same template with the attributes of each element in the reverse order. The value of each
    /// attribute stays the same, because the order inside a class list is part of the value.
    /// </summary>
    private static ReadOnlySpan<byte> ReversedTemplate => """
        <ui:Element focusable="true" pickable="false" class="hud panel" name="root" xmlns:ui="brutal-ui">
          <ui:Label text="Score" name="score" />
          <ui:Button text="Pause" name="pause" />
          <ui:Toggle checked="true" name="sound" />
          <ui:Slider value="7" step="1" max="10" min="0" name="level" />
          <ui:TextField text="abc" capacity="8" name="player" />
          <ui:ScrollView scroll-offset="0 30" scroll-speed="60" name="list" />
          <ui:Image scale-mode="scale-to-fit" tint="#ff000080" uv="0 0 0.5 0.5" texture="icon" name="icon" />
        </ui:Element>
        """u8;

    /// <summary>The loaded eight-type tree equals the tree that the builder API makes.</summary>
    [Fact]
    public void AnEightTypeTemplateEqualsTheBuilderTree()
    {
        Element actual = Load(EightTypeTemplate);
        Element expected = ExpectedTree();

        Assert.Null(TreeCompare.FirstDifference(expected, actual));
    }

    /// <summary>The loader runs the rows in table order, so the source order of the attributes changes nothing.</summary>
    [Fact]
    public void TheAttributeOrderOfTheSourceDoesNotChangeTheTree()
    {
        Element first = Load(EightTypeTemplate);
        Element second = Load(ReversedTemplate);

        Assert.Null(TreeCompare.FirstDifference(first, second));
    }

    /// <summary>Loads one template with the texture map that holds <c>icon</c>.</summary>
    private static Element Load(ReadOnlySpan<byte> utf8)
    {
        Dictionary<string, TextureHandle> textures = new(StringComparer.Ordinal) { ["icon"] = Icon };
        TemplateLoader loader = new(new TemplateLoader.CreateInfo { Textures = textures });
        return loader.Load(utf8, "eight.xml").GetRootOrThrow();
    }

    /// <summary>Builds the tree that equals the template: the fluent methods and the create-info fields.</summary>
    private static Element ExpectedTree()
    {
        return new Element()
            .Name("root")
            .Class("hud")
            .Class("panel")
            .Pickable(false)
            .Focusable(true)
            .Child(new Label(default).Name("score").Text("Score"u8))
            .Child(new Button(default).Name("pause").Text("Pause"u8))
            .Child(new Toggle(default).Name("sound").Checked(true))
            .Child(new Slider(new Slider.CreateInfo { Maximum = 10f }).Name("level").Range(0f, 10f, 1f).Value(7f))
            .Child(new TextField(new TextField.CreateInfo { Capacity = 8 }).Name("player").Text("abc"u8))
            .Child(new ScrollView(new ScrollView.CreateInfo { ScrollSpeed = 60f }).Name("list").ScrollOffset(new float2(0f, 30f)))
            .Child(new Image(new Image.CreateInfo { Texture = Icon }).Name("icon").Uv(new float4(0f, 0f, 0.5f, 0.5f)).Tint(Tint).ScaleMode(ImageScaleMode.ScaleToFit));
    }
}
