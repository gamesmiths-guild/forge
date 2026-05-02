// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a vector clamped to a maximum magnitude.
/// </summary>
/// <param name="value">The resolver for the vector value.</param>
/// <param name="maxLength">The resolver for the maximum length.</param>
public class ClampMagnitudeResolver(IPropertyResolver value, IPropertyResolver maxLength) : IPropertyResolver
{
	private readonly IPropertyResolver _value = value;
	private readonly IPropertyResolver _maxLength = maxLength;

	/// <inheritdoc/>
	public Type ValueType { get; } = ValidateTypes(value.ValueType, maxLength.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 valueResult = _value.Resolve(graphContext);
		float maxLengthValue = _maxLength.Resolve(graphContext).AsFloat();
		Type resultType = ValueType;

		if (resultType == typeof(Vector2))
		{
			return new Variant128(GameplayMathUtils.ClampMagnitude(valueResult.AsVector2(), maxLengthValue));
		}

		if (resultType == typeof(Vector3))
		{
			return new Variant128(GameplayMathUtils.ClampMagnitude(valueResult.AsVector3(), maxLengthValue));
		}

		if (resultType == typeof(Vector4))
		{
			return new Variant128(GameplayMathUtils.ClampMagnitude(valueResult.AsVector4(), maxLengthValue));
		}

		throw new InvalidOperationException($"ClampMagnitudeResolver encountered unexpected result type '{resultType}'.");
	}

	private static Type ValidateTypes(Type valueType, Type maxLengthType)
	{
		if (maxLengthType != typeof(float))
		{
			throw new ArgumentException(
				$"ClampMagnitudeResolver requires maxLength to be float. Got '{maxLengthType}'.");
		}

		if (valueType == typeof(Vector2) || valueType == typeof(Vector3) || valueType == typeof(Vector4))
		{
			return valueType;
		}

		throw new ArgumentException(
			$"ClampMagnitudeResolver only supports Vector2, Vector3, and Vector4. Got '{valueType}'.");
	}
}
