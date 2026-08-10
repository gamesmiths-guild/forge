// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;

namespace Gamesmiths.Forge.Statescript.Nodes.Condition;

/// <summary>
/// Tries to commit the cost and/or cooldown of the ability driving the current graph, routing to the True port when
/// the commit succeeds.
/// </summary>
/// <remarks>
/// <para>Costs and cooldowns are only paid when explicitly committed. Place this node early in an ability graph
/// (typically right after the Entry node) to pay the ability's cost and start its cooldown, or later if the graph
/// tests conditions before committing.</para>
/// <para>A graph can reach this node long after the ability activated, by which point the cooldown may have started or
/// the resources may have been spent elsewhere. The commit is therefore re-checked and can fail, which is what the
/// False port is for: route it to whatever the graph should do when the ability cannot pay, usually a
/// <see cref="Action.CancelAbilityNode"/>.</para>
/// <para>The node reads the <see cref="AbilityBehaviorContext"/> from the graph's activation context. When the graph
/// runs without an ability context (standalone execution), there is nothing to commit and the node routes to
/// False.</para>
/// </remarks>
/// <param name="operation">What to commit when the node executes.</param>
public class TryCommitAbilityNode(CommitAbilityOperation operation = CommitAbilityOperation.CostAndCooldown)
	: ConditionNode
{
	private readonly CommitAbilityOperation _operation = operation;

	/// <inheritdoc/>
	public override string Description =>
		"Tries to commit the cost and/or cooldown of the ability driving the graph; True when committed.";

	/// <inheritdoc/>
	protected override bool Test(GraphContext graphContext)
	{
		if (!graphContext.TryGetActivationContext(out AbilityBehaviorContext? abilityContext))
		{
			return false;
		}

		return _operation switch
		{
			CommitAbilityOperation.CooldownOnly => abilityContext.AbilityHandle.TryCommitCooldown(),
			CommitAbilityOperation.CostOnly => abilityContext.AbilityHandle.TryCommitCost(),

			// CostAndCooldown, and equally anything a corrupted configuration deserializes into. Committing both is
			// the node's default and, unlike a no-op, can never hand out a free cast; it also keeps the True/False
			// result honest, since the port still reports a commit that was genuinely attempted.
			_ => abilityContext.AbilityHandle.TryCommitAbility(),
		};
	}
}
