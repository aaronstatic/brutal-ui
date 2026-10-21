using System;
using Brutal.Numerics;

namespace BrutalUi.Core;

/// <summary>
/// An element that shows a texture. The layout pass gives the image the size of its texture region, and the draw sends
/// one <see cref="IUiRenderer.Image"/> call.
/// </summary>
/// <remarks>
/// <para>
/// An image is a leaf. Do not add children to an image. The engine calls a measure callback for an element with no
/// children only, so an image with children loses the size of its texture.
/// </para>
/// <para>
/// The natural size of the image is <see cref="TextureHandle.Size"/> times the span of <see cref="Uv"/>, one pixel for
/// each texel. Thus a region of an atlas takes the size of the region. The none handle, or a texture size with a
/// component of 0, gives a natural size of (0, 0).
/// </para>
/// <para>
/// The measure rule, for each axis: <see cref="MeasureMode.Undefined"/> gives the natural size,
/// <see cref="MeasureMode.AtMost"/> gives the smaller value of the natural size and the limit, and
/// <see cref="MeasureMode.Exactly"/> gives the limit. A style width or height overrides the natural size.
/// </para>
/// <para>
/// An image with the none handle, or with a texture size of 0, takes no space and draws nothing. It does not throw.
/// The image draws with square corners. <see cref="ScaleMode"/> sets the draw rect and the UV rect.
/// </para>
/// <para>
/// Each property setter marks the dirty bits that the change needs. The game calls no
/// <see cref="Element.MarkDirty(DirtyFlags)"/> for an image property.
/// </para>
/// <para>
/// The image draws in its content box: the border box minus the padding and the border. A content box with no area
/// uses the rule of <see cref="ImageScaleMode.StretchToFill"/>.
/// </para>
/// </remarks>
public sealed class Image : Element
{
    /// <summary>The measure callback of each image. The constructor sets it one time, so the layout pass allocates nothing.</summary>
    private static readonly MeasureCallback s_measure = MeasureImage;

    /// <summary>The type id of each image.</summary>
    private static readonly ElementTypeId s_typeId = ElementType.Intern("Image");

    /// <summary>The texture.</summary>
    private TextureHandle _texture;

    /// <summary>The texture coordinates: u0, v0, u1, and v1.</summary>
    private float4 _uv;

    /// <summary>The tint.</summary>
    private Rgba8 _tint;

    /// <summary>The scale mode.</summary>
    private ImageScaleMode _scaleMode;

    /// <summary>Creates an image with the texture, the UV rect, the tint, and the scale mode of the create-info.</summary>
    /// <param name="info">The create-info.</param>
    public Image(in CreateInfo info)
    {
        _texture = info.Texture;
        _uv = info.Uv ?? new float4(0f, 0f, 1f, 1f);
        _tint = info.Tint ?? new Rgba8(255, 255, 255);
        _scaleMode = info.ScaleMode;
        Layout.Measure = s_measure;
    }

    /// <summary>Gets the type id of the image: <c>Image</c>.</summary>
    public override ElementTypeId TypeId => s_typeId;

    /// <summary>Gets or sets the texture. The none handle is valid: the image then takes no space and draws nothing.</summary>
    /// <remarks>
    /// <para>
    /// The setter compares the id and the size, because the equality of two handles uses the id only. A set to the same
    /// id and the same size marks nothing.
    /// </para>
    /// <para>
    /// A new size, or a change from the none handle or to the none handle, marks <see cref="DirtyFlags.Layout"/> and
    /// <see cref="DirtyFlags.Paint"/>, because the natural size changes. A new id with the same size marks
    /// <see cref="DirtyFlags.Paint"/> only.
    /// </para>
    /// </remarks>
    public TextureHandle Texture
    {
        get => _texture;
        set
        {
            TextureHandle old = _texture;
            bool sameSize = old.Size.X == value.Size.X && old.Size.Y == value.Size.Y;
            if (old.Id == value.Id && sameSize)
            {
                return;
            }

            _texture = value;
            bool layout = !sameSize || old.IsNone != value.IsNone;
            MarkDirty(layout ? DirtyFlags.Layout | DirtyFlags.Paint : DirtyFlags.Paint);
        }
    }

    /// <summary>Gets or sets the texture coordinates: u0, v0, u1, and v1. The default is (0, 0, 1, 1).</summary>
    /// <remarks>
    /// <para>
    /// A flipped rect (u1 less than u0, or v1 less than v0) flips the image. The natural size uses the absolute span.
    /// </para>
    /// <para>
    /// A component that is not finite draws nothing. A span that is not finite also gives 0 on its axis of the natural
    /// size.
    /// </para>
    /// <para>
    /// A set to a new value marks <see cref="DirtyFlags.Layout"/> and <see cref="DirtyFlags.Paint"/>, because the natural
    /// size uses the span. The setter compares each component with <see cref="float.Equals(float)"/>, so a set of
    /// <c>NaN</c> to <c>NaN</c> marks nothing.
    /// </para>
    /// </remarks>
    public float4 Uv
    {
        get => _uv;
        set
        {
            if (SameUv(in _uv, in value))
            {
                return;
            }

            _uv = value;
            MarkDirty(DirtyFlags.Layout | DirtyFlags.Paint);
        }
    }

    /// <summary>Gets or sets the tint. The renderer multiplies each texel by the tint. The default is opaque white.</summary>
    /// <remarks>
    /// A set to a new value marks <see cref="DirtyFlags.Paint"/>. A tint with an alpha of 0 after the scale by the opacity
    /// draws nothing.
    /// </remarks>
    public Rgba8 Tint
    {
        get => _tint;
        set
        {
            if (_tint == value)
            {
                return;
            }

            _tint = value;
            MarkDirty(DirtyFlags.Paint);
        }
    }

    /// <summary>Gets or sets the rule that puts the texture into the box. The default is <see cref="ImageScaleMode.StretchToFill"/>.</summary>
    /// <remarks>A set to a new value marks <see cref="DirtyFlags.Paint"/>. The mode does not change the natural size.</remarks>
    public ImageScaleMode ScaleMode
    {
        get => _scaleMode;
        set
        {
            if (_scaleMode == value)
            {
                return;
            }

            _scaleMode = value;
            MarkDirty(DirtyFlags.Paint);
        }
    }

    /// <summary>
    /// Draws the texture in the content box per <see cref="ScaleMode"/>, with the tint scaled by the opacity and square
    /// corners.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The none handle, a texture size with a component of 0, a UV component that is not finite, or a tint with an alpha
    /// of 0 after the scale draws nothing. An unknown scale mode draws as <see cref="ImageScaleMode.StretchToFill"/>.
    /// </para>
    /// <para>
    /// The content box is the border box minus the padding and the border of each edge
    /// (<see cref="LayoutNode.ContentBox(in float4)"/>). A content box with no area uses the rule of
    /// <see cref="ImageScaleMode.StretchToFill"/>, and the renderer skips a rect with no area.
    /// </para>
    /// </remarks>
    /// <param name="renderer">The renderer that gets the call.</param>
    /// <param name="borderBox">The border box of the image in viewport pixels: x, y, width, and height.</param>
    /// <param name="opacity">The product of the opacities from the root to the image, from 0 to 1.</param>
    internal override void PaintContent(IUiRenderer renderer, in float4 borderBox, float opacity)
    {
        TextureHandle texture = _texture;
        if (texture.IsNone || texture.Size.X <= 0 || texture.Size.Y <= 0)
        {
            return;
        }

        if (!float.IsFinite(_uv.X) || !float.IsFinite(_uv.Y) || !float.IsFinite(_uv.Z) || !float.IsFinite(_uv.W))
        {
            return;
        }

        Rgba8 tint = _tint.ScaleAlpha(opacity);
        if (tint.A == 0)
        {
            return;
        }

        float4 content = Layout.ContentBox(in borderBox);
        float2 natural = NaturalSize(texture, in _uv);
        float4 rect = content;
        float4 uv = _uv;
        switch (_scaleMode)
        {
            case ImageScaleMode.ScaleToFit:
                rect = FitRect(in content, natural);
                break;
            case ImageScaleMode.ScaleAndCrop:
                uv = CropUv(in _uv, natural, in content);
                break;
        }

        Corners radius = default;
        renderer.Image(in rect, texture, in uv, in tint, in radius);
    }

    /// <summary>Gives the natural size of a texture region: the texture size times the absolute UV span.</summary>
    /// <remarks>
    /// The none handle, or a texture size with a component of 0 or less, gives (0, 0). A span that is not finite gives
    /// 0 on its axis.
    /// </remarks>
    /// <param name="texture">The texture.</param>
    /// <param name="uv">The texture coordinates: u0, v0, u1, and v1.</param>
    /// <returns>The natural size in pixels.</returns>
    internal static float2 NaturalSize(TextureHandle texture, in float4 uv)
    {
        if (texture.IsNone || texture.Size.X <= 0 || texture.Size.Y <= 0)
        {
            return default;
        }

        float width = texture.Size.X * MathF.Abs(uv.Z - uv.X);
        float height = texture.Size.Y * MathF.Abs(uv.W - uv.Y);
        return new float2(float.IsFinite(width) ? width : 0f, float.IsFinite(height) ? height : 0f);
    }

    /// <summary>Gives the size of one axis of an image for the layout pass.</summary>
    /// <remarks>
    /// <see cref="MeasureMode.Undefined"/> gives the natural size. <see cref="MeasureMode.AtMost"/> gives the smaller
    /// value of the natural size and the limit. <see cref="MeasureMode.Exactly"/> gives the limit. A limit that is not
    /// finite gives the natural size.
    /// </remarks>
    /// <param name="natural">The natural size of the axis in pixels.</param>
    /// <param name="available">The available size of the axis in pixels.</param>
    /// <param name="mode">The rule for <paramref name="available"/>.</param>
    /// <returns>The size of the axis in pixels.</returns>
    internal static float MeasureAxis(float natural, float available, MeasureMode mode)
    {
        if (!float.IsFinite(available))
        {
            return natural;
        }

        return mode switch
        {
            MeasureMode.Exactly => available,
            MeasureMode.AtMost => available < natural ? available : natural,
            _ => natural,
        };
    }

    /// <summary>
    /// Gives the largest rect with the aspect of the natural size that fits in a box. The rect is in the center of the
    /// box.
    /// </summary>
    /// <remarks>
    /// A natural size with a component of 0, or a box with no area, gives the box (the rule of
    /// <see cref="ImageScaleMode.StretchToFill"/>).
    /// </remarks>
    /// <param name="box">The box: x, y, width, and height in pixels.</param>
    /// <param name="natural">The natural size of the image in pixels.</param>
    /// <returns>The draw rect: x, y, width, and height in pixels.</returns>
    internal static float4 FitRect(in float4 box, float2 natural)
    {
        if (!HasArea(natural.X, natural.Y) || !HasArea(box.Z, box.W))
        {
            return box;
        }

        float aspect = natural.X / natural.Y;
        float width = box.Z;
        float height = box.Z / aspect;
        if (height > box.W)
        {
            height = box.W;
            width = box.W * aspect;
        }

        return new float4(box.X + ((box.Z - width) * 0.5f), box.Y + ((box.W - height) * 0.5f), width, height);
    }

    /// <summary>
    /// Cuts a UV rect around its center to the aspect of a box. A box that is wider than the image cuts the v span. A box
    /// that is taller than the image cuts the u span.
    /// </summary>
    /// <remarks>
    /// The cut scales the signed span, so a flipped UV rect keeps its direction. A natural size with a component of 0, or
    /// a box with no area, gives the UV rect with no change (the rule of <see cref="ImageScaleMode.StretchToFill"/>).
    /// </remarks>
    /// <param name="uv">The texture coordinates: u0, v0, u1, and v1.</param>
    /// <param name="natural">The natural size of the image in pixels.</param>
    /// <param name="box">The box: x, y, width, and height in pixels.</param>
    /// <returns>The cut texture coordinates: u0, v0, u1, and v1.</returns>
    internal static float4 CropUv(in float4 uv, float2 natural, in float4 box)
    {
        if (!HasArea(natural.X, natural.Y) || !HasArea(box.Z, box.W))
        {
            return uv;
        }

        float imageAspect = natural.X / natural.Y;
        float boxAspect = box.Z / box.W;
        if (imageAspect > boxAspect)
        {
            float center = (uv.X + uv.Z) * 0.5f;
            float half = (uv.Z - uv.X) * 0.5f * (boxAspect / imageAspect);
            return new float4(center - half, uv.Y, center + half, uv.W);
        }

        if (imageAspect < boxAspect)
        {
            float center = (uv.Y + uv.W) * 0.5f;
            float half = (uv.W - uv.Y) * 0.5f * (imageAspect / boxAspect);
            return new float4(uv.X, center - half, uv.Z, center + half);
        }

        return uv;
    }

    /// <summary>Measures an image: the measure rule of <see cref="MeasureAxis(float, float, MeasureMode)"/> on each axis.</summary>
    /// <remarks>
    /// The rule keeps the contract of <see cref="MeasureCallback"/>: with <see cref="MeasureMode.AtMost"/>, a natural
    /// size that fits gives the size of <see cref="MeasureMode.Undefined"/>. The callback reads no renderer.
    /// </remarks>
    /// <param name="element">The image.</param>
    /// <param name="width">The available inner width in pixels.</param>
    /// <param name="widthMode">The rule for <paramref name="width"/>.</param>
    /// <param name="height">The available inner height in pixels.</param>
    /// <param name="heightMode">The rule for <paramref name="height"/>.</param>
    /// <returns>The size of the image in pixels.</returns>
    private static float2 MeasureImage(
        Element element,
        float width,
        MeasureMode widthMode,
        float height,
        MeasureMode heightMode)
    {
        Image image = (Image)element;
        float2 natural = NaturalSize(image._texture, in image._uv);
        return new float2(MeasureAxis(natural.X, width, widthMode), MeasureAxis(natural.Y, height, heightMode));
    }

    /// <summary>Gives a value that shows whether a size has a finite area above 0.</summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <returns><see langword="true"/> when both values are finite and above 0.</returns>
    private static bool HasArea(float width, float height)
    {
        return width > 0f && height > 0f && float.IsFinite(width) && float.IsFinite(height);
    }

    /// <summary>Compares two UV rects with <see cref="float.Equals(float)"/> on each component.</summary>
    /// <param name="a">The first rect.</param>
    /// <param name="b">The second rect.</param>
    /// <returns><see langword="true"/> when each component is the same.</returns>
    private static bool SameUv(in float4 a, in float4 b)
    {
        return a.X.Equals(b.X) && a.Y.Equals(b.Y) && a.Z.Equals(b.Z) && a.W.Equals(b.W);
    }

    /// <summary>The create-info of an <see cref="Image"/>.</summary>
    public struct CreateInfo
    {
        /// <summary>The texture. The none handle is valid: the image then takes no space and draws nothing.</summary>
        public required TextureHandle Texture;

        /// <summary>
        /// The texture coordinates: u0, v0, u1, and v1. <see langword="null"/> gives (0, 0, 1, 1). A component that is not
        /// finite draws nothing.
        /// </summary>
        public float4? Uv;

        /// <summary>The tint. <see langword="null"/> gives opaque white.</summary>
        public Rgba8? Tint;

        /// <summary>The scale mode. The default is <see cref="ImageScaleMode.StretchToFill"/>.</summary>
        public ImageScaleMode ScaleMode;
    }
}
