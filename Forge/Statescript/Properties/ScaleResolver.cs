// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the scalar multiplication (scaling) of a vector by a <see langword="float"/> value. Returns a vector of the
/// same type as the vector operand. Supports <see cref="Vector2"/>, <see cref="Vector3"/>, and <see cref="Vector4"/>.
/// The scalar operand must be <see langword="float"/>. This is distinct from <see cref="MultiplyResolver"/>, which
/// performs component-wise multiplication between two matching vectors.
/// </summary>
/// <param name="vector">The resolver for the vector operand.</param>
/// <param name="scalar">The resolver for the scalar (float) operand.</param>
public class ScaleResolver(IPropertyResolver vector, IPropertyResolver scalar) : IPropertyResolver
{
	private readonly IPropertyResolver _vector = vector;

	private readonly IPropertyResolver _scalar = scalar;

	/// <inheritdoc/>
	public Type ValueType { get; } = ValidateTypes(vector.ValueType, scalar.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 vectorValue = _vector.Resolve(graphContext);
		float scalarValue = _scalar.Resolve(graphContext).AsFloat();
		Type resultType = ValueType;

		if (resultType == typeof(Vector2))
		{
			return new Variant128(vectorValue.AsVector2() * scalarValue);
		}

		if (resultType == typeof(Vector3))
		{
			return new Variant128(vectorValue.AsVector3() * scalarValue);
		}

		if (resultType == typeof(Vector4))
		{
			return new Variant128(vectorValue.AsVector4() * scalarValue);
		}

		throw new InvalidOperationException(
			$"ScaleResolver encountered unexpected result type '{resultType}'.");
	}

	private static Type ValidateTypes(Type vectorType, Type scalarType)
	{
		if (!MathTypeUtils.IsVectorType(vectorType))
		{
			throw new ArgumentException(
				$"ScaleResolver requires a vector type for 'vector'. Got '{vectorType}'.");
		}

		if (scalarType != typeof(float))
		{
			throw new ArgumentException(
				$"ScaleResolver requires 'scalar' to be float. Got '{scalarType}'.");
		}

		return vectorType;
	}
}
