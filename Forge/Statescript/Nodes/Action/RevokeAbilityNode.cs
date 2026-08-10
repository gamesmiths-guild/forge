// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Nodes.Action;

/// <summary>
/// Revokes granted abilities through their <see cref="AbilityHandle"/>.
/// </summary>
/// <remarks>
/// <para>Revoking removes the grant; it is not the same as canceling, which only stops the instances an ability is
/// currently running and leaves it granted. To cancel, use CancelAbility or CancelAbilities instead.</para>
/// <para>The handle input accepts either a single <see cref="AbilityHandle"/> or an array of handles (such as the
/// handle output of a GrantAbilityPermanently node, or a GetAbilityHandle resolver). Invalid handles are skipped
/// silently.</para>
/// <para>With <see cref="AbilityRevokeScope.AllGrants"/> the ability goes away even when an effect was granting it,
/// and that effect will not grant it again when it ends — an ability cleared while an item was providing it does not
/// come back when the item is unequipped and re-equipped. To take an ability away temporarily, prefer inhibition
/// through a block-ability-tags effect component, which is reversible.</para>
/// </remarks>
/// <param name="scope">Which grant sources to remove.</param>
/// <param name="removalPolicy">How active instances are treated once the last grant source is gone.</param>
public class RevokeAbilityNode(
	AbilityRevokeScope scope = AbilityRevokeScope.PermanentGrants,
	AbilityDeactivationPolicy removalPolicy = AbilityDeactivationPolicy.CancelImmediately) : ActionNode
{
	/// <summary>
	/// Input property index for the ability handle(s).
	/// </summary>
	public const byte AbilityInput = 0;

	private readonly AbilityRevokeScope _scope = scope;
	private readonly AbilityDeactivationPolicy _removalPolicy = removalPolicy;

	/// <inheritdoc/>
	public override string Description => "Revokes granted abilities through their handles.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Ability", typeof(AbilityHandle)));
	}

	/// <inheritdoc/>
	protected override void Execute(GraphContext graphContext)
	{
		if (!AbilityNodeUtilities.TryResolveHandles(
			graphContext,
			InputProperties[AbilityInput].BoundName,
			out IReadOnlyList<AbilityHandle> handles))
		{
			return;
		}

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

			if (_scope == AbilityRevokeScope.AllGrants)
			{
				abilities.ClearAbility(handle, _removalPolicy);
			}
			else
			{
				abilities.RevokeAbility(handle, _removalPolicy);
			}
		}
	}
}
