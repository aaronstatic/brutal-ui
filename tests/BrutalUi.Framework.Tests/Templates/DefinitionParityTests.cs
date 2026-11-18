using System;
using Brutal.Numerics;
using BrutalUi.Core;
using BrutalUi.Core.Tests;
using Xunit;

namespace BrutalUi.Framework.Tests;

/// <summary>
/// The parity of loaded records with C# records (A9): the same steps give the same mode diff on two managers, and
/// the two record sets hold the same data.
/// </summary>
/// <remarks>
/// <para>
/// Each test builds two contexts with a <see cref="RecordingRenderer"/> measurer, a default font size of 16, and a
/// viewport of 800 by 600, and one <see cref="UiManager"/> on each context. <see cref="DefinitionSet.RegisterCode"/>
/// registers the C# records on the first manager, and <see cref="DefinitionSet.Load"/> registers the loaded records
/// on the second one.
/// </para>
/// <para>
/// The diff test compares the layout root of each context after each step (Decision 10): the root holds the slots,
/// the mounted panel roots, and the panel order. The current mode name, <c>History.Count</c>, and the map state
/// cover the rest of the diff.
/// </para>
/// </remarks>
public sealed class DefinitionParityTests
{
    /// <summary>
    /// The same five steps give the same diff on the two managers: <c>set.main</c>, <c>set.detail</c>,
    /// <c>set.overlay</c>, a back step, and <c>set.main</c>. After each step the layout roots compare equal under
    /// <c>TreeCompare.FirstDifference</c>, the contexts hold one root each, the current mode names match, the
    /// history counts match, and the two maps read the same state.
    /// </summary>
    [Fact]
    public void TheLoadedRecordsGiveTheSameModeDiffAsTheCodeRecords()
    {
        UiContext codeContext = CreateContext();
        UiManager codeUi = CreateManager(codeContext);
        UiActionMap codeMap = CreateMap();
        DefinitionSet.RegisterCode(codeUi, codeMap);

        UiContext loadedContext = CreateContext();
        UiManager loadedUi = CreateManager(loadedContext);
        UiActionMap loadedMap = CreateMap();
        DefinitionSet.Load(loadedUi, loadedMap);

        Step("set.main"u8);
        Step("set.detail"u8);
        Step("set.overlay"u8);
        Back();
        Step("set.main"u8);

        void Step(ReadOnlySpan<byte> mode)
        {
            codeUi.SetMode(mode);
            codeContext.Update(0.016f);
            loadedUi.SetMode(mode);
            loadedContext.Update(0.016f);
            Compare();
        }

        void Back()
        {
            Assert.True(codeUi.History.Back());
            codeContext.Update(0.016f);
            Assert.True(loadedUi.History.Back());
            loadedContext.Update(0.016f);
            Compare();
        }

        void Compare()
        {
            Assert.Equal(1, codeContext.Roots.Length);
            Assert.Equal(1, loadedContext.Roots.Length);
            Assert.Null(TreeCompare.FirstDifference(codeContext.Roots[0], loadedContext.Roots[0]));
            Assert.True(codeUi.CurrentMode!.Name.SequenceEqual(loadedUi.CurrentMode!.Name));
            Assert.Equal(codeUi.History.Count, loadedUi.History.Count);
            Assert.Equal(codeMap.IsActive, loadedMap.IsActive);
        }
    }

    /// <summary>
    /// The two record sets hold the same data in the order of the text: each panel name, each default slot, each
    /// layout name, each mode name, each slot name and panel name of each mode, and each map reference. The one map
    /// instance that the test passes to both calls stands in the records of both managers.
    /// </summary>
    [Fact]
    public void TheLoadedRecordsHoldTheSameDataAsTheCodeRecords()
    {
        UiContext codeContext = CreateContext();
        UiManager codeUi = CreateManager(codeContext);
        UiContext loadedContext = CreateContext();
        UiManager loadedUi = CreateManager(loadedContext);
        UiActionMap map = CreateMap();

        DefinitionSet.Records code = DefinitionSet.RegisterCode(codeUi, map);
        DefinitionSet.Records loaded = DefinitionSet.Load(loadedUi, map);

        Assert.Equal(4, loaded.Panels.Length);
        for (int i = 0; i < code.Panels.Length; i++)
        {
            Assert.True(code.Panels[i].Name.SequenceEqual(loaded.Panels[i].Name));
            Assert.True(code.Panels[i].DefaultSlot.SequenceEqual(loaded.Panels[i].DefaultSlot));
        }

        Assert.Equal(2, loaded.Layouts.Length);
        Assert.True(loaded.Layouts[0].Name.SequenceEqual("set.column"u8));
        Assert.True(code.Layouts[0].Name.SequenceEqual(loaded.Layouts[0].Name));
        Assert.True(loaded.Layouts[1].Name.SequenceEqual("set.overlay"u8));
        Assert.True(code.Layouts[1].Name.SequenceEqual(loaded.Layouts[1].Name));

        Assert.Equal(3, loaded.Modes.Length);
        for (int i = 0; i < code.Modes.Length; i++)
        {
            ModeDefinition codeMode = code.Modes[i];
            ModeDefinition loadedMode = loaded.Modes[i];
            Assert.True(codeMode.Name.SequenceEqual(loadedMode.Name));
            Assert.True(codeMode.Layout.SequenceEqual(loadedMode.Layout));
            Assert.Equal(codeMode.SlotCount, loadedMode.SlotCount);
            for (int slot = 0; slot < codeMode.SlotCount; slot++)
            {
                Assert.True(codeMode.GetSlotName(slot).SequenceEqual(loadedMode.GetSlotName(slot)));
                Assert.Equal(codeMode.GetPanelCount(slot), loadedMode.GetPanelCount(slot));
                for (int panel = 0; panel < codeMode.GetPanelCount(slot); panel++)
                {
                    Assert.True(codeMode.GetPanelName(slot, panel).SequenceEqual(loadedMode.GetPanelName(slot, panel)));
                }
            }

            if (codeMode.ActionMap is null)
            {
                Assert.Null(loadedMode.ActionMap);
            }
            else
            {
                Assert.Same(codeMode.ActionMap, loadedMode.ActionMap);
                Assert.Same(map, loadedMode.ActionMap);
            }
        }
    }

    /// <summary>Creates a context with a measurer, a default font size of 16, and a viewport of 800 by 600.</summary>
    /// <returns>The context.</returns>
    private static UiContext CreateContext()
    {
        return new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer(), DefaultFontSize = 16f })
        {
            ViewportSize = new float2(800f, 600f),
        };
    }

    /// <summary>Creates a manager on one context, with a fresh panel context.</summary>
    /// <param name="context">The context.</param>
    /// <returns>The manager.</returns>
    private static UiManager CreateManager(UiContext context)
    {
        return new UiManager(new UiManager.CreateInfo { Context = context, Panels = CreatePanels() });
    }

    /// <summary>Creates a panel context with a new command registry, event bus, and service registry.</summary>
    /// <returns>The panel context.</returns>
    private static PanelContext CreatePanels()
    {
        return new PanelContext
        {
            Commands = new CommandRegistry(),
            Events = new EventBus(),
            Services = new ServiceRegistry(),
        };
    }

    /// <summary>Creates an action map with no action.</summary>
    /// <returns>The map.</returns>
    private static UiActionMap CreateMap()
    {
        return new UiActionMap(new UiActionMap.CreateInfo { Commands = new CommandRegistry() });
    }
}
