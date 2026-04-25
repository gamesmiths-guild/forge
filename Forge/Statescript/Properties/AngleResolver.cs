// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the unsigned angle in radians between two vectors or two quaternions.
/// </summary>
/// <param name="from">The resolver for the first operand.</param>
/// <param name="to">The resolver for the second operand.</param>
public class AngleResolver(IPropertyResolver from, IPropertyResolver to) : IPropertyResolver
{
	private readonly IPropertyResolver _from = from;
	private readonly IPropertyResolver _to = to;

	private readonly Type _operandType = ValidateTypes(from.ValueType, to.ValueType);

	/// <inheritdoc/>
	public Type ValueType { get; } = typeof(float);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 fromValue = _from.Resolve(graphContext);
		Variant128 toValue = _to.Resolve(graphContext);

		if (_operandType == typeof(Vector2))
		{
			return new Variant128(GameplayMathUtils.Angle(fromValue.AsVector2(), toValue.AsVector2()));
		}

		if (_operandType == typeof(Vector3))
		{
			return new Variant128(GameplayMathUtils.Angle(fromValue.AsVector3(), toValue.AsVector3()));
		}

		if (_operandType == typeof(Quaternion))
		{
			return new Variant128(GameplayMathUtils.QuaternionAngle(fromValue.AsQuaternion(), toValue.AsQuaternion()));
		}

		throw new InvalidOperationException($"AngleResolver encountered unexpected operand type '{_operandType}'.");
	}

	private static Type ValidateTypes(Type fromType, Type toType)
	{
		if (fromType != toType)
		{
			throw new ArgumentException(
				$"AngleResolver requires matching operand types. Got '{fromType}' and '{toType}'.");
		}

		if (fromType == typeof(Vector2) || fromType == typeof(Vector3) || fromType == typeof(Quaternion))
		{
			return fromType;
		}

		throw new ArgumentException(
			$"AngleResolver only supports Vector2, Vector3, and Quaternion. Got '{fromType}'.");
	}
}
