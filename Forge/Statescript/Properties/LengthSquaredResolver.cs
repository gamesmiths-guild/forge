// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the squared length (squared magnitude) of a vector operand. Returns a <see langword="float"/>. This is
/// more efficient than <see cref="LengthResolver"/> when only relative comparisons are needed, as it avoids the
/// square root computation. Supports <see cref="Vector2"/>, <see cref="Vector3"/>, and <see cref="Vector4"/>. Scalar,
/// quaternion, and plane types are not supported.
/// </summary>
/// <param name="operand">The resolver for the vector operand.</param>
public class LengthSquaredResolver(IPropertyResolver operand) : IPropertyResolver
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
			return new Variant128(value.AsVector2().LengthSquared());
		}

		if (operandType == typeof(Vector3))
		{
			return new Variant128(value.AsVector3().LengthSquared());
		}

		if (operandType == typeof(Vector4))
		{
			return new Variant128(value.AsVector4().LengthSquared());
		}

		throw new InvalidOperationException(
			$"LengthSquaredResolver encountered unexpected operand type '{operandType}'.");
	}

	private static Type ValidateType(Type type)
	{
		if (MathTypeUtils.IsVectorType(type))
		{
			return typeof(float);
		}

		throw new ArgumentException(
			$"LengthSquaredResolver only supports vector types (Vector2, Vector3, Vector4). Got '{type}'.");
	}
}
