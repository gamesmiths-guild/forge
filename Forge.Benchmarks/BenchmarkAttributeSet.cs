// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Attributes;

namespace Gamesmiths.Forge.Benchmarks;

internal sealed class BenchmarkAttributeSet : AttributeSet
{
	public EntityAttribute Health { get; }

	public EntityAttribute Speed { get; }

	public BenchmarkAttributeSet()
	{
		Health = InitializeAttribute(nameof(Health), 1_000_000, 0, 2_000_000, 0);
		Speed = InitializeAttribute(nameof(Speed), 5, 0, 99, 2);
	}
}
