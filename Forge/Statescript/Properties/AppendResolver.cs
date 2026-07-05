// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a nested array resolver with additional elements appended to the end. Each appended element is produced by
/// its own nested resolver, allowing constants, variables, or computed values to be added.
/// </summary>
public class AppendResolver : IArrayPropertyResolver
{
	private readonly IArrayPropertyResolver _source;

	private readonly IPropertyResolver[] _elements;

	/// <inheritdoc/>
	public Type ElementType { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="AppendResolver"/> class.
	/// </summary>
	/// <param name="source">The resolver providing the source array.</param>
	/// <param name="elements">The nested resolvers producing the elements to append. Each must resolve to the source
	/// element type.</param>
	public AppendResolver(IArrayPropertyResolver source, params IPropertyResolver[] elements)
	{
		ValidateElements(source.ElementType, elements);
		_source = source;
		_elements = elements;
		ElementType = source.ElementType;
	}

	/// <inheritdoc/>
	public Variant128[] ResolveArray(GraphContext graphContext)
	{
		Variant128[] values = _source.ResolveArray(graphContext);
		var result = new Variant128[values.Length + _elements.Length];
		Array.Copy(values, result, values.Length);

		for (int i = 0; i < _elements.Length; i++)
		{
			result[values.Length + i] = _elements[i].Resolve(graphContext);
		}

		return result;
	}

	private static void ValidateElements(Type elementType, IPropertyResolver[] elements)
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
			IPropertyResolver? element = elements[i];

			if (element is null)
			{
				throw new ArgumentException(
					"AppendResolver does not allow null element resolvers.",
					nameof(elements));
			}

			if (element.ValueType != elementType)
			{
				throw new ArgumentException(
					$"AppendResolver element resolver at index {i} produces '{element.ValueType}', which does not " +
					$"match the source element type '{elementType}'.",
					nameof(elements));
			}
		}
	}
}
