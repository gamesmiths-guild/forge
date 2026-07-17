// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Nodes.Condition;

/// <summary>
/// Tries to activate an ability through its <see cref="AbilityHandle"/>, routing to the True port when the
/// activation succeeds.
/// </summary>
/// <remarks>
/// <para>The handle input must resolve to an <see cref="AbilityHandle"/>, typically produced by a GetAbilityHandle
/// resolver or the output of a grant node. The optional target input is passed as the activation target.</para>
/// <para>The magnitude input must resolve to a <see langword="double"/> and defaults to <c>0</c> when unbound.</para>
/// </remarks>
public class TryActivateAbilityNode : ConditionNode
{
	/// <summary>
	/// Input property index for the ability handle to activate.
	/// </summary>
	public const byte AbilityInput = 0;

	/// <summary>
	/// Input property index for the optional activation target.
	/// </summary>
	public const byte TargetInput = 1;

	/// <summary>
	/// Input property index for the optional activation magnitude.
	/// </summary>
	public const byte MagnitudeInput = 2;

	/// <inheritdoc/>
	public override string Description => "Tries to activate an ability through its handle; True when activated.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Ability", typeof(AbilityHandle)));
		inputProperties.Add(new InputProperty("Target", typeof(IForgeEntity)));
		inputProperties.Add(new InputProperty("Magnitude", typeof(double)));
	}

	/// <inheritdoc/>
	protected override bool Test(GraphContext graphContext)
	{
		if (!graphContext.TryResolveObject(
			InputProperties[AbilityInput].BoundName,
			typeof(AbilityHandle),
			out object? resolved)
			|| resolved is not AbilityHandle handle)
		{
			return false;
		}

		IForgeEntity? target = AbilityNodeUtilities.ResolveOptionalEntity(
			graphContext,
			InputProperties[TargetInput].BoundName);

		if (!graphContext.TryResolve(InputProperties[MagnitudeInput].BoundName, out double magnitude))
		{
			magnitude = 0;
		}

		return handle.Activate(out _, target, (float)magnitude);
	}
}
