// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Networking.Replication;

// An effect the server says is on a predicted entity, as it last said, with when it landed for one from outside.
internal sealed class ServerEffect(EffectData data, EffectApplicationContext? context, bool fromOutside, long afterTick)
{
	public EffectData Data { get; } = data;

	public EffectApplicationContext? Context { get; } = context;

	public bool FromOutside { get; } = fromOutside;

	// The tick the effect landed after, for one from outside.
	public long AfterTick { get; } = afterTick;

	public int Level { get; set; }

	public int StackCount { get; set; }

	public bool IsInhibited { get; set; }

	public bool IsTimed { get; set; }

	public long EndTick { get; set; }

	public uint OwnerId { get; set; }

	public uint SourceId { get; set; }

	public int ExecutionCount { get; set; }

	public KeyValuePair<Tag, float>[] SetByCallers { get; set; } = [];
}
