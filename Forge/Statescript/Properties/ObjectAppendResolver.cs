// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a nested object-backed array resolver with additional elements appended to the end. Each appended element
/// is produced by its own nested object resolver, allowing stored references or computed ones to be added.
/// </summary>
/// <typeparam name="T">The element type to read.</typeparam>
public class ObjectAppendResolver<T> : ObjectArrayResolver<T>
{
	private readonly IObjectArrayResolver<T> _source;

	private readonly IObjectResolver<T>[] _elements;

	/// <summary>
	/// Initializes a new instance of the <see cref="ObjectAppendResolver{T}"/> class.
	/// </summary>
	/// <param name="source">The resolver providing the source array.</param>
	/// <param name="elements">The nested resolvers producing the elements to append.</param>
	public ObjectAppendResolver(IObjectArrayResolver<T> source, params IObjectResolver<T>[] elements)
	{
		ValidateElements(elements);
		_source = source;
		_elements = elements;
	}

	/// <inheritdoc/>
	public override T[] ResolveArray(GraphContext graphContext)
	{
		T[] values = _source.ResolveArray(graphContext);
		var result = new T[values.Length + _elements.Length];
		Array.Copy(values, result, values.Length);

		for (int i = 0; i < _elements.Length; i++)
		{
			result[values.Length + i] = _elements[i].Resolve(graphContext)!;
		}

		return result;
	}

	private static void ValidateElements(IObjectResolver<T>[] elements)
	{
#if NET8_0_OR_GREATER
		ArgumentNullException.ThrowIfNull(elements);
#else
		if (elements is null)
		{
			throw new ArgumentNullException(nameof(elements));
		}
#endif

		for (int i = 0; i < elements.Length; i++)
		{
			if (elements[i] is null)
			{
				throw new ArgumentException(
					"ObjectAppendResolver does not allow null element resolvers.",
					nameof(elements));
			}
		}
	}
}
