// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the concatenation of two nested object-backed array resolvers, producing the elements of the first followed
/// by the elements of the second.
/// </summary>
/// <typeparam name="T">The element type to read.</typeparam>
/// <param name="first">The resolver providing the leading elements.</param>
/// <param name="second">The resolver providing the trailing elements.</param>
public class ObjectConcatResolver<T>(IObjectArrayResolver<T> first, IObjectArrayResolver<T> second)
	: ObjectArrayResolver<T>
{
	private readonly IObjectArrayResolver<T> _first = first;

	private readonly IObjectArrayResolver<T> _second = second;

	/// <inheritdoc/>
	public override T[] ResolveArray(GraphContext graphContext)
	{
		T[] firstValues = _first.ResolveArray(graphContext);
		T[] secondValues = _second.ResolveArray(graphContext);

		if (firstValues.Length == 0)
		{
			return secondValues;
		}

		if (secondValues.Length == 0)
		{
			return firstValues;
		}

		var result = new T[firstValues.Length + secondValues.Length];
		Array.Copy(firstValues, result, firstValues.Length);
		Array.Copy(secondValues, 0, result, firstValues.Length, secondValues.Length);
		return result;
	}
}
