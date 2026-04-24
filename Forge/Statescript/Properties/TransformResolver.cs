// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the transformation of a vector or plane by a quaternion rotation. Supports <see cref="Vector2"/>,
/// <see cref="Vector3"/>, <see cref="Vector4"/>, and <see cref="Plane"/> for the value operand. The transform operand
/// must be a <see cref="Quaternion"/>.
/// </summary>
/// <param name="value">The resolver for the value to transform.</param>
/// <param name="rotation">The resolver for the quaternion rotation.</param>
public class TransformResolver(IPropertyResolver value, IPropertyResolver rotation) : IPropertyResolver
{
	private readonly IPropertyResolver _value = value;

	private readonly IPropertyResolver _rotation = rotation;

	/// <inheritdoc/>
	public Type ValueType { get; } = ValidateTypes(value.ValueType, rotation.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 valueResult = _value.Resolve(graphContext);
		Quaternion rotationValue = _rotation.Resolve(graphContext).AsQuaternion();
		Type resultType = ValueType;

		if (resultType == typeof(Vector2))
		{
			return new Variant128(Vector2.Transform(valueResult.AsVector2(), rotationValue));
		}

		if (resultType == typeof(Vector3))
		{
			return new Variant128(Vector3.Transform(valueResult.AsVector3(), rotationValue));
		}

		if (resultType == typeof(Vector4))
		{
			return new Variant128(Vector4.Transform(valueResult.AsVector4(), rotationValue));
		}

		if (resultType == typeof(Plane))
		{
			return new Variant128(Plane.Transform(valueResult.AsPlane(), rotationValue));
		}

		throw new InvalidOperationException(
			$"TransformResolver encountered unexpected result type '{resultType}'.");
	}

	private static Type ValidateTypes(Type valueType, Type rotationType)
	{
		if (rotationType != typeof(Quaternion))
		{
			throw new ArgumentException(
				$"TransformResolver requires 'rotation' to be Quaternion. Got '{rotationType}'.");
		}

		if (valueType == typeof(Vector2)
			|| valueType == typeof(Vector3)
			|| valueType == typeof(Vector4)
			|| valueType == typeof(Plane))
		{
			return valueType;
		}

		throw new ArgumentException(
			$"TransformResolver only supports Vector2, Vector3, Vector4, and Plane values. Got '{valueType}'.");
	}
}
