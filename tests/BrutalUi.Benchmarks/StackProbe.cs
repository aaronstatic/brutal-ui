using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BrutalUi.Core.Tests;

namespace BrutalUi.Benchmarks;

/// <summary>
/// Finds the deepest plain column chain that one <c>Update</c> lays out on a thread with a set stack size.
/// </summary>
/// <remarks>
/// <para>
/// This class closes the hand-off of CR-10 of the flexbox-layout code review. The layout pass uses several stack
/// frames for each level, so a deep chain ends the process with a stack overflow. A process cannot catch a stack
/// overflow. So <see cref="FindLimit"/> starts the program again for each probe, with the arguments
/// <c>--stack-child &lt;depth&gt; &lt;stackBytes&gt;</c>, and reads the exit code and the standard error text. An exit
/// code that is not 0 with a text that starts with <c>Stack overflow.</c> means an overflow. Any other non-zero
/// code is a fault of the child, and the probe throws.
/// </para>
/// <para>
/// The parent redirects the standard output and the standard error of the child and reads both to the end. So the
/// overflow trace does not reach the console, and a full pipe does not block the child. The parent waits for at most
/// one minute for each child, for the streams and the exit together, and kills a child that hangs. The class runs outside the frame loop and can allocate
/// (Assumption A7).
/// </para>
/// <para>
/// The search is exponential. It starts at <see cref="StartDepth"/>, doubles the depth until a child fails, then
/// halves the range between the last pass and the first fail. The search stops at <see cref="MaxDepth"/>, because
/// the build of a chain is quadratic and a deep chain is slow to build.
/// </para>
/// </remarks>
internal static class StackProbe
{
    /// <summary>The first depth that <see cref="FindLimit"/> tries.</summary>
    public const int StartDepth = 64;

    /// <summary>The largest depth that <see cref="FindLimit"/> tries (16 MB at 2 KB for each level).</summary>
    public const int MaxDepth = 8192;

    private const string OverflowPrefix = "Stack overflow.";

    private static readonly TimeSpan ChildTimeout = TimeSpan.FromMinutes(1);

    private const float FrameTime = 0f;

    /// <summary>
    /// Builds a plain column chain and runs one <c>Update</c> on a thread with a set stack size.
    /// </summary>
    /// <param name="depth">The number of levels below the root.</param>
    /// <param name="stackBytes">The stack size of the layout thread, in bytes.</param>
    /// <returns>0. A stack overflow ends the process with a non-zero code before the method returns.</returns>
    public static int RunChild(int depth, int stackBytes)
    {
        ChainTree chain = ChainTree.Build(ChainShape.PlainColumn, depth);

        Thread thread = new(() => chain.Context.Update(FrameTime), stackBytes);
        thread.Start();
        thread.Join();

        return 0;
    }

    /// <summary>
    /// Finds the largest depth that passes on a stack of the given size.
    /// </summary>
    /// <param name="stackBytes">The stack size of the layout thread, in bytes.</param>
    /// <returns>
    /// The largest depth that passes, from 1 to <see cref="MaxDepth"/>. The value <see cref="MaxDepth"/> means
    /// that the depth passes and that the limit is not below it ("or more").
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// The depth 1 fails, a child process does not start, a child ends with a fault that is not a stack overflow,
    /// or a child does not end in time.
    /// </exception>
    public static int FindLimit(int stackBytes)
    {
        // Invariant: low passes and high fails. A low of 0 means that no depth passed yet.
        int low = 0;
        int high = 0;
        int depth = StartDepth;
        while (true)
        {
            if (Passes(depth, stackBytes))
            {
                low = depth;
                if (depth >= MaxDepth)
                {
                    return MaxDepth;
                }

                depth = Math.Min(depth * 2, MaxDepth);
            }
            else
            {
                high = depth;
                break;
            }
        }

        if (low == 0)
        {
            if (!Passes(1, stackBytes))
            {
                throw new InvalidOperationException("A chain of depth 1 fails on this stack size.");
            }

            low = 1;
        }

        while (high - low > 1)
        {
            int middle = low + ((high - low) / 2);
            if (Passes(middle, stackBytes))
            {
                low = middle;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }

    /// <summary>
    /// Decides whether a finished child passed, failed with a stack overflow, or ended with another fault.
    /// </summary>
    /// <param name="depth">The depth of the child, for the message.</param>
    /// <param name="exitCode">The exit code of the child.</param>
    /// <param name="errorText">The standard error text of the child.</param>
    /// <returns><see langword="true"/> for exit code 0, <see langword="false"/> for a stack overflow.</returns>
    /// <exception cref="InvalidOperationException">The exit code is not 0 and the text is not a stack overflow.</exception>
    internal static bool Interpret(int depth, int exitCode, string errorText)
    {
        if (exitCode == 0)
        {
            return true;
        }

        if (errorText.StartsWith(OverflowPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        int lineEnd = errorText.IndexOfAny(['\r', '\n']);
        string firstLine = lineEnd < 0 ? errorText : errorText[..lineEnd];
        throw new InvalidOperationException(string.Create(
            CultureInfo.InvariantCulture,
            $"The child of depth {depth} ended with exit code {exitCode} and no stack overflow text. First line: \"{firstLine}\""));
    }

    private static TimeSpan Remaining(Stopwatch clock)
    {
        TimeSpan left = ChildTimeout - clock.Elapsed;
        return left > TimeSpan.Zero ? left : TimeSpan.Zero;
    }

    private static bool Passes(int depth, int stackBytes)
    {
        string processPath = Environment.ProcessPath
            ?? throw new InvalidOperationException("The path of the current process is not known.");

        ProcessStartInfo startInfo = new()
        {
            FileName = processPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        string fileName = Path.GetFileName(processPath);
        if (string.Equals(fileName, "dotnet", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fileName, "dotnet.exe", StringComparison.OrdinalIgnoreCase))
        {
            string assemblyPath = Assembly.GetEntryAssembly()?.Location
                ?? throw new InvalidOperationException("The path of the entry assembly is not known.");
            startInfo.ArgumentList.Add(assemblyPath);
        }

        startInfo.ArgumentList.Add("--stack-child");
        startInfo.ArgumentList.Add(depth.ToString(CultureInfo.InvariantCulture));
        startInfo.ArgumentList.Add(stackBytes.ToString(CultureInfo.InvariantCulture));

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("The child process did not start.");

        // Read both streams to the end before the wait. The overflow trace stays out of the console.
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();

        // One deadline covers the stream wait and the exit wait.
        Stopwatch clock = Stopwatch.StartNew();
        if (!Task.WaitAll([output, error], ChildTimeout)
            || !process.WaitForExit(Remaining(clock)))
        {
            process.Kill(entireProcessTree: true);
            throw new InvalidOperationException(string.Create(
                CultureInfo.InvariantCulture,
                $"The child of depth {depth} did not end in {ChildTimeout.TotalSeconds:F0} s. The probe killed it."));
        }

        return Interpret(depth, process.ExitCode, error.Result);
    }
}
