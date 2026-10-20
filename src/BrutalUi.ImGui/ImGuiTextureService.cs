using System;
using System.Diagnostics.CodeAnalysis;
using Brutal.ImGuiApi;
using Brutal.ImGuiApi.Backends;
using Brutal.Numerics;
using Brutal.VulkanApi;
using BrutalUi.Core;

namespace BrutalUi.ImGui;

/// <summary>
/// Maps a <see cref="TextureHandle"/> to an <c>ImTextureRef</c> and a size.
/// </summary>
/// <remarks>
/// <para>
/// The service registers a Vulkan texture through <c>VulkanBackend.AddTexture</c>, or it accepts a texture reference
/// that the game registered.
/// </para>
/// <para>
/// The ids start at 1 and never repeat, so a stale handle never names a new texture. A handle holds the size of its
/// texture, so an image element reads <see cref="TextureHandle.Size"/> with no renderer.
/// </para>
/// <para>
/// The game owns the image, the image view, and the sampler. The service owns the registration only.
/// </para>
/// <para>
/// A reference with texture data must stay valid until <see cref="Remove"/>. Do not register the font atlas: ImGui
/// replaces its texture data when the atlas grows.
/// </para>
/// <para>
/// The service reads no ImGui state, so it works with no ImGui context. Use the service on the main thread only.
/// <see cref="GetTexture"/> and <see cref="GetSize"/> allocate no managed memory when they succeed.
/// </para>
/// </remarks>
public sealed class ImGuiTextureService
{
    private const int StartSize = 4;

    /// <summary>The table of entries. The index is the id. Index 0 is not used.</summary>
    private Entry[] _entries = new Entry[StartSize];

    /// <summary>The last id that the service gave.</summary>
    private int _count;

    /// <summary>Creates a texture service with no registered texture.</summary>
    /// <remarks>The constructor reads no ImGui state.</remarks>
    public ImGuiTextureService()
    {
    }

    /// <summary>Registers a texture reference that the game registered with ImGui, and gives its handle.</summary>
    /// <remarks>
    /// <para>
    /// The caller owns the registration. <see cref="Remove"/> only makes the handle unknown. The ids start at 1 and
    /// never repeat. A second call with the same reference gives a new id.
    /// </para>
    /// <para>
    /// The handle holds <paramref name="size"/>. The service does not compare it with the real size of the texture.
    /// </para>
    /// <para>
    /// A reference with texture data must stay valid until <see cref="Remove"/>. Do not register the font atlas: ImGui
    /// replaces its texture data when the atlas grows. The service reads no ImGui state, so it cannot detect this case.
    /// </para>
    /// </remarks>
    /// <param name="texture">
    /// The texture reference. It names an ImGui texture data or holds a texture id that is not 0.
    /// </param>
    /// <param name="size">The size of the texture in texels.</param>
    /// <returns>The handle of the texture, with <paramref name="size"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A component of <paramref name="size"/> is 0 or less.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="texture"/> has no texture data and a texture id of 0, so it names no texture.
    /// </exception>
    public unsafe TextureHandle Add(ImTextureRef texture, int2 size)
    {
        ThrowIfNoSize(size);
        if (texture._TexData.Ptr == null && texture._TexID.Value == IntPtr.Zero)
        {
            throw new ArgumentException(
                "The texture reference names no texture. Give a texture data or a texture id that is not 0.",
                nameof(texture));
        }

        return Store(texture, size, null);
    }

    /// <summary>
    /// Registers a Vulkan texture with <c>VulkanBackend.AddTexture</c>, and gives its handle.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The game owns the image, the image view, and the sampler. Keep them alive until <see cref="Remove"/>, or until
    /// the backend is disposed. The service owns the registration only.
    /// </para>
    /// <para>
    /// The order of the checks: (1) the size, (2) the sampler, (3) the image view, (4) the backend. Each check runs
    /// before the backend call, so a failed check registers nothing.
    /// </para>
    /// </remarks>
    /// <param name="backend">The ImGui Vulkan backend that draws the frame.</param>
    /// <param name="sampler">The sampler of the texture.</param>
    /// <param name="imageView">The image view of the texture.</param>
    /// <param name="layout">The layout of the image when the backend samples it.</param>
    /// <param name="size">The size of the texture in texels.</param>
    /// <returns>The handle of the texture, with <paramref name="size"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A component of <paramref name="size"/> is 0 or less.</exception>
    /// <exception cref="ArgumentException"><paramref name="sampler"/> or <paramref name="imageView"/> is a null handle.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="backend"/> is <see langword="null"/>.</exception>
    public TextureHandle AddVulkan(
        VulkanBackend backend, VkSampler sampler, VkImageView imageView, VkImageLayout layout, int2 size)
    {
        ThrowIfNoSize(size);
        if (sampler.IsNull())
        {
            throw new ArgumentException("The sampler is a null handle.", nameof(sampler));
        }

        if (imageView.IsNull())
        {
            throw new ArgumentException("The image view is a null handle.", nameof(imageView));
        }

        ArgumentNullException.ThrowIfNull(backend);

        // The table grows before the backend call. A failed growth then leaks no registration of the backend.
        EnsureCapacity();
        ImTextureRef texture = backend.AddTexture(sampler, imageView, layout);
        return Store(texture, size, backend);
    }

    /// <summary>Removes a texture. The handle is then unknown, and its id never comes back.</summary>
    /// <remarks>
    /// <para>
    /// For a texture of <see cref="AddVulkan"/>, the method calls <c>VulkanBackend.RemoveTexture</c>, which frees
    /// the descriptor set of the texture. For a texture of <see cref="Add"/>, the caller owns the registration, and the
    /// method makes no ImGui call.
    /// </para>
    /// <para>
    /// Call the method when no frame in flight uses the texture, for example after <c>vkDeviceWaitIdle</c>. Call it
    /// before the backend is disposed. A backend dispose frees all its descriptor sets, so a game that ends needs no
    /// call of this method.
    /// </para>
    /// <para>
    /// The method marks the entry as removed before the backend call. When the backend call throws, the handle stays
    /// unknown.
    /// </para>
    /// </remarks>
    /// <param name="texture">The handle.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The handle is the none handle, or the service gave no texture with its id, or the texture is removed.
    /// </exception>
    public void Remove(TextureHandle texture)
    {
        ref Entry entry = ref Find(texture.Id);
        VulkanBackend? owner = entry.Owner;
        ImTextureRef reference = entry.Texture;
        entry = new Entry { Removed = true };
        owner?.RemoveTexture(reference);
    }

    /// <summary>Gives the ImGui texture reference of a handle.</summary>
    /// <remarks>
    /// The method looks up the id only. It does not compare the size of <paramref name="texture"/>. It allocates no
    /// managed memory when it succeeds.
    /// </remarks>
    /// <param name="texture">The handle.</param>
    /// <returns>The texture reference.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The handle is the none handle, or the service gave no texture with its id, or the texture is removed.
    /// </exception>
    public ImTextureRef GetTexture(TextureHandle texture)
    {
        return Find(texture.Id).Texture;
    }

    /// <summary>Gives the size in texels of a texture.</summary>
    /// <remarks>
    /// The result equals the <see cref="TextureHandle.Size"/> of the handle that <see cref="Add"/> or
    /// <see cref="AddVulkan"/> gave. The method looks up the id only. It allocates no managed memory when it succeeds.
    /// </remarks>
    /// <param name="texture">The handle.</param>
    /// <returns>The size in texels.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The handle is the none handle, or the service gave no texture with its id, or the texture is removed.
    /// </exception>
    public int2 GetSize(TextureHandle texture)
    {
        return Find(texture.Id).Size;
    }

    /// <summary>Gives the live entry of an id.</summary>
    /// <param name="id">The id.</param>
    /// <returns>A reference to the entry in the table.</returns>
    private ref Entry Find(int id)
    {
        // One compare covers id 0, a negative id, and an id above the last id. The subtraction wraps on purpose.
        if (unchecked((uint)(id - 1)) >= (uint)_count)
        {
            ThrowUnknownTexture(id);
        }

        ref Entry entry = ref _entries[id];
        if (entry.Removed)
        {
            ThrowRemovedTexture(id);
        }

        return ref entry;
    }

    /// <summary>Grows the table when the next id does not fit.</summary>
    private void EnsureCapacity()
    {
        if (_count + 1 >= _entries.Length)
        {
            Array.Resize(ref _entries, _entries.Length * 2);
        }
    }

    /// <summary>Stores a new entry with the next id.</summary>
    /// <param name="texture">The texture reference.</param>
    /// <param name="size">The size in texels.</param>
    /// <param name="owner">The backend that registered the texture, or <see langword="null"/>.</param>
    /// <returns>The handle of the entry.</returns>
    private TextureHandle Store(ImTextureRef texture, int2 size, VulkanBackend? owner)
    {
        EnsureCapacity();
        int id = _count + 1;
        _entries[id] = new Entry { Texture = texture, Size = size, Owner = owner };
        _count = id;
        return new TextureHandle(id, size);
    }

    private static void ThrowIfNoSize(int2 size)
    {
        if (size.X <= 0 || size.Y <= 0)
        {
            ThrowNoSize(size);
        }
    }

    [DoesNotReturn]
    private static void ThrowNoSize(int2 size)
    {
        throw new ArgumentOutOfRangeException(
            nameof(size), $"{size.X}x{size.Y}", "Each component of the texture size must be above 0.");
    }

    [DoesNotReturn]
    private static void ThrowUnknownTexture(int id)
    {
        throw new ArgumentOutOfRangeException(
            "texture", id, "The texture service gave no texture with this id. Use an id of Add or AddVulkan.");
    }

    [DoesNotReturn]
    private static void ThrowRemovedTexture(int id)
    {
        throw new ArgumentOutOfRangeException(
            "texture", id, "The texture with this id is removed. A removed id never comes back.");
    }

    /// <summary>One registered texture.</summary>
    private struct Entry
    {
        /// <summary>The texture reference. It is also the value that <c>VulkanBackend.RemoveTexture</c> takes.</summary>
        public ImTextureRef Texture;

        /// <summary>The size in texels.</summary>
        public int2 Size;

        /// <summary>The backend that registered the texture, or <see langword="null"/> for a texture of the game.</summary>
        public VulkanBackend? Owner;

        /// <summary>A value that is true after <see cref="Remove"/>.</summary>
        public bool Removed;
    }
}
