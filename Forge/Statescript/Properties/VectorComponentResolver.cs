// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a single component from a vector value.
/// </summary>
/// <param name="vector">The resolver for the vector operand.</param>
/// <param name="component">The component to extract.</param>
public class VectorComponentResolver(IPropertyResolver vector, VectorComponent component) : IPropertyResolver
{
	private readonly IPropertyResolver _vector = vector;

	private readonly VectorComponent _component = component;

	private readonly Type _vectorType = ValidateType(vector.ValueType, component);

	/// <inheritdoc/>
	public Type ValueType { get; } = typeof(float);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 value = _vector.Resolve(graphContext);
		return _vectorType switch
		{
			not null when _vectorType == typeof(Vector2) => new Variant128(_component switch
			{
				VectorComponent.X => value.AsVector2().X,
				VectorComponent.Y => value.AsVector2().Y,
				_ => throw new InvalidOperationException(
					$"VectorComponentResolver component '{_component}' is invalid for Vector2."),
			}),
			not null when _vectorType == typeof(Vector3) => new Variant128(_component switch
			{
				VectorComponent.X => value.AsVector3().X,
				VectorComponent.Y => value.AsVector3().Y,
				VectorComponent.Z => value.AsVector3().Z,
				_ => throw new InvalidOperationException(
					$"VectorComponentResolver component '{_component}' is invalid for Vector3."),
			}),
			not null when _vectorType == typeof(Vector4) => new Variant128(_component switch
			{
				VectorComponent.X => value.AsVector4().X,
				VectorComponent.Y => value.AsVector4().Y,
				VectorComponent.Z => value.AsVector4().Z,
				VectorComponent.W => value.AsVector4().W,
				_ => throw new InvalidOperationException(
					$"VectorComponentResolver component '{_component}' is invalid for Vector4."),
			}),
			_ => throw new InvalidOperationException(
				$"VectorComponentResolver encountered unexpected vector type '{_vectorType}'."),
		};
	}

	private static Type ValidateType(Type vectorType, VectorComponent component)
	{
		if (vectorType == typeof(Vector2)
			&& (component == VectorComponent.X || component == VectorComponent.Y))
		{
			return vectorType;
		}

		if (vectorType == typeof(Vector3)
			&& (component == VectorComponent.X || component == VectorComponent.Y || component == VectorComponent.Z))
		{
			return vectorType;
		}

		if (vectorType == typeof(Vector4))
		{
			return vectorType;
		}

		throw new ArgumentException(
			$"VectorComponentResolver does not support component '{component}' for type '{vectorType}'.");
	}
}
