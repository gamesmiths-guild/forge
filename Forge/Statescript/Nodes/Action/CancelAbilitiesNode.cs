// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Nodes.Action;

/// <summary>
/// Cancels active abilities on an entity, selected by the ability tags they carry.
/// </summary>
/// <remarks>
/// <para>An ability is canceled when it carries any of the <c>With Tags</c> and none of the <c>Without Tags</c>. Each
/// tag input accepts a single <see cref="Tag"/> or an array of tags, and an unbound input drops that side of the
/// filter, so binding only <c>Without Tags</c> cancels everything except the abilities carrying them.</para>
/// <para>Leaving both tag inputs unbound cancels nothing. Wiping every active ability is available through
/// <see cref="EntityAbilities.CancelAbilities"/>, but it should be asked for explicitly rather than being what an
/// unconfigured node happens to do.</para>
/// <para>The target input selects the entity whose abilities are canceled, defaulting to the ability context's owner
/// when unbound.</para>
/// </remarks>
public class CancelAbilitiesNode : ActionNode
{
	/// <summary>
	/// Input property index for the tag(s) an ability must carry to be canceled.
	/// </summary>
	public const byte WithTagsInput = 0;

	/// <summary>
	/// Input property index for the tag(s) that spare an ability from being canceled.
	/// </summary>
	public const byte WithoutTagsInput = 1;

	/// <summary>
	/// Input property index for the entity whose abilities are canceled.
	/// </summary>
	public const byte TargetInput = 2;

	/// <inheritdoc/>
	public override string Description => "Cancels active abilities matching the given tags.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("With Tags", typeof(Tag)));
		inputProperties.Add(new InputProperty("Without Tags", typeof(Tag)));
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

		TagContainer? withTags = AbilityNodeUtilities.BuildTagContainer(
			graphContext,
			InputProperties[WithTagsInput].BoundName,
			target);

		TagContainer? withoutTags = AbilityNodeUtilities.BuildTagContainer(
			graphContext,
			InputProperties[WithoutTagsInput].BoundName,
			target);

		if (withTags is null && withoutTags is null)
		{
			return;
		}

		target.Abilities.CancelAbilities(withTags, withoutTags);
	}
}
