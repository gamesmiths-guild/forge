// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Nodes.Condition;

/// <summary>
/// Tries to revoke granted abilities through their <see cref="AbilityHandle"/>, routing to the True port when at least
/// one revocation succeeds.
/// </summary>
/// <remarks>
/// <para>Revoking removes the grant; it is not the same as canceling, which only stops the instances an ability is
/// currently running and leaves it granted. To cancel, use CancelAbility or CancelAbilities instead.</para>
/// <para>The handle input accepts either a single <see cref="AbilityHandle"/> or an array of handles (such as the
/// handle output of a GrantAbilityPermanently node, or a GetAbilityHandle resolver). Invalid handles are skipped, and
/// every handle is attempted before the result is reported.</para>
/// <para>The False port is the useful half: it says the entity had nothing to revoke — no such grant, or none of the
/// kind this node removes — which is what a respec-and-refund flow needs to know. A resolver cannot answer that,
/// since it cannot tell a permanent grant from an effect's grant.</para>
/// <para>With <see cref="AbilityRevokeScope.AllGrants"/> the ability goes away even when an effect was granting it.
/// That effect <i>application</i> keeps an invalid handle and goes inert with respect to the ability: it can no longer
/// remove or inhibit it, and will not restore it if it is un-inhibited later. Applying the effect again grants the
/// ability afresh, so re-equipping the item that provides it does bring it back. To suppress an ability reversibly
/// without orphaning its grants, prefer inhibition through a block-ability-tags effect component.</para>
/// </remarks>
/// <param name="scope">Which grant sources to remove.</param>
/// <param name="removalPolicy">How active instances are treated once the last grant source is gone.</param>
public class TryRevokeAbilityNode(
	AbilityRevokeScope scope = AbilityRevokeScope.PermanentGrants,
	AbilityDeactivationPolicy removalPolicy = AbilityDeactivationPolicy.CancelImmediately) : ConditionNode
{
	/// <summary>
	/// Input property index for the ability handle(s).
	/// </summary>
	public const byte AbilityInput = 0;

	private readonly AbilityRevokeScope _scope = scope;
	private readonly AbilityDeactivationPolicy _removalPolicy = removalPolicy;

	/// <inheritdoc/>
	public override string Description =>
		"Tries to revoke granted abilities through their handles; True when any was revoked.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Ability", typeof(AbilityHandle)));
	}

	/// <inheritdoc/>
	protected override bool Test(GraphContext graphContext)
	{
		if (!AbilityNodeUtilities.TryResolveHandles(
			graphContext,
			InputProperties[AbilityInput].BoundName,
			out IReadOnlyList<AbilityHandle> handles))
		{
			return false;
		}

		bool revokedAny = false;

		for (int i = 0; i < handles.Count; i++)
		{
			AbilityHandle handle = handles[i];

			// Read per handle rather than once: revoking one ability can tear down the graph that is running this
			// node, and a handle freed by an earlier iteration reports no owner at all.
			EntityAbilities? abilities = handle.Ability?.Owner.Abilities;

			if (abilities is null)
			{
				continue;
			}

			// Deliberately not short-circuiting: every handle is attempted, and the result reports whether any of
			// them had something to revoke.
			revokedAny |= _scope == AbilityRevokeScope.AllGrants
				? abilities.ClearAbility(handle, _removalPolicy)
				: abilities.RevokeAbility(handle, _removalPolicy);
		}

		return revokedAny;
	}
}
