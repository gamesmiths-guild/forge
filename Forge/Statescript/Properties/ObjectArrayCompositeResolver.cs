// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves an object-backed array by evaluating a nested object resolver for each element in order.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public class ObjectArrayCompositeResolver<T> : ObjectArrayResolver<T>
{
	private readonly IObjectResolver<T>[] _elementResolvers;

	/// <summary>
	/// Initializes a new instance of the <see cref="ObjectArrayCompositeResolver{T}"/> class.
	/// </summary>
	/// <param name="elementResolvers">The nested resolvers that produce the array elements.</param>
	public ObjectArrayCompositeResolver(params IObjectResolver<T>[] elementResolvers)
	{
#if NET8_0_OR_GREATER
		ArgumentNullException.ThrowIfNull(elementResolvers);
#else
		if (elementResolvers is null)
		{
			throw new ArgumentNullException(nameof(elementResolvers));
		}
#endif

		for (int i = 0; i < elementResolvers.Length; i++)
		{
			if (elementResolvers[i] is null)
			{
				throw new ArgumentException(
					"ObjectArrayCompositeResolver does not allow null element resolvers.",
					nameof(elementResolvers));
			}
		}

		_elementResolvers = elementResolvers;
	}

	/// <inheritdoc/>
	public override T[] ResolveArray(GraphContext graphContext)
	{
		var values = new T[_elementResolvers.Length];

		for (int i = 0; i < _elementResolvers.Length; i++)
		{
			values[i] = _elementResolvers[i].Resolve(graphContext)!;
		}

		return values;
	}
}
