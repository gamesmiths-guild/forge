// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves an array by evaluating a nested resolver for each element in order.
/// </summary>
/// <remarks>
/// Use this when the array contents should be composed from other resolvers rather than read from a variable. All
/// nested resolvers must produce the same value type, either inferred from the first resolver or supplied explicitly.
/// </remarks>
public class ArrayResolver : IArrayPropertyResolver
{
	private readonly IPropertyResolver[] _elementResolvers;

	/// <inheritdoc/>
	public Type ElementType { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ArrayResolver"/> class, inferring the element type from the first
	/// nested resolver.
	/// </summary>
	/// <param name="elementResolvers">The nested resolvers that produce the array elements.</param>
	public ArrayResolver(params IPropertyResolver[] elementResolvers)
		: this(ResolveElementType(elementResolvers), elementResolvers)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ArrayResolver"/> class with an explicit element type.
	/// </summary>
	/// <param name="elementType">The type of each array element.</param>
	/// <param name="elementResolvers">The nested resolvers that produce the array elements.</param>
	public ArrayResolver(Type elementType, params IPropertyResolver[] elementResolvers)
	{
#if NET8_0_OR_GREATER
		ArgumentNullException.ThrowIfNull(elementType);
		ArgumentNullException.ThrowIfNull(elementResolvers);
#else
		if (elementType is null)
		{
			throw new ArgumentNullException(nameof(elementType));
		}

		if (elementResolvers is null)
		{
			throw new ArgumentNullException(nameof(elementResolvers));
		}
#endif

		ValidateResolverTypes(elementType, elementResolvers);
		ElementType = elementType;
		_elementResolvers = elementResolvers;
	}

	/// <inheritdoc/>
	public Variant128[] ResolveArray(GraphContext graphContext)
	{
		var values = new Variant128[_elementResolvers.Length];

		for (int i = 0; i < _elementResolvers.Length; i++)
		{
			values[i] = _elementResolvers[i].Resolve(graphContext);
		}

		return values;
	}

	private static Type ResolveElementType(IPropertyResolver[] elementResolvers)
	{
#if NET8_0_OR_GREATER
		ArgumentNullException.ThrowIfNull(elementResolvers);
#else
		if (elementResolvers is null)
		{
			throw new ArgumentNullException(nameof(elementResolvers));
		}
#endif

		if (elementResolvers.Length == 0)
		{
			throw new ArgumentException(
				"ArrayResolver requires at least one element resolver when no explicit element type is provided.",
				nameof(elementResolvers));
		}

		if (elementResolvers[0] is null)
		{
			throw new ArgumentException(
				"ArrayResolver does not allow null element resolvers.",
				nameof(elementResolvers));
		}

		Type elementType = elementResolvers[0].ValueType;
		ValidateResolverTypes(elementType, elementResolvers);
		return elementType;
	}

	private static void ValidateResolverTypes(Type elementType, IPropertyResolver[] elementResolvers)
	{
		for (int i = 0; i < elementResolvers.Length; i++)
		{
			IPropertyResolver? resolver = elementResolvers[i];

			if (resolver is null)
			{
				throw new ArgumentException(
					"ArrayResolver does not allow null element resolvers.",
					nameof(elementResolvers));
			}

			if (resolver.ValueType != elementType)
			{
				throw new ArgumentException(
					$"ArrayResolver element resolver at index {i} produces '{resolver.ValueType}', " +
					$"which does not match the configured element type '{elementType}'.",
					nameof(elementResolvers));
			}
		}
	}
}
