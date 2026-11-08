// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Networking.Replication;

// A client's own entity, which it predicts: the ticks it simulated, and what the server last said of it, which a
// packet's tick is checked against.
internal sealed class Prediction(IForgeEntity entity, int attributeCount, int capacity, double fixedDeltaTime)
{
	public EntityHistory History { get; } = new EntityHistory(entity, capacity, fixedDeltaTime);

	// The base value, minimum and maximum of each attribute, as the server last sent them.
	public ServerAttribute?[] Attributes { get; } = new ServerAttribute?[attributeCount];

	// The effects the server says are on the entity, by instance, for the ones it heard of.
	public Dictionary<uint, ServerEffect> Effects { get; } = [];

	// This machine's copies of the effects that landed on the server's entity from outside its ticks.
	public Dictionary<uint, ActiveEffect> Taken { get; } = [];

	// The runs the server started from outside the entity's ticks, by instance, while they run there.
	public Dictionary<uint, ServerRun> Runs { get; } = [];

	// This machine's runs started for those.
	public Dictionary<uint, AbilityInstance> TakenRuns { get; } = [];

	public QuantizedBody? Body { get; set; }

	public bool HasBody { get; set; }

	public ulong Checksum { get; set; }

	// The tick of the packet whose checksum is the last one read, or -1 before any.
	public long ChecksumTick { get; set; } = -1;

	// The newest tick the server agreed with, or -1 before any.
	public long ConfirmedTick { get; set; } = -1;

	public readonly record struct ServerAttribute(int BaseValue, int Min, int Max);

	// A run the server started from outside the entity's ticks, after the tick it started after.
	public readonly record struct ServerRun(
		AbilityData Data,
		uint TargetId,
		uint SourceId,
		float Magnitude,
		long AfterTick);
}
