// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the length (magnitude) of a vector or quaternion operand. Returns a <see langword="float"/>. Supports
/// <see cref="Vector2"/>, <see cref="Vector3"/>, <see cref="Vector4"/>, and <see cref="Quaternion"/>. Scalar and
/// plane types are not supported.
/// </summary>
/// <param name="operand">The resolver for the vector or quaternion operand.</param>
public class LengthResolver(IPropertyResolver operand) : IPropertyResolver
{
	private readonly IPropertyResolver _operand = operand;

	/// <inheritdoc/>
	public Type ValueType { get; } = ValidateType(operand.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 value = _operand.Resolve(graphContext);
		Type operandType = _operand.ValueType;

		if (operandType == typeof(Vector2))
		{
			return new Variant128(value.AsVector2().Length());
		}

		if (operandType == typeof(Vector3))
		{
			return new Variant128(value.AsVector3().Length());
		}

		if (operandType == typeof(Vector4))
		{
			return new Variant128(value.AsVector4().Length());
		}

		if (operandType == typeof(Quaternion))
		{
			return new Variant128(value.AsQuaternion().Length());
		}

		throw new InvalidOperationException(
			$"LengthResolver encountered unexpected operand type '{operandType}'.");
	}

	private static Type ValidateType(Type type)
	{
		if (MathTypeUtils.IsVectorType(type) || type == typeof(Quaternion))
		{
			return typeof(float);
		}

		throw new ArgumentException(
			$"LengthResolver only supports Vector2, Vector3, Vector4, and Quaternion. Got '{type}'.");
	}
}
