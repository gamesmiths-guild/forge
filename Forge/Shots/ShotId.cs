// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Shots;

/// <summary>
/// Names a projectile an entity launched: the tick it was launched in, and how many the entity launched before it in
/// that tick. The server and the client that predicts the entity launch the same projectiles in the same order, so they
/// name them alike.
/// </summary>
/// <param name="Tick">The tick the projectile was launched in.</param>
/// <param name="Sequence">How many projectiles the entity launched before it in that tick. One its tick launched that
/// the client predicting it doesn't run, as an unpredicted ability's, counts down from -1 instead.</param>
public readonly record struct ShotId(long Tick, int Sequence);
