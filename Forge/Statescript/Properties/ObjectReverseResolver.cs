// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the elements of a nested object-backed array resolver in reverse order.
/// </summary>
/// <typeparam name="T">The element type to read.</typeparam>
/// <param name="source">The resolver providing the source array.</param>
public class ObjectReverseResolver<T>(IObjectArrayResolver<T> source) : ObjectArrayResolver<T>
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

		var result = new T[values.Length];
		Array.Copy(values, result, values.Length);
		Array.Reverse(result);
		return result;
	}
}
