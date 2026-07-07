// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Shared helper for comparing two <see cref="Variant128"/> values of a known element type. Used by array resolvers
/// that need value equality (contains, index-of, distinct, except). Floating-point values are compared exactly.
/// </summary>
internal static class VariantEquality
{
	internal static bool AreEqual(Variant128 left, Variant128 right, Type type)
	{
		if (type == typeof(bool))
		{
			return left.AsBool() == right.AsBool();
		}

		if (type == typeof(byte))
		{
			return left.AsByte() == right.AsByte();
		}

		if (type == typeof(sbyte))
		{
			return left.AsSByte() == right.AsSByte();
		}

		if (type == typeof(char))
		{
			return left.AsChar() == right.AsChar();
		}

		if (type == typeof(decimal))
		{
			return left.AsDecimal() == right.AsDecimal();
		}

#pragma warning disable S1244 // Floating point numbers should not be tested for equality
		if (type == typeof(double))
		{
			return left.AsDouble() == right.AsDouble();
		}

		if (type == typeof(float))
		{
			return left.AsFloat() == right.AsFloat();
		}
#pragma warning restore S1244 // Floating point numbers should not be tested for equality

		if (type == typeof(int))
		{
			return left.AsInt() == right.AsInt();
		}

		if (type == typeof(uint))
		{
			return left.AsUInt() == right.AsUInt();
		}

		if (type == typeof(long))
		{
			return left.AsLong() == right.AsLong();
		}

		if (type == typeof(ulong))
		{
			return left.AsULong() == right.AsULong();
		}

		if (type == typeof(short))
		{
			return left.AsShort() == right.AsShort();
		}

		if (type == typeof(ushort))
		{
			return left.AsUShort() == right.AsUShort();
		}

		if (type == typeof(Vector2))
		{
			return left.AsVector2() == right.AsVector2();
		}

		if (type == typeof(Vector3))
		{
			return left.AsVector3() == right.AsVector3();
		}

		if (type == typeof(Vector4))
		{
			return left.AsVector4() == right.AsVector4();
		}

		if (type == typeof(Plane))
		{
			return left.AsPlane() == right.AsPlane();
		}

		if (type == typeof(Quaternion))
		{
			return left.AsQuaternion() == right.AsQuaternion();
		}

		throw new ArgumentException($"VariantEquality does not support element type '{type}'.");
	}
}
