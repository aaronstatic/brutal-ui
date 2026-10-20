using System;
using Brutal.Numerics;

namespace BrutalUi.Core;

/// <summary>
/// A handle to a font of the renderer.
/// </summary>
/// <remarks>
/// Id 0 (the default value) is the default font of the renderer. A font service in <c>BrutalUi.ImGui</c> gives the other ids.
/// The ids have a meaning for one renderer only. Do not store them between runs.
/// </remarks>
public readonly struct FontHandle : IEquatable<FontHandle>
{
    /// <summary>Creates a handle with an id.</summary>
    /// <param name="id">The id of the font. The id 0 is the default font of the renderer.</param>
    public FontHandle(int id)
    {
        Id = id;
    }

    /// <summary>Gets the id of the font. The id 0 is the default font of the renderer.</summary>
    public int Id { get; }

    /// <summary>Compares two handles.</summary>
    /// <param name="left">The first handle.</param>
    /// <param name="right">The second handle.</param>
    /// <returns><see langword="true"/> when both handles have the same id.</returns>
    public static bool operator ==(FontHandle left, FontHandle right) => left.Id == right.Id;

    /// <summary>Compares two handles.</summary>
    /// <param name="left">The first handle.</param>
    /// <param name="right">The second handle.</param>
    /// <returns><see langword="true"/> when the handles have different ids.</returns>
    public static bool operator !=(FontHandle left, FontHandle right) => left.Id != right.Id;

    /// <summary>Compares this handle with another handle.</summary>
    /// <param name="other">The other handle.</param>
    /// <returns><see langword="true"/> when both handles have the same id.</returns>
    public bool Equals(FontHandle other) => Id == other.Id;

    /// <summary>Compares this handle with an object.</summary>
    /// <param name="obj">The object.</param>
    /// <returns><see langword="true"/> when <paramref name="obj"/> is a <see cref="FontHandle"/> with the same id.</returns>
    public override bool Equals(object? obj) => obj is FontHandle other && Equals(other);

    /// <summary>Gets a hash code from the id.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => Id;
}

/// <summary>
/// A handle to a texture of the renderer, with the size of the texture in texels.
/// </summary>
/// <remarks>
/// <para>
/// Id 0 (the default value) is no texture. <see cref="IUiRenderer.Image"/> draws nothing for it.
/// The renderer gives the other ids. The ids have a meaning for one renderer only. Do not store them between runs.
/// </para>
/// <para>
/// <see cref="Size"/> is the size of the texture in texels. An image element reads it for its natural size.
/// The default handle has the size (0, 0).
/// </para>
/// <para>
/// Equality and the hash code use the id only, not the size. One renderer gives one size for each id, so two
/// handles with the same id from the same renderer always have the same size.
/// </para>
/// </remarks>
public readonly struct TextureHandle : IEquatable<TextureHandle>
{
    /// <summary>Creates a handle with an id and the size (0, 0).</summary>
    /// <param name="id">The id of the texture. The id 0 is no texture.</param>
    public TextureHandle(int id)
    {
        Id = id;
    }

    /// <summary>Creates a handle with an id and a size.</summary>
    /// <param name="id">The id of the texture. The id 0 is no texture.</param>
    /// <param name="size">The size of the texture in texels.</param>
    public TextureHandle(int id, int2 size)
    {
        Id = id;
        Size = size;
    }

    /// <summary>Gets the id of the texture. The id 0 is no texture.</summary>
    public int Id { get; }

    /// <summary>Gets the size of the texture in texels. An image element reads it for its natural size.</summary>
    /// <remarks>The equality of two handles does not use the size.</remarks>
    public int2 Size { get; }

    /// <summary>Gets a value that shows whether the handle is no texture (id 0).</summary>
    public bool IsNone => Id == 0;

    /// <summary>Compares two handles.</summary>
    /// <param name="left">The first handle.</param>
    /// <param name="right">The second handle.</param>
    /// <returns><see langword="true"/> when both handles have the same id.</returns>
    public static bool operator ==(TextureHandle left, TextureHandle right) => left.Id == right.Id;

    /// <summary>Compares two handles.</summary>
    /// <param name="left">The first handle.</param>
    /// <param name="right">The second handle.</param>
    /// <returns><see langword="true"/> when the handles have different ids.</returns>
    public static bool operator !=(TextureHandle left, TextureHandle right) => left.Id != right.Id;

    /// <summary>Compares this handle with another handle.</summary>
    /// <param name="other">The other handle.</param>
    /// <returns><see langword="true"/> when both handles have the same id.</returns>
    public bool Equals(TextureHandle other) => Id == other.Id;

    /// <summary>Compares this handle with an object.</summary>
    /// <param name="obj">The object.</param>
    /// <returns><see langword="true"/> when <paramref name="obj"/> is a <see cref="TextureHandle"/> with the same id.</returns>
    public override bool Equals(object? obj) => obj is TextureHandle other && Equals(other);

    /// <summary>Gets a hash code from the id.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => Id;
}
