// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Nodes.Condition;

/// <summary>
/// A concrete <see cref="ConditionNode"/> that evaluates a <see langword="bool"/> graph property to determine which
/// output port to activate. The property can be a simple variable, a <see cref="Properties.TagResolver"/>, a
/// <see cref="Properties.ComparisonResolver"/>, or any arbitrarily nested <see cref="Properties.IPropertyResolver"/>
/// chain that produces a <see langword="bool"/>.
/// </summary>
/// <remarks>
/// <para>This node eliminates the need to create custom <see cref="ConditionNode"/> subclasses for data-driven
/// conditions. Instead of writing C# logic, the scripter composes an expression from resolvers at graph construction
/// time.</para>
/// </remarks>
/// <param name="conditionPropertyName">The name of the graph property that provides the condition result. Must resolve
/// to a <see langword="bool"/> value.</param>
public class ExpressionConditionNode(StringKey conditionPropertyName) : ConditionNode
{
	private readonly StringKey _conditionPropertyName = conditionPropertyName;

	/// <inheritdoc/>
	protected override bool Test(GraphContext graphContext)
	{
		if (!graphContext.GraphVariables.TryGet(_conditionPropertyName, graphContext, out bool result))
		{
			return false;
		}

		return result;
	}
}
