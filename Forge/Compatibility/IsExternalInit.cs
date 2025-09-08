// Copyright © Gamesmiths Guild.

#if NETSTANDARD2_1
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Runtime.CompilerServices;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Reserved to be used by the compiler for tracking metadata.
/// This class should not be used by developers in source code.
/// </summary>
#pragma warning disable S2094 // Remove this empty class, write its code or make it an "interface".
internal static class IsExternalInit;
#pragma warning restore S2094 // Remove this empty class, write its code or make it an "interface".
#endif
