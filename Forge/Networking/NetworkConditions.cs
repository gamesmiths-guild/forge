// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Networking;

/// <summary>
/// What a <see cref="ConditionedTransport"/> does to the packets it sends, to stand in for a real link.
/// </summary>
/// <param name="LatencySeconds">How long every packet takes to arrive, one way. Half the round trip.</param>
/// <param name="JitterSeconds">How much the latency varies: each packet's is drawn between the latency minus this and
/// plus this. Enough jitter reorders unreliable packets.</param>
/// <param name="LossRate">The share of unreliable packets that never arrive, from 0 to 1. Reliable packets always
/// arrive, since a real reliable channel resends them; they only take longer.</param>
public readonly record struct NetworkConditions(double LatencySeconds, double JitterSeconds = 0, float LossRate = 0)
{
	/// <summary>
	/// A perfect link: nothing is delayed or lost.
	/// </summary>
	public static readonly NetworkConditions Perfect = new(0);

	/// <summary>
	/// A good home connection: 50 ms each way, a little jitter, no loss.
	/// </summary>
	public static readonly NetworkConditions Good = new(0.05, 0.005);

	/// <summary>
	/// A long-distance connection: 100 ms each way, some jitter, 2% loss.
	/// </summary>
	public static readonly NetworkConditions Far = new(0.1, 0.02, 0.02f);

	/// <summary>
	/// A poor connection: 200 ms each way, heavy jitter, 5% loss. The conditions the plan tests prediction under.
	/// </summary>
	public static readonly NetworkConditions Poor = new(0.2, 0.05, 0.05f);
}
