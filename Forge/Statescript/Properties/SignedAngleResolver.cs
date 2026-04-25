// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the signed angle in radians between two vectors. Supports <see cref="Vector2"/> directly, or
/// <see cref="Vector3"/> with an explicit axis.
/// </summary>
public class SignedAngleResolver : IPropertyResolver
{
	private readonly IPropertyResolver _from;

	private readonly IPropertyResolver _to;

	private readonly IPropertyResolver? _axis;

	private readonly Type _operandType;

	/// <inheritdoc/>
	public Type ValueType { get; } = typeof(float);

	/// <summary>
	/// Initializes a new instance of the <see cref="SignedAngleResolver"/> class using the specified source and target
	/// property resolvers.
	/// </summary>
	/// <remarks>Both property resolvers must provide compatible value types for angle resolution. An exception is
	/// thrown if the types are not compatible.</remarks>
	/// <param name="from">The resolver for the first vector.</param>
	/// <param name="to">The resolver for the second vector.</param>
	public SignedAngleResolver(IPropertyResolver from, IPropertyResolver to)
	{
		_from = from;
		_to = to;
		_operandType = ValidateTypes(from.ValueType, to.ValueType, null);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SignedAngleResolver"/> class using the specified source, target,
	/// and axis property resolvers.
	/// </summary>
	/// <param name="from">The resolver for the first vector.</param>
	/// <param name="to">The resolver for the second vector.</param>
	/// <param name="axis">The optional resolver for the signed axis when using <see cref="Vector3"/> operands.</param>
	public SignedAngleResolver(IPropertyResolver from, IPropertyResolver to, IPropertyResolver axis)
	{
		_from = from;
		_to = to;
		_axis = axis;
		_operandType = ValidateTypes(from.ValueType, to.ValueType, axis.ValueType);
	}

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 fromValue = _from.Resolve(graphContext);
		Variant128 toValue = _to.Resolve(graphContext);

		if (_operandType == typeof(Vector2))
		{
			return new Variant128(GameplayMathUtils.SignedAngle(fromValue.AsVector2(), toValue.AsVector2()));
		}

		if (_operandType == typeof(Vector3) && _axis is not null)
		{
			Vector3 axisValue = _axis.Resolve(graphContext).AsVector3();
			return new Variant128(GameplayMathUtils.SignedAngle(fromValue.AsVector3(), toValue.AsVector3(), axisValue));
		}

		throw new InvalidOperationException(
			$"SignedAngleResolver encountered unexpected operand type '{_operandType}'.");
	}

	private static Type ValidateTypes(Type fromType, Type toType, Type? axisType)
	{
		if (fromType != toType)
		{
			throw new ArgumentException(
				$"SignedAngleResolver requires matching operand types. Got '{fromType}' and '{toType}'.");
		}

		if (fromType == typeof(Vector2))
		{
			if (axisType is not null)
			{
				throw new ArgumentException("SignedAngleResolver does not accept an axis for Vector2 operands.");
			}

			return typeof(Vector2);
		}

		if (fromType == typeof(Vector3) && axisType == typeof(Vector3))
		{
			return typeof(Vector3);
		}

		throw new ArgumentException(
			"SignedAngleResolver supports Vector2 without axis, or Vector3 with a Vector3 axis. " +
			$"Got '{fromType}', '{toType}', and '{axisType}'.");
	}
}
