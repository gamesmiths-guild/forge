// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a <see cref="bool"/> value by comparing the results of two nested <see cref="IPropertyResolver"/> instances
/// using a specified <see cref="ComparisonOperation"/>. Both operands are converted to <see langword="double"/> for
/// comparison based on each resolver's <see cref="IPropertyResolver.ValueType"/>, allowing any numeric property (int
/// attributes, float variables, etc.) to be compared directly.
/// </summary>
/// <remarks>
/// <para>This resolver enables data-driven expressions such as "is the entity's health greater than 50" or "is variable
/// A less than or equal to variable B" without requiring custom <see cref="Nodes.ConditionNode"/> subclasses.</para>
/// <para>Operand resolvers can be any <see cref="IPropertyResolver"/> implementation, including other
/// <see cref="ComparisonResolver"/> instances, enabling arbitrarily nested expressions.</para>
/// </remarks>
/// <param name="left">The resolver for the left operand of the comparison.</param>
/// <param name="operation">The comparison operation to apply.</param>
/// <param name="right">The resolver for the right operand of the comparison.</param>
public class ComparisonResolver(
	IPropertyResolver left,
	ComparisonOperation operation,
	IPropertyResolver right) : IPropertyResolver
{
	private readonly IPropertyResolver _left = left;

	private readonly ComparisonOperation _operation = operation;

	private readonly IPropertyResolver _right = right;

	/// <inheritdoc/>
	public Type ValueType => typeof(bool);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		var leftValue = ResolveAsDouble(_left, graphContext);
		var rightValue = ResolveAsDouble(_right, graphContext);

		var result = _operation switch
		{
#pragma warning disable S1244 // Floating point numbers should not be tested for equality
			ComparisonOperation.Equal => leftValue == rightValue,
			ComparisonOperation.NotEqual => leftValue != rightValue,
#pragma warning restore S1244 // Floating point numbers should not be tested for equality
			ComparisonOperation.LessThan => leftValue < rightValue,
			ComparisonOperation.LessThanOrEqual => leftValue <= rightValue,
			ComparisonOperation.GreaterThan => leftValue > rightValue,
			ComparisonOperation.GreaterThanOrEqual => leftValue >= rightValue,
			_ => false,
		};

		return new Variant128(result);
	}

	private static double ResolveAsDouble(IPropertyResolver resolver, GraphContext graphContext)
	{
		Variant128 value = resolver.Resolve(graphContext);

		Type type = resolver.ValueType;

		if (type == typeof(double))
		{
			return value.AsDouble();
		}

		if (type == typeof(int))
		{
			return value.AsInt();
		}

		if (type == typeof(float))
		{
			return value.AsFloat();
		}

		if (type == typeof(long))
		{
			return value.AsLong();
		}

		if (type == typeof(short))
		{
			return value.AsShort();
		}

		if (type == typeof(byte))
		{
			return value.AsByte();
		}

		if (type == typeof(uint))
		{
			return value.AsUInt();
		}

		if (type == typeof(ulong))
		{
			return value.AsULong();
		}

		if (type == typeof(ushort))
		{
			return value.AsUShort();
		}

		if (type == typeof(sbyte))
		{
			return value.AsSByte();
		}

		if (type == typeof(decimal))
		{
			return (double)value.AsDecimal();
		}

		throw new ArgumentException(
			$"ComparisonResolver does not support operand type '{type}'. Only numeric types are allowed.");
	}
}
