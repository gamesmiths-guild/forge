// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.AotTests;

internal static class Program
{
	private static int _failures;

	private static int Main()
	{
		var tagsManager = new TagsManager(["event.hit"]);
		var eventTag = Tag.RequestTag(tagsManager, "event.hit");

		Check("Typed event trigger with a class payload", () =>
			RaiseTypedEvent(tagsManager, eventTag, new ClassPayload(1)) == new ClassPayload(1));
		Check("Typed event trigger with a struct payload", () =>
			RaiseTypedEvent(tagsManager, eventTag, new StructPayload(2)) == new StructPayload(2));

		return _failures == 0 ? 0 : 1;
	}

	private static void Check(string name, Func<bool> check)
	{
		try
		{
			bool passed = check();
			Console.WriteLine($"{(passed ? "PASS" : "FAIL")} {name}");
			_failures += passed ? 0 : 1;
		}
		catch (Exception exception)
		{
			Console.WriteLine($"FAIL {name}: {exception}");
			_failures++;
		}
	}

	private static TPayload? RaiseTypedEvent<TPayload>(TagsManager tagsManager, Tag eventTag, TPayload payload)
	{
		var entity = new SmokeEntity(tagsManager);
		TPayload? received = default;

		var abilityData = new AbilityData(
			"Typed trigger",
			abilityTriggerData: AbilityTriggerData.ForEvent<TPayload>(eventTag),
			behaviorFactory: () => new PayloadBehavior<TPayload>(x => received = x));

		entity.Abilities.GrantAbilityPermanently(abilityData, 1, LevelComparison.None, null);
		entity.Events.Raise(new EventData<TPayload>
		{
			EventTags = eventTag.GetSingleTagContainer()!,
			Payload = payload,
		});

		return received;
	}

	private sealed record ClassPayload(int Value);

	private readonly record struct StructPayload(int Value);

	private sealed class PayloadBehavior<TPayload>(Action<TPayload> onStarted) : IAbilityBehavior<TPayload>
	{
		public void OnStarted(AbilityBehaviorContext context, TPayload data)
		{
			onStarted(data);
			context.InstanceHandle.End();
		}

		public void OnEnded(AbilityBehaviorContext context)
		{
			// Nothing to clean up.
		}
	}

	private sealed class SmokeEntity : IForgeEntity
	{
		public EntityAttributes Attributes { get; }

		public EntityTags Tags { get; }

		public EffectsManager EffectsManager { get; }

		public CuesManager CuesManager { get; }

		public EntityAbilities Abilities { get; }

		public EventManager Events { get; } = new();

		public Variables SharedVariables { get; } = new();

		public SmokeEntity(TagsManager tagsManager)
		{
			Attributes = new EntityAttributes(this, []);
			Tags = new EntityTags(new TagContainer(tagsManager, []));
			CuesManager = new CuesManager();
			EffectsManager = new EffectsManager(this, CuesManager);
			Abilities = new EntityAbilities(this);
		}
	}
}
