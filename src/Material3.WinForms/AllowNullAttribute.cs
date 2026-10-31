#if NETFRAMEWORK
namespace System.Diagnostics.CodeAnalysis {
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue)]
    internal sealed class AllowNullAttribute : Attribute { }
}
#endif
