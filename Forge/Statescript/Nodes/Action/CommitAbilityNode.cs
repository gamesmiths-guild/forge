// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;

namespace Gamesmiths.Forge.Statescript.Nodes.Action;

/// <summary>
/// Commits the cost and/or cooldown of the ability driving the current graph.
/// </summary>
/// <remarks>
/// <para>Costs and cooldowns are only paid when explicitly committed. Place this node early in an ability graph
/// (typically right after the Entry node) to pay the ability's cost and start its cooldown, or later if the graph
/// tests conditions before committing.</para>
/// <para>The node reads the <see cref="AbilityBehaviorContext"/> from the graph's activation context. When the graph
/// runs without an ability context (standalone execution), the node does nothing.</para>
/// </remarks>
/// <param name="operation">What to commit when the node executes.</param>
public class CommitAbilityNode(CommitAbilityOperation operation = CommitAbilityOperation.CostAndCooldown) : ActionNode
{
	private readonly CommitAbilityOperation _operation = operation;

	/// <inheritdoc/>
	public override string Description => "Commits the cost and/or cooldown of the ability driving the graph.";

	/// <inheritdoc/>
	protected override void Execute(GraphContext graphContext)
	{
		if (!graphContext.TryGetActivationContext(out AbilityBehaviorContext? abilityContext))
		{
			return;
		}

		switch (_operation)
		{
			case CommitAbilityOperation.CostAndCooldown:
				abilityContext.AbilityHandle.CommitAbility();
				break;

			case CommitAbilityOperation.CooldownOnly:
				abilityContext.AbilityHandle.CommitCooldown();
				break;

			case CommitAbilityOperation.CostOnly:
				abilityContext.AbilityHandle.CommitCost();
				break;
		}
	}
}
