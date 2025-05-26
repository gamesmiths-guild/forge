// Copyright © Gamesmiths Guild.

#if NETSTANDARD2_1
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Collections.Generic;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>Compatibility interface for netstandard2.1.</summary>
/// <typeparam name="T">Generic T.</typeparam>
public interface IReadOnlySet<out T> : IReadOnlyCollection<T>
{
	// Intentionally left blank.
}

#endif
