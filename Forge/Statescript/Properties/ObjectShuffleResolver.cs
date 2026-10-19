// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a random permutation of a nested object-backed array resolver using a Fisher-Yates shuffle.
/// </summary>
/// <remarks>
/// Combine with an ObjectTake resolver to pick N random elements without repetition.
/// </remarks>
/// <typeparam name="T">The element type to read.</typeparam>
/// <param name="source">The resolver providing the source array.</param>
/// <param name="randomProvider">The random provider used to shuffle when the graph has no
/// <see cref="GraphContext.Random"/>.</param>
public class ObjectShuffleResolver<T>(IObjectArrayResolver<T> source, IRandom randomProvider)
	: ObjectArrayResolver<T>
{
	private readonly IObjectArrayResolver<T> _source = source;

	private readonly IRandom _randomProvider = randomProvider
		?? throw new ArgumentNullException(nameof(randomProvider));

	/// <inheritdoc/>
	public override T[] ResolveArray(GraphContext graphContext)
	{
		T[] sourceValues = _source.ResolveArray(graphContext);

		if (sourceValues.Length <= 1)
		{
			return sourceValues;
		}

		var values = new T[sourceValues.Length];

		IRandom random = graphContext.Random ?? _randomProvider;

		// Inside-out Fisher-Yates: fills the copy while shuffling in a single pass.
		for (int i = 0; i < sourceValues.Length; i++)
		{
			int j = random.NextIntInclusive(0, i);

			if (j != i)
			{
				values[i] = values[j];
			}

			values[j] = sourceValues[i];
		}

		return values;
	}
}
