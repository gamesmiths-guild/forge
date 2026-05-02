// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a quaternion rotated toward a target quaternion by a maximum angular delta.
/// </summary>
/// <param name="current">The resolver for the current quaternion.</param>
/// <param name="target">The resolver for the target quaternion.</param>
/// <param name="maxRadiansDelta">The resolver for the maximum angular delta in radians.</param>
public class RotateTowardsResolver(
	IPropertyResolver current,
	IPropertyResolver target,
	IPropertyResolver maxRadiansDelta)
		: IPropertyResolver
{
	private readonly IPropertyResolver _current = current;
	private readonly IPropertyResolver _target = target;
	private readonly IPropertyResolver _maxRadiansDelta = maxRadiansDelta;

	/// <inheritdoc/>
	public Type ValueType { get; } = ValidateTypes(current.ValueType, target.ValueType, maxRadiansDelta.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Quaternion currentValue = _current.Resolve(graphContext).AsQuaternion();
		Quaternion targetValue = _target.Resolve(graphContext).AsQuaternion();
		float deltaValue = _maxRadiansDelta.Resolve(graphContext).AsFloat();
		return new Variant128(GameplayMathUtils.RotateTowards(currentValue, targetValue, deltaValue));
	}

	private static Type ValidateTypes(Type currentType, Type targetType, Type deltaType)
	{
		if (currentType != typeof(Quaternion) || targetType != typeof(Quaternion) || deltaType != typeof(float))
		{
			throw new ArgumentException(
				"RotateTowardsResolver requires current and target to be Quaternion and maxRadiansDelta to be float. " +
				$"Got '{currentType}', '{targetType}', and '{deltaType}'.");
		}

		return typeof(Quaternion);
	}
}
