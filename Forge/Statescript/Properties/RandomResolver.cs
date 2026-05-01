// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a random value within a range defined by two <see cref="IPropertyResolver"/> operands, using a provided
/// <see cref="IRandom"/> implementation. The random value is in the range [min, max] by default, or [min, max) when
/// configured for an exclusive maximum bound. Supports <see langword="int"/>, <see langword="float"/>, and
/// <see langword="double"/> types. Sub-int operand types (<see langword="byte"/>, <see langword="sbyte"/>,
/// <see langword="short"/>, <see langword="ushort"/>) are promoted to <see langword="int"/>. Other numeric, vector,
/// and quaternion types are not supported.
/// </summary>
/// <param name="random">The random provider to use for generating values.</param>
/// <param name="min">The resolver for the inclusive minimum bound.</param>
/// <param name="max">The resolver for the maximum bound.</param>
/// <param name="maxInclusive">Whether the maximum bound is inclusive. Defaults to <see langword="true"/>.</param>
public class RandomResolver(IRandom random, IPropertyResolver min, IPropertyResolver max, bool maxInclusive = true)
	: IPropertyResolver
{
	private readonly IRandom _random = random;

	private readonly IPropertyResolver _min = min;

	private readonly IPropertyResolver _max = max;

	private readonly bool _maxInclusive = maxInclusive;

	/// <inheritdoc/>
	public Type ValueType { get; } = DetermineResultType(min.ValueType, max.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 minValue = _min.Resolve(graphContext);
		Variant128 maxValue = _max.Resolve(graphContext);
		Type resultType = ValueType;

		if (resultType == typeof(int))
		{
			var minInt = MathTypeUtils.ResolveAsInt(_min.ValueType, minValue);
			var maxInt = MathTypeUtils.ResolveAsInt(_max.ValueType, maxValue);

			return new Variant128(
				_maxInclusive
					? (int)_random.NextInt64(minInt, (long)maxInt + 1)
					: _random.NextInt(minInt, maxInt));
		}

		if (resultType == typeof(float))
		{
			var minFloat = MathTypeUtils.ResolveAsFloat(_min.ValueType, minValue);
			var maxFloat = MathTypeUtils.ResolveAsFloat(_max.ValueType, maxValue);
			var randomValue = _random.NextSingle();

			if (_maxInclusive && randomValue >= 1.0f)
			{
				return new Variant128(maxFloat);
			}

			if (!_maxInclusive && randomValue >= 1.0f)
			{
				return new Variant128(BitDecrement(maxFloat));
			}

			var upperBound = _maxInclusive ? BitIncrement(maxFloat) : maxFloat;
			var result = minFloat + (randomValue * (upperBound - minFloat));
			return new Variant128(_maxInclusive && result > maxFloat ? maxFloat : result);
		}

		if (resultType == typeof(double))
		{
			var minDouble = MathTypeUtils.ResolveAsDouble(_min.ValueType, minValue);
			var maxDouble = MathTypeUtils.ResolveAsDouble(_max.ValueType, maxValue);
			var randomValue = _random.NextDouble();

			if (_maxInclusive && randomValue >= 1.0)
			{
				return new Variant128(maxDouble);
			}

			if (!_maxInclusive && randomValue >= 1.0)
			{
				return new Variant128(BitDecrement(maxDouble));
			}

			var upperBound = _maxInclusive ? BitIncrement(maxDouble) : maxDouble;
			var result = minDouble + (randomValue * (upperBound - minDouble));
			return new Variant128(_maxInclusive && result > maxDouble ? maxDouble : result);
		}

		throw new InvalidOperationException(
			$"RandomResolver encountered unexpected result type '{resultType}'.");
	}

	private static Type DetermineResultType(Type minType, Type maxType)
	{
		if (MathTypeUtils.IsVectorOrQuaternionType(minType))
		{
			throw new ArgumentException($"RandomResolver does not support operand type '{minType}'.");
		}

		if (MathTypeUtils.IsVectorOrQuaternionType(maxType))
		{
			throw new ArgumentException($"RandomResolver does not support operand type '{maxType}'.");
		}

		if (!MathTypeUtils.IsNumericType(minType))
		{
			throw new ArgumentException($"RandomResolver does not support operand type '{minType}'.");
		}

		if (!MathTypeUtils.IsNumericType(maxType))
		{
			throw new ArgumentException($"RandomResolver does not support operand type '{maxType}'.");
		}

		if (minType == typeof(decimal) || maxType == typeof(decimal))
		{
			throw new ArgumentException("RandomResolver does not support decimal operands. Use double instead.");
		}

		if (minType == typeof(long) || maxType == typeof(long)
			|| minType == typeof(uint) || maxType == typeof(uint)
			|| minType == typeof(ulong) || maxType == typeof(ulong))
		{
			throw new ArgumentException("RandomResolver only supports int, float, and double operand types.");
		}

		// Both int (or sub-int) → int.
		if (IsIntOrSubInt(minType) && IsIntOrSubInt(maxType))
		{
			return typeof(int);
		}

		// Both float → float; otherwise double.
		if (minType == typeof(float) && maxType == typeof(float))
		{
			return typeof(float);
		}

		// Mixed int/float promotes to float for practical game use.
		if ((IsIntOrSubInt(minType) && maxType == typeof(float))
			|| (minType == typeof(float) && IsIntOrSubInt(maxType)))
		{
			return typeof(float);
		}

		return typeof(double);
	}

	private static bool IsIntOrSubInt(Type type)
	{
		return type == typeof(int)
			|| type == typeof(byte)
			|| type == typeof(sbyte)
			|| type == typeof(short)
			|| type == typeof(ushort);
	}

	private static float BitIncrement(float value)
	{
		if (float.IsNaN(value) || float.IsPositiveInfinity(value))
		{
			return value;
		}

#pragma warning disable S1244 // Floating point numbers should not be tested for equality
		if (value == 0.0f)
		{
			return float.Epsilon;
		}
#pragma warning restore S1244 // Floating point numbers should not be tested for equality

		var bits = BitConverter.SingleToInt32Bits(value);
		bits += value > 0.0f ? 1 : -1;
		return BitConverter.Int32BitsToSingle(bits);
	}

	private static double BitIncrement(double value)
	{
		if (double.IsNaN(value) || double.IsPositiveInfinity(value))
		{
			return value;
		}

#pragma warning disable S1244 // Floating point numbers should not be tested for equality
		if (value == 0.0)
		{
			return double.Epsilon;
		}
#pragma warning restore S1244 // Floating point numbers should not be tested for equality

		var bits = BitConverter.DoubleToInt64Bits(value);
		bits += value > 0.0 ? 1 : -1;
		return BitConverter.Int64BitsToDouble(bits);
	}

	private static float BitDecrement(float value)
	{
		if (float.IsNaN(value) || float.IsNegativeInfinity(value))
		{
			return value;
		}

#pragma warning disable S1244 // Floating point numbers should not be tested for equality
		if (value == 0.0f)
		{
			return -float.Epsilon;
		}
#pragma warning restore S1244 // Floating point numbers should not be tested for equality

		var bits = BitConverter.SingleToInt32Bits(value);
		bits += value > 0.0f ? -1 : 1;
		return BitConverter.Int32BitsToSingle(bits);
	}

	private static double BitDecrement(double value)
	{
		if (double.IsNaN(value) || double.IsNegativeInfinity(value))
		{
			return value;
		}

#pragma warning disable S1244 // Floating point numbers should not be tested for equality
		if (value == 0.0)
		{
			return -double.Epsilon;
		}
#pragma warning restore S1244 // Floating point numbers should not be tested for equality

		var bits = BitConverter.DoubleToInt64Bits(value);
		bits += value > 0.0 ? -1 : 1;
		return BitConverter.Int64BitsToDouble(bits);
	}
}
