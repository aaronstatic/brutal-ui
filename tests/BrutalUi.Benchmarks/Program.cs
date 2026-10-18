using System;
using System.Globalization;
using BenchmarkDotNet.Running;

namespace BrutalUi.Benchmarks;

/// <summary>
/// The entry point of the benchmark program. The first argument selects one of three modes.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description><c>--facts</c> prints four <c>facts:</c> lines (see <see cref="FactsReport"/>).</description></item>
/// <item><description><c>--stack-child &lt;depth&gt; &lt;stackBytes&gt;</c> is the child process of the stack probe (see <see cref="StackProbe"/>). The facts report starts it. A developer does not run it by hand.</description></item>
/// <item><description>Any other arguments go to the BenchmarkDotNet switcher.</description></item>
/// </list>
/// </remarks>
internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--facts")
        {
            return FactsReport.Run();
        }

        if (args.Length == 3 && args[0] == "--stack-child")
        {
            int depth = int.Parse(args[1], CultureInfo.InvariantCulture);
            int stackBytes = int.Parse(args[2], CultureInfo.InvariantCulture);

            return StackProbe.RunChild(depth, stackBytes);
        }

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

        return 0;
    }
}
