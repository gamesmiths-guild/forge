// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Nodes.Condition;

/// <summary>
/// Tries to activate every granted ability on an entity whose ability tags match any of the given tags, routing to
/// the True port when at least one activation succeeds.
/// </summary>
/// <remarks>
/// <para>The tag input accepts a single <see cref="Tag"/> or an array of tags. The entity input selects whose
/// abilities are activated, defaulting to the ability context's owner when unbound. The optional target input is
/// passed as the activation target.</para>
/// <para>The optional activation-data input supplies an <see cref="AbilityActivator"/> (typically built by an
/// <see cref="Providers.IAbilityActivationDataProvider"/>) carrying strongly-typed data for the activations. Because a
/// tag can select several abilities, the data is not guaranteed to match all of them: abilities whose behavior does not
/// accept the provider's data type still activate and simply ignore it. When the input is unbound, the abilities are
/// activated without custom data.</para>
/// </remarks>
public class TryActivateAbilitiesByTagNode : ConditionNode
{
	/// <summary>
	/// Input property index for the tag(s) selecting which abilities to activate.
	/// </summary>
	public const byte TagInput = 0;

	/// <summary>
	/// Input property index for the entity whose abilities are activated.
	/// </summary>
	public const byte EntityInput = 1;

	/// <summary>
	/// Input property index for the optional activation target.
	/// </summary>
	public const byte TargetInput = 2;

	/// <summary>
	/// Input property index for the optional custom activation data.
	/// </summary>
	public const byte ActivationDataInput = 3;

	/// <inheritdoc/>
	public override string Description =>
		"Tries to activate abilities matching the given tags; True when any activated.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Tags", typeof(Tag)));
		inputProperties.Add(new InputProperty("Entity", typeof(IForgeEntity)));
		inputProperties.Add(new InputProperty("Target", typeof(IForgeEntity), IsOptional: true));
		inputProperties.Add(new InputProperty("Activation Data", typeof(AbilityActivator), IsOptional: true));
	}

	/// <inheritdoc/>
	protected override bool Test(GraphContext graphContext)
	{
		IForgeEntity? entity = AbilityNodeUtilities.ResolveEntityOrOwner(
			graphContext,
			InputProperties[EntityInput].BoundName);

		if (entity is null)
		{
			return false;
		}

		TagContainer? tags = AbilityNodeUtilities.BuildTagContainer(
			graphContext,
			InputProperties[TagInput].BoundName,
			entity);

		if (tags is null)
		{
			return false;
		}

		IForgeEntity? target = AbilityNodeUtilities.ResolveOptionalEntity(
			graphContext,
			InputProperties[TargetInput].BoundName);

		AbilityActivator? activator = AbilityNodeUtilities.ResolveActivator(
			graphContext,
			InputProperties[ActivationDataInput].BoundName);

		return activator is not null
			? activator.ActivateByTag(entity.Abilities, tags, target, graphContext)
			: entity.Abilities.TryActivateAbilitiesByTag(tags, target, out _);
	}
}
