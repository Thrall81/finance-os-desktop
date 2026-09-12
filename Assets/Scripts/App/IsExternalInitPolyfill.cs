namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// See Assets/Scripts/Forecast/IsExternalInitPolyfill.cs and docs/09-Decisions_techniques.md
    /// (ADR-111) — each assembly using `record`/`init` needs its own copy of this marker type.
    /// </summary>
    internal static class IsExternalInit
    {
    }
}
