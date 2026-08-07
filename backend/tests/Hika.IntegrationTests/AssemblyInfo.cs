using Xunit;

// Each test class fixture spins up its own Postgres container via Testcontainers. Running
// multiple classes in parallel multiplies container-startup load and has been a source of
// flaky "entry point exited without building an IHost" failures under resource contention —
// running collections sequentially trades a bit of wall-clock time for reliability, which is
// the right trade for integration tests.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
