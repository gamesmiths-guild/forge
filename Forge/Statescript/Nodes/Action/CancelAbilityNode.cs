// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;

namespace Gamesmiths.Forge.Statescript.Nodes.Action;

/// <summary>
/// Cancels the ability driving the current graph.
/// </summary>
/// <remarks>
/// <para>Unlike reaching an Exit node, which ends the ability instance gracefully, canceling marks the ability as
/// canceled (<see cref="AbilityEndedData.WasCanceled"/>) and stops the whole graph immediately.</para>
/// <para>When the graph runs without an ability context (standalone execution), the node does nothing.</para>
/// </remarks>
public class CancelAbilityNode : ActionNode
{
	/// <inheritdoc/>
	public override string Description => "Cancels the ability driving the graph.";

	/// <inheritdoc/>
	protected override void Execute(GraphContext graphContext)
	{
		if (!graphContext.TryGetActivationContext(out AbilityBehaviorContext? abilityContext))
		{
			return;
		}

		abilityContext.AbilityHandle.Cancel();
	}
}
