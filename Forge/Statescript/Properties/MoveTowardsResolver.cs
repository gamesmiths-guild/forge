// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a value moved toward a target by a maximum delta. Supports <see cref="float"/>, <see cref="Vector2"/>,
/// <see cref="Vector3"/>, and <see cref="Vector4"/>.
/// </summary>
/// <param name="current">The resolver for the current value.</param>
/// <param name="target">The resolver for the target value.</param>
/// <param name="maxDelta">The resolver for the maximum delta.</param>
public class MoveTowardsResolver(IPropertyResolver current, IPropertyResolver target, IPropertyResolver maxDelta)
	: IPropertyResolver
{
	private readonly IPropertyResolver _current = current;
	private readonly IPropertyResolver _target = target;
	private readonly IPropertyResolver _maxDelta = maxDelta;

	/// <inheritdoc/>
	public Type ValueType { get; } = ValidateTypes(current.ValueType, target.ValueType, maxDelta.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 currentValue = _current.Resolve(graphContext);
		Variant128 targetValue = _target.Resolve(graphContext);
		float maxDeltaValue = _maxDelta.Resolve(graphContext).AsFloat();
		Type resultType = ValueType;

		if (resultType == typeof(float))
		{
			return new Variant128(
				GameplayMathUtils.MoveTowards(currentValue.AsFloat(), targetValue.AsFloat(), maxDeltaValue));
		}

		if (resultType == typeof(Vector2))
		{
			return new Variant128(
				GameplayMathUtils.MoveTowards(currentValue.AsVector2(), targetValue.AsVector2(), maxDeltaValue));
		}

		if (resultType == typeof(Vector3))
		{
			return new Variant128(
				GameplayMathUtils.MoveTowards(currentValue.AsVector3(), targetValue.AsVector3(), maxDeltaValue));
		}

		if (resultType == typeof(Vector4))
		{
			return new Variant128(
				GameplayMathUtils.MoveTowards(currentValue.AsVector4(), targetValue.AsVector4(), maxDeltaValue));
		}

		throw new InvalidOperationException($"MoveTowardsResolver encountered unexpected result type '{resultType}'.");
	}

	private static Type ValidateTypes(Type currentType, Type targetType, Type maxDeltaType)
	{
		if (maxDeltaType != typeof(float))
		{
			throw new ArgumentException(
				$"MoveTowardsResolver requires maxDelta to be float. Got '{maxDeltaType}'.");
		}

		if (currentType != targetType)
		{
			throw new ArgumentException(
				"MoveTowardsResolver requires current and target to have matching types. " +
				$"Got '{currentType}' and '{targetType}'.");
		}

		if (currentType == typeof(float)
			|| currentType == typeof(Vector2)
			|| currentType == typeof(Vector3)
			|| currentType == typeof(Vector4))
		{
			return currentType;
		}

		throw new ArgumentException(
			$"MoveTowardsResolver only supports float, Vector2, Vector3, and Vector4. Got '{currentType}'.");
	}
}
