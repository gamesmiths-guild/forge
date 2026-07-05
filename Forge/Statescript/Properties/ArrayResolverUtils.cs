// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Shared utility methods for array resolvers: operand validation and numeric conversion of index/count operands.
/// </summary>
internal static class ArrayResolverUtils
{
	internal static IPropertyResolver ValidateNumericOperand(
		string resolverName,
		string parameterName,
		IPropertyResolver operand)
	{
		if (!MathTypeUtils.IsNumericType(operand.ValueType))
		{
			throw new ArgumentException(
				$"{resolverName} requires {parameterName} to resolve to a numeric type. Got '{operand.ValueType}'.",
				parameterName);
		}

		return operand;
	}

	internal static IPropertyResolver ValidateElementOperand(
		string resolverName,
		string parameterName,
		Type elementType,
		IPropertyResolver operand)
	{
		if (operand.ValueType != elementType)
		{
			throw new ArgumentException(
				$"{resolverName} requires {parameterName} to resolve to the source element type '{elementType}'. " +
				$"Got '{operand.ValueType}'.",
				parameterName);
		}

		return operand;
	}

	internal static Type ValidateMatchingElementTypes(
		string resolverName,
		Type firstElementType,
		Type secondElementType)
	{
		if (firstElementType != secondElementType)
		{
			throw new ArgumentException(
				$"{resolverName} requires matching element types. Got '{firstElementType}' and " +
				$"'{secondElementType}'.");
		}

		return firstElementType;
	}

	internal static Type ValidateNumericElementType(string resolverName, Type elementType)
	{
		if (!MathTypeUtils.IsNumericType(elementType))
		{
			throw new ArgumentException(
				$"{resolverName} requires a numeric element type. Got '{elementType}'.");
		}

		return elementType;
	}

	/// <summary>
	/// Resolves a numeric operand (index or count) as an <see langword="int"/>, truncating any fractional part.
	/// </summary>
	/// <param name="graphContext">The graph context providing the runtime state.</param>
	/// <param name="resolver">The numeric resolver to evaluate.</param>
	/// <returns>The resolved value truncated to an <see langword="int"/>.</returns>
	internal static int ResolveInt(GraphContext graphContext, IPropertyResolver resolver)
	{
		return (int)ResolveAsDouble(resolver.ValueType, resolver.Resolve(graphContext));
	}

	/// <summary>
	/// Converts a numeric variant to <see langword="double"/>, extending <see cref="MathTypeUtils.ResolveAsDouble"/>
	/// with <see langword="decimal"/> support.
	/// </summary>
	/// <param name="type">The numeric type of the stored value.</param>
	/// <param name="value">The variant holding the value.</param>
	/// <returns>The value converted to <see langword="double"/>.</returns>
	internal static double ResolveAsDouble(Type type, Variant128 value)
	{
		if (type == typeof(decimal))
		{
			return (double)value.AsDecimal();
		}

		return MathTypeUtils.ResolveAsDouble(type, value);
	}

	/// <summary>
	/// Produces the element indexes of a source array sorted by their pre-computed keys. The sort is stable: elements
	/// with equal keys keep their original relative order.
	/// </summary>
	/// <param name="keys">The sort key of each source element, by element index.</param>
	/// <param name="direction">The ordering to apply.</param>
	/// <returns>The source indexes in sorted order.</returns>
	internal static int[] SortIndexesByKey(double[] keys, SortDirection direction)
	{
		int[] indexes = new int[keys.Length];
		for (int i = 0; i < indexes.Length; i++)
		{
			indexes[i] = i;
		}

		Array.Sort(indexes, (left, right) =>
		{
			int comparison = keys[left].CompareTo(keys[right]);

			if (direction == SortDirection.Descending)
			{
				comparison = -comparison;
			}

			return comparison != 0 ? comparison : left.CompareTo(right);
		});

		return indexes;
	}
}
