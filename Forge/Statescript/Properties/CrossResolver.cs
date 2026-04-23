// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the cross product of two <see cref="Vector3"/> operands. Returns a <see cref="Vector3"/> that is
/// perpendicular to both input vectors. Only <see cref="Vector3"/> operands are supported.
/// </summary>
/// <param name="left">The resolver for the left vector operand.</param>
/// <param name="right">The resolver for the right vector operand.</param>
public class CrossResolver(IPropertyResolver left, IPropertyResolver right) : IPropertyResolver
{
	private readonly IPropertyResolver _left = left;

	private readonly IPropertyResolver _right = right;

	/// <inheritdoc/>
	public Type ValueType { get; } = ValidateTypes(left.ValueType, right.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 leftValue = _left.Resolve(graphContext);
		Variant128 rightValue = _right.Resolve(graphContext);

		return new Variant128(Vector3.Cross(leftValue.AsVector3(), rightValue.AsVector3()));
	}

	private static Type ValidateTypes(Type leftType, Type rightType)
	{
		if (leftType != typeof(Vector3) || rightType != typeof(Vector3))
		{
			throw new ArgumentException(
				$"CrossResolver only supports Vector3 operands. Got '{leftType}' and '{rightType}'.");
		}

		return typeof(Vector3);
	}
}
