using System;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The shared helper of the allocation gates. Each gate calls <see cref="ReleaseAllocationContext"/> after the last
/// allocation of the test and right before each measured region.
/// </summary>
/// <remarks>
/// <para>
/// The cause: a background GC can add the free part of the allocation context of a thread (less than 8 KB) to
/// <see cref="GC.GetAllocatedBytesForCurrentThread"/>, also when the thread allocates nothing. Other test classes
/// allocate in parallel, so a background GC can start during a measure. An empty context has no free part, so the
/// counter then changes only when the measured code allocates.
/// </para>
/// <para>
/// The benchmark project and the ImGui test project link this file, so the file uses no test framework type.
/// "Phase 3 Fix 1" in <c>docs/features/imgui-renderer/hello-panel-sample/context.md</c> holds the evidence.
/// </para>
/// </remarks>
internal static class AllocationGate
{
    /// <summary>
    /// Runs a blocking GC of generation 0. Each GC empties the allocation context of each thread, so the context of
    /// this thread has no free part after the call.
    /// </summary>
    /// <remarks>
    /// The release does not change the count of a later allocation. A measured region that allocates still fails the
    /// gate. When the test allocates between two measured regions, call the release again before the second region.
    /// </remarks>
    public static void ReleaseAllocationContext()
    {
        GC.Collect(0, GCCollectionMode.Forced, blocking: true);
    }
}
