// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Nodes.Action;

/// <summary>
/// An action node that writes a value to a graph variable. The value is read from a bound input property (which can
/// resolve from a variable, an entity attribute, or any other <see cref="Properties.IPropertyResolver"/>) and written
/// to a bound output variable.
/// </summary>
/// <remarks>
/// <para>The source (input) and target (output) are bound by name at graph construction time via
/// <see cref="Node.BindInput"/> and <see cref="Node.BindOutput"/>. At runtime, the node resolves the input and writes
/// its <see cref="Variant128"/> value to the output variable.</para>
/// </remarks>
public class SetVariableNode : ActionNode
{
	/// <summary>
	/// Input property index for the source value.
	/// </summary>
	public const byte SourceInput = 0;

	/// <summary>
	/// Output variable index for the target variable.
	/// </summary>
	public const byte TargetOutput = 0;

	/// <inheritdoc/>
	public override string Description => "Sets a graph variable to the value of a property.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Source", typeof(Variant128)));
		outputVariables.Add(new OutputVariable("Target", typeof(Variant128)));
	}

	/// <inheritdoc/>
	protected override void Execute(GraphContext graphContext)
	{
		if (!graphContext.TryResolveVariant(InputProperties[SourceInput].BoundName, out Variant128 value))
		{
			return;
		}

		OutputVariable target = OutputVariables[TargetOutput];

		if (target.Scope == VariableScope.Shared)
		{
			graphContext.SharedVariables?.SetVariant(target.BoundName, value);
		}
		else
		{
			graphContext.GraphVariables.SetVariant(target.BoundName, value);
		}
	}
}
