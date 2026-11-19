using System;
using System.Collections.Generic;
using System.IO;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using BrutalUi.Framework;
using BrutalUi.Markup;
using HelloPanel;
using Xunit;

namespace BrutalUi.Framework.Tests;

public sealed partial class SampleMarkupAllocationTests
{
    /// <summary>The class of the layout root: the class of the <c>.hello-slot-root</c> rule.</summary>
    private static readonly ClassId SlotRootClass = ClassName.Intern("hello-slot-root");

    /// <summary>The class of a panel: the class of the <c>.hello-panel</c> rules.</summary>
    private static readonly ClassId PanelClass = ClassName.Intern("hello-panel");

    /// <summary>The class of each panel root: the class of the <c>.hello-panel-root</c> rule.</summary>
    private static readonly ClassId PanelRootClass = ClassName.Intern("hello-panel-root");

    /// <summary>The class of a row: the class of the <c>.hello-row</c> rules.</summary>
    private static readonly ClassId RowClass = ClassName.Intern("hello-row");

    /// <summary>The handler of <c>hello.add</c>. It adds 1 to the store and counts the call.</summary>
    private static readonly CommandHandler s_add = static target =>
    {
        Holder holder = (Holder)target!;
        holder.Score.Set(holder.Score.Value + 1);
        holder.Adds++;
    };

    /// <summary>The handler of <c>hello.reset</c>. It sets the store to 0 and counts the call.</summary>
    private static readonly CommandHandler s_reset = static target =>
    {
        Holder holder = (Holder)target!;
        holder.Score.Set(0);
        holder.Resets++;
    };

    /// <summary>The handler of <c>hello.rebind</c> and <c>hello.restore</c>. It counts the call.</summary>
    private static readonly CommandHandler s_count = static target => ((Holder)target!).Calls++;

    /// <summary>The selector of the score binding. It returns the score itself.</summary>
    private static readonly Func<int, int> s_score = static s => s;

    /// <summary>The scene: the loaded sample shape, the manager, the events, and the round counter.</summary>
    private sealed class Scene
    {
        public required UiContext Context { get; init; }

        public required RecordingRenderer Renderer { get; init; }

        public required UiManager Ui { get; init; }

        public required Store<int> Store { get; init; }

        public required Holder Count { get; init; }

        public required Element StateRoot { get; init; }

        public required Element InfoRoot { get; init; }

        public required SlotContainer Bottom { get; init; }

        public required ModeDefinition Main { get; init; }

        public required ModeDefinition Detail { get; init; }

        public required Label ScoreLabel { get; init; }

        public required Button AddButton { get; init; }

        /// <summary>The press event. It is a field, so the body passes it by reference.</summary>
        public required PointerEvent Press;

        /// <summary>The release event. It is a field, so the body passes it by reference.</summary>
        public required PointerEvent Release;

        /// <summary>The frame number. The toggle body sets the mode by its parity.</summary>
        public int Round;
    }

    /// <summary>The target of the command handlers: the store and the counts.</summary>
    private sealed class Holder
    {
        /// <summary>The score store. The scene sets it before the registration.</summary>
        public Store<int> Score = null!;

        /// <summary>The count of <c>hello.add</c> calls.</summary>
        public int Adds;

        /// <summary>The count of <c>hello.reset</c> calls.</summary>
        public int Resets;

        /// <summary>The count of the other three commands' calls.</summary>
        public int Calls;
    }

    /// <summary>A panel or layout twin factory: it returns one built root, so no factory runs in a frame.</summary>
    private sealed class Twin
    {
        /// <summary>The built root.</summary>
        public readonly Element Root = null!;

        /// <summary>Creates the twin of one built root.</summary>
        /// <param name="root">The root. The twin never builds another one.</param>
        public Twin(Element root)
        {
            Root = root;
        }

        /// <summary>Returns the built root.</summary>
        /// <param name="context">The panel context. The twin reads nothing from it.</param>
        /// <returns>The built root.</returns>
        public Element Create(PanelContext context) => Root;
    }

    /// <summary>
    /// Builds the sample shape: it loads the linked files, registers the records, applies <c>hello.main</c>, runs
    /// one update, and caches the center of the <c>Add</c> button and the two pointer events.
    /// </summary>
    /// <returns>The scene, ready for warm-up and a measure.</returns>
    private static Scene BuildScene()
    {
        byte[] stateText = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "HelloState.xml"));
        byte[] definitionsText = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "HelloDefinitions.xml"));
        byte[] cssText = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "HelloScreen.css"));

        Store<int> store = new(new Store<int>.CreateInfo { Initial = 0 });
        Holder holder = new() { Score = store };
        CommandRegistry registry = new();
        registry.Register("hello.add"u8, s_add, holder);
        registry.Register("hello.reset"u8, s_reset, holder);
        registry.Register("hello.rebind"u8, s_count, holder);
        registry.Register("hello.restore"u8, s_count, holder);

        UiActionMap map = new(new UiActionMap.CreateInfo { Commands = registry });

        RecordingRenderer renderer = new();
        UiContext context = new(new UiContext.CreateInfo { Measurer = renderer, DefaultFontSize = 16f })
        {
            ViewportSize = new float2(1280f, 720f),
        };
        StyleSheet sheet = new StyleSheetParser().Parse(cssText, "HelloScreen.css").GetSheetOrThrow();
        context.AddSheet(sheet);

        // The twins stand in for the three sample factories. The roots build at build time.
        Twin widgetTwin = new(BuildPanel("Widgets"));
        Twin infoTwin = new(BuildPanel("Info"));
        Twin layoutTwin = new(BuildLayout());

        TemplateLoader templates = new();
        TemplateBindings bindings = new(registry);
        bindings.AddText("hello.score", store, s_score);
        bindings.AttachTo(templates);
        templates.RegisterBrutalUiFrameworkTestsElements();

        // The check load of Decision 7: a fault in the panel text lands here.
        TemplateLoadResult check = templates.Load(stateText, "HelloState.xml");
        Assert.True(check.Succeeded);

        DefinitionLoader definitions = new(templates);
        definitions.AddTemplate("HelloState.xml", stateText);
        definitions.AddFactory("HelloModes.CreateWidgetPanel", widgetTwin.Create);
        definitions.AddFactory("HelloModes.CreateInfoPanel", infoTwin.Create);
        definitions.AddFactory("HelloModes.CreateLayout", layoutTwin.Create);
        definitions.AddActionMap("hello.actions", map);

        DefinitionLoadResult result = definitions.Load(definitionsText, "HelloDefinitions.xml");
        Assert.True(result.Succeeded);

        PanelContext panels = new()
        {
            Commands = registry,
            Events = new EventBus(),
            Services = new ServiceRegistry(),
        };
        UiManager ui = new(new UiManager.CreateInfo { Context = context, Panels = panels });

        IReadOnlyList<PanelController> controllers = result.RegisterTo(ui);
        Element stateRoot = null!;
        Element infoRoot = null!;

        // The build rule: each root exists before the first frame, so no factory runs in a frame.
        foreach (PanelController controller in controllers)
        {
            Element root = controller.EnsureRoot();
            if (controller.Definition.Name.SequenceEqual("hello.state"u8))
            {
                stateRoot = root;
            }

            if (controller.Definition.Name.SequenceEqual("hello.info"u8))
            {
                infoRoot = root;
            }
        }

        Assert.NotNull(stateRoot);
        Assert.NotNull(infoRoot);

        ui.SetMode("hello.main"u8);
        context.Update(FrameTime);

        SlotContainer? bottom = SlotContainer.Find(context.Roots[0], "bottom"u8);
        Assert.NotNull(bottom);
        Button? addButton = stateRoot.Find<Button>("add"u8);
        Assert.NotNull(addButton);
        Label? scoreLabel = stateRoot.Find<Label>("score"u8);
        Assert.NotNull(scoreLabel);
        Assert.True(addButton.Layout.Rect.Z > 0f);

        float2 center = ThousandWidgetTree.ViewportCenter(addButton);

        return new Scene
        {
            Context = context,
            Renderer = renderer,
            Ui = ui,
            Store = store,
            Count = holder,
            StateRoot = stateRoot,
            InfoRoot = infoRoot,
            Bottom = bottom,
            Main = FindMode(result.Modes, "hello.main"u8),
            Detail = FindMode(result.Modes, "hello.detail"u8),
            ScoreLabel = scoreLabel,
            AddButton = addButton,
            Press = Pointer(UiEventType.PointerDown, center),
            Release = Pointer(UiEventType.PointerUp, center),
        };
    }

    /// <summary>Builds a panel twin: a root with the two panel classes and one label.</summary>
    /// <param name="text">The label text.</param>
    /// <returns>The panel root.</returns>
    private static Element BuildPanel(string text)
    {
        Label label = new(new Label.CreateInfo { Text = text }) { IsPickable = false };
        Element panel = new();
        panel.Classes.Add(PanelClass);
        panel.Classes.Add(PanelRootClass);
        panel.Children.Add(label);
        return panel;
    }

    /// <summary>Builds the layout twin: the shape of <c>HelloScreen.BuildLayout</c>.</summary>
    /// <returns>The layout root.</returns>
    private static Element BuildLayout()
    {
        SlotContainer top = new("top"u8) { IsPickable = false };
        SlotContainer bottom = new("bottom"u8) { IsPickable = false };

        Element root = new() { IsPickable = false };
        root.Classes.Add(SlotRootClass);
        root.Children.Add(top);
        root.Children.Add(bottom);
        return root;
    }

    /// <summary>Finds the loaded mode with the given name.</summary>
    /// <param name="modes">The loaded modes.</param>
    /// <param name="name">The name bytes.</param>
    /// <returns>The mode definition.</returns>
    /// <exception cref="InvalidOperationException">No loaded mode carries the name.</exception>
    private static ModeDefinition FindMode(IReadOnlyList<ModeDefinition> modes, ReadOnlySpan<byte> name)
    {
        foreach (ModeDefinition mode in modes)
        {
            if (mode.Name.SequenceEqual(name))
            {
                return mode;
            }
        }

        throw new InvalidOperationException("The definitions load gave no mode with this name.");
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
}
