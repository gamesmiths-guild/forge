// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the normalized (unit-length) form of a vector, plane, or quaternion operand. Supports
/// <see cref="Vector2"/>, <see cref="Vector3"/>, <see cref="Vector4"/>, <see cref="Plane"/>, and
/// <see cref="Quaternion"/>. Scalar types are not supported.
/// </summary>
/// <param name="operand">The resolver for the vector or quaternion operand.</param>
public class NormalizeResolver(IPropertyResolver operand) : IPropertyResolver
{
	private readonly IPropertyResolver _operand = operand;

	/// <inheritdoc/>
	public Type ValueType { get; } = ValidateType(operand.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 value = _operand.Resolve(graphContext);
		Type resultType = ValueType;

		if (resultType == typeof(Vector2))
		{
			return new Variant128(Vector2.Normalize(value.AsVector2()));
		}

		if (resultType == typeof(Vector3))
		{
			return new Variant128(Vector3.Normalize(value.AsVector3()));
		}

		if (resultType == typeof(Vector4))
		{
			return new Variant128(Vector4.Normalize(value.AsVector4()));
		}

		if (resultType == typeof(Plane))
		{
			return new Variant128(Plane.Normalize(value.AsPlane()));
		}

		if (resultType == typeof(Quaternion))
		{
			return new Variant128(Quaternion.Normalize(value.AsQuaternion()));
		}

		throw new InvalidOperationException(
			$"NormalizeResolver encountered unexpected result type '{resultType}'.");
	}

	private static Type ValidateType(Type type)
	{
		if (MathTypeUtils.IsVectorType(type) || type == typeof(Plane) || type == typeof(Quaternion))
		{
			return type;
		}

		throw new ArgumentException(
			$"NormalizeResolver only supports vector, plane, and quaternion types. Got '{type}'.");
	}
}
