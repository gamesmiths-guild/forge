// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Four input actions read together as one analog two-dimensional value, such as a movement stick: the X axis runs from
/// <paramref name="NegativeX"/> to <paramref name="PositiveX"/>, the Y axis from <paramref name="NegativeY"/> to
/// <paramref name="PositiveY"/>.
/// </summary>
/// <param name="NegativeX">The action that pushes X towards -1.</param>
/// <param name="PositiveX">The action that pushes X towards 1.</param>
/// <param name="NegativeY">The action that pushes Y towards -1.</param>
/// <param name="PositiveY">The action that pushes Y towards 1.</param>
public readonly record struct InputVector(string NegativeX, string PositiveX, string NegativeY, string PositiveY);
