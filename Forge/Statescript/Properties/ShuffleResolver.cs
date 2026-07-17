// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a random permutation of a nested array resolver using a Fisher-Yates shuffle.
/// </summary>
/// <remarks>
/// Combine with a Take resolver to pick N random elements without repetition.
/// </remarks>
/// <param name="source">The resolver providing the source array.</param>
/// <param name="randomProvider">The random provider used to shuffle.</param>
public class ShuffleResolver(IArrayPropertyResolver source, IRandom randomProvider) : IArrayPropertyResolver
{
	private readonly IArrayPropertyResolver _source = source;

	private readonly IRandom _randomProvider = randomProvider
		?? throw new ArgumentNullException(nameof(randomProvider));

	/// <inheritdoc/>
	public Type ElementType { get; } = source.ElementType;

	/// <inheritdoc/>
	public Variant128[] ResolveArray(GraphContext graphContext)
	{
		Variant128[] sourceValues = _source.ResolveArray(graphContext);

		if (sourceValues.Length <= 1)
		{
			return sourceValues;
		}

		var values = new Variant128[sourceValues.Length];

		// Inside-out Fisher-Yates: fills the copy while shuffling in a single pass.
		for (int i = 0; i < sourceValues.Length; i++)
		{
			int j = _randomProvider.NextIntInclusive(0, i);

			if (j != i)
			{
				values[i] = values[j];
			}

			values[j] = sourceValues[i];
		}

		return values;
	}
}
