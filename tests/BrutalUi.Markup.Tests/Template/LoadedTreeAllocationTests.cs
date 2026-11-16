using System;
using System.Collections.Generic;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Markup.Tests.Template;

/// <summary>
/// The allocation gate of a loaded tree: after warm-up, <c>Update</c> and <c>Draw</c> on a template with each
/// built-in type must allocate 0 bytes.
/// </summary>
/// <remarks>
/// <para>
/// The scene is one template: a root, six panels, and in each panel one <see cref="Label"/>,
/// <see cref="Button"/>, <see cref="Toggle"/>, <see cref="Slider"/>, <see cref="TextField"/>, and
/// <see cref="Image"/> with the texture <c>icon</c>, plus one <see cref="ScrollView"/> with one
/// <see cref="Label"/> child. The test sets the inline fill of each panel after the load, so each frame holds a rect
/// call. The markup author sets no inline style in a template (Q5); the panel code may.
/// </para>
/// <para>
/// Each step toggles the hover state of one panel, writes the value of one slider, sets the text of one label from
/// one of two constants, then runs <c>Update</c> and <c>Draw</c>. The class table is global and xUnit runs the test
/// classes in parallel, so the scene interns its own class names.
/// </para>
/// </remarks>
public sealed class LoadedTreeAllocationTests
{
    private const int WarmUpSteps = 16;
    private const int MeasuredSteps = 100;
    private const int PanelCount = 6;
    private const float StepTime = 0.016f;
    private const float LowValue = 3f;
    private const float HighValue = 7f;

    private static readonly Rgba8 PanelFill = new(0x30, 0x60, 0xC0, 255);
    private static readonly TextureHandle Icon = new(7, new int2(16, 16));

    /// <summary>The two text constants that the steps write in turn. Each holds five bytes.</summary>
    private static ReadOnlySpan<byte> TextA => "alpha"u8;

    /// <summary>Gets the second text constant.</summary>
    private static ReadOnlySpan<byte> TextB => "omega"u8;

    /// <summary>Runs 16 warm-up steps, then measures 100 steps of <c>Update</c> and <c>Draw</c> at 0 bytes.</summary>
    [Fact]
    public void ALoadedTreeAllocatesNothingInUpdateAndDraw()
    {
        GateScene scene = GateScene.Build();

        // Warm-up: the steps let the JIT compile the load, the passes, the walk, and the paint hooks.
        for (int i = 0; i < WarmUpSteps; i++)
        {
            scene.Step();
        }

        // The release empties the allocation context, so a background GC cannot add bytes. See AllocationGate.
        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < MeasuredSteps; i++)
        {
            scene.Step();
        }

        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    /// <summary>Checks that the measured steps do real work, so an empty body cannot pass the gate.</summary>
    [Fact]
    public void TheMeasuredStepsDoRealWork()
    {
        GateScene scene = GateScene.Build();
        for (int i = 0; i < WarmUpSteps + MeasuredSteps; i++)
        {
            scene.Step();
        }

        Assert.Equal(WarmUpSteps + MeasuredSteps, scene.StepCount);

        bool sawText = false;
        bool sawImage = false;
        bool sawRect = false;
        foreach (ref readonly DrawCall call in scene.Renderer.Calls)
        {
            sawText |= call.Kind == DrawCallKind.Text;
            sawRect |= call.Kind == DrawCallKind.Rect;
            sawImage |= call.Kind == DrawCallKind.Image && call.Texture.Id == 7;
        }

        Assert.True(sawText, "The last frame holds no text call.");
        Assert.True(sawRect, "The last frame holds no rect call.");
        Assert.True(sawImage, "The last frame holds no image call with the texture id 7.");
    }

    /// <summary>Gets the template of the gate: a root and six panels with each built-in type.</summary>
    private static ReadOnlySpan<byte> GateTemplate => """
        <ui:Element xmlns:ui="brutal-ui" class="loaded-root" name="loaded-root">
          <ui:Element class="loaded-panel" name="loaded-panel-0">
            <ui:Label name="loaded-label-0" text="alpha" />
            <ui:Button name="loaded-button-0" text="go" />
            <ui:Toggle name="loaded-toggle-0" checked="true" />
            <ui:Slider name="loaded-slider-0" min="0" max="10" step="1" value="3" />
            <ui:TextField name="loaded-field-0" capacity="8" text="abc" />
            <ui:Image name="loaded-image-0" texture="icon" />
            <ui:ScrollView name="loaded-view-0"><ui:Label name="loaded-row-0" text="row" /></ui:ScrollView>
          </ui:Element>
          <ui:Element class="loaded-panel" name="loaded-panel-1">
            <ui:Label name="loaded-label-1" text="alpha" />
            <ui:Button name="loaded-button-1" text="go" />
            <ui:Toggle name="loaded-toggle-1" checked="true" />
            <ui:Slider name="loaded-slider-1" min="0" max="10" step="1" value="3" />
            <ui:TextField name="loaded-field-1" capacity="8" text="abc" />
            <ui:Image name="loaded-image-1" texture="icon" />
            <ui:ScrollView name="loaded-view-1"><ui:Label name="loaded-row-1" text="row" /></ui:ScrollView>
          </ui:Element>
          <ui:Element class="loaded-panel" name="loaded-panel-2">
            <ui:Label name="loaded-label-2" text="alpha" />
            <ui:Button name="loaded-button-2" text="go" />
            <ui:Toggle name="loaded-toggle-2" checked="true" />
            <ui:Slider name="loaded-slider-2" min="0" max="10" step="1" value="3" />
            <ui:TextField name="loaded-field-2" capacity="8" text="abc" />
            <ui:Image name="loaded-image-2" texture="icon" />
            <ui:ScrollView name="loaded-view-2"><ui:Label name="loaded-row-2" text="row" /></ui:ScrollView>
          </ui:Element>
          <ui:Element class="loaded-panel" name="loaded-panel-3">
            <ui:Label name="loaded-label-3" text="alpha" />
            <ui:Button name="loaded-button-3" text="go" />
            <ui:Toggle name="loaded-toggle-3" checked="true" />
            <ui:Slider name="loaded-slider-3" min="0" max="10" step="1" value="3" />
            <ui:TextField name="loaded-field-3" capacity="8" text="abc" />
            <ui:Image name="loaded-image-3" texture="icon" />
            <ui:ScrollView name="loaded-view-3"><ui:Label name="loaded-row-3" text="row" /></ui:ScrollView>
          </ui:Element>
          <ui:Element class="loaded-panel" name="loaded-panel-4">
            <ui:Label name="loaded-label-4" text="alpha" />
            <ui:Button name="loaded-button-4" text="go" />
            <ui:Toggle name="loaded-toggle-4" checked="true" />
            <ui:Slider name="loaded-slider-4" min="0" max="10" step="1" value="3" />
            <ui:TextField name="loaded-field-4" capacity="8" text="abc" />
            <ui:Image name="loaded-image-4" texture="icon" />
            <ui:ScrollView name="loaded-view-4"><ui:Label name="loaded-row-4" text="row" /></ui:ScrollView>
          </ui:Element>
          <ui:Element class="loaded-panel" name="loaded-panel-5">
            <ui:Label name="loaded-label-5" text="alpha" />
            <ui:Button name="loaded-button-5" text="go" />
            <ui:Toggle name="loaded-toggle-5" checked="true" />
            <ui:Slider name="loaded-slider-5" min="0" max="10" step="1" value="3" />
            <ui:TextField name="loaded-field-5" capacity="8" text="abc" />
            <ui:Image name="loaded-image-5" texture="icon" />
            <ui:ScrollView name="loaded-view-5"><ui:Label name="loaded-row-5" text="row" /></ui:ScrollView>
          </ui:Element>
        </ui:Element>
        """u8;

    /// <summary>
    /// The tree of the gate and the measured body. The test builds the tree, the context, and the element arrays
    /// before the measure. <see cref="Step"/> uses no lambda, no new string, no boxed value, and no Assert.
    /// </summary>
    private sealed class GateScene
    {
        public required UiContext Context { get; init; }

        public required RecordingRenderer Renderer { get; init; }

        public required Element Root { get; init; }

        /// <summary>Gets the six panels, in child order.</summary>
        public required Element[] Panels { get; init; }

        /// <summary>Gets one label of each panel, in child order.</summary>
        public required Label[] Labels { get; init; }

        /// <summary>Gets one slider of each panel, in child order.</summary>
        public required Slider[] Sliders { get; init; }

        /// <summary>Gets the count of the steps that ran.</summary>
        public int StepCount { get; private set; }

        /// <summary>Loads the template, fills the panel, and adds the root to a context with a measurer.</summary>
        public static GateScene Build()
        {
            Dictionary<string, TextureHandle> textures = new(StringComparer.Ordinal) { ["icon"] = Icon };
            TemplateLoader loader = new(new TemplateLoader.CreateInfo { Textures = textures });
            Element root = loader.Load(GateTemplate, "gate.xml").GetRootOrThrow();
            Assert.Equal(PanelCount, root.Children.Count);

            Element[] panels = new Element[PanelCount];
            Label[] labels = new Label[PanelCount];
            Slider[] sliders = new Slider[PanelCount];
            for (int i = 0; i < PanelCount; i++)
            {
                Element panel = root.Children[i];
                panel.Inline.BackgroundColor = PanelFill;
                panels[i] = panel;
                labels[i] = (Label)panel.Children[0];
                sliders[i] = (Slider)panel.Children[3];
            }

            RecordingRenderer renderer = new();
            UiContext context = new(new UiContext.CreateInfo { Measurer = renderer })
            {
                ViewportSize = new float2(800f, 600f),
            };
            context.AddRoot(root);

            return new GateScene
            {
                Context = context,
                Renderer = renderer,
                Root = root,
                Panels = panels,
                Labels = labels,
                Sliders = sliders,
            };
        }

        /// <summary>One measured step: one state change, one value write, one text write, then update and draw.</summary>
        public void Step()
        {
            int step = StepCount++;

            Element panel = Panels[step % PanelCount];
            panel.States = panel.States == PseudoStates.None ? PseudoStates.Hover : PseudoStates.None;

            Sliders[step % PanelCount].Value = (step & 1) == 0 ? LowValue : HighValue;
            Labels[step % PanelCount].SetText((step & 1) == 0 ? TextA : TextB);

            Context.Update(StepTime);
            Context.Draw(Renderer);
        }
    }
}
