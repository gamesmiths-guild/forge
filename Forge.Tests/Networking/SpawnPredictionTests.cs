// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Networking;
using Gamesmiths.Forge.Networking.Replication;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Networking;

/// <summary>
/// A client predicts what its own entity's graphs spawn: it makes the spawn as the graph does, ahead of the server,
/// under the key the server gives the spawn it makes in the same tick, and the server's spawn, arriving, binds the one
/// the client made. Spawns join the entity on the client's timeline, rolling back and running again with it.
/// </summary>
public class SpawnPredictionTests
{
	private const int TickRate = 60;

	private const double TickLength = 1.0 / TickRate;

	private const string SpeedAttribute = "TestAttributeSet.Attribute100";

	private static readonly string[] _tagNames = ["state.slowed"];

	private static readonly InputActionSet _actions = new(["right", "fire", "volley"]);

	[Fact]
	[Trait("Networking", "Prediction")]
	public void A_shot_shows_at_once_and_the_server_s_spawn_binds_it()
	{
		var rig = new Rig(NetworkConditions.Good);
		rig.JoinWithPlayer();
		long corrections = rig.Mirror.Corrections;

		rig.Press("fire");

		Bolt bolt = rig.ClientBolts.Should().ContainSingle("the client made it as it fired, ahead of the server")
			.Subject;
		bolt.NetworkId.IsValid.Should().BeFalse("the server's spawn of it hasn't arrived yet");
		rig.Mirror.Predicted.Should().Contain(bolt);
		long confirmations = rig.Mirror.Confirmations;

		rig.Idle(30);

		Bolt served = rig.ServerBolts
			.Should().ContainSingle().Subject;
		bolt.NetworkId
			.Should().Be(served.NetworkId);
		rig.Mirror.Find(served.NetworkId)
			.Should().BeSameAs(bolt);
		rig.ClientBolts
			.Should().ContainSingle("the server's spawn bound the one the client made");
		rig.Mirror.Corrections
			.Should().Be(corrections, "the bolt flew as the server's did");
		rig.Mirror.Confirmations
			.Should().BeGreaterThan(confirmations + 10);
		bolt.Position.X
			.Should().BeApproximately(served.Position.X + (float)(Bolt.Speed * rig.TicksAhead * TickLength), 0.01f);
	}

	[Fact]
	[Trait("Networking", "Prediction")]
	public void The_server_and_the_owner_key_two_shots_of_one_node_in_a_tick_apart()
	{
		var rig = new Rig(NetworkConditions.Good);
		rig.JoinWithPlayer();

		rig.Press("volley");
		rig.Idle(30);

		rig.ClientBolts.Should().HaveCount(2);
		rig.ClientKeys.Should().Equal(rig.ServerKeys, "both machines number a node's spawns in a tick the same");
		rig.ClientKeys.Select(x => x.Sequence).Should().Equal(0, 1);

		for (int i = 0; i < 2; i++)
		{
			rig.ClientBolts[i].NetworkId.Should().Be(rig.ServerBolts[i].NetworkId, "each was bound to its own");
		}
	}

	[Fact]
	[Trait("Networking", "Prediction")]
	public void A_shot_the_server_never_fires_goes_to_limbo_and_is_let_go_once_nothing_can_bring_it_back()
	{
		var rig = new Rig(NetworkConditions.Good);
		rig.JoinWithPlayer();
		rig.ServerRefuses = true;

		rig.Press("fire");
		Bolt bolt = rig.ClientBolts.Should().ContainSingle().Subject;
		rig.Idle(30);

		rig.Limbo.Should().Contain(bolt, "the server's packets reached its tick without spawning it");
		rig.Mirror.Predicted.Should().NotContain(bolt);
		rig.Despawned.Should().NotContain(bolt, "a late spawn could still bring it back");

		rig.Idle(130);

		rig.Despawned.Should().Contain(bolt, "no correction reaches the tick it was made in anymore");
		rig.Returned.Should().NotContain(bolt);
	}

	[Fact]
	[Trait("Networking", "Prediction")]
	public void A_correction_from_before_a_shot_finds_the_same_bolt_again()
	{
		var rig = new Rig(NetworkConditions.Good);
		rig.JoinWithPlayer();
		long corrections = rig.Mirror.Corrections;

		rig.SlowServerPlayer();
		rig.Press("fire");
		Bolt bolt = rig.ClientBolts.Should().ContainSingle().Subject;
		rig.Idle(30);

		rig.Mirror.Corrections.Should().BeGreaterThan(corrections, "the slow landed before the shot");
		rig.Limbo.Should().Contain(bolt, "the rollback went back to before the tick it was made in");
		rig.Returned.Should().Contain(bolt, "running that tick again made it again");
		rig.ClientBolts.Should().ContainSingle("the correction found the bolt it made instead of making another");
		bolt.NetworkId.Should().Be(rig.ServerBolts.Single().NetworkId);
		rig.Mirror.Predicted.Should().Contain(bolt);
	}

	[Fact]
	[Trait("Networking", "Prediction")]
	public void A_bolt_freed_on_its_owner_waits_in_limbo_for_the_server_s_despawn()
	{
		var rig = new Rig(NetworkConditions.Good);
		rig.JoinWithPlayer();

		rig.Press("fire");
		Bolt bolt = rig.ClientBolts.Should().ContainSingle().Subject;

		for (int i = 0; i < 90 && !rig.Limbo.Contains(bolt); i++)
		{
			rig.Idle(1);
		}

		rig.Limbo.Should().Contain(bolt, "its graph freed it");
		rig.Despawned.Should().NotContain(bolt, "the server freed its own later, ahead being behind");

		rig.Idle(30);

		rig.Despawned.Should().Contain(bolt, "the server's despawn of it arrived");
		rig.Returned.Should().NotContain(bolt);
	}

	[Fact]
	[Trait("Networking", "Prediction")]
	public void A_spawn_the_owner_did_not_make_joins_its_timeline_at_the_tick_the_server_made_it_in()
	{
		var rig = new Rig(NetworkConditions.Good);
		rig.JoinWithPlayer();
		rig.ClientRefuses = true;

		rig.Press("fire");
		rig.ClientBolts.Should().BeEmpty();
		rig.Idle(30);

		Bolt bolt = rig.ClientBolts.Should().ContainSingle("the server's spawn made it").Subject;
		Bolt served = rig.ServerBolts.Single();
		bolt.NetworkId.Should().Be(served.NetworkId);
		rig.Mirror.Predicted.Should().Contain(bolt);
		long corrections = rig.Mirror.Corrections;

		rig.Idle(10);

		rig.Mirror.Corrections
			.Should().Be(corrections, "it ran from the tick the server made it in, as the server's did");
		bolt.Position.X
			.Should().BeApproximately(served.Position.X + (float)(Bolt.Speed * rig.TicksAhead * TickLength), 0.01f);
	}

	[Fact]
	[Trait("Networking", "Prediction")]
	public void A_shot_whose_spawn_comes_late_comes_back_from_limbo()
	{
		var rig = new Rig(NetworkConditions.Good);
		rig.JoinWithPlayer();

		// A packet holds one entry, so the bolt's spawn waits a send behind its owner's state.
		rig.Server.Replication!.BudgetBytes = 1;
		rig.Press("fire");
		Bolt bolt = rig.ClientBolts.Should().ContainSingle().Subject;
		rig.Idle(30);

		rig.Limbo.Should().Contain(bolt, "a packet of its tick came without its spawn");
		rig.Returned.Should().Contain(bolt, "its spawn, in the next, brought it back");
		bolt.NetworkId.Should().Be(rig.ServerBolts.Single().NetworkId);
		rig.Mirror.Predicted.Should().Contain(bolt);
		long corrections = rig.Mirror.Corrections;

		rig.Idle(10);

		rig.Mirror.Corrections.Should().Be(corrections, "it came back where the server's is");
	}

	[Fact]
	[Trait("Networking", "Prediction")]
	public void A_bolt_freed_in_the_tick_it_was_made_in_is_made_again_by_a_correction_from_before_it()
	{
		var rig = new Rig(NetworkConditions.Good);
		rig.JoinWithPlayer();
		rig.FreesAtOnce = true;
		long corrections = rig.Mirror.Corrections;

		rig.SlowServerPlayer();
		rig.Press("fire");
		Bolt bolt = rig.ClientBolts.Should().ContainSingle().Subject;
		rig.Limbo.Should().Contain(bolt, "its graph freed it as it made it");
		rig.Idle(30);

		rig.Mirror.Corrections.Should().BeGreaterThan(corrections, "the slow landed before the shot");
		rig.Returned.Should().Contain(bolt, "running its tick again made it again, as that tick left it");
		rig.ClientBolts.Should().ContainSingle();
		rig.Mirror.Predicted.Should().NotContain(bolt, "the tick that made it again freed it again");
	}

	[Fact]
	[Trait("Networking", "Prediction")]
	public void A_correction_from_before_a_shot_the_server_never_fired_keeps_it_in_limbo()
	{
		var rig = new Rig(NetworkConditions.Good);
		rig.JoinWithPlayer();
		rig.ServerRefuses = true;
		long corrections = rig.Mirror.Corrections;

		// The slow lands before the shot, and the packets saying so are lost until the server is past the shot's tick.
		rig.SlowServerPlayer();
		rig.ServerLink.Conditions = NetworkConditions.Good with { LossRate = 1 };
		rig.Press("fire");
		Bolt bolt = rig.ClientBolts.Should().ContainSingle().Subject;
		rig.Idle(10);
		rig.ServerLink.Conditions = NetworkConditions.Good;
		rig.Idle(30);

		rig.Mirror.Corrections.Should().BeGreaterThan(corrections);
		rig.Limbo.Should().Contain(bolt, "the server's packets reached its tick without spawning it");
		rig.Returned.Should().NotContain(bolt, "running its tick again doesn't bring back what the server didn't make");
		rig.ClientBolts.Should().ContainSingle("the correction found it, kept out, instead of making another");
	}

	[Fact]
	[Trait("Networking", "Prediction")]
	public void A_shot_whose_spawn_comes_after_the_timeline_can_reach_its_ticks_comes_back_as_last_recorded()
	{
		var rig = new Rig(NetworkConditions.Good);
		rig.JoinWithPlayer();

		// A packet holds one entry, so a packet of the shot's tick comes without its spawn.
		rig.Server.Replication!.BudgetBytes = 1;
		rig.Press("fire");
		Bolt bolt = rig.ClientBolts.Should().ContainSingle().Subject;
		long shot = rig.ClientKeys.Single().Tick;

		while (rig.Server.Replication.LastSendTick < shot)
		{
			rig.Idle(1);
		}

		// Then nothing arrives until a spawn of the server's that no key finds comes with the shot's, joining the
		// timeline after the last tick the shot was recorded in, so the timeline can't go back to before it.
		rig.ServerLink.Conditions = NetworkConditions.Good with { LossRate = 1 };
		rig.Idle(10);
		rig.Limbo.Should().Contain(bolt, "a packet of its tick came without its spawn");
		rig.SpawnServerBolt();
		rig.Idle(20);
		rig.Server.Replication.BudgetBytes = ReplicationServer.DefaultBudgetBytes;
		rig.ServerLink.Conditions = NetworkConditions.Good;
		rig.Idle(20);

		bolt.NetworkId.Should().Be(rig.ServerBolts[0].NetworkId);
		rig.Returned.Should().Contain(bolt, "its spawn arrived, though the timeline can't go back to its ticks");
		rig.Mirror.Predicted.Should().Contain(bolt);
		long corrections = rig.Mirror.Corrections;

		rig.Idle(10);

		rig.Mirror.Corrections.Should().Be(corrections, "the server's word put it right as it came back");
	}

	private sealed class Assets
	{
		public TagsManager Tags { get; } = new(_tagNames);

		public NetworkAssetRegistry Registry { get; } = new();

		// Half a second: -0.5 to the speed, and the state.slowed tag.
		public EffectData Slow { get; }

		// Bound to fire: shoots a bolt.
		public AbilityData Fire { get; }

		// Bound to volley: shoots two bolts from one node.
		public AbilityData Volley { get; }

		// A bolt's: frees it after a second.
		public AbilityData Fade { get; }

		// Makes the bolts a graph shoots, on its machine, with the node's index.
		public Action<GraphContext, int>? Shoot { get; set; }

		// Frees a bolt its graph is done with, on its machine.
		public Action<IForgeEntity>? Vanish { get; set; }

		public Assets()
		{
			Slow = new EffectData(
				"Slow",
				new DurationData(
					DurationType.HasDuration,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(0.5f))),
				[
					new Modifier(
						SpeedAttribute,
						ModifierOperation.FlatBonus,
						new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(-50))),
				],
				effectComponents:
				[
					new ModifierTagsEffectComponent(new TagContainer(Tags, [Tag.RequestTag(Tags, "state.slowed")])),
				]);

			Graph fire = Shooting(1);
			Fire = new AbilityData(
				"Fire",
				behaviorFactory: () => new GraphAbilityBehavior(fire),
				inputBinding: AbilityInputBinding.ForAction("fire"));

			Graph volley = Shooting(2);
			Volley = new AbilityData(
				"Volley",
				behaviorFactory: () => new GraphAbilityBehavior(volley),
				inputBinding: AbilityInputBinding.ForAction("volley"));

			Graph fade = Fading();
			Fade = new AbilityData("Fade", behaviorFactory: () => new GraphAbilityBehavior(fade));

			Registry.Register(AssetKind.Scene, "uid://player", "Player");
			Registry.Register(AssetKind.Scene, "uid://bolt", "Bolt");
			Registry.Register(AssetKind.Effect, "uid://slow", Slow);
			Registry.Register(AssetKind.Ability, "uid://fire", Fire);
			Registry.Register(AssetKind.Ability, "uid://volley", Volley);
			Registry.Register(AssetKind.Ability, "uid://fade", Fade);
			Registry.Seal();
		}

		private Graph Shooting(int bolts)
		{
			var graph = new Graph();
			var shoot = new ShootNode(this, bolts);
			graph.AddNode(shoot);
			graph.AddConnection(new Connection(
				graph.EntryNode.OutputPorts[EntryNode.OutputPort],
				shoot.InputPorts[ActionNode.InputPort]));
			return graph;
		}

		private Graph Fading()
		{
			var graph = new Graph();
			var wait = new TimerNode();
			var vanish = new VanishNode(this);
			graph.AddNode(wait);
			graph.AddNode(vanish);
			graph.VariableDefinitions.DefineProperty(
				"length", new VariantResolver(new Variant128(1.0), typeof(double)));
			wait.BindInput(TimerNode.DurationInput, "length");
			graph.AddConnection(new Connection(
				graph.EntryNode.OutputPorts[EntryNode.OutputPort],
				wait.InputPorts[StateNode<StateNodeContext>.InputPort]));
			graph.AddConnection(new Connection(
				wait.OutputPorts[TimerNode.OnTimerEndPort],
				vanish.InputPorts[ActionNode.InputPort]));
			return graph;
		}
	}

	// Shoots bolts through its machine's spawning, as a networked scene's spawn node would.
	private sealed class ShootNode(Assets assets, int bolts) : ActionNode
	{
		protected override void Execute(GraphContext graphContext)
		{
			for (int i = 0; i < bolts; i++)
			{
				assets.Shoot!(graphContext, Index);
			}
		}
	}

	private sealed class VanishNode(Assets assets) : ActionNode
	{
		protected override void Execute(GraphContext graphContext)
		{
			graphContext.TryGetActivationContext(out AbilityBehaviorContext? context);
			assets.Vanish!(context!.Owner);
		}
	}

	// Walks along X at the speed attribute, a hundredth of a unit per point each second, while right is held.
	private sealed class WalkingMover(Player player) : IEntityMover
	{
		public BodyState Body { get; private set; } =
			new(Vector3.Zero, Quaternion.Identity, Vector3.Zero, Grounded: true);

		public void Move(double fixedDeltaTime)
		{
			float speed = player.Input!.IsHeld("right") ? player.Attributes[SpeedAttribute].CurrentValue / 100f : 0f;
			var velocity = new Vector3(speed, 0, 0);
			Body = Body with { Position = Body.Position + (velocity * (float)fixedDeltaTime), Velocity = velocity };
		}

		public BodyState CaptureState()
		{
			return Body;
		}

		public void RestoreState(in BodyState state)
		{
			Body = state;
		}
	}

	// Flies along X at a bolt's speed.
	private sealed class FlyingMover(Vector3 from, float speed) : IEntityMover
	{
		public BodyState Body { get; private set; } =
			new(from, Quaternion.Identity, new Vector3(speed, 0, 0), Grounded: false);

		public void Move(double fixedDeltaTime)
		{
			Body = Body with { Position = Body.Position + (Body.Velocity * (float)fixedDeltaTime) };
		}

		public BodyState CaptureState()
		{
			return Body;
		}

		public void RestoreState(in BodyState state)
		{
			Body = state;
		}
	}

	private sealed class Player : IForgeEntity
	{
		public NetworkEntityId NetworkId { get; set; }

		public DeterministicRandom? Random { get; init; }

		public EntityAttributes Attributes { get; }

		public EntityTags Tags { get; }

		public EffectsManager EffectsManager { get; }

		public CuesManager CuesManager { get; }

		public EntityAbilities Abilities { get; }

		public EventManager Events { get; } = new();

		public Variables SharedVariables { get; } = new();

		public EntityInput? Input { get; } = new(_actions);

		public WalkingMover Mover { get; }

		IEntityMover? IForgeEntity.Mover => Mover;

		public Player(Assets assets, CuesManager cues)
		{
			Attributes = new EntityAttributes(this, new TestAttributeSet());
			Tags = new EntityTags(new TagContainer(assets.Tags));
			EffectsManager = new EffectsManager(this, cues);
			CuesManager = cues;
			Abilities = new EntityAbilities(this);
			Mover = new WalkingMover(this);
			Abilities.GrantAbilityPermanently(assets.Fire, 1, LevelComparison.None, this);
			Abilities.GrantAbilityPermanently(assets.Volley, 1, LevelComparison.None, this);
		}
	}

	private sealed class Bolt : IForgeEntity
	{
		private readonly AbilityData _fade;

		public static float Speed => 5;

		public NetworkEntityId NetworkId { get; set; }

		public DeterministicRandom? Random { get; init; }

		public EntityAttributes Attributes { get; }

		public EntityTags Tags { get; }

		public EffectsManager EffectsManager { get; }

		public CuesManager CuesManager { get; }

		public EntityAbilities Abilities { get; }

		public EventManager Events { get; } = new();

		public Variables SharedVariables { get; } = new();

		public EntityInput? Input => null;

		public FlyingMover Mover { get; }

		public Vector3 Position => Mover.Body.Position;

		IEntityMover? IForgeEntity.Mover => Mover;

		public Bolt(Assets assets, CuesManager cues, Vector3 from)
		{
			Attributes = new EntityAttributes(this);
			Tags = new EntityTags(new TagContainer(assets.Tags));
			EffectsManager = new EffectsManager(this, cues);
			CuesManager = cues;
			Abilities = new EntityAbilities(this);
			Mover = new FlyingMover(from, Speed);
			_fade = assets.Fade;
		}

		// Its setup, as a scene's would: it starts fading.
		public void Setup()
		{
			Abilities.GrantAbilityPermanently(_fade, 1, LevelComparison.None, this).TryActivate(out _);
		}
	}

	private sealed class Rig : IReplicationHost
	{
		private readonly ConditionedTransport _clientLink;
		private readonly ClockDilation _dilation = new();
		private readonly List<Bolt> _serverVanished = [];
		private InputFrame _frame;
		private double _time;

		public Assets ServerAssets { get; } = new();

		public Assets Assets { get; } = new();

		public NetworkSession Server { get; }

		public NetworkSession Client { get; }

		public CuesManager ServerCues { get; } = new();

		public CuesManager ClientCues { get; } = new() { DeferDispatch = true };

		public Player ServerPlayer { get; private set; } = null!;

		public Player? ClientPlayer { get; private set; }

		public List<Bolt> ServerBolts { get; } = [];

		public List<Bolt> ClientBolts { get; } = [];

		public List<SpawnKey> ServerKeys { get; } = [];

		public List<SpawnKey> ClientKeys { get; } = [];

		public List<IForgeEntity> Limbo { get; } = [];

		public List<IForgeEntity> Returned { get; } = [];

		public List<IForgeEntity> Despawned { get; } = [];

		// Whether the server's graph shoots without the server making the bolt.
		public bool ServerRefuses { get; set; }

		// Whether the client's graph shoots without the client making the bolt, leaving it to the server's spawn.
		public bool ClientRefuses { get; set; }

		// Whether the client's graph frees each bolt in the tick it makes it in.
		public bool FreesAtOnce { get; set; }

		public ConditionedTransport ServerLink { get; }

		public ReplicationClient Mirror => Client.Mirror!;

		// How many ticks the client's clock is ahead of the server's.
		public long TicksAhead => Client.Clock.Tick - Server.Clock.Tick;

		public Rig(NetworkConditions conditions)
		{
			var network = new LoopbackNetwork();
			ServerLink = new ConditionedTransport(network.Server, conditions, new DeterministicRandom(1));
			_clientLink = new ConditionedTransport(network.Connect(), conditions, new DeterministicRandom(31));
			Server = new NetworkSession(
				ServerLink,
				new SimulationClock(TickRate, 100),
				ServerAssets.Registry,
				ServerAssets.Tags,
				_actions);
			Client = new NetworkSession(
				_clientLink,
				new SimulationClock(TickRate),
				Assets.Registry,
				Assets.Tags,
				_actions);
			Mirror.Host = this;
			Mirror.EnteredLimbo += Limbo.Add;
			Mirror.LeftLimbo += Returned.Add;
			Mirror.Spawned += x => (x as Bolt)?.Setup();
			ServerAssets.Shoot = ServerShoot;
			ServerAssets.Vanish = x => _serverVanished.Add((Bolt)x);
			Assets.Shoot = ClientShoot;
			Assets.Vanish = x => Mirror.PredictDespawn(x).Should().BeTrue();
		}

		public void JoinWithPlayer()
		{
			for (int i = 0; i < 120 && Client.State != SessionState.Ready; i++)
			{
				Step();
			}

			Client.State.Should().Be(SessionState.Ready);
			NetworkEntityId id = Server.Replication!.AllocateId();
			ServerPlayer = new Player(ServerAssets, ServerCues)
			{
				NetworkId = id,
				Random = new DeterministicRandom(id.Value),
			};

			Server.Replication.Register(ServerPlayer, 0, Server.Peers[0].Id);

			for (int i = 0; i < 120 && ClientPlayer is null; i++)
			{
				Step();
			}

			ClientPlayer.Should().NotBeNull();
			Idle(60);
		}

		// Spawns a bolt of the server's for the client's peer, not from a graph, so no key finds it there.
		public void SpawnServerBolt()
		{
			NetworkEntityId id = Server.Replication!.AllocateId();
			var bolt = new Bolt(ServerAssets, ServerCues, Vector3.Zero)
			{
				NetworkId = id,
				Random = new DeterministicRandom(id.Value),
			};

			bolt.Setup();
			Server.Replication.Register(bolt, 1, Server.Peers[0].Id);
			ServerBolts.Add(bolt);
		}

		// Slows the server's player from outside its ticks, as the world would.
		public void SlowServerPlayer()
		{
			ServerPlayer.EffectsManager.ApplyEffect(
				new Effect(ServerAssets.Slow, new EffectOwnership(ServerPlayer, ServerPlayer)));
		}

		public void Press(string action)
		{
			ulong bit = 1UL << _actions.IndexOf(action);
			_frame = new InputFrame { Held = bit, Pressed = bit };
			Step();
			_frame = new InputFrame { Released = bit };
			Step();
			_frame = default;
		}

		public void Idle(int ticks)
		{
			_frame = default;

			for (int i = 0; i < ticks; i++)
			{
				Step();
			}
		}

		// One frame on both machines: what's in flight arrives, the client puts its timeline right, steers its clock
		// and predicts the ticks the step simulates, and the server sends what the last tick left and simulates the
		// next, its player and bolts together.
		public void Step()
		{
			_time += TickLength;
			ServerLink.Update(_time);
			_clientLink.Update(_time);
			Client.Update(_time);

			DilatedStep step = Client.State == SessionState.Ready
				? _dilation.Steer(Client.Clock.Tick, Client.ClockSync, _time, Client.ServerInputDepth)
				: new DilatedStep(Client.Clock.Tick, 1);
			Client.Clock.Set(step.FirstTick);

			if (Client.Input.NewestTick > Client.Clock.Tick)
			{
				Client.Input.Clear();
			}

			ClientPlayer?.Input!.Submit(_frame);

			for (int i = 0; i < step.Ticks; i++)
			{
				if (ClientPlayer is Player player)
				{
					Mirror.Predict(Client.Clock.Tick, TickLength);
					Client.SendInput(new InputCommand(Client.Clock.Tick, player.Input!.Current));
				}

				Client.Clock.Advance();
			}

			ClientCues.PresentCues();

			Server.Update(_time);
			Server.Replication!.Send(Server.Clock.Tick - 1);
			Server.TakeInput(Server.Clock.Tick);

			if (ServerPlayer is not null)
			{
				EntitySimulation.Tick([ServerPlayer, .. ServerBolts], Server.Clock.Tick, TickLength);
			}

			foreach (Bolt bolt in _serverVanished)
			{
				Server.Replication.Unregister(bolt);
				ServerBolts.Remove(bolt);
				EntitySimulation.Teardown(bolt);
			}

			_serverVanished.Clear();
			Server.Clock.Advance();
		}

		public IForgeEntity? SpawnPredicted(
			NetworkEntityId id,
			int sceneIndex,
			PeerId owner,
			BodyState? body,
			SpawnKey? key)
		{
			if (sceneIndex == 0)
			{
				return Spawn(id, sceneIndex, owner);
			}

			var bolt = new Bolt(Assets, ClientCues, body!.Value.Position)
			{
				NetworkId = id,
				Random = key is SpawnKey made ? Seeded(made, Client.SessionSeed) : new DeterministicRandom(id.Value),
			};

			ClientBolts.Add(bolt);
			return bolt;
		}

		public IForgeEntity? Spawn(NetworkEntityId id, int sceneIndex, PeerId owner)
		{
			if (sceneIndex != 0)
			{
				return null;
			}

			var player = new Player(Assets, ClientCues) { NetworkId = id, Random = new DeterministicRandom(id.Value) };
			ClientPlayer = player;
			return player;
		}

		public void Despawn(IForgeEntity entity)
		{
			Despawned.Add(entity);
		}

		// Where a shot's bolt starts: ahead of its shooter, each of a node's bolts in a tick a step further.
		private static Vector3 Muzzle(IForgeEntity shooter, SpawnKey key)
		{
			return shooter.Mover!.CaptureState().Position + new Vector3(0.5f * (key.Sequence + 1), 0, 0);
		}

		private static DeterministicRandom Seeded(SpawnKey key, ulong sessionSeed)
		{
			var random = new DeterministicRandom(key.SeedFor(sessionSeed));
			random.BeginTick(0);
			return random;
		}

		private void ServerShoot(GraphContext graphContext, int node)
		{
			SpawnKey key = Server.Replication!.KeySpawn(graphContext, node)!.Value;

			if (ServerRefuses)
			{
				return;
			}

			ServerKeys.Add(key);
			var bolt = new Bolt(ServerAssets, ServerCues, Muzzle(ServerPlayer, key))
			{
				NetworkId = Server.Replication.AllocateId(),
				Random = Seeded(key, Server.SessionSeed),
			};

			bolt.Setup();
			Server.Replication.Register(bolt, 1, Server.Peers[0].Id, key);
			ServerBolts.Add(bolt);
		}

		private void ClientShoot(GraphContext graphContext, int node)
		{
			SpawnKey key = Mirror.KeySpawn(graphContext, node)!.Value;

			if (ClientRefuses)
			{
				return;
			}

			if (Mirror.FindSpawn(key) is IForgeEntity found)
			{
				if (FreesAtOnce)
				{
					Mirror.PredictDespawn(found);
				}

				return;
			}

			var bolt = new Bolt(Assets, ClientCues, Muzzle(ClientPlayer!, key))
			{
				Random = Seeded(key, Client.SessionSeed),
			};

			if (!Mirror.PredictSpawn(bolt, key, x => bolt.NetworkId = x))
			{
				return;
			}

			bolt.Setup();
			ClientKeys.Add(key);
			ClientBolts.Add(bolt);

			if (FreesAtOnce)
			{
				Mirror.PredictDespawn(bolt);
			}
		}
	}
}
