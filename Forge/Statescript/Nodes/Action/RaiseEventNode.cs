// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Nodes.Action;

/// <summary>
/// Raises an event (<see cref="EventManager.Raise(in EventData)"/>) on one or more target entities' event buses, then
/// continues execution.
/// </summary>
/// <remarks>
/// <para>The event-tag input accepts either a single <see cref="Tag"/> or an array of tags; all selected tags are
/// combined into the event's <see cref="EventData.EventTags"/> container.</para>
/// <para>The target input accepts either a single <see cref="IForgeEntity"/> or an array of entities; the same event is
/// raised on each target's <see cref="IForgeEntity.Events"/> bus.</para>
/// <para>Source, magnitude, and payload are optional. When a payload provider is bound, the node builds the provider's
/// typed payload and raises a typed (non-boxing) event
/// (<see cref="EventManager.Raise{TPayload}(in EventData{TPayload})"/>) so that typed
/// (<c>Subscribe&lt;TPayload&gt;</c>) listeners receive it; otherwise it raises a non-generic event with no payload.
/// </para>
/// </remarks>
public class RaiseEventNode : ActionNode
{
	/// <summary>
	/// Input property index for the event tag(s).
	/// </summary>
	public const byte EventTagInput = 0;

	/// <summary>
	/// Input property index for the event target(s).
	/// </summary>
	public const byte TargetInput = 1;

	/// <summary>
	/// Input property index for the optional event source entity.
	/// </summary>
	public const byte SourceInput = 2;

	/// <summary>
	/// Input property index for the optional event magnitude.
	/// </summary>
	public const byte MagnitudeInput = 3;

	/// <summary>
	/// Input property index for the optional event payload.
	/// </summary>
	public const byte PayloadInput = 4;

	/// <inheritdoc/>
	public override string Description => "Raises an event on target entities.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Event Tags", typeof(Tag)));
		inputProperties.Add(new InputProperty("Target", typeof(IForgeEntity)));
		inputProperties.Add(new InputProperty("Source", typeof(IForgeEntity)));
		inputProperties.Add(new InputProperty("Magnitude", typeof(float)));
		inputProperties.Add(new InputProperty("Payload", typeof(EventPayloadRaiser)));
	}

	/// <inheritdoc/>
	protected override void Execute(GraphContext graphContext)
	{
		TagContainer? eventTags = ResolveEventTags(graphContext);
		if (eventTags is null || !ResolveTargets(graphContext, out IReadOnlyList<IForgeEntity> targets))
		{
			return;
		}

		IForgeEntity? source = ResolveSource(graphContext);
		float magnitude = ResolveMagnitude(graphContext);
		EventPayloadRaiser? payloadRaiser = ResolvePayloadRaiser(graphContext);

		for (int i = 0; i < targets.Count; i++)
		{
			IForgeEntity target = targets[i];

			if (payloadRaiser is not null)
			{
				// Build and raise the provider's typed payload with no boxing so typed listeners receive it.
				payloadRaiser.Raise(target.Events, eventTags, source, target, magnitude, graphContext);
			}
			else
			{
				target.Events.Raise(new EventData
				{
					EventTags = eventTags,
					Source = source,
					Target = target,
					EventMagnitude = magnitude,
				});
			}
		}
	}

	private TagContainer? ResolveEventTags(GraphContext graphContext)
	{
		StringKey inputName = InputProperties[EventTagInput].BoundName;

		if (inputName == StringKey.Empty)
		{
			return null;
		}

		var tags = new List<Tag>();

		if (graphContext.TryResolveObjectArray(inputName, typeof(Tag), out object?[]? resolvedArray))
		{
			for (int i = 0; i < resolvedArray.Length; i++)
			{
				if (resolvedArray[i] is Tag tag)
				{
					tags.Add(tag);
				}
			}
		}
		else if (graphContext.TryResolveObject(inputName, typeof(Tag), out object? resolved)
			&& resolved is Tag singleTag)
		{
			tags.Add(singleTag);
		}

		if (tags.Count == 0)
		{
			return null;
		}

		return tags.Count == 1
			? new TagContainer(tags[0])
			: new TagContainer(tags[0].TagsManager!, [.. tags]);
	}

	private bool ResolveTargets(GraphContext graphContext, out IReadOnlyList<IForgeEntity> targets)
	{
		StringKey inputName = InputProperties[TargetInput].BoundName;

		if (graphContext.TryResolveObjectArray(inputName, typeof(IForgeEntity), out object?[]? resolvedArray))
		{
			var resolvedTargets = new List<IForgeEntity>(resolvedArray.Length);

			for (int i = 0; i < resolvedArray.Length; i++)
			{
				if (resolvedArray[i] is IForgeEntity entity)
				{
					resolvedTargets.Add(entity);
				}
			}

			targets = resolvedTargets;
			return resolvedTargets.Count > 0;
		}

		if (graphContext.TryResolveObject(inputName, typeof(IForgeEntity), out object? resolved)
			&& resolved is IForgeEntity singleTarget)
		{
			targets = [singleTarget];
			return true;
		}

		targets = [];
		return false;
	}

	private IForgeEntity? ResolveSource(GraphContext graphContext)
	{
		StringKey inputName = InputProperties[SourceInput].BoundName;

		return inputName != StringKey.Empty
			&& graphContext.TryResolveObject(inputName, typeof(IForgeEntity), out object? resolved)
			? resolved as IForgeEntity
			: null;
	}

	private float ResolveMagnitude(GraphContext graphContext)
	{
		StringKey inputName = InputProperties[MagnitudeInput].BoundName;
		float magnitude = 0f;

		if (inputName != StringKey.Empty)
		{
			graphContext.TryResolve(inputName, out magnitude);
		}

		return magnitude;
	}

	private EventPayloadRaiser? ResolvePayloadRaiser(GraphContext graphContext)
	{
		StringKey inputName = InputProperties[PayloadInput].BoundName;

		return inputName != StringKey.Empty
			&& graphContext.TryResolveObject(inputName, typeof(EventPayloadRaiser), out object? resolved)
			? resolved as EventPayloadRaiser
			: null;
	}
}
