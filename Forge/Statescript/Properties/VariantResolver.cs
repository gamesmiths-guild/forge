// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// A mutable property resolver that stores a <see cref="Variant128"/> value directly. This is the resolver used for
/// graph variables: properties that can be both read and written at runtime.
/// </summary>
/// <param name="initialValue">The initial value of the variable.</param>
/// <param name="valueType">The type of the value this variable holds.</param>
public class VariantResolver(Variant128 initialValue, Type valueType) : IPropertyResolver
{
	/// <summary>
	/// Gets or sets the current value of this variable.
	/// </summary>
	public Variant128 Value { get; set; } = initialValue;

	/// <inheritdoc/>
	public Type ValueType { get; } = valueType;

	/// <summary>
	/// Creates a <see cref="Variant128"/> from a typed value.
	/// </summary>
	/// <typeparam name="T">The type of the value. Must be supported by <see cref="Variant128"/>.</typeparam>
	/// <param name="value">The value to convert.</param>
	/// <returns>A <see cref="Variant128"/> containing the value.</returns>
	/// <exception cref="ArgumentException">Thrown if the type T is not supported by <see cref="Variant128"/>.
	/// </exception>
	public static Variant128 CreateVariant<T>(T value)
	{
		return value switch
		{
			bool @bool => new Variant128(@bool),
			byte @byte => new Variant128(@byte),
			sbyte @sbyte => new Variant128(@sbyte),
			char @char => new Variant128(@char),
			decimal @decimal => new Variant128(@decimal),
			double @double => new Variant128(@double),
			float @float => new Variant128(@float),
			int @int => new Variant128(@int),
			uint @uint => new Variant128(@uint),
			long @long => new Variant128(@long),
			ulong @ulong => new Variant128(@ulong),
			short @short => new Variant128(@short),
			ushort @ushort => new Variant128(@ushort),
			Vector2 vector2 => new Variant128(vector2),
			Vector3 vector3 => new Variant128(vector3),
			Vector4 vector4 => new Variant128(vector4),
			Plane plane => new Variant128(plane),
			Quaternion quaternion => new Variant128(quaternion),
			_ => throw new ArgumentException($"{typeof(T)} is not supported by Variant128"),
		};
	}

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		return Value;
	}

	/// <summary>
	/// Sets the value from a typed input, converting it to a <see cref="Variant128"/>.
	/// </summary>
	/// <typeparam name="T">The type of the value to set. Must be supported by <see cref="Variant128"/>.</typeparam>
	/// <param name="value">The value to set.</param>
	/// <exception cref="ArgumentException">Thrown if the type T is not supported by <see cref="Variant128"/>.
	/// </exception>
	public void Set<T>(T value)
	{
		Value = CreateVariant(value);
	}
}
