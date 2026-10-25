// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Core;

/// <summary>
/// The state of an entity's body that a tick's movement starts from: where it is, which way it faces, how fast it moves
/// and whether it stands on the ground.
/// </summary>
/// <remarks>
/// A 2D body uses the X and Y components, leaves Z at zero, and turns about the Z axis.
/// </remarks>
/// <param name="Position">The body's position.</param>
/// <param name="Rotation">The body's rotation.</param>
/// <param name="Velocity">The body's velocity, in units per second.</param>
/// <param name="Grounded">Whether the body stands on the ground.</param>
public readonly record struct BodyState(Vector3 Position, Quaternion Rotation, Vector3 Velocity, bool Grounded);
