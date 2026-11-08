// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Core;

// What a predicting client and the server compare of an entity, its prediction checksum: what the server sends the
// entity's owner and the client can be put right on. The effects the owner hears of, abilities' runs the owner
// predicts, and times in whole ticks, since an effect the client made from the server's word counts its time from a
// tick rather than from the same sums of fixed steps. Shared variables, input and outside state aren't sent, and the
// body is compared on its own, within a quantum.
internal sealed class PredictionScope(Func<ActiveEffect, bool> heardByOwner, int tickRate)
{
	// A time this close above a whole number of ticks is that number: a time is a sum of fixed steps with their
	// rounding errors.
	private const double TickTolerance = 1e-6;

	public bool HeardByOwner(ActiveEffect effect)
	{
		return heardByOwner(effect);
	}

	public long Ticks(double seconds)
	{
		return (long)Math.Ceiling((seconds * tickRate) - TickTolerance);
	}
}
