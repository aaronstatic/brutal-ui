using System;
using System.Diagnostics.CodeAnalysis;
using Brutal.ImGuiApi;
using BrutalUi.Core;
using Gui = Brutal.ImGuiApi.ImGui;

namespace BrutalUi.ImGui;

/// <summary>
/// Maps a <see cref="FontHandle"/> to an <c>ImFontPtr</c> of the ImGui font atlas (<c>io.Fonts</c>).
/// </summary>
/// <remarks>
/// <para>
/// Id 0 is the default font. The other ids start at 1 and never repeat.
/// </para>
/// <para>
/// The service uses the default loader of ImGui (stb) and the current ImGui context. Create the service after
/// <c>ImGui.CreateContext()</c>. Use one ImGui context for each process. A font stays valid only while its context
/// lives, so do not use the service after <c>ImGui.DestroyContext</c>.
/// </para>
/// <para>
/// The service loads no file. To use a TTF file, load it with the default loader, then register the result:
/// </para>
/// <code>
/// ImFontPtr font = ImGui.GetIO().Fonts.AddFontFromFileTTF("Fonts/Inter.ttf", 18f);
/// FontHandle body = fonts.Add(font);
/// </code>
/// <para>
/// Load the fonts of the game, or set <c>io.FontDefault</c>, before the first call of <see cref="GetFont"/> with
/// id 0. Else the ImGui default font becomes font 0 of the atlas, and it stays the default font of ImGui.
/// </para>
/// <para>
/// Do not call <c>io.Fonts.Clear()</c>, <c>ClearFonts()</c>, or <c>RemoveFont</c> for a font that the service
/// holds. A registered id then names freed memory, and a call with it stops the process. Id 0 works after a clear.
/// </para>
/// <para>
/// Use the service on the main thread only. <see cref="GetFont"/> allocates no managed memory when it succeeds.
/// </para>
/// </remarks>
public sealed unsafe class ImGuiFontService
{
    private const int StartSize = 4;

    private ImFontPtr[] _fonts = new ImFontPtr[StartSize];
    private int _count;

    /// <summary>Creates a font service with no registered font. Id 0 gives the default font.</summary>
    /// <remarks>The constructor reads no ImGui state.</remarks>
    public ImGuiFontService()
    {
    }

    /// <summary>Registers a font of the ImGui font atlas and gives its handle.</summary>
    /// <remarks>
    /// The ids start at 1 and never repeat. A second call with the same font gives a new id. The font must come from
    /// <c>ImGui.GetIO().Fonts</c>, because the renderer draws on the background draw list, and that list binds the
    /// texture of this atlas.
    /// </remarks>
    /// <param name="font">A font of <c>ImGui.GetIO().Fonts</c>.</param>
    /// <returns>The handle of the font.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="font"/> is null.</exception>
    /// <exception cref="InvalidOperationException">No ImGui context is current.</exception>
    /// <exception cref="ArgumentException"><paramref name="font"/> is a font of another atlas.</exception>
    public FontHandle Add(ImFontPtr font)
    {
        if (font.IsNull())
        {
            throw new ArgumentNullException(nameof(font), "The font is null.");
        }

        ImGuiIOPtr io = CurrentIO();
        if (font.ContainerAtlas.Ptr != io.Fonts.Ptr)
        {
            throw new ArgumentException(
                "The font is not a font of ImGui.GetIO().Fonts. Load the font into that atlas.", nameof(font));
        }

        int id = _count + 1;
        if (id == _fonts.Length)
        {
            Array.Resize(ref _fonts, _fonts.Length * 2);
        }

        _fonts[id] = font;
        _count = id;
        return new FontHandle(id);
    }

    /// <summary>Gives the ImGui font of a handle.</summary>
    /// <remarks>
    /// <para>
    /// Id 0 gives the default font: <c>io.FontDefault</c> when it is set, else the first font of <c>io.Fonts</c>.
    /// When the atlas holds no font, the method calls <c>io.Fonts.AddFontDefault()</c> one time and gives the result.
    /// The method reads <c>io</c> at each call, so a later write of <c>io.FontDefault</c> takes effect.
    /// </para>
    /// <para>
    /// Load the fonts of the game, or set <c>io.FontDefault</c>, before the first call with id 0. Else the ImGui
    /// default font becomes font 0 of the atlas, and it stays the default font of ImGui.
    /// </para>
    /// <para>
    /// The method allocates no managed memory when it succeeds. It works inside and outside an ImGui frame.
    /// </para>
    /// </remarks>
    /// <param name="font">The handle.</param>
    /// <returns>The font. The value is never null.</returns>
    /// <exception cref="InvalidOperationException">
    /// No ImGui context is current, or the default loader of ImGui gives no font.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">The id is below 0 or above the last id of the service.</exception>
    public ImFontPtr GetFont(FontHandle font)
    {
        ImGuiIOPtr io = CurrentIO();
        int id = font.Id;
        if ((uint)id > (uint)_count)
        {
            ThrowUnknownFont(id);
        }

        if (id != 0)
        {
            return _fonts[id];
        }

        ImFontPtr fallback = io.FontDefault;
        if (!fallback.IsNull())
        {
            return fallback;
        }

        ImFontAtlasPtr atlas = io.Fonts;
        if (atlas.Fonts.Count > 0)
        {
            return atlas.Fonts[0];
        }

        ImFont* added = Gui.PInvoke.ImFontAtlas_AddFontDefault(atlas.Ptr, null);
        if (added == null)
        {
            throw new InvalidOperationException("ImGui gives no default font. The call io.Fonts.AddFontDefault() failed.");
        }

        return new ImFontPtr(added);
    }

    private static ImGuiIOPtr CurrentIO()
    {
        if (Gui.GetCurrentContext().IsNull())
        {
            throw new InvalidOperationException(
                "No ImGui context is current. Call ImGui.CreateContext() before you use the font service.");
        }

        return Gui.GetIO();
    }

    [DoesNotReturn]
    private static void ThrowUnknownFont(int id)
    {
        throw new ArgumentOutOfRangeException(
            "font", id, "The font service gave no font with this id. Use id 0 or an id of Add.");
    }
}
