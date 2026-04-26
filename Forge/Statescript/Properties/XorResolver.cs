// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the logical exclusive OR of two nested <see cref="IPropertyResolver"/> operands. Both operands must
/// resolve to <see cref="bool"/>.
/// </summary>
/// <remarks>
/// Both operands are resolved on every call before applying the boolean operator.
/// </remarks>
/// <param name="left">The resolver for the left operand.</param>
/// <param name="right">The resolver for the right operand.</param>
public class XorResolver(IPropertyResolver left, IPropertyResolver right) : IPropertyResolver
{
	private readonly IPropertyResolver _left =
		BooleanTypeUtils.ValidateBoolOperand(nameof(XorResolver), nameof(left), left);

	private readonly IPropertyResolver _right =
		BooleanTypeUtils.ValidateBoolOperand(nameof(XorResolver), nameof(right), right);

	/// <inheritdoc/>
	public Type ValueType => typeof(bool);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		var leftValue = _left.Resolve(graphContext).AsBool();
		var rightValue = _right.Resolve(graphContext).AsBool();

		return new Variant128(leftValue ^ rightValue);
	}
}
