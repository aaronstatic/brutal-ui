using Xunit.Sdk;
using Xunit.v3;

// ImGui has one current context for the process. Each test creates a context, makes it current, and destroys it.
// Two tests in parallel would change the current context of each other, so the tests run one at a time.
// xunit.v3 4.0 makes CollectionBehavior(DisableTestParallelization = true) an obsolete error. Mode None is its
// replacement: it disables all parallel runs in this assembly.
[assembly: Parallelization(Mode = ParallelMode.None)]
