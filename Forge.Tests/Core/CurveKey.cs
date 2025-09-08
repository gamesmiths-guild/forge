// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Tests.Core;

/// <summary>
/// Represents a key into a <see cref="Curve"/>.
/// </summary>
/// <param name="Time">The time of the curve key.</param>
/// <param name="Value">The value of the curve key.</param>
public readonly record struct CurveKey(float Time, float Value);
