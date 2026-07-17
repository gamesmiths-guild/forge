// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript.Ports;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// A state node that listens for tag changes on an entity while active, emitting events when watched tags are added
/// to or removed from the entity's tag view.
/// </summary>
/// <remarks>
/// <para>The entity input selects whose tags are observed, defaulting to the ability context's owner when unbound.
/// The tag input accepts a single <see cref="Tag"/> or an array of tags to watch. Presence checks use the entity's
/// full tag view (base and modifier tags) with hierarchical matching.</para>
/// <para>On each transition the node writes the changed tag to the Tag output variable and emits
/// <see cref="OnTagAddedPort"/> or <see cref="OnTagRemovedPort"/>. The handler emits synchronously from the tag
/// change.</para>
/// <para>The node stays active until deactivated externally, unsubscribing on deactivation.</para>
/// </remarks>
public class TagListenerNode : StateNode<TagListenerNodeContext>
{
	/// <summary>
	/// Input property index for the entity whose tags are observed.
	/// </summary>
	public const byte EntityInput = 0;

	/// <summary>
	/// Input property index for the tag(s) to watch.
	/// </summary>
	public const byte TagInput = 1;

	/// <summary>
	/// Output variable index for the tag that changed.
	/// </summary>
	public const byte TagOutput = 0;

	/// <summary>
	/// Output port index for the event emitted when a watched tag is added.
	/// </summary>
	public const byte OnTagAddedPort = 4;

	/// <summary>
	/// Output port index for the event emitted when a watched tag is removed.
	/// </summary>
	public const byte OnTagRemovedPort = 5;

	/// <inheritdoc/>
	public override string Description =>
		"Listens for watched tags being added to or removed from an entity while active.";

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		base.DefinePorts(inputPorts, outputPorts);
		outputPorts.Add(CreatePort<EventPort>(OnTagAddedPort, "OnTagAdded"));
		outputPorts.Add(CreatePort<EventPort>(OnTagRemovedPort, "OnTagRemoved"));
	}

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Entity", typeof(IForgeEntity)));
		inputProperties.Add(new InputProperty("Tags", typeof(Tag)));
		outputVariables.Add(new OutputVariable("Tag", typeof(Tag)));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		TagListenerNodeContext nodeContext = graphContext.GetNodeContext<TagListenerNodeContext>(NodeID);
		nodeContext.SubscribedEntity = null;
		nodeContext.Handler = null;
		nodeContext.LastPresence.Clear();

		IForgeEntity? entity = AbilityNodeUtilities.ResolveEntityOrOwner(
			graphContext,
			InputProperties[EntityInput].BoundName);

		if (entity is null
			|| !EffectApplicationUtilities.TryResolveTags(
				graphContext,
				InputProperties[TagInput].BoundName,
				out IReadOnlyList<Tag> tags))
		{
			return;
		}

		for (int i = 0; i < tags.Count; i++)
		{
			nodeContext.LastPresence[tags[i]] = entity.Tags.AllTags.HasTag(tags[i]);
		}

		void Handler(TagContainer allTags)
		{
			OnTagsChanged(graphContext, allTags);
		}

		nodeContext.SubscribedEntity = entity;
		nodeContext.Handler = Handler;

		entity.Tags.OnTagsChanged += Handler;
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		TagListenerNodeContext nodeContext = graphContext.GetNodeContext<TagListenerNodeContext>(NodeID);

		if (nodeContext.SubscribedEntity is not null && nodeContext.Handler is not null)
		{
			nodeContext.SubscribedEntity.Tags.OnTagsChanged -= nodeContext.Handler;
		}

		nodeContext.SubscribedEntity = null;
		nodeContext.Handler = null;
		nodeContext.LastPresence.Clear();
	}

	private void OnTagsChanged(GraphContext graphContext, TagContainer allTags)
	{
		if (!graphContext.HasNodeContext(NodeID))
		{
			return;
		}

		TagListenerNodeContext nodeContext = graphContext.GetNodeContext<TagListenerNodeContext>(NodeID);

		if (!nodeContext.Active)
		{
			return;
		}

		// Snapshot the watched tags to allow presence updates while iterating.
		foreach (Tag watchedTag in nodeContext.LastPresence.Keys.ToArray())
		{
			bool isPresent = allTags.HasTag(watchedTag);
			bool wasPresent = nodeContext.LastPresence[watchedTag];

			if (isPresent == wasPresent)
			{
				continue;
			}

			nodeContext.LastPresence[watchedTag] = isPresent;

			WriteTagOutput(graphContext, watchedTag);
			OutputPorts[isPresent ? OnTagAddedPort : OnTagRemovedPort].EmitMessage(graphContext);
		}
	}

	private void WriteTagOutput(GraphContext graphContext, Tag tag)
	{
		OutputVariable output = OutputVariables[TagOutput];

		if (output.BoundName == StringKey.Empty)
		{
			return;
		}

		Variables? variables = output.Scope == VariableScope.Shared
			? graphContext.SharedVariables
			: graphContext.GraphVariables;

		variables?.SetObject(output.BoundName, tag);
	}
}
