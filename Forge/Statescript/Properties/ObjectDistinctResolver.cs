// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the distinct elements of a nested object-backed array resolver, keeping the first occurrence of each
/// reference and preserving the original order. Elements are matched by reference identity, making this useful to avoid
/// processing the same target twice.
/// </summary>
/// <typeparam name="T">The element type to read.</typeparam>
/// <param name="source">The resolver providing the source array.</param>
public class ObjectDistinctResolver<T>(IObjectArrayResolver<T> source) : ObjectArrayResolver<T>
{
	private readonly IObjectArrayResolver<T> _source = source;

	/// <inheritdoc/>
	public override T[] ResolveArray(GraphContext graphContext)
	{
		T[] values = _source.ResolveArray(graphContext);

		if (values.Length <= 1)
		{
			return values;
		}

		var result = new List<T>(values.Length);

		for (int i = 0; i < values.Length; i++)
		{
			if (!ContainsReference(result, values[i]))
			{
				result.Add(values[i]);
			}
		}

		return [.. result];
	}

	private static bool ContainsReference(List<T> values, T value)
	{
		for (int i = 0; i < values.Count; i++)
		{
			if (ReferenceEquals(values[i], value))
			{
				return true;
			}
		}

		return false;
	}
}
