using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using Gui = Brutal.ImGuiApi.ImGui;

namespace BrutalUi.ImGui.Tests;

/// <summary>
/// An ImGui context with no GPU, no window, and no backend. Each test creates one harness and disposes it.
/// </summary>
/// <remarks>
/// <para>
/// The constructor creates the context and makes it current. It sets no ini file, the display size, a delta time of
/// 1/60, and <see cref="ImGuiBackendFlags.RendererHasTextures"/>. The flag lets the atlas bake glyphs on demand with no
/// built texture.
/// </para>
/// <para>
/// <see cref="EndFrame"/> calls <c>ImGui.Render</c> and then acts as a null backend: it sets each texture that wants a
/// create or an update to <see cref="ImTextureStatus.OK"/>, and each texture that wants a destroy to
/// <see cref="ImTextureStatus.Destroyed"/>. It sets no texture id, so the atlas id stays 0. The next
/// <c>ImGui.NewFrame</c> then finds no pending texture.
/// </para>
/// <para>
/// ImGui has one current context for the process. The constructor throws when a context is current already, because
/// <c>ImGui.CreateContext</c> keeps the old context current, and the harness then sets the wrong context.
/// </para>
/// </remarks>
internal sealed unsafe class HeadlessImGui : IDisposable
{
    /// <summary>The default display width in pixels.</summary>
    public const float DefaultWidth = 400f;

    /// <summary>The default display height in pixels.</summary>
    public const float DefaultHeight = 300f;

    private readonly ImGuiContextPtr _context;
    private bool _frameOpen;
    private bool _disposed;

    /// <summary>Creates the context and makes it current.</summary>
    /// <param name="width">The display width in pixels.</param>
    /// <param name="height">The display height in pixels.</param>
    /// <exception cref="InvalidOperationException">An ImGui context is current already.</exception>
    public HeadlessImGui(float width = DefaultWidth, float height = DefaultHeight)
    {
        if (!Gui.GetCurrentContext().IsNull())
        {
            throw new InvalidOperationException(
                "An ImGui context is current already. Dispose the harness of the last test before you create a new one.");
        }

        _context = Gui.CreateContext();
        ImGuiIOPtr io = Gui.GetIO();
        io.IniFilename = default;
        io.DisplaySize = new float2(width, height);
        io.DeltaTime = 1f / 60f;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasTextures;
    }

    /// <summary>Gets the context of the harness.</summary>
    public ImGuiContextPtr Context => _context;

    /// <summary>Gets a value that is true between <see cref="BeginFrame"/> and <see cref="EndFrame"/>.</summary>
    public bool FrameOpen => _frameOpen;

    /// <summary>Starts an ImGui frame with <c>ImGui.NewFrame</c>.</summary>
    /// <exception cref="InvalidOperationException">A frame is open already.</exception>
    public void BeginFrame()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_frameOpen)
        {
            throw new InvalidOperationException("A frame is open already. Call EndFrame first.");
        }

        Gui.NewFrame();
        _frameOpen = true;
    }

    /// <summary>Ends the frame with <c>ImGui.Render</c>, then moves each pending texture to its end status.</summary>
    /// <exception cref="InvalidOperationException">No frame is open.</exception>
    public void EndFrame()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_frameOpen)
        {
            throw new InvalidOperationException("No frame is open. Call BeginFrame first.");
        }

        Gui.Render();
        _frameOpen = false;
        CompleteTextureRequests();
    }

    /// <summary>
    /// Gives one line for each draw command of the background list of the main viewport, after <see cref="EndFrame"/>.
    /// </summary>
    /// <remarks>
    /// The line shape is <c>cmd &lt;i&gt; clip &lt;x0&gt;,&lt;y0&gt;,&lt;x1&gt;,&lt;y1&gt; tex &lt;atlas|id N|data N&gt;
    /// elems &lt;n&gt; verts &lt;minx&gt;,&lt;miny&gt;-&lt;maxx&gt;,&lt;maxy&gt; colors &lt;#RRGGBBAA ...&gt;</c>. The clip is in
    /// min-max form, as ImGui stores it. <c>atlas</c> is the texture of <c>io.Fonts</c>. <c>id N</c> is a texture
    /// that the game registered, with its id. <c>data N</c> is another ImGui texture, with its unique id.
    /// <c>verts</c> gives the bounds of the vertices that the indices of the command use, or <c>none</c>.
    /// <c>colors</c> lists the distinct vertex colors in first-seen order. The lines join with <c>\n</c>, with no line
    /// feed at the end. The method allocates, so a measured region never calls it.
    /// </remarks>
    /// <returns>
    /// The dump of the list. A frame with no draw call gives one command with 0 elements and <c>verts none</c>.
    /// </returns>
    /// <exception cref="InvalidOperationException">A frame is open.</exception>
    public string DescribeBackgroundList()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_frameOpen)
        {
            throw new InvalidOperationException("A frame is open. Call EndFrame first.");
        }

        // ImGui resets the list at the first access of a frame. After Render the frame count does not change, so this
        // access reads the commands of the last frame.
        ImDrawListPtr list = Gui.GetBackgroundDrawList(Gui.GetMainViewport());
        ImTextureData* atlas = Gui.GetIO().Fonts.TexRef._TexData.Ptr;
        ref ImVector<ImDrawCmd> commands = ref list.CmdBuffer;
        ref ImVector<ushort> indices = ref list.IdxBuffer;
        ref ImVector<ImDrawVert> vertices = ref list.VtxBuffer;

        StringBuilder text = new();
        List<uint> colors = [];
        for (int i = 0; i < commands.Count; i++)
        {
            ImDrawCmd command = commands[i];
            if (i > 0)
            {
                text.Append('\n');
            }

            float4 clip = command.ClipRect;
            text.Append("cmd ").Append(i.ToString(CultureInfo.InvariantCulture))
                .Append(" clip ").Append(Format(clip.X)).Append(',').Append(Format(clip.Y))
                .Append(',').Append(Format(clip.Z)).Append(',').Append(Format(clip.W))
                .Append(" tex ").Append(DescribeTexture(command.TexRef, atlas))
                .Append(" elems ").Append(command.ElemCount.ToString(CultureInfo.InvariantCulture))
                .Append(" verts ");

            colors.Clear();
            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;
            for (uint k = 0; k < command.ElemCount; k++)
            {
                int index = indices[(int)(command.IdxOffset + k)];
                ImDrawVert vertex = vertices[(int)command.VtxOffset + index];
                minX = MathF.Min(minX, vertex.pos.X);
                minY = MathF.Min(minY, vertex.pos.Y);
                maxX = MathF.Max(maxX, vertex.pos.X);
                maxY = MathF.Max(maxY, vertex.pos.Y);
                if (!colors.Contains(vertex.col))
                {
                    colors.Add(vertex.col);
                }
            }

            if (command.ElemCount == 0)
            {
                text.Append("none");
            }
            else
            {
                text.Append(Format(minX)).Append(',').Append(Format(minY))
                    .Append('-').Append(Format(maxX)).Append(',').Append(Format(maxY));
            }

            text.Append(" colors");
            foreach (uint color in colors)
            {
                text.Append(' ').Append(FormatColor(color));
            }
        }

        return text.ToString();
    }

    /// <summary>Renders an open frame, then destroys the context.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_frameOpen && Gui.GetCurrentContext().Ptr == _context.Ptr)
        {
            Gui.Render();
        }

        _frameOpen = false;
        Gui.DestroyContext(_context);
    }

    /// <summary>Formats an ImU32 vertex color (byte order R, G, B, A in memory) as <c>#RRGGBBAA</c>.</summary>
    internal static string FormatColor(uint color)
    {
        uint r = color & 0xFF;
        uint g = (color >> 8) & 0xFF;
        uint b = (color >> 16) & 0xFF;
        uint a = color >> 24;
        return string.Create(CultureInfo.InvariantCulture, $"#{r:X2}{g:X2}{b:X2}{a:X2}");
    }

    /// <summary>Formats a number with the invariant culture. A whole value has no decimals, and -0 prints as 0.</summary>
    internal static string Format(float value) =>
        value == 0f ? "0" : value.ToString(CultureInfo.InvariantCulture);

    private static string DescribeTexture(ImTextureRef texture, ImTextureData* atlas)
    {
        ImTextureData* data = texture._TexData.Ptr;
        if (data == null)
        {
            return "id " + texture._TexID.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (data == atlas)
        {
            return "atlas";
        }

        return "data " + texture._TexData.UniqueID.ToString(CultureInfo.InvariantCulture);
    }

    private static void CompleteTextureRequests()
    {
        ref ImVector<ImTextureDataPtr> textures = ref Gui.GetPlatformIO().Textures;
        for (int i = 0; i < textures.Count; i++)
        {
            ImTextureDataPtr texture = textures[i];
            switch (texture.Status)
            {
                case ImTextureStatus.WantCreate:
                case ImTextureStatus.WantUpdates:
                    texture.Status = ImTextureStatus.OK;
                    break;
                case ImTextureStatus.WantDestroy:
                    texture.Status = ImTextureStatus.Destroyed;
                    break;
                default:
                    break;
            }
        }
    }
}
