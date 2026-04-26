// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the projection of one vector onto another.
/// </summary>
/// <param name="value">The resolver for the source vector.</param>
/// <param name="onto">The resolver for the vector to project onto.</param>
public class ProjectResolver(IPropertyResolver value, IPropertyResolver onto) : IPropertyResolver
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
			return new Variant128(GameplayMathUtils.Project(valueResult.AsVector2(), ontoResult.AsVector2()));
		}

		if (resultType == typeof(Vector3))
		{
			return new Variant128(GameplayMathUtils.Project(valueResult.AsVector3(), ontoResult.AsVector3()));
		}

		if (resultType == typeof(Vector4))
		{
			return new Variant128(GameplayMathUtils.Project(valueResult.AsVector4(), ontoResult.AsVector4()));
		}

		throw new InvalidOperationException($"ProjectResolver encountered unexpected result type '{resultType}'.");
	}

	private static Type ValidateTypes(Type valueType, Type ontoType)
	{
		if (valueType != ontoType)
		{
			throw new ArgumentException(
				$"ProjectResolver requires matching vector types. Got '{valueType}' and '{ontoType}'.");
		}

		if (valueType == typeof(Vector2) || valueType == typeof(Vector3) || valueType == typeof(Vector4))
		{
			return valueType;
		}

		throw new ArgumentException(
			$"ProjectResolver only supports Vector2, Vector3, and Vector4. Got '{valueType}'.");
	}
}
