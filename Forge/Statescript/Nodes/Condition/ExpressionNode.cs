// Copyright © Gamesmiths Guild.

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
public class ExpressionNode : ConditionNode
{
	/// <summary>
	/// Input property index for the condition expression.
	/// </summary>
	public const byte ConditionInput = 0;

	/// <inheritdoc/>
	public override string Description => "Evaluates the given property to determine which port to activate.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Condition", typeof(bool)));
	}

	/// <inheritdoc/>
	protected override bool Test(GraphContext graphContext)
	{
		if (!graphContext.TryResolve(InputProperties[ConditionInput].BoundName, out bool result))
		{
			return false;
		}

		return result;
	}
}
