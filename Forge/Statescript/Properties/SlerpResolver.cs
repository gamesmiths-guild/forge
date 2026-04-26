// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a spherical linear interpolation between two <see cref="Quaternion"/> operands. This produces a smooth
/// constant-speed rotation between the two orientations, which is preferred over <see cref="LerpResolver"/> for
/// rotation interpolation when uniform angular velocity is needed. The <c>t</c> parameter must be
/// <see langword="float"/>.
/// </summary>
/// <param name="a">The resolver for the start quaternion.</param>
/// <param name="b">The resolver for the end quaternion.</param>
/// <param name="t">The resolver for the interpolation parameter (typically 0 to 1).</param>
public class SlerpResolver(IPropertyResolver a, IPropertyResolver b, IPropertyResolver t) : IPropertyResolver
{
	private readonly IPropertyResolver _a = a;

	private readonly IPropertyResolver _b = b;

	private readonly IPropertyResolver _t = t;

	/// <inheritdoc/>
	public Type ValueType { get; } = ValidateTypes(a.ValueType, b.ValueType, t.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 aValue = _a.Resolve(graphContext);
		Variant128 bValue = _b.Resolve(graphContext);
		Variant128 tValue = _t.Resolve(graphContext);

		return new Variant128(
			Quaternion.Slerp(aValue.AsQuaternion(), bValue.AsQuaternion(), tValue.AsFloat()));
	}

	private static Type ValidateTypes(Type aType, Type bType, Type tType)
	{
		if (aType != typeof(Quaternion))
		{
			throw new ArgumentException(
				$"SlerpResolver requires Quaternion operands for 'a'. Got '{aType}'.");
		}

		if (bType != typeof(Quaternion))
		{
			throw new ArgumentException(
				$"SlerpResolver requires Quaternion operands for 'b'. Got '{bType}'.");
		}

		if (tType != typeof(float))
		{
			throw new ArgumentException(
				$"SlerpResolver requires 't' to be float. Got '{tType}'.");
		}

		return typeof(Quaternion);
	}
}
