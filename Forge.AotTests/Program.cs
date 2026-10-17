// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.Action;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Statescript.Providers;
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
		Check("Activation-data resolver reads a struct payload member", () =>
			ResolveActivationData(tagsManager, eventTag, new StructPayload(3)) == 3);
		Check("Provider-built graph behavior and trigger read a struct payload member", () =>
			ResolveThroughProvider(tagsManager, eventTag, new ProviderPayload(4)) == 4);

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
		return RaiseTypedEvent(tagsManager, eventTag, payload, static (_, _) => { });
	}

	private static TPayload? RaiseTypedEvent<TPayload>(
		TagsManager tagsManager,
		Tag eventTag,
		TPayload payload,
		Action<AbilityBehaviorContext, TPayload> onStarted)
	{
		var entity = new SmokeEntity(tagsManager);
		TPayload? received = default;

		var abilityData = new AbilityData(
			"Typed trigger",
			abilityTriggerData: AbilityTriggerData.ForEvent<TPayload>(eventTag),
			behaviorFactory: () => new PayloadBehavior<TPayload>((context, data) =>
			{
				received = data;
				onStarted(context, data);
			}));

		entity.Abilities.GrantAbilityPermanently(abilityData, 1, LevelComparison.None, null);
		entity.Events.Raise(new EventData<TPayload>
		{
			EventTags = eventTag.GetSingleTagContainer()!,
			Payload = payload,
		});

		return received;
	}

	private static int ResolveActivationData(TagsManager tagsManager, Tag eventTag, StructPayload payload)
	{
		var resolver = new AbilityActivationDataResolver(typeof(StructPayload), nameof(StructPayload.Value));
		int resolved = 0;

		RaiseTypedEvent(
			tagsManager,
			eventTag,
			payload,
			(context, _) => resolved = resolver.Resolve(new GraphContext { ActivationContext = context }).AsInt());

		return resolved;
	}

	// Builds everything through IAbilityActivationDataProvider, the way a host such as forge-godot does. Nothing else
	// hands ProviderPayload to the resolver as a Type, so its members survive trimming only through the provider's
	// annotation.
	private static int ResolveThroughProvider(TagsManager tagsManager, Tag eventTag, ProviderPayload payload)
	{
		const string valueProperty = "activationValue";
		const string resolvedVariable = "resolved";

		IAbilityActivationDataProvider provider = new ProviderPayloadProvider();

		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable(resolvedVariable, 0);
		graph.VariableDefinitions.DefineProperty(
			valueProperty,
			new AbilityActivationDataResolver(provider.DataType, nameof(ProviderPayload.Value)));

		var setVariable = new SetVariableNode();
		setVariable.BindInput(SetVariableNode.SourceInput, valueProperty);
		setVariable.BindOutput(SetVariableNode.TargetOutput, resolvedVariable);
		graph.AddNode(setVariable);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			setVariable.InputPorts[ActionNode.InputPort]));

		GraphAbilityBehavior behavior = provider.CreateGraphBehavior(graph);
		var abilityData = new AbilityData(
			"Provider trigger",
			abilityTriggerData: provider.CreateEventTrigger(eventTag, 0),
			behaviorFactory: () => behavior);

		var entity = new SmokeEntity(tagsManager);
		entity.Abilities.GrantAbilityPermanently(abilityData, 1, LevelComparison.None, null);
		entity.Events.Raise(new EventData<ProviderPayload>
		{
			EventTags = eventTag.GetSingleTagContainer()!,
			Payload = payload,
		});

		return behavior.Processor.GraphContext.GraphVariables.TryGetVar(resolvedVariable, out int resolved)
			? resolved
			: -1;
	}

	private sealed record ClassPayload(int Value);

	private readonly record struct StructPayload(int Value);

	private readonly record struct ProviderPayload(int Value);

	private sealed class ProviderPayloadProvider : AbilityActivationDataProvider<ProviderPayload>
	{
		public override ProviderPayload CreateData(GraphContext graphContext, AbilityActivationDataInputs inputs)
		{
			return default;
		}
	}

	private sealed class PayloadBehavior<TPayload>(Action<AbilityBehaviorContext, TPayload> onStarted)
		: IAbilityBehavior<TPayload>
	{
		public void OnStarted(AbilityBehaviorContext context, TPayload data)
		{
			onStarted(context, data);
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
