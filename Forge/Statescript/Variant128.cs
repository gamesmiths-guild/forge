// Copyright © Gamesmiths Guild.

using System.Numerics;
using System.Runtime.InteropServices;

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// Represents a 128-bit variant type that can hold different data types.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 16)]
public struct Variant128
{
	[FieldOffset(0)]
	private readonly bool _bool;

	[FieldOffset(0)]
	private readonly byte _byte;

	[FieldOffset(0)]
	private readonly sbyte _sbyte;

	[FieldOffset(0)]
	private readonly char _char;

	[FieldOffset(0)]
	private readonly decimal _decimal;

	[FieldOffset(0)]
	private readonly double _double;

	[FieldOffset(0)]
	private readonly float _float;

	[FieldOffset(0)]
	private readonly int _int;

	[FieldOffset(0)]
	private readonly uint _uint;

	[FieldOffset(0)]
	private readonly long _long;

	[FieldOffset(0)]
	private readonly ulong _ulong;

	[FieldOffset(0)]
	private readonly short _short;

	[FieldOffset(0)]
	private readonly ushort _ushort;

	[FieldOffset(0)]
	private readonly Vector2 _vector2;

	[FieldOffset(0)]
	private readonly Vector3 _vector3;

	[FieldOffset(0)]
	private readonly Vector4 _vector4;

	[FieldOffset(0)]
	private readonly Plane _plane;

	[FieldOffset(0)]
	private Quaternion _quaternion;

	/// <summary>
	/// Initializes a new instance of the <see cref="Variant128"/> struct with the specified bool value.
	/// </summary>
	/// <param name="value">The bool value to store.</param>
	public Variant128(bool value)
		: this()
	{
		_bool = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Variant128"/> struct with the specified byte value.
	/// </summary>
	/// <param name="value">The byte value to store.</param>
	public Variant128(byte value)
		: this()
	{
		_byte = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Variant128"/> struct with the specified sbyte value.
	/// </summary>
	/// <param name="value">The sbyte value to store.</param>
	public Variant128(sbyte value)
		: this()
	{
		_sbyte = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Variant128"/> struct with the specified char value.
	/// </summary>
	/// <param name="value">The char value to store.</param>
	public Variant128(char value)
		: this()
	{
		_char = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Variant128"/> struct with the specified decimal value.
	/// </summary>
	/// <param name="value">The decimal value to store.</param>
	public Variant128(decimal value)
		: this()
	{
		_decimal = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Variant128"/> struct with the specified double value.
	/// </summary>
	/// <param name="value">The double value to store.</param>
	public Variant128(double value)
		: this()
	{
		_double = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Variant128"/> struct with the specified float value.
	/// </summary>
	/// <param name="value">The float value to store.</param>
	public Variant128(float value)
		: this()
	{
		_float = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Variant128"/> struct with the specified int value.
	/// </summary>
	/// <param name="value">The int value to store.</param>
	public Variant128(int value)
		: this()
	{
		_int = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Variant128"/> struct with the specified uint value.
	/// </summary>
	/// <param name="value">The uint value to store.</param>
	public Variant128(uint value)
		: this()
	{
		_uint = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Variant128"/> struct with the specified long value.
	/// </summary>
	/// <param name="value">The long value to store.</param>
	public Variant128(long value)
		: this()
	{
		_long = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Variant128"/> struct with the specified ulong value.
	/// </summary>
	/// <param name="value">The ulong value to store.</param>
	public Variant128(ulong value)
		: this()
	{
		_ulong = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Variant128"/> struct with the specified short value.
	/// </summary>
	/// <param name="value">The short value to store.</param>
	public Variant128(short value)
		: this()
	{
		_short = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Variant128"/> struct with the specified ushort value.
	/// </summary>
	/// <param name="value">The ushort value to store.</param>
	public Variant128(ushort value)
		: this()
	{
		_ushort = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Variant128"/> struct with the specified Vector2 value.
	/// </summary>
	/// <param name="value">The Vector2 value to store.</param>
	public Variant128(Vector2 value)
		: this()
	{
		_vector2 = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Variant128"/> struct with the specified Vector3 value.
	/// </summary>
	/// <param name="value">The Vector3 value to store.</param>
	public Variant128(Vector3 value)
		: this()
	{
		_vector3 = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Variant128"/> struct with the specified Vector4 value.
	/// </summary>
	/// <param name="value">The Vector4 value to store.</param>
	public Variant128(Vector4 value)
		: this()
	{
		_vector4 = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Variant128"/> struct with the specified Plane value.
	/// </summary>
	/// <param name="value">The Plane value to store.</param>
	public Variant128(Plane value)
		: this()
	{
		_plane = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Variant128"/> struct with the specified Quaternion value.
	/// </summary>
	/// <param name="value">The Quaternion value to store.</param>
	public Variant128(Quaternion value)
		: this()
	{
		_quaternion = value;
	}

	/// <summary>
	/// Creates a <see cref="Variant128"/> instance from a byte array.
	/// </summary>
	/// <param name="bytes">The byte array to reconstruct from.</param>
	/// <remarks>
	/// Always reconstruct from Quaternion bytes, regardless of what is actually stored.
	/// </remarks>
	/// <returns>The reconstructed <see cref="Variant128"/> instance.</returns>
	public static Variant128 FromBytes(byte[] bytes)
	{
		Quaternion v = MemoryMarshal.Read<Quaternion>(bytes);
		return new Variant128(v);
	}

	/// <summary>
	/// Converts the stored value to a byte array.
	/// </summary>
	/// <returns>The byte array representation of the stored value.</returns>
	public byte[] ToBytes()
	{
		var result = new byte[16];
#pragma warning disable CS9191 // Suppress "in" argument
		MemoryMarshal.Write(result.AsSpan(), ref _quaternion);
#pragma warning restore CS9191
		return result;
	}

	/// <summary>
	/// Gets the stored value as the specified unmanaged type.
	/// </summary>
	/// <typeparam name="T">The unmanaged type to retrieve.</typeparam>
	/// <returns>The stored value as type T.</returns>
	/// <exception cref="InvalidOperationException">Exception thrown if the type T is unsupported.</exception>
	public readonly T Get<T>()
		where T : unmanaged
	{
		if (typeof(T) == typeof(bool))
		{
			return (T)(object)_bool;
		}

		if (typeof(T) == typeof(byte))
		{
			return (T)(object)_byte;
		}

		if (typeof(T) == typeof(sbyte))
		{
			return (T)(object)_sbyte;
		}

		if (typeof(T) == typeof(char))
		{
			return (T)(object)_char;
		}

		if (typeof(T) == typeof(decimal))
		{
			return (T)(object)_decimal;
		}

		if (typeof(T) == typeof(double))
		{
			return (T)(object)_double;
		}

		if (typeof(T) == typeof(float))
		{
			return (T)(object)_float;
		}

		if (typeof(T) == typeof(int))
		{
			return (T)(object)_int;
		}

		if (typeof(T) == typeof(uint))
		{
			return (T)(object)_uint;
		}

		if (typeof(T) == typeof(long))
		{
			return (T)(object)_long;
		}

		if (typeof(T) == typeof(ulong))
		{
			return (T)(object)_ulong;
		}

		if (typeof(T) == typeof(short))
		{
			return (T)(object)_short;
		}

		if (typeof(T) == typeof(ushort))
		{
			return (T)(object)_ushort;
		}

		if (typeof(T) == typeof(Vector2))
		{
			return (T)(object)_vector2;
		}

		if (typeof(T) == typeof(Vector3))
		{
			return (T)(object)_vector3;
		}

		if (typeof(T) == typeof(Vector4))
		{
			return (T)(object)_vector4;
		}

		if (typeof(T) == typeof(Plane))
		{
			return (T)(object)_plane;
		}

		if (typeof(T) == typeof(Quaternion))
		{
			return (T)(object)_quaternion;
		}

		throw new InvalidOperationException($"Unsupported type {typeof(T)}");
	}

	/// <summary>
	/// Retrieves the stored value as a boolean.
	/// </summary>
	/// <returns>The stored boolean value.</returns>
	public readonly bool AsBool()
	{
		return _bool;
	}

	/// <summary>
	/// Retrieves the stored value as a byte.
	/// </summary>
	/// <returns>The stored byte value.</returns>
	public readonly byte AsByte()
	{
		return _byte;
	}

	/// <summary>
	/// Retrieves the stored value as an sbyte.
	/// </summary>
	/// <returns>The stored sbyte value.</returns>
	public readonly sbyte AsSByte()
	{
		return _sbyte;
	}

	/// <summary>
	/// Retrieves the stored value as a char.
	/// </summary>
	/// <returns>The stored char value.</returns>
	public readonly char AsChar()
	{
		return _char;
	}

	/// <summary>
	/// Retrieves the stored value as a decimal.
	/// </summary>
	/// <returns>The stored decimal value.</returns>
	public readonly decimal AsDecimal()
	{
		return _decimal;
	}

	/// <summary>
	/// Retrieves the stored value as a double.
	/// </summary>
	/// <returns>The stored double value.</returns>
	public readonly double AsDouble()
	{
		return _double;
	}

	/// <summary>
	/// Retrieves the stored value as a float.
	/// </summary>
	/// <returns>The stored float value.</returns>
	public readonly float AsFloat()
	{
		return _float;
	}

	/// <summary>
	/// Retrieves the stored value as an int.
	/// </summary>
	/// <returns>The stored int value.</returns>
	public readonly int AsInt()
	{
		return _int;
	}

	/// <summary>
	/// Retrieves the stored value as a uint.
	/// </summary>
	/// <returns>The stored uint value.</returns>
	public readonly uint AsUInt()
	{
		return _uint;
	}

	/// <summary>
	/// Retrieves the stored value as a long.
	/// </summary>
	/// <returns>The stored long value.</returns>
	public readonly long AsLong()
	{
		return _long;
	}

	/// <summary>
	/// Retrieves the stored value as a ulong.
	/// </summary>
	/// <returns>The stored ulong value.</returns>
	public readonly ulong AsULong()
	{
		return _ulong;
	}

	/// <summary>
	/// Retrieves the stored value as a short.
	/// </summary>
	/// <returns>The stored short value.</returns>
	public readonly short AsShort()
	{
		return _short;
	}

	/// <summary>
	/// Retrieves the stored value as a ushort.
	/// </summary>
	/// <returns>The stored ushort value.</returns>
	public readonly ushort AsUShort()
	{
		return _ushort;
	}

	/// <summary>
	/// Retrieves the stored value as a Vector2.
	/// </summary>
	/// <returns>The stored Vector2 value.</returns>
	public readonly Vector2 AsVector2()
	{
		return _vector2;
	}

	/// <summary>
	/// Retrieves the stored value as a Vector3.
	/// </summary>
	/// <returns>The stored Vector3 value.</returns>
	public readonly Vector3 AsVector3()
	{
		return _vector3;
	}

	/// <summary>
	/// Retrieves the stored value as a Vector4.
	/// </summary>
	/// <returns>The stored Vector4 value.</returns>
	public readonly Vector4 AsVector4()
	{
		return _vector4;
	}

	/// <summary>
	/// Retrieves the stored value as a Plane.
	/// </summary>
	/// <returns>The stored Plane value.</returns>
	public readonly Plane AsPlane()
	{
		return _plane;
	}

	/// <summary>
	/// Retrieves the stored value as a Quaternion.
	/// </summary>
	/// <returns>The stored Quaternion value.</returns>
	public readonly Quaternion AsQuaternion()
	{
		return _quaternion;
	}
}
