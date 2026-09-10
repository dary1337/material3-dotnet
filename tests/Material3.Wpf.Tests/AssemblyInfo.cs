using Xunit;

// WPF resource dictionaries loaded from a pack:// URI are cached per process, so two test classes running at
// once merge the same instance from two UI threads — and a DynamicResource intermittently resolves to nothing.
// These tests each own a window; running them one at a time is what the framework they exercise expects.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
