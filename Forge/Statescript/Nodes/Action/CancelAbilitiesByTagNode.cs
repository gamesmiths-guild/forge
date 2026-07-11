// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Nodes.Action;

/// <summary>
/// Cancels every active ability on an entity whose ability tags match any of the given tags.
/// </summary>
/// <remarks>
/// <para>The tag input accepts a single <see cref="Tag"/> or an array of tags. The target input selects the entity
/// whose abilities are canceled, defaulting to the ability context's owner when unbound.</para>
/// </remarks>
public class CancelAbilitiesByTagNode : ActionNode
{
	/// <summary>
	/// Input property index for the tag(s) selecting which abilities to cancel.
	/// </summary>
	public const byte TagInput = 0;

	/// <summary>
	/// Input property index for the entity whose abilities are canceled.
	/// </summary>
	public const byte TargetInput = 1;

	/// <inheritdoc/>
	public override string Description => "Cancels active abilities matching the given tags.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Tags", typeof(Tag)));
		inputProperties.Add(new InputProperty("Target", typeof(IForgeEntity)));
	}

	/// <inheritdoc/>
	protected override void Execute(GraphContext graphContext)
	{
		IForgeEntity? target = AbilityNodeUtilities.ResolveEntityOrOwner(
			graphContext,
			InputProperties[TargetInput].BoundName);

		if (target is null)
		{
			return;
		}

		TagContainer? tags = AbilityNodeUtilities.BuildTagContainer(
			graphContext,
			InputProperties[TagInput].BoundName,
			target);

		if (tags is null)
		{
			return;
		}

		target.Abilities.CancelAbilitiesWithTag(tags);
	}
}
