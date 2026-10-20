using System;
using Brutal.ImGuiApi;
using BrutalUi.Core;
using Xunit;
using Gui = Brutal.ImGuiApi.ImGui;

namespace BrutalUi.ImGui.Tests;

/// <summary>Tests of <see cref="ImGuiFontService"/> on a headless ImGui context.</summary>
public sealed unsafe class ImGuiFontServiceTests
{
    /// <summary>An empty atlas gets one default font, and a second call gives the same font with no new font.</summary>
    [Fact]
    public void ReturnsTheDefaultFontForIdZero()
    {
        using HeadlessImGui imgui = new();
        ImGuiFontService fonts = new();
        ImFontAtlasPtr atlas = Gui.GetIO().Fonts;
        Assert.Equal(0, AtlasFontCount());

        ImFontPtr first = fonts.GetFont(default);

        Assert.False(first.IsNull());
        Assert.Equal(1, AtlasFontCount());
        Assert.True(first.Ptr == atlas.Fonts[0].Ptr);
        Assert.True(first.ContainerAtlas.Ptr == atlas.Ptr);
        Assert.Equal(13f, first.LegacySize);

        ImFontPtr second = fonts.GetFont(new FontHandle(0));

        Assert.True(second.Ptr == first.Ptr);
        Assert.Equal(1, AtlasFontCount());
    }

    /// <summary>The first font of the atlas is the default font when <c>io.FontDefault</c> is not set.</summary>
    [Fact]
    public void UsesTheFirstAtlasFontWithNoIoDefault()
    {
        using HeadlessImGui imgui = new();
        ImGuiFontService fonts = new();
        ImFontPtr a = Gui.GetIO().Fonts.AddFontDefault();
        _ = Gui.GetIO().Fonts.AddFontDefault();

        Assert.True(fonts.GetFont(default).Ptr == a.Ptr);
        Assert.Equal(2, AtlasFontCount());
    }

    /// <summary>
    /// <c>io.FontDefault</c> wins over the first font of the atlas, and a later write of it takes effect.
    /// </summary>
    [Fact]
    public void PrefersTheIoDefaultFont()
    {
        using HeadlessImGui imgui = new();
        ImGuiFontService fonts = new();
        ImGuiIOPtr io = Gui.GetIO();
        ImFontPtr a = io.Fonts.AddFontDefault();
        ImFontPtr b = io.Fonts.AddFontDefault();

        io.FontDefault = b;
        Assert.True(fonts.GetFont(default).Ptr == b.Ptr);

        io.FontDefault = a;
        Assert.True(fonts.GetFont(default).Ptr == a.Ptr);
    }

    /// <summary>Two fonts get the ids 1 and 2, and <see cref="ImGuiFontService.GetFont"/> gives each back.</summary>
    [Fact]
    public void GivesIdsFromOne()
    {
        using HeadlessImGui imgui = new();
        ImGuiFontService fonts = new();
        ImFontPtr a = Gui.GetIO().Fonts.AddFontDefault();
        ImFontPtr b = Gui.GetIO().Fonts.AddFontDefault();

        FontHandle ha = fonts.Add(a);
        FontHandle hb = fonts.Add(b);

        Assert.Equal(1, ha.Id);
        Assert.Equal(2, hb.Id);
        Assert.True(fonts.GetFont(ha).Ptr == a.Ptr);
        Assert.True(fonts.GetFont(hb).Ptr == b.Ptr);
    }

    /// <summary>
    /// The table grows past its start size of 4, the ids never repeat, and the same font added two times gets two ids.
    /// </summary>
    [Fact]
    public void GrowsTheTableAndNeverRepeatsAnId()
    {
        using HeadlessImGui imgui = new();
        ImGuiFontService fonts = new();
        ImFontPtr a = Gui.GetIO().Fonts.AddFontDefault();
        ImFontPtr b = Gui.GetIO().Fonts.AddFontDefault();

        for (int i = 1; i <= 9; i++)
        {
            ImFontPtr font = i % 2 == 0 ? b : a;
            Assert.Equal(i, fonts.Add(font).Id);
        }

        for (int i = 1; i <= 9; i++)
        {
            ImFontPtr expected = i % 2 == 0 ? b : a;
            Assert.True(fonts.GetFont(new FontHandle(i)).Ptr == expected.Ptr);
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => fonts.GetFont(new FontHandle(10)));
    }

    /// <summary>An id above the last id, and an id below 0, throw.</summary>
    [Fact]
    public void ThrowsForAnUnknownId()
    {
        using HeadlessImGui imgui = new();
        ImGuiFontService fonts = new();
        _ = fonts.Add(Gui.GetIO().Fonts.AddFontDefault());
        _ = fonts.Add(Gui.GetIO().Fonts.AddFontDefault());

        ArgumentOutOfRangeException above = Assert.Throws<ArgumentOutOfRangeException>(
            () => fonts.GetFont(new FontHandle(3)));
        ArgumentOutOfRangeException below = Assert.Throws<ArgumentOutOfRangeException>(
            () => fonts.GetFont(new FontHandle(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => fonts.GetFont(new FontHandle(int.MinValue)));
        Assert.Throws<ArgumentOutOfRangeException>(() => fonts.GetFont(new FontHandle(int.MaxValue)));

        Assert.Equal("font", above.ParamName);
        Assert.Equal(3, above.ActualValue);
        Assert.Equal(-1, below.ActualValue);
    }

    /// <summary>A service with no registered font knows id 0 only.</summary>
    [Fact]
    public void ThrowsForIdOneWithNoRegisteredFont()
    {
        using HeadlessImGui imgui = new();
        ImGuiFontService fonts = new();

        Assert.Throws<ArgumentOutOfRangeException>(() => fonts.GetFont(new FontHandle(1)));
    }

    /// <summary>A null font throws, and the service gives no id for it.</summary>
    [Fact]
    public void ThrowsForANullFont()
    {
        using HeadlessImGui imgui = new();
        ImGuiFontService fonts = new();

        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => fonts.Add(default));

        Assert.Equal("font", error.ParamName);
        Assert.Equal(1, fonts.Add(Gui.GetIO().Fonts.AddFontDefault()).Id);
    }

    /// <summary>A font of the atlas of another context throws, and the service gives no id for it.</summary>
    [Fact]
    public void ThrowsForAFontOfAnotherAtlas()
    {
        using HeadlessImGui imgui = new();
        ImGuiFontService fonts = new();
        ImGuiContextPtr other = Gui.CreateContext();
        ImFontPtr foreign;
        try
        {
            Gui.SetCurrentContext(other);
            foreign = Gui.GetIO().Fonts.AddFontDefault();
        }
        finally
        {
            Gui.SetCurrentContext(imgui.Context);
        }

        try
        {
            Assert.False(foreign.IsNull());

            ArgumentException error = Assert.Throws<ArgumentException>(() => fonts.Add(foreign));

            Assert.Equal("font", error.ParamName);
            Assert.Equal(1, fonts.Add(Gui.GetIO().Fonts.AddFontDefault()).Id);
        }
        finally
        {
            Gui.DestroyContext(other);
        }

        Assert.True(Gui.GetCurrentContext().Ptr == imgui.Context.Ptr);
    }

    /// <summary>With no current context, each member throws before a native call that needs the context.</summary>
    [Fact]
    public void ThrowsWithNoContext()
    {
        Assert.True(Gui.GetCurrentContext().IsNull());
        ImGuiFontService fonts = new();

        Assert.Throws<InvalidOperationException>(() => fonts.GetFont(default));
        Assert.Throws<InvalidOperationException>(() => fonts.GetFont(new FontHandle(1)));

        // The pointer is not a real font. The context check comes before the service reads the font.
        ImFontPtr fake = new((ImFont*)16);
        Assert.Throws<InvalidOperationException>(() => fonts.Add(fake));
        Assert.Throws<ArgumentNullException>(() => fonts.Add(default));
    }

    /// <summary>The default font works before the first frame, inside a frame, and between frames.</summary>
    [Fact]
    public void GivesTheSameDefaultFontOutsideAndInsideAFrame()
    {
        using HeadlessImGui imgui = new();
        ImGuiFontService fonts = new();

        ImFontPtr before = fonts.GetFont(default);
        imgui.BeginFrame();
        ImFontPtr inside = fonts.GetFont(default);
        imgui.EndFrame();
        ImFontPtr after = fonts.GetFont(default);

        Assert.True(inside.Ptr == before.Ptr);
        Assert.True(after.Ptr == before.Ptr);
        Assert.Equal(1, AtlasFontCount());
    }

    /// <summary>Gives the number of fonts in the atlas of the current context.</summary>
    private static int AtlasFontCount() => Gui.GetIO().Fonts.Fonts.Count;
}
