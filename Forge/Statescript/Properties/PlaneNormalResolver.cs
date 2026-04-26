// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the normal component of a plane.
/// </summary>
/// <param name="plane">The resolver for the plane operand.</param>
public class PlaneNormalResolver(IPropertyResolver plane) : IPropertyResolver
{
	private readonly IPropertyResolver _plane = plane;

	/// <inheritdoc/>
	public Type ValueType { get; } = ValidateType(plane.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		return new Variant128(_plane.Resolve(graphContext).AsPlane().Normal);
	}

	private static Type ValidateType(Type planeType)
	{
		if (planeType != typeof(Plane))
		{
			throw new ArgumentException($"PlaneNormalResolver only supports Plane operands. Got '{planeType}'.");
		}

		return typeof(Vector3);
	}
}
