// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the dot product of a plane and a 3D coordinate, using <see cref="Plane.DotCoordinate(Plane, Vector3)"/>.
/// Returns a <see langword="float"/>.
/// </summary>
/// <param name="plane">The resolver for the plane operand.</param>
/// <param name="coordinate">The resolver for the coordinate operand.</param>
public class DotCoordinateResolver(IPropertyResolver plane, IPropertyResolver coordinate) : IPropertyResolver
{
	private readonly IPropertyResolver _plane = plane;

	private readonly IPropertyResolver _coordinate = coordinate;

	/// <inheritdoc/>
	public Type ValueType { get; } = ValidateTypes(plane.ValueType, coordinate.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Plane planeValue = _plane.Resolve(graphContext).AsPlane();
		Vector3 coordinateValue = _coordinate.Resolve(graphContext).AsVector3();
		return new Variant128(Plane.DotCoordinate(planeValue, coordinateValue));
	}

	private static Type ValidateTypes(Type planeType, Type coordinateType)
	{
		if (planeType != typeof(Plane))
		{
			throw new ArgumentException(
				$"DotCoordinateResolver requires 'plane' to be Plane. Got '{planeType}'.");
		}

		if (coordinateType != typeof(Vector3))
		{
			throw new ArgumentException(
				$"DotCoordinateResolver requires 'coordinate' to be Vector3. Got '{coordinateType}'.");
		}

		return typeof(float);
	}
}
