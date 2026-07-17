// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the elements of a nested object-backed array resolver that also appear in a second nested object-backed
/// array resolver, preserving their original order. Elements are matched by reference identity.
/// </summary>
/// <remarks>
/// Unlike LINQ's set-based <c>Intersect</c>, duplicates in the source are preserved when they appear in
/// <paramref name="other"/>.
/// </remarks>
/// <typeparam name="T">The element type to read.</typeparam>
/// <param name="source">The resolver providing the source array.</param>
/// <param name="other">The resolver providing the elements to keep.</param>
public class ObjectIntersectResolver<T>(IObjectArrayResolver<T> source, IObjectArrayResolver<T> other)
	: ObjectArrayResolver<T>
{
	private readonly IObjectArrayResolver<T> _source = source;

	private readonly IObjectArrayResolver<T> _other = other;

	/// <inheritdoc/>
	public override T[] ResolveArray(GraphContext graphContext)
	{
		T[] values = _source.ResolveArray(graphContext);

		if (values.Length == 0)
		{
			return values;
		}

		T[] keptValues = _other.ResolveArray(graphContext);

		if (keptValues.Length == 0)
		{
			return [];
		}

		var result = new List<T>(values.Length);

		for (int i = 0; i < values.Length; i++)
		{
			if (ContainsReference(keptValues, values[i]))
			{
				result.Add(values[i]);
			}
		}

		return [.. result];
	}

	private static bool ContainsReference(T[] values, T value)
	{
		for (int i = 0; i < values.Length; i++)
		{
			if (ReferenceEquals(values[i], value))
			{
				return true;
			}
		}

		return false;
	}
}
