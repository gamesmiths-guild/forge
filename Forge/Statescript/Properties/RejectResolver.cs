// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the rejection of one vector from another, removing the projected component.
/// </summary>
/// <param name="value">The resolver for the source vector.</param>
/// <param name="onto">The resolver for the vector to reject from.</param>
public class RejectResolver(IPropertyResolver value, IPropertyResolver onto) : IPropertyResolver
{
	private readonly IPropertyResolver _value = value;
	private readonly IPropertyResolver _onto = onto;

	/// <inheritdoc/>
	public Type ValueType { get; } = ValidateTypes(value.ValueType, onto.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 valueResult = _value.Resolve(graphContext);
		Variant128 ontoResult = _onto.Resolve(graphContext);
		Type resultType = ValueType;

		if (resultType == typeof(Vector2))
		{
			Vector2 valueVector = valueResult.AsVector2();
			return new Variant128(valueVector - GameplayMathUtils.Project(valueVector, ontoResult.AsVector2()));
		}

		if (resultType == typeof(Vector3))
		{
			Vector3 valueVector = valueResult.AsVector3();
			return new Variant128(valueVector - GameplayMathUtils.Project(valueVector, ontoResult.AsVector3()));
		}

		if (resultType == typeof(Vector4))
		{
			Vector4 valueVector = valueResult.AsVector4();
			return new Variant128(valueVector - GameplayMathUtils.Project(valueVector, ontoResult.AsVector4()));
		}

		throw new InvalidOperationException($"RejectResolver encountered unexpected result type '{resultType}'.");
	}

	private static Type ValidateTypes(Type valueType, Type ontoType)
	{
		if (valueType != ontoType)
		{
			throw new ArgumentException(
				$"RejectResolver requires matching vector types. Got '{valueType}' and '{ontoType}'.");
		}

		if (valueType == typeof(Vector2) || valueType == typeof(Vector3) || valueType == typeof(Vector4))
		{
			return valueType;
		}

		throw new ArgumentException(
			$"RejectResolver only supports Vector2, Vector3, and Vector4. Got '{valueType}'.");
	}
}
