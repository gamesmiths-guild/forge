// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a plane created from a normal vector and distance.
/// </summary>
/// <param name="normal">The resolver for the plane normal.</param>
/// <param name="distance">The resolver for the plane distance.</param>
public class PlaneFromNormalResolver(IPropertyResolver normal, IPropertyResolver distance) : IPropertyResolver
{
	private readonly IPropertyResolver _normal = normal;

	private readonly IPropertyResolver _distance = distance;

	/// <inheritdoc/>
	public Type ValueType { get; } = ValidateTypes(normal.ValueType, distance.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Vector3 normalValue = _normal.Resolve(graphContext).AsVector3();
		var distanceValue = _distance.Resolve(graphContext).AsFloat();
		return new Variant128(new Plane(normalValue, distanceValue));
	}

	private static Type ValidateTypes(Type normalType, Type distanceType)
	{
		if (normalType != typeof(Vector3) || distanceType != typeof(float))
		{
			throw new ArgumentException(
				"PlaneFromNormalResolver requires normal to be Vector3 and distance to be float. " +
				$"Got '{normalType}' and '{distanceType}'.");
		}

		return typeof(Plane);
	}
}
