using System;

namespace BrutalUi.Framework.Tests;

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
/// This file is a copy of <c>tests/BrutalUi.Core.Tests/Shared/AllocationGate.cs</c>. The Framework tests do not
/// reference <c>BrutalUi.Core.Tests</c>, so they cannot use the original. Keep the two files the same in code.
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
