// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Networking.Replication;

// Where an entity a client predicts is on the client's timeline.
internal enum PredictionState
{
	// Advancing with the timeline.
	InWorld = 0,

	// Freed in the tick being simulated, or since the last one, and out of the simulation once it's recorded.
	Leaving = 1,

	// Out of the simulation, having let go of everything, until something brings it back.
	Limbo = 2,
}

// An entity a client predicts, on the client's timeline: one the server spawned for this client's peer, or one this
// client spawned ahead of the server. It keeps the ticks the client simulated, and what the server last said of the
// entity, which a packet's tick is checked against.
internal sealed class Prediction(
	IForgeEntity entity,
	int attributeCount,
	int capacity,
	double fixedDeltaTime,
	SpawnKey? key)
{
	public IForgeEntity Entity { get; } = entity;

	public EntityHistory History { get; } = new EntityHistory(entity, capacity, fixedDeltaTime);

	// What the server's spawn of one this client made ahead of it is found by.
	public SpawnKey? Key { get; } = key;

	// Gives one this client made the id of the server's spawn of it.
	public Action<NetworkEntityId>? Bind { get; init; }

	// Whether the server spawned it, or its spawn of one this client made arrived.
	public bool IsBound { get; set; }

	public PredictionState State { get; set; }

	// The first tick recorded with it on the timeline, or -1 before it's recorded.
	public long JoinTick { get; set; } = -1;

	// The first tick recorded without it after it was freed.
	public long? LeaveTick { get; set; }

	// Made by this client and no longer what its timeline makes: no rollback brings it back, but the server's spawn of
	// it does.
	public bool Unconfirmed { get; set; }

	// The base value, minimum and maximum of each attribute, as the server last sent them.
	public ServerAttribute?[] Attributes { get; } = new ServerAttribute?[attributeCount];

	// The tags the server says the entity has.
	public HashSet<Tag> Tags { get; } = [];

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

	// Whether a rollback to the tick has it come back as the tick left it.
	public bool WasIn(long tick)
	{
		return !Unconfirmed
			&& JoinTick >= 0
			&& JoinTick <= tick
			&& (LeaveTick is null || tick < LeaveTick)
			&& History.Holds(tick);
	}

	public readonly record struct ServerAttribute(int BaseValue, int Min, int Max);

	// A run the server started from outside the entity's ticks, after the tick it started after.
	public readonly record struct ServerRun(
		AbilityData Data,
		uint TargetId,
		uint SourceId,
		float Magnitude,
		long AfterTick);
}
