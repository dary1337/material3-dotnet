using System.Runtime.CompilerServices;
using Material3.WinForms.Tokens;

namespace Material3.WinForms.Tests {
    internal static class TestSetup {
        // A runner with Windows animations turned off must still take the animated paths the tests cover.
#pragma warning disable CA2255
        [ModuleInitializer]
#pragma warning restore CA2255
        internal static void PinAnimations() => Motion.IgnoreSystemSetting = true;
    }
}

#if NET472
namespace System.Runtime.CompilerServices {
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class ModuleInitializerAttribute : Attribute { }
}
#endif
