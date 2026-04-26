// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the concatenation of two quaternion operands, using
/// <see cref="Quaternion.Concatenate(Quaternion, Quaternion)"/>.
/// </summary>
/// <param name="left">The resolver for the left quaternion operand.</param>
/// <param name="right">The resolver for the right quaternion operand.</param>
public class ConcatenateResolver(IPropertyResolver left, IPropertyResolver right) : IPropertyResolver
{
	private readonly IPropertyResolver _left = left;

	private readonly IPropertyResolver _right = right;

	/// <inheritdoc/>
	public Type ValueType { get; } = ValidateTypes(left.ValueType, right.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Quaternion leftValue = _left.Resolve(graphContext).AsQuaternion();
		Quaternion rightValue = _right.Resolve(graphContext).AsQuaternion();
		return new Variant128(Quaternion.Concatenate(leftValue, rightValue));
	}

	private static Type ValidateTypes(Type leftType, Type rightType)
	{
		if (leftType != typeof(Quaternion) || rightType != typeof(Quaternion))
		{
			throw new ArgumentException(
				$"ConcatenateResolver only supports Quaternion operands. Got '{leftType}' and '{rightType}'.");
		}

		return typeof(Quaternion);
	}
}
