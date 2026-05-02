// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a random value within a range defined by two <see cref="IPropertyResolver"/> operands, using a provided
/// <see cref="IRandom"/> implementation. The random value is in the range [min, max) by default, or [min, max] when
/// configured for an inclusive maximum bound. Supports <see langword="int"/>, <see langword="float"/>, and
/// <see langword="double"/> types. Sub-int operand types (<see langword="byte"/>, <see langword="sbyte"/>,
/// <see langword="short"/>, <see langword="ushort"/>) are promoted to <see langword="int"/>. Other numeric, vector,
/// and quaternion types are not supported.
/// </summary>
/// <param name="random">The random provider to use for generating values.</param>
/// <param name="min">The resolver for the inclusive minimum bound.</param>
/// <param name="max">The resolver for the maximum bound.</param>
/// <param name="maxInclusive">Whether the maximum bound is inclusive. Defaults to <see langword="false"/>.</param>
public class RandomResolver(IRandom random, IPropertyResolver min, IPropertyResolver max, bool maxInclusive = false)
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
					? _random.NextIntInclusive(minInt, maxInt)
					: _random.NextInt(minInt, maxInt));
		}

		if (resultType == typeof(float))
		{
			var minFloat = MathTypeUtils.ResolveAsFloat(_min.ValueType, minValue);
			var maxFloat = MathTypeUtils.ResolveAsFloat(_max.ValueType, maxValue);

			if (maxFloat <= minFloat)
			{
				return new Variant128(minFloat);
			}

			var randomValue = _maxInclusive ? _random.NextSingleInclusive() : _random.NextSingle();
			var result = minFloat + (randomValue * (maxFloat - minFloat));

			if (_maxInclusive)
			{
				result = Math.Clamp(result, minFloat, maxFloat);
			}

			return new Variant128(result);
		}

		if (resultType == typeof(double))
		{
			var minDouble = MathTypeUtils.ResolveAsDouble(_min.ValueType, minValue);
			var maxDouble = MathTypeUtils.ResolveAsDouble(_max.ValueType, maxValue);

			if (maxDouble <= minDouble)
			{
				return new Variant128(minDouble);
			}

			var randomValue = _maxInclusive ? _random.NextDoubleInclusive() : _random.NextDouble();
			var result = minDouble + (randomValue * (maxDouble - minDouble));

			if (_maxInclusive)
			{
				result = Math.Clamp(result, minDouble, maxDouble);
			}

			return new Variant128(result);
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
}
