// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a quaternion created from Euler angles stored in a <see cref="Vector3"/>.
/// </summary>
/// <param name="eulerAngles">The resolver for the Euler angles in radians, stored as pitch, yaw, and roll.</param>
/// <param name="order">The optional resolver for the <see cref="EulerOrder"/> value, encoded as an <see cref="int"/>.
/// </param>
public class QuaternionFromEulerAnglesResolver(IPropertyResolver eulerAngles, IPropertyResolver? order = null)
	: IPropertyResolver
{
	private readonly IPropertyResolver _eulerAngles = eulerAngles;

	private readonly IPropertyResolver? _order = order;

	/// <inheritdoc/>
	public Type ValueType { get; } = ValidateTypes(eulerAngles.ValueType, order?.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Vector3 eulerAnglesValue = _eulerAngles.Resolve(graphContext).AsVector3();
		EulerOrder orderValue = ResolveOrder(graphContext);
		return new Variant128(GameplayMathUtils.QuaternionFromEulerAngles(eulerAnglesValue, orderValue));
	}

	private static Type ValidateTypes(Type eulerAnglesType, Type? orderType)
	{
		if (eulerAnglesType != typeof(Vector3))
		{
			throw new ArgumentException(
				$"QuaternionFromEulerAnglesResolver requires 'eulerAngles' to be Vector3. Got '{eulerAnglesType}'.");
		}

		if (orderType is not null && orderType != typeof(int))
		{
			throw new ArgumentException(
				$"QuaternionFromEulerAnglesResolver requires 'order' to be int when provided. Got '{orderType}'.");
		}

		return typeof(Quaternion);
	}

	private EulerOrder ResolveOrder(GraphContext graphContext)
	{
		if (_order is null)
		{
			return EulerOrder.XYZ;
		}

		var orderValue = _order.Resolve(graphContext).AsInt();
		if (!Enum.IsDefined(typeof(EulerOrder), orderValue))
		{
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
			throw new ArgumentOutOfRangeException("order", orderValue, "Unsupported Euler order.");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
		}

		return (EulerOrder)orderValue;
	}
}
