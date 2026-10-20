using System;
using Brutal.ImGuiApi;
using Brutal.ImGuiApi.Backends;
using Brutal.Numerics;
using Brutal.VulkanApi;
using BrutalUi.Core;
using Xunit;

namespace BrutalUi.ImGui.Tests;

/// <summary>Tests of <see cref="ImGuiTextureService"/>.</summary>
/// <remarks>
/// The service reads no ImGui state, so most tests need no ImGui context. No test creates a
/// <see cref="VulkanBackend"/>: each <see cref="ImGuiTextureService.AddVulkan"/> test gives a null backend, and the
/// checks run before the backend call.
/// </remarks>
public sealed unsafe class ImGuiTextureServiceTests
{
    /// <summary>The texture id of the test reference.</summary>
    private const int TestId = 4660;

    private static readonly int2 TestSize = new(64, 32);

    /// <summary>The ids start at 1, and each handle holds the size of its texture.</summary>
    [Fact]
    public void GivesIdsFromOneWithTheSize()
    {
        ImGuiTextureService textures = new();

        TextureHandle first = textures.Add(TestTexture(TestId), TestSize);
        TextureHandle second = textures.Add(TestTexture(TestId + 1), new int2(8, 4));

        Assert.Equal(1, first.Id);
        Assert.Equal(64, first.Size.X);
        Assert.Equal(32, first.Size.Y);
        Assert.Equal(2, second.Id);
        Assert.Equal(8, second.Size.X);
        Assert.Equal(4, second.Size.Y);
        Assert.Equal(64, textures.GetSize(first).X);
        Assert.Equal(32, textures.GetSize(first).Y);
        Assert.Equal(8, textures.GetSize(second).X);
        Assert.Equal(4, textures.GetSize(second).Y);
    }

    /// <summary><see cref="ImGuiTextureService.GetTexture"/> gives the reference of <see cref="ImGuiTextureService.Add"/>.</summary>
    [Fact]
    public void GetTextureReturnsTheReference()
    {
        ImGuiTextureService textures = new();
        TextureHandle first = textures.Add(TestTexture(TestId), TestSize);
        TextureHandle second = textures.Add(TestTexture(TestId + 1), TestSize);

        ImTextureRef firstRef = textures.GetTexture(first);
        ImTextureRef secondRef = textures.GetTexture(second);

        Assert.Equal((IntPtr)TestId, firstRef._TexID.Value);
        Assert.True(firstRef._TexData.Ptr == null);
        Assert.Equal((IntPtr)(TestId + 1), secondRef._TexID.Value);
    }

    /// <summary>The service looks up the id only. A handle with the same id and another size names the same texture.</summary>
    [Fact]
    public void LooksUpTheIdOnly()
    {
        ImGuiTextureService textures = new();
        TextureHandle handle = textures.Add(TestTexture(TestId), TestSize);

        TextureHandle noSize = new(handle.Id);
        TextureHandle otherSize = new(handle.Id, new int2(1, 1));

        Assert.Equal((IntPtr)TestId, textures.GetTexture(noSize)._TexID.Value);
        Assert.Equal(64, textures.GetSize(otherSize).X);
        Assert.Equal(32, textures.GetSize(otherSize).Y);
    }

    /// <summary>
    /// A reference with texture data and no texture id is valid. The test owns the texture data on its stack, so no
    /// ImGui context is needed and nothing leaks. The test does not register the font atlas: ImGui replaces the
    /// texture data of the atlas when the atlas grows, so the atlas is a short-lived case.
    /// </summary>
    [Fact]
    public void AcceptsAReferenceWithTextureData()
    {
        ImGuiTextureService textures = new();
        ImTextureData owned = default;
        ImTextureRef reference = new() { _TexData = new ImTextureDataPtr(&owned) };
        Assert.True(reference._TexData.Ptr == &owned);
        Assert.Equal(IntPtr.Zero, reference._TexID.Value);

        TextureHandle handle = textures.Add(reference, new int2(512, 128));

        Assert.True(textures.GetTexture(handle)._TexData.Ptr == &owned);
        Assert.Equal(512, textures.GetSize(handle).X);
        Assert.Equal(128, textures.GetSize(handle).Y);
    }

    /// <summary>A reference with no texture data and a texture id of 0 names no texture.</summary>
    [Fact]
    public void AddThrowsForAnEmptyReference()
    {
        ImGuiTextureService textures = new();

        ArgumentException error = Assert.Throws<ArgumentException>(() => textures.Add(default, TestSize));

        Assert.Equal("texture", error.ParamName);
        Assert.Equal(1, textures.Add(TestTexture(TestId), TestSize).Id);
    }

    /// <summary>After <see cref="ImGuiTextureService.Remove"/>, each member throws for the handle.</summary>
    [Fact]
    public void RemoveMakesTheHandleUnknown()
    {
        ImGuiTextureService textures = new();
        TextureHandle removed = textures.Add(TestTexture(TestId), TestSize);
        TextureHandle kept = textures.Add(TestTexture(TestId + 1), TestSize);

        textures.Remove(removed);

        AssertUnknown(textures, removed);
        Assert.Equal((IntPtr)(TestId + 1), textures.GetTexture(kept)._TexID.Value);
    }

    /// <summary>The ids never repeat, also after a remove and after the table grows.</summary>
    [Fact]
    public void NeverReusesAnId()
    {
        ImGuiTextureService textures = new();
        TextureHandle first = textures.Add(TestTexture(TestId), TestSize);
        textures.Remove(first);

        TextureHandle second = textures.Add(TestTexture(TestId), TestSize);

        Assert.Equal(2, second.Id);
        AssertUnknown(textures, first);

        // Nine more textures grow the table two times (start size 4).
        for (int i = 0; i < 9; i++)
        {
            TextureHandle next = textures.Add(TestTexture(TestId + 10 + i), new int2(i + 1, 1));
            Assert.Equal(3 + i, next.Id);
        }

        Assert.Equal((IntPtr)TestId, textures.GetTexture(second)._TexID.Value);
        for (int i = 0; i < 9; i++)
        {
            TextureHandle handle = new(3 + i);
            Assert.Equal((IntPtr)(TestId + 10 + i), textures.GetTexture(handle)._TexID.Value);
            Assert.Equal(i + 1, textures.GetSize(handle).X);
        }

        AssertUnknown(textures, first);
    }

    /// <summary>The none handle, a negative id, and an id above the last id are unknown.</summary>
    [Fact]
    public void ThrowsForANoneHandle()
    {
        ImGuiTextureService textures = new();
        AssertUnknown(textures, default);
        AssertUnknown(textures, new TextureHandle(1));

        textures.Add(TestTexture(TestId), TestSize);

        AssertUnknown(textures, default);
        AssertUnknown(textures, new TextureHandle(0, TestSize));
        AssertUnknown(textures, new TextureHandle(-1));
        AssertUnknown(textures, new TextureHandle(int.MinValue));
        AssertUnknown(textures, new TextureHandle(2));
    }

    /// <summary>A size with a component of 0 or less throws, and registers nothing.</summary>
    [Theory]
    [InlineData(0, 32)]
    [InlineData(64, 0)]
    [InlineData(0, 0)]
    [InlineData(-1, 32)]
    [InlineData(64, -5)]
    public void ThrowsForASizeOfZero(int width, int height)
    {
        ImGuiTextureService textures = new();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(
            () => textures.Add(TestTexture(TestId), new int2(width, height)));

        Assert.Equal("size", error.ParamName);
        Assert.Equal(1, textures.Add(TestTexture(TestId), TestSize).Id);
    }

    /// <summary>A null backend throws before the backend call. The test needs no GPU.</summary>
    [Fact]
    public void AddVulkanThrowsForANullBackend()
    {
        ImGuiTextureService textures = new();

        ArgumentNullException error = Assert.Throws<ArgumentNullException>(
            () => textures.AddVulkan(null!, FakeSampler(), FakeImageView(), default, TestSize));

        Assert.Equal("backend", error.ParamName);
        Assert.Equal(1, textures.Add(TestTexture(TestId), TestSize).Id);
    }

    /// <summary>The size, the sampler, and the image view are checked before the backend, in this order.</summary>
    [Fact]
    public void AddVulkanChecksTheArgumentsBeforeTheBackend()
    {
        ImGuiTextureService textures = new();

        ArgumentOutOfRangeException size = Assert.Throws<ArgumentOutOfRangeException>(
            () => textures.AddVulkan(null!, default, default, default, new int2(0, 32)));
        ArgumentException sampler = Assert.Throws<ArgumentException>(
            () => textures.AddVulkan(null!, default, default, default, TestSize));
        ArgumentException imageView = Assert.Throws<ArgumentException>(
            () => textures.AddVulkan(null!, FakeSampler(), default, default, TestSize));

        Assert.Equal("size", size.ParamName);
        Assert.Equal("sampler", sampler.ParamName);
        Assert.Equal("imageView", imageView.ParamName);
        Assert.Equal(1, textures.Add(TestTexture(TestId), TestSize).Id);
    }

    /// <summary>
    /// <see cref="ImGuiTextureService.GetTexture"/> and <see cref="ImGuiTextureService.GetSize"/> allocate no managed
    /// memory after warm-up.
    /// </summary>
    [Fact]
    public void LookupsAllocateNothing()
    {
        ImGuiTextureService textures = new();
        TextureHandle handle = textures.Add(TestTexture(TestId), TestSize);
        long sum = 0;
        for (int i = 0; i < 10; i++)
        {
            sum += textures.GetTexture(handle)._TexID.Value + textures.GetSize(handle).X;
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
        {
            sum += textures.GetTexture(handle)._TexID.Value + textures.GetSize(handle).X;
        }

        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0, after - before);
        Assert.Equal(1010L * (TestId + 64), sum);
    }

    /// <summary>Gives a texture reference with a texture id and no texture data, as a game registers it.</summary>
    internal static ImTextureRef TestTexture(int id)
    {
        return new ImTextureRef { _TexID = new ImTextureID(id) };
    }

    private static VkSampler FakeSampler() => new((IntPtr)1);

    private static VkImageView FakeImageView() => new((IntPtr)2);

    private static void AssertUnknown(ImGuiTextureService textures, TextureHandle handle)
    {
        ArgumentOutOfRangeException get = Assert.Throws<ArgumentOutOfRangeException>(() => textures.GetTexture(handle));
        ArgumentOutOfRangeException size = Assert.Throws<ArgumentOutOfRangeException>(() => textures.GetSize(handle));
        ArgumentOutOfRangeException remove = Assert.Throws<ArgumentOutOfRangeException>(() => textures.Remove(handle));
        Assert.Equal("texture", get.ParamName);
        Assert.Equal("texture", size.ParamName);
        Assert.Equal("texture", remove.ParamName);
        Assert.Equal(handle.Id, get.ActualValue);
    }
}
