// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a plane created from three vertices, using
/// <see cref="Plane.CreateFromVertices(Vector3, Vector3, Vector3)"/>.
/// Returns a <see cref="Plane"/>.
/// </summary>
/// <param name="point1">The resolver for the first vertex.</param>
/// <param name="point2">The resolver for the second vertex.</param>
/// <param name="point3">The resolver for the third vertex.</param>
public class PlaneFromVerticesResolver(IPropertyResolver point1, IPropertyResolver point2, IPropertyResolver point3)
	: IPropertyResolver
{
	private readonly IPropertyResolver _point1 = point1;

	private readonly IPropertyResolver _point2 = point2;

	private readonly IPropertyResolver _point3 = point3;

	/// <inheritdoc/>
	public Type ValueType { get; } = ValidateTypes(point1.ValueType, point2.ValueType, point3.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Vector3 point1Value = _point1.Resolve(graphContext).AsVector3();
		Vector3 point2Value = _point2.Resolve(graphContext).AsVector3();
		Vector3 point3Value = _point3.Resolve(graphContext).AsVector3();
		return new Variant128(Plane.CreateFromVertices(point1Value, point2Value, point3Value));
	}

	private static Type ValidateTypes(Type point1Type, Type point2Type, Type point3Type)
	{
		if (point1Type != typeof(Vector3) || point2Type != typeof(Vector3) || point3Type != typeof(Vector3))
		{
			throw new ArgumentException(
				"PlaneFromVerticesResolver requires all operands to be Vector3. " +
				$"Got '{point1Type}', '{point2Type}', and '{point3Type}'.");
		}

		return typeof(Plane);
	}
}
