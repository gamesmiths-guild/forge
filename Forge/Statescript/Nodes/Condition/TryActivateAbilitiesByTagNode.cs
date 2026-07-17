// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
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

	/// <inheritdoc/>
	public override string Description =>
		"Tries to activate abilities matching the given tags; True when any activated.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Tags", typeof(Tag)));
		inputProperties.Add(new InputProperty("Entity", typeof(IForgeEntity)));
		inputProperties.Add(new InputProperty("Target", typeof(IForgeEntity)));
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

		return entity.Abilities.TryActivateAbilitiesByTag(tags, target, out _);
	}
}
