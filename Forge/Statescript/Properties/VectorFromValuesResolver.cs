// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a vector created from component resolver values.
/// </summary>
/// <param name="x">The resolver for the X component.</param>
/// <param name="y">The resolver for the Y component.</param>
public class VectorFromValuesResolver(IPropertyResolver x, IPropertyResolver y) : IPropertyResolver
{
	private readonly IPropertyResolver _x = x;

	private readonly IPropertyResolver _y = y;

	private readonly IPropertyResolver? _z;

	private readonly IPropertyResolver? _w;

	/// <inheritdoc/>
	public Type ValueType { get; } = ValidateValueType(x.ValueType, y.ValueType, null, null);

	/// <summary>
	/// Initializes a new instance of the <see cref="VectorFromValuesResolver"/> class that creates a
	/// <see cref="Vector3"/> from component resolvers.
	/// </summary>
	/// <param name="x">The resolver for the X component.</param>
	/// <param name="y">The resolver for the Y component.</param>
	/// <param name="z">The resolver for the Z component.</param>
	public VectorFromValuesResolver(IPropertyResolver x, IPropertyResolver y, IPropertyResolver z)
		: this(x, y)
	{
		_z = z;
		ValueType = ValidateValueType(x.ValueType, y.ValueType, z.ValueType, null);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="VectorFromValuesResolver"/> class that creates a
	/// <see cref="Vector4"/> from component resolvers.
	/// </summary>
	/// <param name="x">The resolver for the X component.</param>
	/// <param name="y">The resolver for the Y component.</param>
	/// <param name="z">The resolver for the Z component.</param>
	/// <param name="w">The resolver for the W component.</param>
	public VectorFromValuesResolver(IPropertyResolver x, IPropertyResolver y, IPropertyResolver z, IPropertyResolver w)
		: this(x, y, z)
	{
		_w = w;
		ValueType = ValidateValueType(x.ValueType, y.ValueType, z.ValueType, w.ValueType);
	}

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		float xValue = _x.Resolve(graphContext).AsFloat();
		float yValue = _y.Resolve(graphContext).AsFloat();

		if (ValueType == typeof(Vector2))
		{
			return new Variant128(new Vector2(xValue, yValue));
		}

		float zValue = _z!.Resolve(graphContext).AsFloat();

		if (ValueType == typeof(Vector3))
		{
			return new Variant128(new Vector3(xValue, yValue, zValue));
		}

		float wValue = _w!.Resolve(graphContext).AsFloat();
		return new Variant128(new Vector4(xValue, yValue, zValue, wValue));
	}

	private static Type ValidateValueType(Type xType, Type yType, Type? zType, Type? wType)
	{
		ValidateComponentType(xType, nameof(x));
		ValidateComponentType(yType, nameof(y));

		if (zType is null)
		{
			return typeof(Vector2);
		}

		ValidateComponentType(zType, "z");

		if (wType is null)
		{
			return typeof(Vector3);
		}

		ValidateComponentType(wType, "w");
		return typeof(Vector4);
	}

	private static void ValidateComponentType(Type componentType, string componentName)
	{
		if (componentType != typeof(float))
		{
			throw new ArgumentException(
				$"VectorFromValuesResolver requires component '{componentName}' to be float. Got '{componentType}'.");
		}
	}
}
