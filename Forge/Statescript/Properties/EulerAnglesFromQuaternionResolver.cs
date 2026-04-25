// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a <see cref="Vector3"/> containing Euler angles in radians extracted from a quaternion.
/// </summary>
/// <param name="quaternion">The resolver for the quaternion operand.</param>
/// <param name="order">The optional resolver for the <see cref="EulerOrder"/> value, encoded as an <see cref="int"/>.
/// </param>
public class EulerAnglesFromQuaternionResolver(IPropertyResolver quaternion, IPropertyResolver? order = null)
	: IPropertyResolver
{
	private readonly IPropertyResolver _quaternion = quaternion;

	private readonly IPropertyResolver? _order = order;

	/// <inheritdoc/>
	public Type ValueType { get; } = ValidateTypes(quaternion.ValueType, order?.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Quaternion value = _quaternion.Resolve(graphContext).AsQuaternion();
		EulerOrder orderValue = ResolveOrder(graphContext);
		return new Variant128(GameplayMathUtils.QuaternionToEulerAngles(value, orderValue));
	}

	private static Type ValidateTypes(Type quaternionType, Type? orderType)
	{
		if (quaternionType != typeof(Quaternion))
		{
			throw new ArgumentException(
				$"EulerAnglesFromQuaternionResolver only supports Quaternion operands. Got '{quaternionType}'.");
		}

		if (orderType is not null && orderType != typeof(int))
		{
			throw new ArgumentException(
				$"EulerAnglesFromQuaternionResolver requires 'order' to be int when provided. Got '{orderType}'.");
		}

		return typeof(Vector3);
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
#pragma warning disable CA2208, S3928 // Instantiate argument exceptions correctly
			throw new ArgumentOutOfRangeException(nameof(_order), orderValue, "Unsupported Euler order.");
#pragma warning restore CA2208, S3928 // Instantiate argument exceptions correctly
		}

		return (EulerOrder)orderValue;
	}
}
