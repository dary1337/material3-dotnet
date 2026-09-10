using System.Runtime.CompilerServices;

// ValueAt is the pointer-to-value mapping the slider chrome runs on: a pure function worth pinning, and not
// worth a public API of its own.
[assembly: InternalsVisibleTo("Material3.Wpf.Tests")]
