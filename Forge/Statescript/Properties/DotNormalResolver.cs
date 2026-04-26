// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the dot product of a plane normal and a vector, using <see cref="Plane.DotNormal(Plane, Vector3)"/>.
/// Returns a <see langword="float"/>.
/// </summary>
/// <param name="plane">The resolver for the plane operand.</param>
/// <param name="normal">The resolver for the vector operand.</param>
public class DotNormalResolver(IPropertyResolver plane, IPropertyResolver normal) : IPropertyResolver
{
	private readonly IPropertyResolver _plane = plane;

	private readonly IPropertyResolver _normal = normal;

	/// <inheritdoc/>
	public Type ValueType { get; } = ValidateTypes(plane.ValueType, normal.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Plane planeValue = _plane.Resolve(graphContext).AsPlane();
		Vector3 normalValue = _normal.Resolve(graphContext).AsVector3();
		return new Variant128(Plane.DotNormal(planeValue, normalValue));
	}

	private static Type ValidateTypes(Type planeType, Type normalType)
	{
		if (planeType != typeof(Plane))
		{
			throw new ArgumentException(
				$"DotNormalResolver requires 'plane' to be Plane. Got '{planeType}'.");
		}

		if (normalType != typeof(Vector3))
		{
			throw new ArgumentException(
				$"DotNormalResolver requires 'normal' to be Vector3. Got '{normalType}'.");
		}

		return typeof(float);
	}
}
