// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the logical negation of a single nested <see cref="IPropertyResolver"/> operand. The operand must resolve
/// to <see cref="bool"/>.
/// </summary>
/// <param name="operand">The resolver for the operand to negate.</param>
public class NotResolver(IPropertyResolver operand) : IPropertyResolver
{
	private readonly IPropertyResolver _operand =
		BooleanTypeUtils.ValidateBoolOperand(nameof(NotResolver), nameof(operand), operand);

	/// <inheritdoc/>
	public Type ValueType => typeof(bool);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		return new Variant128(!_operand.Resolve(graphContext).AsBool());
	}
}
