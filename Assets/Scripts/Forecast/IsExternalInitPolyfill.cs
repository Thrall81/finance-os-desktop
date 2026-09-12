namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Unity's scripting runtime does not ship this marker type, which the C# compiler requires
    /// to emit `record` types and `init`-only properties (an empty compile-time-only marker,
    /// standard workaround for targets older than .NET 5). See docs/09-Decisions_techniques.md
    /// (ADR-110 covers the same class of runtime gap for DateOnly).
    /// </summary>
    internal static class IsExternalInit
    {
    }
}
