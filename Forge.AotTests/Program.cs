// Copyright © Gamesmiths Guild.

using System.Numerics;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Networking;
using Gamesmiths.Forge.Networking.Replication;
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
		var tagsManager = new TagsManager(["event.hit", "status.burning"]);
		var eventTag = Tag.RequestTag(tagsManager, "event.hit");

		Check("Typed event trigger with a class payload", () =>
			RaiseTypedEvent(tagsManager, eventTag, new ClassPayload(1)) == new ClassPayload(1));
		Check("Typed event trigger with a struct payload", () =>
			RaiseTypedEvent(tagsManager, eventTag, new StructPayload(2)) == new StructPayload(2));
		Check("Activation-data resolver reads a struct payload member", () =>
			ResolveActivationData(tagsManager, eventTag, new StructPayload(3)) == 3);
		Check("Provider-built graph behavior and trigger read a struct payload member", () =>
			ResolveThroughProvider(tagsManager, eventTag, new ProviderPayload(4)) == 4);
		Check("Deterministic random repeats a tick's pinned draws", RepeatsTickDraws);
		Check("Entity simulation ticks an entity through its default random member", () =>
			TicksThroughDefaultRandom(tagsManager));
		Check("Input binding activates a typed ability with data built from the tick's input", () =>
			ActivateFromInput(tagsManager) == new StructPayload(5));
		Check("Graph snapshot copies a node context and restores it", RestoresGraphSnapshot);
		Check("Entity history rolls an entity back and replays its ticks", () =>
			ReplaysEntityHistory(tagsManager));
		Check("World history rolls a world back past a death and a spawn", () =>
			RollsWorldBack(tagsManager));
		Check("World replay reconciles cues and finds a tick that ran differently", () =>
			ReplaysWorld(tagsManager));
		Check("Network session handshakes and carries input over a loopback", () =>
			NetworksOverLoopback(tagsManager));
		Check("Replication spawns a mirror and carries its tags, effects and graphs", () =>
			ReplicatesOverLoopback(tagsManager));

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

	private static bool RepeatsTickDraws()
	{
		var random = new DeterministicRandom(12345UL);
		random.BeginTick(7);
		long first = random.NextInt64();
		random.BeginTick(7);

		return first == 5229388492309676965 && random.NextInt64() == first;
	}

	private static bool TicksThroughDefaultRandom(TagsManager tagsManager)
	{
		var entity = new SmokeEntity(tagsManager);
		EntitySimulation.Tick(entity, 1, 1.0 / 64.0);

		return ((IForgeEntity)entity).Random is null;
	}

	private static StructPayload ActivateFromInput(TagsManager tagsManager)
	{
		var input = new EntityInput(new InputActionSet(["fire"]));
		var entity = new SmokeEntity(tagsManager) { Input = input };
		StructPayload received = default;

		var abilityData = new AbilityData(
			"Bound",
			behaviorFactory: () => new PayloadBehavior<StructPayload>((_, data) => received = data),
			inputBinding: AbilityInputBinding.ForAction(
				"fire",
				static (_, entityInput) => new StructPayload(entityInput.IsHeld("fire") ? 5 : 0)));

		entity.Abilities.GrantAbilityPermanently(abilityData, 1, LevelComparison.None, null);
		input.Submit(new InputFrame { Held = 1, Pressed = 1 });
		EntitySimulation.Tick(entity, 1, 1.0 / 64.0);

		return received;
	}

	private static bool RestoresGraphSnapshot()
	{
		var node = new CountingNode();
		var graph = new Graph();
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[CountingNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		processor.UpdateGraph(0.1);
		GraphSnapshot snapshot = processor.CaptureSnapshot();
		processor.UpdateGraph(0.1);
		processor.RestoreSnapshot(snapshot);
		processor.UpdateGraph(0.1);
		processor.RestoreSnapshot(snapshot);

		return processor.GraphContext.GetNodeContext<CountingNodeContext>(node.NodeID).Updates.Count == 1;
	}

	private static bool ReplaysEntityHistory(TagsManager tagsManager)
	{
		var node = new CountingNode();
		var graph = new Graph();
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[CountingNode.InputPort]));

		var entity = new SmokeEntity(tagsManager);
		GraphAbilityBehavior? behavior = null;
		AbilityHandle ability = entity.Abilities.GrantAbilityPermanently(
			new AbilityData("Counting", behaviorFactory: () => behavior = new GraphAbilityBehavior(graph)),
			1,
			LevelComparison.None,
			null);

		var ward = new EffectData(
			"Ward",
			new DurationData(DurationType.Infinite),
			effectComponents:
			[
				new TargetTagRequirementsEffectComponent(ongoingTagRequirements: new TagRequirements(
					IgnoreTags: Tag.RequestTag(tagsManager, "status.burning").GetSingleTagContainer())),
			]);

		ActiveEffectHandle? wardHandle =
			entity.EffectsManager.ApplyEffect(new Effect(ward, new EffectOwnership(entity, entity)));
		ability.TryActivate(out _);

		var history = new EntityHistory(entity, 8, 1.0 / 64.0);

		for (long tick = 0; tick < 4; tick++)
		{
			EntitySimulation.Tick(entity, tick, history.FixedDeltaTime);
			history.Record(tick);
		}

		history.Rollback(1);
		history.Replay(3);

		return wardHandle?.IsValid == true
			&& behavior!.Processor.GraphContext.GetNodeContext<CountingNodeContext>(node.NodeID).Updates.Count == 4;
	}

	private static bool RollsWorldBack(TagsManager tagsManager)
	{
		var stays = new SmokeEntity(tagsManager);
		var dies = new SmokeEntity(tagsManager);
		var spawned = new SmokeEntity(tagsManager);
		var world = new WorldHistory(8, 1.0 / 64.0);
		bool returned = false;
		world.LeftLimbo += x => returned |= x == dies;
		world.Add(stays);
		world.Add(dies);

		for (long tick = 1; tick <= 4; tick++)
		{
			if (tick == 3)
			{
				world.Remove(dies);
				world.Add(spawned);
			}

			EntitySimulation.Tick(world.Entities, tick, world.FixedDeltaTime);
			world.Record(tick);
		}

		world.Rollback(2);

		return returned && world.Entities.Count == 2 && world.Entities[1] == dies;
	}

	// A rollback past an effect's application ends its cue, a replay finds the tick an effect was applied from outside
	// the simulation, and its description names the effect.
	private static bool ReplaysWorld(TagsManager tagsManager)
	{
		var entity = new SmokeEntity(tagsManager);
		var cueTag = Tag.RequestTag(tagsManager, "status.burning");
		var cue = new CountingCue();
		entity.CuesManager.RegisterCue(cueTag, cue);

		var shield = new EffectData(
			"Shield",
			new DurationData(DurationType.Infinite),
			cues: [new CueData(cueTag.GetSingleTagContainer(), 0, 1, CueMagnitudeType.EffectLevel)]);
		var world = new WorldHistory(8, 1.0 / 64.0);
		world.Add(entity);

		for (long tick = 1; tick <= 4; tick++)
		{
			if (tick == 3)
			{
				entity.EffectsManager.ApplyEffect(new Effect(shield, new EffectOwnership(entity, entity)));
			}

			EntitySimulation.Tick(world.Entities, tick, world.FixedDeltaTime);
			world.Record(tick);
		}

		ulong checksum = world.ChecksumAt(4);
		world.Rollback(2);
		bool removed = cue.Removes == 1;
		IReadOnlyList<WorldDesync> desyncs = world.Replay(4);

		return removed
			&& cue.Applies == 2
			&& desyncs.Count == 1
			&& desyncs[0].Tick == 3
			&& desyncs[0].Describe().Contains("Shield", StringComparison.Ordinal)
			&& world.ChecksumAt(4) == checksum
			&& world.ChecksumAt(4) != world.ChecksumAt(2);
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

	// The networking types with generics and collections: the registry's typed lookup, the session's handshake, the
	// input codec through the conditioner, and clock sync.
	private static bool NetworksOverLoopback(TagsManager tagsManager)
	{
		var actions = new InputActionSet(["jump"], [new InputVector("l", "r", "u", "d")]);
		var registry = new NetworkAssetRegistry();
		registry.Register(AssetKind.Effect, "uid://burning", "Burning");
		registry.Seal();

		var network = new LoopbackNetwork();

		var server = new NetworkSession(
			network.Server,
			new SimulationClock(60, 100),
			registry,
			tagsManager,
			actions,
			7);

		var link = new ConditionedTransport(
			network.Connect(),
			NetworkConditions.Good,
			new DeterministicRandom(1));

		var client = new NetworkSession(
			link,
			new SimulationClock(60),
			registry,
			tagsManager,
			actions);

		InputDelivery? delivery = null;
		InputCommand taken = default;

		for (int step = 0; step < 120; step++)
		{
			double time = step / 60.0;
			link.Update(time);
			client.Update(time);
			server.Update(time);

			if (client.State == SessionState.Ready)
			{
				InputFrame frame = new InputFrame { Held = 1 }.WithAim(Vector3.One, Vector3.UnitZ);
				client.SendInput(new InputCommand(client.Clock.Tick + 12, frame));
				client.Clock.Advance();
			}

			if (step > 60 && server.Peers.Count == 1 && server.Peers[0].Input.NewestTick >= server.Clock.Tick)
			{
				delivery = server.Peers[0].Input.Take(server.Clock.Tick, out taken);
			}

			server.Clock.Advance();
		}

		return delivery == InputDelivery.Received
			&& taken.Frame.Held == 1
			&& taken.Frame.HasAim
			&& client.ClockSync.HasEstimate
			&& client.SessionSeed == 7
			&& registry.Get<string>(AssetKind.Effect, 0) == "Burning";
	}

	// Replication's keyed collections: entities by network id on both ends, and tags by net index. A graph's run, with
	// the variable its presentation state reads.
	private static bool ReplicatesOverLoopback(TagsManager tagsManager)
	{
		var actions = new InputActionSet(["jump"]);
		var effectData = new EffectData("Burning", new DurationData(DurationType.Infinite));
		var shown = new ShownNode();
		Graph graph = ShowLevel(shown);
		var ability = new AbilityData("Show", behaviorFactory: () => new GraphAbilityBehavior(graph));
		var registry = new NetworkAssetRegistry();
		registry.Register(AssetKind.Scene, "uid://creature", "Creature");
		registry.Register(AssetKind.Effect, "uid://burning", effectData);
		registry.Register(AssetKind.Ability, "uid://show", ability);
		registry.Register(
			AssetKind.EffectContext,
			"payload",
			EffectContextCodec.For(
				static (writer, payload) => writer.WriteVarInt(payload.Value),
				static reader => new StructPayload((int)reader.ReadVarInt())));
		registry.Seal();

		var network = new LoopbackNetwork();
		var server = new NetworkSession(network.Server, new SimulationClock(60), registry, tagsManager, actions);
		var client = new NetworkSession(network.Connect(), new SimulationClock(60), registry, tagsManager, actions);
		var host = new SmokeHost(tagsManager);
		client.Mirror!.Host = host;

		var tag = Tag.RequestTag(tagsManager, "status.burning");
		var entity = new SmokeEntity(tagsManager, tag) { NetworkId = server.Replication!.AllocateId() };
		server.Replication.Register(entity, 0, PeerId.None);
		entity.EffectsManager.ApplyEffect(new Effect(effectData, default), new StructPayload(7));
		entity.Abilities.GrantAbilityPermanently(ability, 1, LevelComparison.Higher, null).TryActivate(out _);

		for (int step = 0; step < 10; step++)
		{
			double time = step / 60.0;
			client.Update(time);
			server.Update(time);
			server.Replication.Send(server.Clock.Tick);
			server.Clock.Advance();
		}

		return host.Spawned is SmokeEntity mirror
			&& mirror.NetworkId == entity.NetworkId
			&& client.Mirror.Find(entity.NetworkId) == mirror
			&& mirror.Tags.AllTags.HasTag(tag)
			&& mirror.EffectsManager.HasAnyActiveEffect(new EffectQuery(EffectDefinition: effectData))
			&& mirror.EffectsManager.GetActiveEffects().First().TryGetContextData(out StructPayload context)
			&& context.Value == 7
			&& shown.Seen.Contains(new Showing(NodeActivationReason.Link, 3))
			&& shown.Seen.Contains(new Showing(NodeActivationReason.Replication, 3));
	}

	// Sets the level variable to 3, then shows it.
	private static Graph ShowLevel(ShownNode shown)
	{
		var graph = new Graph();
		var level = new SetVariableNode();
		graph.AddNode(level);
		graph.AddNode(shown);
		graph.VariableDefinitions.DefineVariable("level", 0);
		graph.VariableDefinitions.DefineProperty("three", new VariantResolver(new Variant128(3), typeof(int)));
		level.BindInput(SetVariableNode.SourceInput, "three");
		level.BindOutput(SetVariableNode.TargetOutput, "level");
		shown.BindInput(0, "level");
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			level.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			level.OutputPorts[ActionNode.OutputPort],
			shown.InputPorts[StateNode<StateNodeContext>.InputPort]));
		return graph;
	}

	private sealed record ClassPayload(int Value);

	private readonly record struct StructPayload(int Value);

	private readonly record struct Showing(NodeActivationReason Reason, int Level);

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

	private sealed class CountingNodeContext : StateNodeContext
	{
		public List<double> Updates { get; private set; } = [];

		protected override void OnCopied()
		{
			base.OnCopied();
			Updates = [.. Updates];
		}
	}

	private sealed class CountingNode : StateNode<CountingNodeContext>
	{
		protected override void OnActivate(GraphContext graphContext)
		{
		}

		protected override void OnDeactivate(GraphContext graphContext)
		{
		}

		protected override void OnUpdate(double deltaTime, GraphContext graphContext)
		{
			graphContext.GetNodeContext<CountingNodeContext>(NodeID).Updates.Add(deltaTime);
		}

		protected override void OnFixedUpdate(double deltaTime, GraphContext graphContext)
		{
			graphContext.GetNodeContext<CountingNodeContext>(NodeID).Updates.Add(deltaTime);
		}
	}

	// A presentation state that notes why it started and the level it read, on every machine that runs it.
	private sealed class ShownNode : StateNode<StateNodeContext>
	{
		public List<Showing> Seen { get; } = [];

		public override NodeNetworkKind NetworkKind => NodeNetworkKind.Presentation;

		protected override void DefineParameters(
			List<InputProperty> inputProperties,
			List<OutputVariable> outputVariables)
		{
			inputProperties.Add(new InputProperty("Level", typeof(int)));
		}

		protected override void OnActivate(GraphContext graphContext)
		{
			graphContext.TryResolve(InputProperties[0].BoundName, out int level);
			Seen.Add(new Showing(graphContext.GetNodeContext<StateNodeContext>(NodeID).ActivationReason, level));
		}

		protected override void OnDeactivate(GraphContext graphContext)
		{
		}
	}

	private sealed class CountingCue : ICueHandler
	{
		public int Applies { get; private set; }

		public int Removes { get; private set; }

		public void OnExecute(IForgeEntity? target, CueParameters? parameters)
		{
		}

		public void OnApply(IForgeEntity? target, CueParameters? parameters)
		{
			Applies++;
		}

		public void OnRemove(IForgeEntity? target, bool interrupted)
		{
			Removes++;
		}

		public void OnUpdate(IForgeEntity? target, CueParameters? parameters)
		{
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

		public EntityInput? Input { get; init; }

		public NetworkEntityId NetworkId { get; init; }

		public SmokeEntity(TagsManager tagsManager, params Tag[] tags)
		{
			Attributes = new EntityAttributes(this, []);
			Tags = new EntityTags(new TagContainer(tagsManager, [.. tags]));
			CuesManager = new CuesManager();
			EffectsManager = new EffectsManager(this, CuesManager);
			Abilities = new EntityAbilities(this);
		}
	}

	private sealed class SmokeHost(TagsManager tagsManager) : IReplicationHost
	{
		public IForgeEntity? Spawned { get; private set; }

		public IForgeEntity? Spawn(NetworkEntityId id, int sceneIndex, PeerId owner)
		{
			Spawned = new SmokeEntity(tagsManager) { NetworkId = id };
			return Spawned;
		}

		public void Despawn(IForgeEntity entity)
		{
			Spawned = null;
		}
	}
}
