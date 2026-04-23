// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the reflection of a vector off a surface defined by a normal vector. Returns a vector of the same type as
/// the operands. Both operands must be the same vector type. Supports <see cref="Vector2"/> and <see cref="Vector3"/>.
/// Scalar, quaternion, and plane types are not supported.
/// </summary>
/// <param name="incident">The resolver for the incident (incoming) vector.</param>
/// <param name="normal">The resolver for the surface normal vector.</param>
public class ReflectResolver(IPropertyResolver incident, IPropertyResolver normal) : IPropertyResolver
{
	private readonly IPropertyResolver _incident = incident;

	private readonly IPropertyResolver _normal = normal;

	/// <inheritdoc/>
	public Type ValueType { get; } = DetermineResultType(incident.ValueType, normal.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 incidentValue = _incident.Resolve(graphContext);
		Variant128 normalValue = _normal.Resolve(graphContext);
		Type resultType = ValueType;

		if (resultType == typeof(Vector2))
		{
			return new Variant128(
				Vector2.Reflect(incidentValue.AsVector2(), normalValue.AsVector2()));
		}

		if (resultType == typeof(Vector3))
		{
			return new Variant128(
				Vector3.Reflect(incidentValue.AsVector3(), normalValue.AsVector3()));
		}

		throw new InvalidOperationException(
			$"ReflectResolver encountered unexpected result type '{resultType}'.");
	}

	private static Type DetermineResultType(Type incidentType, Type normalType)
	{
		if (incidentType != normalType)
		{
			throw new ArgumentException(
				$"ReflectResolver requires matching vector types. Got '{incidentType}' and '{normalType}'.");
		}

		if (incidentType != typeof(Vector2) && incidentType != typeof(Vector3))
		{
			throw new ArgumentException(
				$"ReflectResolver only supports Vector2 and Vector3. Got '{incidentType}'.");
		}

		return incidentType;
	}
}
