// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;

namespace Gamesmiths.Forge.Networking.Replication;

// Numbers the spawns graphs make in each tick, per entity, ability and node, so each spawn gets the key the other
// machine gives it.
internal sealed class SpawnKeys
{
	private readonly Dictionary<Slot, int> _made = [];

	// The key of the next spawn the node makes for the run, in the tick its entity is in, or null before the entity's
	// first tick, which has no tick to key it by.
	public SpawnKey? Next(AbilityBehaviorContext context, int node)
	{
		long tick = context.Owner.EffectsManager.Tick;

		if (tick < 0)
		{
			return null;
		}

		uint ability = (uint)Fnv64.Add(Fnv64.Offset, context.AbilityHandle.Ability!.AbilityData.Name);
		var slot = new Slot(context.Owner.NetworkId.Value, tick, ability, node);
		_made.TryGetValue(slot, out int sequence);
		_made[slot] = sequence + 1;
		return new SpawnKey(context.Owner.NetworkId, tick, ability, node, sequence);
	}

	// Forgets the spawns made before a tick, which nothing numbers again.
	public void ForgetBefore(long tick)
	{
		Forget(x => x.Tick < tick);
	}

	// Forgets the spawns made after a tick, so a correction running those ticks again numbers theirs as they were.
	public void ForgetAfter(long tick)
	{
		Forget(x => x.Tick > tick);
	}

	private void Forget(Func<Slot, bool> which)
	{
		foreach (Slot slot in _made.Keys.Where(which).ToList())
		{
			_made.Remove(slot);
		}
	}

	private readonly record struct Slot(uint Spawner, long Tick, uint Ability, int Node);
}
