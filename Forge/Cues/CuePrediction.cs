// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Cues;

/// <summary>
/// Whether a client shows a cue on an entity it mirrors the moment its own predicted run applies the cue's effect
/// there, as a shot hitting an enemy, instead of when the server's word arrives. The effect itself is always the
/// server's: only the cue is shown early.
/// </summary>
public enum CuePrediction : byte
{
	/// <summary>
	/// Shown early if it's executed, as a hit's flash or sound, and the client predicts cues on others by default
	/// (<see cref="Networking.Replication.ReplicationClient.PredictCuesOnOthers"/>); a persistent cue waits for the
	/// server.
	/// </summary>
	Default = 0,

	/// <summary>
	/// Always shown early, a persistent cue too. A persistent cue the server doesn't apply soon after is withdrawn, as
	/// interrupted.
	/// </summary>
	Always = 1,

	/// <summary>
	/// Never shown early: it waits for the server, as a damage number that shouldn't show a value the server didn't
	/// deal.
	/// </summary>
	Never = 2,
}
