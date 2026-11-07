// Copyright © Gamesmiths Guild.

using System.Globalization;
using FluentAssertions;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Networking;
using Gamesmiths.Forge.Networking.Replication;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.Action;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Statescript.Ports;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Tests.Networking;

/// <summary>
/// Graphs reach a client as what they show: a mirror runs a copy of each graph the server's entity runs, whose
/// presentation states start and end as the server says and whose presentation actions run as the server ran them,
/// without a link followed, while client-only regions run there by themselves and server-only ones never do.
/// </summary>
public class GraphReplicationTests
{
	private const int TickRate = 60;

	private const double TickLength = 1.0 / TickRate;

	private static readonly string[] _tagNames = ["state.casting"];

	private static readonly InputActionSet _actions = new(["fire"]);

	[Fact]
	[Trait("Networking", "Graphs")]
	public void A_node_acting_on_what_presentation_made_is_presentation_in_its_graph()
	{
		var graph = new Graph();
		var spawn = new FlashNode("spawn", []);
		var mover = new MarkNode("mover", []);
		var chaser = new MarkNode("chaser", []);
		var follower = new MarkNode("follower", []);
		var spawner = new AuthorityNode();
		var hidden = new FlashNode("hidden", []) { Region = NodeRegion.ServerOnly };
		var listener = new MarkNode("listener", []);
		var unrelated = new MarkNode("unrelated", []);

		foreach (Node node in new Node[] { spawn, mover, chaser, follower, spawner, hidden, listener, unrelated })
		{
			graph.AddNode(node);
		}

		graph.VariableDefinitions.DefineVariable("made", 0);
		graph.VariableDefinitions.DefineVariable("moved", 0);
		graph.VariableDefinitions.DefineVariable("secret", 0);
		graph.VariableDefinitions.DefineObjectProperty("chased", new ObjectVariableResolver<object>("moved"));
		graph.VariableDefinitions.DefineProperty("followed", new VariableResolver("made", typeof(int)));
		spawn.BindOutput(0, "made");
		mover.BindInput(0, "made");
		mover.BindOutput(0, "moved");
		chaser.BindInput(0, "chased");
		follower.BindInput(0, "followed");
		spawner.BindInput(0, "made");
		hidden.BindOutput(0, "secret");
		listener.BindInput(0, "secret");

		graph.NetworkKindOf(spawn)
			.Should().Be(NodeNetworkKind.Presentation);
		graph.NetworkKindOf(mover)
			.Should().Be(NodeNetworkKind.Presentation, "it acts on what spawn made");
		graph.NetworkKindOf(chaser)
			.Should().Be(NodeNetworkKind.Presentation, "it acts on what mover made in turn");
		graph.NetworkKindOf(follower)
			.Should().Be(NodeNetworkKind.Presentation, "a property reads what spawn made");
		graph.NetworkKindOf(spawner)
			.Should().Be(NodeNetworkKind.Authority);
		graph.NetworkKindOf(listener)
			.Should().Be(NodeNetworkKind.Simulation, "what a server-only node makes stays there");
		graph.NetworkKindOf(unrelated)
			.Should().Be(NodeNetworkKind.Simulation);
	}

	[Fact]
	[Trait("Networking", "Graphs")]
	public void A_mirror_runs_what_the_server_s_graph_shows_and_nothing_else()
	{
		var rig = new Rig();
		Client client = rig.Join();
		Entity entity = rig.Spawn();
		Activate(entity, rig.Assets.Cast);

		rig.Step(40);

		rig.Assets.Log.Should().Contain(["cast", "flash 7", "glow on Link 7"]);
		client.Assets.Log.Should().HaveCount(3);
		client.Assets.Log.Take(2).Should().Equal("flash 7", "glow on Replication 7");
		client.Assets.Log[2].Should().BeOneOf("glow shown 0.00", "glow shown 0.02", "glow shown 0.03");
	}

	[Fact]
	[Trait("Networking", "Graphs")]
	public void A_mirror_s_state_ends_when_the_server_s_does_and_its_graph_with_the_server_s_run()
	{
		var rig = new Rig();
		Client client = rig.Join(glowLength: 0.1);
		Entity entity = rig.Spawn();
		AbilityHandle cast = Activate(entity, rig.Assets.Cast);

		rig.Step(60);

		client.Assets.Log.Should().Contain("glow on Replication 7");
		client.Assets.Log.Should().NotContain("glow off", "a glow the server runs ends when the server's does");

		rig.StepUntil(() => rig.Assets.Log.Contains("glow off"));
		rig.Step(5);

		cast.IsActive.Should().BeFalse();
		client.Assets.Log.Should().EndWith(["glow off", "glow hidden"]);
	}

	[Fact]
	[Trait("Networking", "Graphs")]
	public void A_late_joiner_catches_a_running_state_up_by_how_long_it_already_ran()
	{
		var rig = new Rig();
		Entity entity = rig.Spawn();
		Activate(entity, rig.Assets.Linger);
		rig.Step(120);

		Client client = rig.Join();
		rig.Step(2);

		string shown = client.Assets.Log
			.Should().ContainSingle(x => x.StartsWith("linger shown", StringComparison.Ordinal)).Subject;
		double elapsed = double.Parse(shown["linger shown ".Length..], CultureInfo.InvariantCulture);
		elapsed.Should().BeInRange(1.9, 2.4);
	}

	[Fact]
	[Trait("Networking", "Graphs")]
	public void A_presentation_action_runs_once_on_a_client_and_never_after_it_expired()
	{
		var rig = new Rig();
		Client early = rig.Join();
		Entity entity = rig.Spawn();
		Activate(entity, rig.Assets.Cast);
		rig.Step(60);

		Client late = rig.Join();
		rig.Step(10);

		early.Assets.Log.Should().ContainSingle(x => x == "flash 7");
		late.Assets.Log.Should().NotContain("flash 7", "the flash was long over when it joined");
	}

	[Fact]
	[Trait("Networking", "Graphs")]
	public void A_run_too_short_to_be_seen_still_brings_its_actions()
	{
		var rig = new Rig();
		Client client = rig.Join();
		Entity entity = rig.Spawn();
		AbilityHandle blink = Activate(entity, rig.Assets.Blink);

		blink.IsActive.Should().BeFalse("its graph ran no state and ended at once");

		rig.Step(10);

		client.Assets.Log.Should().Equal("blink 0");
	}

	[Fact]
	[Trait("Networking", "Graphs")]
	public void A_client_only_region_runs_on_a_client_from_the_state_that_leads_into_it()
	{
		var rig = new Rig();
		Client client = rig.Join();
		Entity entity = rig.Spawn();
		Activate(entity, rig.Assets.Sparkle);

		rig.Step(30);

		client.Assets.Log.Should().Contain(["sparkle on Replication 0", "sparks"]);
		client.Assets.Ticks.Should().BePositive("the region simulates on the client");
		rig.Assets.Log.Should().Contain("sparks", "the server runs it as part of the graph");

		rig.Step(60);

		client.Assets.Log.Should().ContainInOrder("sparks off", "sparkle off");
	}

	[Fact]
	[Trait("Networking", "Graphs")]
	public void A_server_only_region_never_runs_on_a_client()
	{
		var rig = new Rig();
		Client client = rig.Join();
		Entity entity = rig.Spawn();
		Activate(entity, rig.Assets.Sparkle);

		rig.Step(30);

		rig.Assets.Log.Should().Contain("secret on Link 0");
		client.Assets.Log.Should().NotContain(x => x.StartsWith("secret", StringComparison.Ordinal));
	}

	[Fact]
	[Trait("Networking", "Graphs")]
	public void A_graph_clients_run_nothing_of_sends_nothing()
	{
		var rig = new Rig();
		rig.Join();
		Entity entity = rig.Spawn();
		rig.Step(20);
		long before = rig.Server.Replication!.BytesSent;
		rig.Step(20);
		long idle = rig.Server.Replication.BytesSent - before;

		Activate(entity, rig.Assets.Ponder);
		before = rig.Server.Replication.BytesSent;
		rig.Step(20);

		(rig.Server.Replication.BytesSent - before).Should().Be(idle);
	}

	[Fact]
	[Trait("Networking", "Graphs")]
	public void A_despawn_brings_what_the_entity_showed_as_it_went()
	{
		var rig = new Rig();
		Client client = rig.Join();
		Entity entity = rig.Spawn();
		rig.Step(10);

		Activate(entity, rig.Assets.Blink);
		rig.Unregister(entity);
		rig.Step(10);

		client.Assets.Log.Should().Equal("blink 0");
		client.Spawned.Should().BeEmpty();
	}

	[Fact]
	[Trait("Networking", "Graphs")]
	public void An_ability_the_registry_doesn_t_list_runs_on_the_server_alone_and_says_so()
	{
		var rig = new Rig();
		var unlisted = new List<AbilityData>();
		rig.Server.Replication!.AbilityNotListed += unlisted.Add;
		Client client = rig.Join();
		Entity entity = rig.Spawn();
		Activate(entity, rig.Assets.Unlisted);
		Activate(entity, rig.Assets.Unlisted);

		rig.Step(30);

		unlisted.Should().ContainSingle().Which.Should().Be(rig.Assets.Unlisted);
		rig.Assets.Log.Should().Contain("hush on Link 0");
		client.Assets.Log.Should().BeEmpty();
	}

	[Fact]
	[Trait("Networking", "Graphs")]
	public void Values_and_arrays_reach_a_client_before_the_states_that_read_them()
	{
		var rig = new Rig();
		Client client = rig.Join();
		Entity entity = rig.Spawn();
		Activate(entity, rig.Assets.Tally);

		rig.Step(30);

		client.Assets.Log.Should().Equal("tally on Replication 3", "tally shown 0.00");
		client.Assets.Tallied.Should().Equal(4, 5, 6);
	}

	[Fact]
	[Trait("Networking", "Graphs")]
	public void Graphs_reach_a_client_whole_through_loss()
	{
		var conditions = new NetworkConditions(0.05, 0.01, 0.1f);
		var rig = new Rig(down: conditions);
		Client client = rig.Join(up: conditions);
		Entity entity = rig.Spawn();

		for (int i = 0; i < 4; i++)
		{
			Activate(entity, rig.Assets.Cast);
			rig.Step(150);
		}

		client.Assets.Log.Count(x => x == "flash 7").Should().Be(4);
		client.Assets.Log.Count(x => x == "glow on Replication 7").Should().Be(4);
		client.Assets.Log.Count(x => x == "glow hidden").Should().Be(4);
	}

	private static AbilityHandle Activate(Entity entity, AbilityData ability)
	{
		AbilityHandle handle = entity.Abilities.GrantAbilityPermanently(ability, 1, LevelComparison.Higher, null);
		handle.TryActivate(out _).Should().BeTrue();
		return handle;
	}

	// What every machine builds for itself: the tags, the graphs with their recording nodes, the abilities that run
	// them and the registry that names them.
	private sealed class Assets
	{
		public TagsManager Tags { get; } = new(_tagNames);

		public NetworkAssetRegistry Registry { get; } = new();

		public List<string> Log { get; } = [];

		public List<int> Tallied { get; } = [];

		public int Ticks { get; set; }

		// Marks itself, sets power to 7, waits half a second, flashes, then glows for a second.
		public AbilityData Cast { get; }

		// Glows for ten seconds.
		public AbilityData Linger { get; }

		// Flashes and ends at once.
		public AbilityData Blink { get; }

		// Sparkles for a second, with sparks in a client-only region while it does, and a secret glow on the server.
		public AbilityData Sparkle { get; }

		// Waits a second, showing nothing.
		public AbilityData Ponder { get; }

		// Sets a value and an array, then glows with the value and keeps the array.
		public AbilityData Tally { get; }

		// Glows quietly for a second, and no machine's registry has it.
		public AbilityData Unlisted { get; }

		public Assets(double glowLength = 1)
		{
			Cast = Ability("Cast", BuildCast(glowLength));
			Linger = Ability("Linger", Glow("linger", 10));
			Blink = Ability("Blink", BuildBlink());
			Sparkle = Ability("Sparkle", BuildSparkle());
			Ponder = Ability("Ponder", BuildPonder());
			Tally = Ability("Tally", BuildTally());
			Unlisted = Ability("Unlisted", Glow("hush", 1));

			Registry.Register(AssetKind.Scene, "uid://creature", "Creature");
			Registry.Register(AssetKind.Ability, "uid://cast", Cast);
			Registry.Register(AssetKind.Ability, "uid://linger", Linger);
			Registry.Register(AssetKind.Ability, "uid://blink", Blink);
			Registry.Register(AssetKind.Ability, "uid://sparkle", Sparkle);
			Registry.Register(AssetKind.Ability, "uid://ponder", Ponder);
			Registry.Register(AssetKind.Ability, "uid://tally", Tally);
			Registry.Seal();
		}

		private static AbilityData Ability(string name, Graph graph)
		{
			return new AbilityData(
				name,
				instancingPolicy: AbilityInstancingPolicy.PerExecution,
				behaviorFactory: () => new GraphAbilityBehavior(graph));
		}

		private static void Constant(Graph graph, string name, Variant128 value, Type type)
		{
			graph.VariableDefinitions.DefineProperty(name, new VariantResolver(value, type));
		}

		private static Connection Link(Node from, byte port, Node to, byte input = 0)
		{
			return new Connection(from.OutputPorts[port], to.InputPorts[input]);
		}

		private static Graph BuildPonder()
		{
			var graph = new Graph();
			var wait = new TimerNode();
			graph.AddNode(wait);
			Constant(graph, "length", new Variant128(1.0), typeof(double));
			wait.BindInput(TimerNode.DurationInput, "length");
			graph.AddConnection(Link(graph.EntryNode, EntryNode.OutputPort, wait));
			return graph;
		}

		private Graph BuildCast(double glowLength)
		{
			var graph = new Graph();
			var mark = new MarkNode("cast", Log);
			var power = new SetVariableNode();
			var windup = new TimerNode();
			var flash = new FlashNode("flash", Log);
			var glow = new GlowNode("glow", Log, glowLength);

			foreach (Node node in new Node[] { mark, power, windup, flash, glow })
			{
				graph.AddNode(node);
			}

			graph.VariableDefinitions.DefineVariable("power", 0);
			Constant(graph, "seven", new Variant128(7), typeof(int));
			Constant(graph, "windup", new Variant128(0.5), typeof(double));
			power.BindInput(SetVariableNode.SourceInput, "seven");
			power.BindOutput(SetVariableNode.TargetOutput, "power");
			windup.BindInput(TimerNode.DurationInput, "windup");
			flash.BindInput(0, "power");
			glow.BindInput(GlowNode.PowerInput, "power");

			graph.AddConnection(Link(graph.EntryNode, EntryNode.OutputPort, mark));
			graph.AddConnection(Link(mark, ActionNode.OutputPort, power));
			graph.AddConnection(Link(power, ActionNode.OutputPort, windup));
			graph.AddConnection(Link(windup, TimerNode.OnTimerEndPort, flash));
			graph.AddConnection(Link(flash, ActionNode.OutputPort, glow));
			return graph;
		}

		private Graph Glow(string name, double length)
		{
			var graph = new Graph();
			var glow = new GlowNode(name, Log, length);
			graph.AddNode(glow);
			graph.AddConnection(Link(graph.EntryNode, EntryNode.OutputPort, glow));
			return graph;
		}

		private Graph BuildBlink()
		{
			var graph = new Graph();
			var blink = new FlashNode("blink", Log);
			graph.AddNode(blink);
			graph.AddConnection(Link(graph.EntryNode, EntryNode.OutputPort, blink));
			return graph;
		}

		private Graph BuildSparkle()
		{
			var graph = new Graph();
			var sparkle = new GlowNode("sparkle", Log, 1);
			var sparks = new TickerNode("sparks", this) { Region = NodeRegion.ClientOnly };
			var secret = new GlowNode("secret", Log, 1) { Region = NodeRegion.ServerOnly };

			graph.AddNode(sparkle);
			graph.AddNode(sparks);
			graph.AddNode(secret);
			graph.AddConnection(Link(graph.EntryNode, EntryNode.OutputPort, sparkle));
			graph.AddConnection(Link(graph.EntryNode, EntryNode.OutputPort, secret));
			graph.AddConnection(Link(sparkle, StateNode<StateNodeContext>.SubgraphPort, sparks));
			return graph;
		}

		private Graph BuildTally()
		{
			var graph = new Graph();
			var count = new SetVariableNode();
			var keep = new KeepNode();
			var tally = new TallyNode(this);

			graph.AddNode(count);
			graph.AddNode(keep);
			graph.AddNode(tally);
			graph.VariableDefinitions.DefineVariable("count", 0);
			graph.VariableDefinitions.DefineArrayVariable("kept", 0, 0);
			Constant(graph, "three", new Variant128(3), typeof(int));
			count.BindInput(SetVariableNode.SourceInput, "three");
			count.BindOutput(SetVariableNode.TargetOutput, "count");
			tally.BindInput(0, "count");

			graph.AddConnection(Link(graph.EntryNode, EntryNode.OutputPort, count));
			graph.AddConnection(Link(count, ActionNode.OutputPort, keep));
			graph.AddConnection(Link(keep, ActionNode.OutputPort, tally));
			return graph;
		}
	}

	private sealed class GlowContext : StateNodeContext
	{
		public double Elapsed { get; set; }

		public int Power { get; set; }
	}

	// A presentation state that shows something for as long as it lasts, counted in fixed steps from activation, the
	// way Play Animation counts its clip, then finishes on its own.
	private sealed class GlowNode(string name, List<string> log, double length) : StateNode<GlowContext>
	{
		public const byte PowerInput = 0;

		public const byte OnFinishedPort = 4;

		public override NodeNetworkKind NetworkKind => NodeNetworkKind.Presentation;

		protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
		{
			base.DefinePorts(inputPorts, outputPorts);
			outputPorts.Add(CreatePort<EventPort>(OnFinishedPort, "OnFinished"));
		}

		protected override void DefineParameters(
			List<InputProperty> inputProperties,
			List<OutputVariable> outputVariables)
		{
			inputProperties.Add(new InputProperty("Power", typeof(int), IsOptional: true));
		}

		protected override void OnActivate(GraphContext graphContext)
		{
			GlowContext context = graphContext.GetNodeContext<GlowContext>(NodeID);
			context.Elapsed = 0;
			graphContext.TryResolve(InputProperties[PowerInput].BoundName, out int power);
			context.Power = power;
			log.Add($"{name} on {context.ActivationReason} {power}");
		}

		protected override void OnDeactivate(GraphContext graphContext)
		{
			log.Add($"{name} off");
		}

		protected override void OnFixedUpdate(double deltaTime, GraphContext graphContext)
		{
			GlowContext context = graphContext.GetNodeContext<GlowContext>(NodeID);
			context.Elapsed += deltaTime;

			if (context.Elapsed >= length - 1e-9)
			{
				DeactivateNodeAndEmitMessage(graphContext, OnFinishedPort);
			}
		}

		protected override object? OnPresentationActivated(GraphContext graphContext)
		{
			GlowContext context = graphContext.GetNodeContext<GlowContext>(NodeID);
			log.Add(string.Create(CultureInfo.InvariantCulture, $"{name} shown {context.Elapsed:0.00}"));
			return name;
		}

		protected override void OnPresentationDeactivated(object? presentation)
		{
			log.Add($"{name} hidden");
		}
	}

	// A presentation action that shows something once.
	private sealed class FlashNode(string name, List<string> log) : ActionNode
	{
		public override NodeNetworkKind NetworkKind => NodeNetworkKind.Presentation;

		protected override void DefineParameters(
			List<InputProperty> inputProperties,
			List<OutputVariable> outputVariables)
		{
			inputProperties.Add(new InputProperty("Power", typeof(int), IsOptional: true));
			outputVariables.Add(new OutputVariable("Made", typeof(int)));
		}

		protected override void Execute(GraphContext graphContext)
		{
			graphContext.TryResolve(InputProperties[0].BoundName, out int power);
			log.Add($"{name} {power}");
		}
	}

	// A simulation action that only notes it ran.
	private sealed class MarkNode(string name, List<string> log) : ActionNode
	{
		protected override void DefineParameters(
			List<InputProperty> inputProperties,
			List<OutputVariable> outputVariables)
		{
			inputProperties.Add(new InputProperty("Input", typeof(object), IsOptional: true));
			outputVariables.Add(new OutputVariable("Output", typeof(int)));
		}

		protected override void Execute(GraphContext graphContext)
		{
			log.Add(name);
		}
	}

	private sealed class AuthorityNode : ActionNode
	{
		public override NodeNetworkKind NetworkKind => NodeNetworkKind.Authority;

		protected override void DefineParameters(
			List<InputProperty> inputProperties,
			List<OutputVariable> outputVariables)
		{
			inputProperties.Add(new InputProperty("Input", typeof(object), IsOptional: true));
		}

		protected override void Execute(GraphContext graphContext)
		{
		}
	}

	// A simulation state that counts its fixed steps while it runs.
	private sealed class TickerNode(string name, Assets assets) : StateNode<StateNodeContext>
	{
		protected override void OnActivate(GraphContext graphContext)
		{
			assets.Log.Add(name);
		}

		protected override void OnDeactivate(GraphContext graphContext)
		{
			assets.Log.Add($"{name} off");
		}

		protected override void OnFixedUpdate(double deltaTime, GraphContext graphContext)
		{
			assets.Ticks++;
		}
	}

	// A simulation action that fills the graph's array.
	private sealed class KeepNode : ActionNode
	{
		protected override void Execute(GraphContext graphContext)
		{
			graphContext.GraphVariables.DefineArrayVariable(
				"kept",
				[new Variant128(4), new Variant128(5), new Variant128(6)]);
		}
	}

	// A presentation state that notes the count it reads and the array it finds.
	private sealed class TallyNode(Assets assets) : StateNode<StateNodeContext>
	{
		public override NodeNetworkKind NetworkKind => NodeNetworkKind.Presentation;

		protected override void DefineParameters(
			List<InputProperty> inputProperties,
			List<OutputVariable> outputVariables)
		{
			inputProperties.Add(new InputProperty("Count", typeof(int)));
		}

		protected override void OnActivate(GraphContext graphContext)
		{
			graphContext.TryResolve(InputProperties[0].BoundName, out int count);
			graphContext.TryResolveArray("kept", out Variant128[]? kept);
			assets.Tallied.AddRange(kept!.Select(x => x.AsInt()));
			StateNodeContext context = graphContext.GetNodeContext<StateNodeContext>(NodeID);
			assets.Log.Add($"tally on {context.ActivationReason} {count}");
		}

		protected override void OnDeactivate(GraphContext graphContext)
		{
		}

		protected override object? OnPresentationActivated(GraphContext graphContext)
		{
			assets.Log.Add("tally shown 0.00");
			return null;
		}
	}

	private sealed class Rig
	{
		private readonly List<Client> _clients = [];
		private readonly List<Entity> _entities = [];
		private readonly int _seed;
		private double _time;

		public LoopbackNetwork Network { get; } = new();

		public NetworkSession Server { get; }

		public ConditionedTransport? ServerLink { get; }

		public Assets Assets { get; } = new();

		public CuesManager Cues { get; } = new();

		public Rig(NetworkConditions? down = null, int seed = 1)
		{
			_seed = seed;
			INetworkTransport transport = Network.Server;

			if (down is NetworkConditions conditions)
			{
				ServerLink = new ConditionedTransport(transport, conditions, new DeterministicRandom((ulong)seed));
				transport = ServerLink;
			}

			Server = new NetworkSession(
				transport,
				new SimulationClock(TickRate, 100),
				Assets.Registry,
				Assets.Tags,
				_actions);
		}

		public Client Join(NetworkConditions? up = null, double glowLength = 1)
		{
			var client = new Client(
				Network.Connect(),
				up,
				(ulong)(_seed * 31) + (ulong)_clients.Count,
				new Assets(glowLength));
			_clients.Add(client);

			for (int i = 0; i < 30 && client.Session.State != SessionState.Ready; i++)
			{
				Step();
			}

			client.Session.State.Should().Be(SessionState.Ready);
			return client;
		}

		public Entity Spawn()
		{
			var entity = new Entity(Assets.Tags, Cues) { NetworkId = Server.Replication!.AllocateId() };
			Server.Replication.Register(entity, 0, PeerId.None);
			_entities.Add(entity);
			return entity;
		}

		public void Unregister(Entity entity)
		{
			Server.Replication!.Unregister(entity);
			_entities.Remove(entity);
		}

		// One tick on every machine: what's in flight arrives, clients advance and present their mirrors, the server
		// simulates and sends.
		public void Step(int ticks = 1)
		{
			for (int i = 0; i < ticks; i++)
			{
				_time += TickLength;
				ServerLink?.Update(_time);

				foreach (Client client in _clients)
				{
					client.Update(_time);
				}

				Server.Update(_time);

				foreach (Entity entity in _entities)
				{
					EntitySimulation.Tick(entity, Server.Clock.Tick, TickLength);
				}

				Server.Replication!.Send(Server.Clock.Tick);
				Server.Clock.Advance();
			}
		}

		public void StepUntil(Func<bool> done)
		{
			for (int i = 0; i < 600 && !done(); i++)
			{
				Step();
			}

			done().Should().BeTrue();
		}
	}

	private sealed class Client : IReplicationHost
	{
		private readonly ConditionedTransport? _link;

		public NetworkSession Session { get; }

		public Assets Assets { get; }

		public CuesManager Cues { get; } = new();

		public List<Entity> Spawned { get; } = [];

		public Client(INetworkTransport transport, NetworkConditions? up, ulong seed, Assets assets)
		{
			Assets = assets;

			if (up is NetworkConditions conditions)
			{
				_link = new ConditionedTransport(transport, conditions, new DeterministicRandom(seed));
				transport = _link;
			}

			Session = new NetworkSession(
				transport,
				new SimulationClock(TickRate),
				Assets.Registry,
				Assets.Tags,
				_actions);
			Session.Mirror!.Host = this;
		}

		public void Update(double time)
		{
			_link?.Update(time);
			Session.Update(time);

			foreach (Entity mirror in Spawned)
			{
				mirror.Abilities.FixedUpdateAbilities(TickLength);
				mirror.Abilities.PresentAbilities();
			}
		}

		public IForgeEntity? Spawn(NetworkEntityId id, int sceneIndex, PeerId owner)
		{
			var entity = new Entity(Assets.Tags, Cues) { NetworkId = id };
			Spawned.Add(entity);
			return entity;
		}

		public void Despawn(IForgeEntity entity)
		{
			Spawned.Remove((Entity)entity);
		}
	}

	private sealed class Entity : IForgeEntity
	{
		public NetworkEntityId NetworkId { get; set; }

		public EntityAttributes Attributes { get; }

		public EntityTags Tags { get; }

		public EffectsManager EffectsManager { get; }

		public CuesManager CuesManager { get; }

		public EntityAbilities Abilities { get; }

		public EventManager Events { get; } = new();

		public Variables SharedVariables { get; } = new();

		public Entity(TagsManager tags, CuesManager cues)
		{
			Attributes = new EntityAttributes(this);
			Tags = new EntityTags(new TagContainer(tags));
			EffectsManager = new EffectsManager(this, cues);
			CuesManager = cues;
			Abilities = new EntityAbilities(this);
		}
	}
}
