// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Core;

// Numbers everything that takes hold of something outside itself - an active effect, a granted ability's trigger, a
// running state node - in the order it does, across every entity and graph. An entity restore takes hold again in this
// order, so handlers sharing an event run in the order they first subscribed in.
internal static class HoldSequence
{
	private static long _last;

	internal static long Next()
	{
		return Interlocked.Increment(ref _last);
	}
}
