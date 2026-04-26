// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the distance component of a plane.
/// </summary>
/// <param name="plane">The resolver for the plane operand.</param>
public class PlaneDistanceResolver(IPropertyResolver plane) : IPropertyResolver
{
	private readonly IPropertyResolver _plane = plane;

	/// <inheritdoc/>
	public Type ValueType { get; } = ValidateType(plane.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		return new Variant128(_plane.Resolve(graphContext).AsPlane().D);
	}

	private static Type ValidateType(Type planeType)
	{
		if (planeType != typeof(Plane))
		{
			throw new ArgumentException($"PlaneDistanceResolver only supports Plane operands. Got '{planeType}'.");
		}

		return typeof(float);
	}
}
