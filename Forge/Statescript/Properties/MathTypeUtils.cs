// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Shared utility methods for math resolvers: type promotion, numeric conversion, and type classification.
/// </summary>
internal static class MathTypeUtils
{
	/// <summary>
	/// Determines the result type for a binary operation between two operand types, applying standard
	/// C# numeric promotion rules. Vector and quaternion types must match exactly.
	/// </summary>
	/// <param name="resolverName">The name of the resolver (for error messages).</param>
	/// <param name="leftType">The type of the left operand.</param>
	/// <param name="rightType">The type of the right operand.</param>
	/// <param name="allowVectors">Whether vector and quaternion types are supported.</param>
	/// <param name="allowQuaternions">Whether quaternion types are supported (only relevant if
	/// <paramref name="allowVectors"/> is true).</param>
	/// <returns>The promoted result type.</returns>
	/// <exception cref="ArgumentException">Thrown when the type combination is unsupported.</exception>
	internal static Type DetermineBinaryResultType(
		string resolverName,
		Type leftType,
		Type rightType,
		bool allowVectors = true,
		bool allowQuaternions = true)
	{
		if (leftType == rightType)
		{
			if (IsVectorType(leftType) || (leftType == typeof(Quaternion)))
			{
				if (!allowVectors && IsVectorType(leftType))
				{
					throw new ArgumentException(
						$"{resolverName} does not support operand type '{leftType}'.");
				}

				if (!allowQuaternions && leftType == typeof(Quaternion))
				{
					throw new ArgumentException(
						$"{resolverName} does not support operand type '{leftType}'.");
				}

				return leftType;
			}

			if (!IsNumericType(leftType))
			{
				throw new ArgumentException(
					$"{resolverName} does not support operand type '{leftType}'.");
			}

			return PromoteNumericTypes(leftType, rightType);
		}

		var leftIsVectorLike = IsVectorOrQuaternionType(leftType);
		var rightIsVectorLike = IsVectorOrQuaternionType(rightType);

		if (leftIsVectorLike || rightIsVectorLike)
		{
			throw new ArgumentException(
				$"{resolverName} cannot mix vector/quaternion types. Left: '{leftType}', Right: '{rightType}'.");
		}

		if (!IsNumericType(leftType))
		{
			throw new ArgumentException(
				$"{resolverName} does not support operand type '{leftType}'.");
		}

		if (!IsNumericType(rightType))
		{
			throw new ArgumentException(
				$"{resolverName} does not support operand type '{rightType}'.");
		}

		return PromoteNumericTypes(leftType, rightType);
	}

	/// <summary>
	/// Determines the result type for a unary operation on a single operand type.
	/// </summary>
	/// <param name="resolverName">The name of the resolver (for error messages).</param>
	/// <param name="operandType">The type of the operand.</param>
	/// <param name="allowVectors">Whether vector and quaternion types are supported.</param>
	/// <param name="allowQuaternions">Whether quaternion types are supported (only relevant if
	/// <paramref name="allowVectors"/> is true).</param>
	/// <returns>The result type (promoted for sub-int numerics).</returns>
	/// <exception cref="ArgumentException">Thrown when the type is unsupported.</exception>
	internal static Type DetermineUnaryResultType(
		string resolverName,
		Type operandType,
		bool allowVectors = true,
		bool allowQuaternions = true)
	{
		if (IsVectorType(operandType))
		{
			if (!allowVectors)
			{
				throw new ArgumentException(
					$"{resolverName} does not support operand type '{operandType}'.");
			}

			return operandType;
		}

		if (operandType == typeof(Quaternion))
		{
			if (!allowQuaternions)
			{
				throw new ArgumentException(
					$"{resolverName} does not support operand type '{operandType}'.");
			}

			return operandType;
		}

		if (!IsNumericType(operandType))
		{
			throw new ArgumentException(
				$"{resolverName} does not support operand type '{operandType}'.");
		}

		return PromoteNumericTypes(operandType, operandType);
	}

	internal static bool IsNumericType(Type type)
	{
		return type == typeof(byte)
			|| type == typeof(sbyte)
			|| type == typeof(short)
			|| type == typeof(ushort)
			|| type == typeof(int)
			|| type == typeof(uint)
			|| type == typeof(long)
			|| type == typeof(ulong)
			|| type == typeof(float)
			|| type == typeof(double)
			|| type == typeof(decimal);
	}

	internal static bool IsVectorType(Type type)
	{
		return type == typeof(Vector2)
			|| type == typeof(Vector3)
			|| type == typeof(Vector4);
	}

	internal static bool IsVectorOrQuaternionType(Type type)
	{
		return IsVectorType(type) || type == typeof(Quaternion);
	}

	internal static Type PromoteNumericTypes(Type leftType, Type rightType)
	{
		var leftRank = GetNumericRank(leftType);
		var rightRank = GetNumericRank(rightType);

		return leftRank >= rightRank ? RankToType(leftRank) : RankToType(rightRank);
	}

	internal static int ResolveAsInt(Type type, Variant128 value)
	{
		if (type == typeof(int))
		{
			return value.AsInt();
		}

		if (type == typeof(byte))
		{
			return value.AsByte();
		}

		if (type == typeof(sbyte))
		{
			return value.AsSByte();
		}

		if (type == typeof(short))
		{
			return value.AsShort();
		}

		if (type == typeof(ushort))
		{
			return value.AsUShort();
		}

		throw new InvalidOperationException($"Cannot convert '{type}' to int.");
	}

	internal static long ResolveAsLong(Type type, Variant128 value)
	{
		if (type == typeof(long))
		{
			return value.AsLong();
		}

		if (type == typeof(uint))
		{
			return value.AsUInt();
		}

		if (type == typeof(int))
		{
			return value.AsInt();
		}

		if (type == typeof(byte))
		{
			return value.AsByte();
		}

		if (type == typeof(sbyte))
		{
			return value.AsSByte();
		}

		if (type == typeof(short))
		{
			return value.AsShort();
		}

		if (type == typeof(ushort))
		{
			return value.AsUShort();
		}

		throw new InvalidOperationException($"Cannot convert '{type}' to long.");
	}

	internal static float ResolveAsFloat(Type type, Variant128 value)
	{
		if (type == typeof(float))
		{
			return value.AsFloat();
		}

		if (type == typeof(int))
		{
			return value.AsInt();
		}

		if (type == typeof(byte))
		{
			return value.AsByte();
		}

		if (type == typeof(sbyte))
		{
			return value.AsSByte();
		}

		if (type == typeof(short))
		{
			return value.AsShort();
		}

		if (type == typeof(ushort))
		{
			return value.AsUShort();
		}

		throw new InvalidOperationException($"Cannot convert '{type}' to float.");
	}

	internal static double ResolveAsDouble(Type type, Variant128 value)
	{
		if (type == typeof(double))
		{
			return value.AsDouble();
		}

		if (type == typeof(float))
		{
			return value.AsFloat();
		}

		if (type == typeof(long))
		{
			return value.AsLong();
		}

		if (type == typeof(ulong))
		{
			return value.AsULong();
		}

		if (type == typeof(uint))
		{
			return value.AsUInt();
		}

		if (type == typeof(int))
		{
			return value.AsInt();
		}

		if (type == typeof(byte))
		{
			return value.AsByte();
		}

		if (type == typeof(sbyte))
		{
			return value.AsSByte();
		}

		if (type == typeof(short))
		{
			return value.AsShort();
		}

		if (type == typeof(ushort))
		{
			return value.AsUShort();
		}

		throw new InvalidOperationException($"Cannot convert '{type}' to double.");
	}

	internal static decimal ResolveAsDecimal(Type type, Variant128 value)
	{
		if (type == typeof(decimal))
		{
			return value.AsDecimal();
		}

		if (type == typeof(double))
		{
			return (decimal)value.AsDouble();
		}

		if (type == typeof(float))
		{
			return (decimal)value.AsFloat();
		}

		if (type == typeof(long))
		{
			return value.AsLong();
		}

		if (type == typeof(ulong))
		{
			return value.AsULong();
		}

		if (type == typeof(uint))
		{
			return value.AsUInt();
		}

		if (type == typeof(int))
		{
			return value.AsInt();
		}

		if (type == typeof(byte))
		{
			return value.AsByte();
		}

		if (type == typeof(sbyte))
		{
			return value.AsSByte();
		}

		if (type == typeof(short))
		{
			return value.AsShort();
		}

		if (type == typeof(ushort))
		{
			return value.AsUShort();
		}

		throw new InvalidOperationException($"Cannot convert '{type}' to decimal.");
	}

	/// <summary>
	/// Rank determines promotion order. Types with the same rank are identical.
	/// The ranking follows C# numeric promotion rules:
	/// byte/sbyte/short/ushort → int → uint → long → ulong → float → double → decimal.
	/// </summary>
	/// <param name="type">The numeric type to rank.</param>
	/// <returns>The rank of the numeric type.</returns>
	/// <exception cref="ArgumentException">Thrown when the type is not a supported numeric type.</exception>
	private static int GetNumericRank(Type type)
	{
		if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort))
		{
			return 0;
		}

		if (type == typeof(int))
		{
			return 1;
		}

		if (type == typeof(uint))
		{
			return 2;
		}

		if (type == typeof(long))
		{
			return 3;
		}

		if (type == typeof(ulong))
		{
			return 4;
		}

		if (type == typeof(float))
		{
			return 5;
		}

		if (type == typeof(double))
		{
			return 6;
		}

		if (type == typeof(decimal))
		{
			return 7;
		}

		throw new ArgumentException($"Unsupported numeric type '{type}'.");
	}

	private static Type RankToType(int rank)
	{
		return rank switch
		{
			0 => typeof(int),
			1 => typeof(int),
			2 => typeof(long),
			3 => typeof(long),
			4 => typeof(double),
			5 => typeof(float),
			6 => typeof(double),
			7 => typeof(decimal),
			_ => throw new ArgumentException($"Invalid numeric rank '{rank}'."),
		};
	}
}
