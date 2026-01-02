// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Data for triggering abilities based on tags or events. Provides factory methods to create trigger configurations.
/// </summary>
public readonly record struct AbilityTriggerData
{
	internal AbitityTriggerSource TriggerSource { get; }

	internal Tag TriggerTag { get; }

	internal int Priority { get; }

	internal Type? PayloadType { get; }

	private AbilityTriggerData(
		AbitityTriggerSource triggerSource,
		Tag triggerTag,
		int priority,
		Type? payloadType = null)
	{
		TriggerSource = triggerSource;
		TriggerTag = triggerTag;
		Priority = priority;
		PayloadType = payloadType;
	}

	/// <summary>
	/// Creates trigger data for event-based activation with strongly-typed payloads.
	/// </summary>
	/// <remarks>
	/// Use this when your ability behavior implements <see cref="IAbilityBehavior{TPayload}"/>
	/// to receive the event payload directly in <see cref="IAbilityBehavior{TPayload}.OnStarted"/>.
	/// </remarks>
	/// <typeparam name="TPayload">The payload type from the triggering event.</typeparam>
	/// <param name="triggerTag">The tag that triggers the ability.</param>
	/// <param name="priority">The priority of the event subscription.</param>
	/// <returns>Configured trigger data for typed event handling.</returns>
	public static AbilityTriggerData ForEvent<TPayload>(Tag triggerTag, int priority = 0)
	{
		return new AbilityTriggerData(AbitityTriggerSource.Event, triggerTag, priority, typeof(TPayload));
	}

	/// <summary>
	/// Creates trigger data for event-based activation without typed payloads.
	/// </summary>
	/// <param name="triggerTag">The tag that triggers the ability.</param>
	/// <param name="priority">The priority of the event subscription.</param>
	/// <returns>Configured trigger data for non-typed event handling.</returns>
	public static AbilityTriggerData ForEvent(Tag triggerTag, int priority = 0)
	{
		return new AbilityTriggerData(AbitityTriggerSource.Event, triggerTag, priority);
	}

	/// <summary>
	/// Creates trigger data that activates when the specified tag is added to the entity.
	/// </summary>
	/// <param name="triggerTag">The tag that triggers the ability when added.</param>
	/// <returns>Configured trigger data for tag-added triggers.</returns>
	public static AbilityTriggerData ForTagAdded(Tag triggerTag)
	{
		return new AbilityTriggerData(AbitityTriggerSource.TagAdded, triggerTag, 0);
	}

	/// <summary>
	/// Creates trigger data that activates while the specified tag is present on the entity.
	/// </summary>
	/// <param name="triggerTag">The tag that triggers the ability while present.</param>
	/// <returns>Configured trigger data for tag-present triggers.</returns>
	public static AbilityTriggerData ForTagPresent(Tag triggerTag)
	{
		return new AbilityTriggerData(AbitityTriggerSource.TagPresent, triggerTag, 0);
	}
}
