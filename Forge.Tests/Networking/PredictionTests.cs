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
using Gamesmiths.Forge.Effects.Periodic;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Networking;
using Gamesmiths.Forge.Networking.Replication;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.Action;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Networking;

/// <summary>
/// A client predicts the entity it owns: it runs the entity's ticks ahead of the server from its own input, checks each
/// state packet against the tick it's of, and where the server's word differs - an effect from elsewhere, damage, a
/// body pushed - rolls the entity back, makes it what the server says and runs the ticks since again.
/// </summary>
public class PredictionTests
{
	private const int TickRate = 60;

	private const double TickLength = 1.0 / TickRate;

	private const string Speed = "TestAttributeSet.Attribute100";

	private const string Health = "TestAttributeSet.Attribute90";

	private static readonly string[] _tagNames =
	[
		"state.dashing",
		"state.slowed",
		"state.marked",
		"cue.dash",
		"cue.trail",
		"cue.fire",
		"event.hit",
		"event.shove"
	];

	private static readonly InputActionSet _actions = new(["right", "dash", "chant", "spark"]);

	[Fact]
	[Trait("Networking", "Prediction")]
	public void The_owner_runs_ahead_of_the_server_and_every_packet_agrees()
	{
		var rig = new Rig(NetworkConditions.Good);
		Player player = rig.JoinWithPlayer();
		long corrections = rig.Mirror.Corrections;

		rig.Hold("right", 120);

		rig.Client.Clock.Tick
			.Should().BeGreaterThan(rig.Server.Clock.Tick + 4, "the owner simulates ahead of the server");
		player.Position.X
			.Should().BeGreaterThan(rig.ServerPlayer.Position.X);
		rig.Mirror.Confirmations
			.Should().BeGreaterThan(30);
		rig.Mirror.Corrections
			.Should().Be(corrections, "nothing the owner predicted was wrong");
		rig.ServerPlayer.Position.X
			.Should().BeApproximately(player.PositionAt(rig.Server.Clock.Tick - 1).X, 1e-4f);
	}

	[Fact]
	[Trait("Networking", "Prediction")]
	public void The_owner_s_entity_agrees_from_the_first_packet_it_is_bound_at()
	{
		var rig = new Rig(NetworkConditions.Good);
		rig.JoinWithPlayer();

		rig.Mirror.Corrections.Should().Be(0, "it rolls from the tick the server first sent it");
	}

	[Fact]
	[Trait("Networking", "Prediction")]
	public void Nothing_is_predicted_before_the_clock_knows_how_far_ahead_to_run()
	{
		var rig = new Rig(new NetworkConditions(0.1, 0, 0));
		Player player = rig.JoinWithPlayer(holding: "right");

		rig.Mirror.Corrections.Should().Be(0, "what it sent before then arrived after the server ran it");
		rig.ServerPlayer.Position.X.Should().BeApproximately(player.PositionAt(rig.Server.Clock.Tick - 1).X, 1e-4f);
		player.Position.X.Should().BePositive("it predicted from then on");
	}

	[Fact]
	[Trait("Networking", "Prediction")]
	public void The_owner_makes_its_entity_where_the_server_s_was_as_it_was_registered()
	{
		var rig = new Rig(NetworkConditions.Good);
		rig.JoinWithPlayer(movedBeforeSent: new Vector3(0.5f, 0, 0));

		rig.SpawnedAt.Should().Be(
			new BodyState(Vector3.Zero, Quaternion.Identity, Vector3.Zero, Grounded: true),
			"the server's setup ran there, before its body moved");
	}

	[Fact]
	[Trait("Networking", "Prediction")]
	public void An_effect_from_elsewhere_is_taken_in_at_the_tick_it_landed_after()
	{
		var rig = new Rig(NetworkConditions.Good);
		Player player = rig.JoinWithPlayer();
		rig.Hold("right", 30);

		rig.ServerPlayer.EffectsManager.ApplyEffect(new Effect(rig.ServerAssets.Slow, new EffectOwnership(null, null)));
		long corrections = rig.Mirror.Corrections;
		rig.Hold("right", 20);

		rig.Mirror.Corrections.Should().Be(corrections + 1);
		rig.Mirror.LastCorrectionTicks.Should().BePositive();
		player.Tags.AllTags.HasTag(rig.Assets.Tag("state.slowed")).Should().BeTrue();
		player.Attributes[Speed].CurrentValue.Should().Be(50);

		rig.Hold("right", 40);

		player.Tags.AllTags.HasTag(rig.Assets.Tag("state.slowed")).Should().BeFalse("it ended where it did there");
		rig.Mirror.Corrections.Should().Be(corrections + 1, "it ends on its own, at the server's tick");
		rig.ServerPlayer.Position.X.Should().BeApproximately(player.PositionAt(rig.Server.Clock.Tick - 1).X, 1e-4f);
	}

	[Fact]
	[Trait("Networking", "Prediction")]
	public void A_persistent_cue_of_an_effect_from_elsewhere_ends_when_the_effect_does()
	{
		var rig = new Rig(NetworkConditions.Good);
		rig.JoinWithPlayer();
		rig.Idle(10);

		rig.ServerPlayer.EffectsManager.ApplyEffect(
			new Effect(rig.ServerAssets.Burning, new EffectOwnership(null, null)));
		rig.Idle(90);

		rig.Mirror.Corrections.Should().BeGreaterThan(1, "each burn corrects the owner");
		rig.ClientFireLog.Applied.Should().Equal("cue.fire");
		rig.ClientFireLog.Removed.Should().Equal("cue.fire");
	}

	[Fact]
	[Trait("Networking", "Prediction")]
	public void Damage_from_elsewhere_reaches_the_owner_as_its_base_value()
	{
		var rig = new Rig(NetworkConditions.Good);
		Player player = rig.JoinWithPlayer();
		rig.Hold("right", 30);

		rig.ServerPlayer.EffectsManager.ApplyEffect(new Effect(rig.ServerAssets.Hurt, new EffectOwnership(null, null)));
		rig.Hold("right", 30);

		player.Attributes[Health].BaseValue.Should().Be(80);
		player.Attributes[Health].CurrentValue.Should().Be(80);
	}

	[Fact]
	[Trait("Networking", "Prediction")]
	public void A_body_the_server_moved_puts_the_owner_right_and_its_ticks_run_again_from_there()
	{
		var rig = new Rig(NetworkConditions.Good);
		Player player = rig.JoinWithPlayer();
		rig.Hold("right", 30);

		long pushed = rig.Server.Clock.Tick - 1;
		rig.ServerPlayer.Mover.Push(new Vector3(0, 0, 2));
		rig.Hold("right", 30);

		player.Position.Z.Should().BeApproximately(2, 1e-3f);
		player.PositionAt(pushed + 20).X.Should().BeApproximately(
			rig.ServerPlayer.PositionAt(pushed + 20).X,
			1e-3f,
			"the owner goes on from the server's body as sent, to a thousandth");
	}

	[Fact]
	[Trait("Networking", "Prediction")]
	public void A_dash_shows_at_once_on_its_owner_and_the_server_agrees()
	{
		var rig = new Rig(NetworkConditions.Good);
		Player player = rig.JoinWithPlayer();
		rig.Hold("right", 30);
		long corrections = rig.Mirror.Corrections;

		rig.Press("dash");

		player.Tags.AllTags.HasTag(rig.Assets.Tag("state.dashing")).Should().BeTrue();
		rig.ServerPlayer.Tags.AllTags.HasTag(rig.Assets.Tag("state.dashing")).Should().BeFalse();

		rig.Hold("right", 60);

		rig.Mirror.Corrections.Should().Be(corrections, "the server dashed where the owner did");
		rig.ClientCueLog.Executed.Should().Equal("cue.dash");
		rig.ServerPlayer.Position.X.Should().BeApproximately(player.PositionAt(rig.Server.Clock.Tick - 1).X, 1e-4f);
	}

	[Fact]
	[Trait("Networking", "Prediction")]
	public void A_correction_during_a_dash_shows_nothing_twice()
	{
		var rig = new Rig(NetworkConditions.Good);
		Player player = rig.JoinWithPlayer();
		rig.Hold("right", 30);

		rig.Press("dash");
		rig.Hold("right", 2);
		rig.ServerPlayer.EffectsManager.ApplyEffect(new Effect(rig.ServerAssets.Hurt, new EffectOwnership(null, null)));
		long corrections = rig.Mirror.Corrections;
		rig.Hold("right", 30);

		rig.Mirror.Corrections.Should().BeGreaterThan(corrections);
		rig.ClientCueLog.Executed.Should().Equal("cue.dash");
		rig.ClientCueLog.Applied.Should().Equal("cue.trail");
		rig.ClientCueLog.Removed.Should().Equal("cue.trail");
		player.Attributes[Health].BaseValue.Should().Be(80);
	}

	[Fact]
	[Trait("Networking", "Prediction")]
	public void A_graph_s_presentation_action_shows_once_through_a_correction()
	{
		var rig = new Rig(NetworkConditions.Good);
		rig.JoinWithPlayer();

		rig.Press("spark");
		rig.ServerPlayer.EffectsManager.ApplyEffect(new Effect(rig.ServerAssets.Hurt, new EffectOwnership(null, null)));
		long corrections = rig.Mirror.Corrections;
		rig.Idle(30);

		rig.Mirror.Corrections.Should().BeGreaterThan(corrections);
		rig.Assets.Log.Should().Equal("spark", "spark again while correcting");
		rig.ServerAssets.Log.Should().Equal(
			["spark", "summon", "glow on Link", "glow off"],
			"only the server summons and runs the Server Only region");
	}

	[Fact]
	[Trait("Networking", "Prediction")]
	public void A_Server_Only_ability_runs_on_the_server_and_its_owner_shows_it_as_a_copy()
	{
		var rig = new Rig(NetworkConditions.Good);
		rig.JoinWithPlayer();
		long corrections = rig.Mirror.Corrections;

		rig.Press("chant");

		rig.Assets.Log.Should().BeEmpty("the owner doesn't run it");

		rig.Idle(20);

		rig.Assets.Log.Should().Equal("glow on Replication", "glow shown");

		rig.Idle(40);

		rig.Assets.Log.Should().Equal("glow on Replication", "glow shown", "glow off", "glow hidden");
		rig.ServerAssets.Log.Should().Equal("glow on Link", "glow off");
		rig.Mirror.Corrections.Should().Be(corrections);
	}

	[Fact]
	[Trait("Networking", "Prediction")]
	public void A_run_the_server_starts_from_elsewhere_is_taken_in_and_its_owner_runs_it()
	{
		var rig = new Rig(NetworkConditions.Good);
		rig.JoinWithPlayer();
		long corrections = rig.Mirror.Corrections;

		rig.ServerPlayer.Events.Raise(new EventData
		{
			EventTags = new TagContainer(rig.ServerAssets.Tags, [rig.ServerAssets.Tag("event.hit")]),
			Target = rig.ServerPlayer,
		});
		rig.Idle(60);

		rig.Assets.Log.Should().Equal("glow on Link", "glow shown", "glow off", "glow hidden");
		rig.ServerAssets.Log.Should().Equal("glow on Link", "glow off");
		rig.Mirror.Corrections.Should().Be(corrections + 1, "taking the run in is the one correction");
	}

	[Fact]
	[Trait("Networking", "Prediction")]
	public void A_Server_Only_region_of_a_predicted_graph_runs_on_the_server_alone_and_every_packet_agrees()
	{
		var rig = new Rig(NetworkConditions.Good);
		rig.JoinWithPlayer();
		long corrections = rig.Mirror.Corrections;

		rig.Press("spark");
		rig.Idle(60);

		rig.Assets.Log.Should().Equal("spark");
		rig.ServerAssets.Log.Should().Equal("spark", "summon", "glow on Link", "glow off");
		rig.Mirror.Corrections.Should().Be(corrections, "the checksum leaves the region out");
	}

	[Fact]
	[Trait("Networking", "Prediction")]
	public void A_checksum_the_server_cannot_take_costs_only_that_check_and_the_send_goes_out()
	{
		var rig = new Rig(NetworkConditions.Good);
		Player player = rig.JoinWithPlayer();
		rig.Press("dash");

		for (int i = 0; i < 30 && !rig.ServerPlayer.Tags.AllTags.HasTag(rig.ServerAssets.Tag("state.dashing")); i++)
		{
			rig.Idle(1);
		}

		long corrections = rig.Mirror.Corrections;
		long failing = rig.Server.Clock.Tick + 10;
		rig.ServerPlayer.EffectsManager.ApplyEffect(new Effect(rig.ServerAssets.Hurt, new EffectOwnership(null, null)));
		rig.ServerAssets.FailsCapture = true;

		Action send = () => rig.Server.Replication!.Send(failing);

		send.Should().Throw<InvalidOperationException>();
		rig.ServerAssets.FailsCapture = false;
		rig.Idle(8);

		rig.Mirror.LastServerTick.Should().Be(failing, "the send went out, without the check");
		rig.Mirror.Corrections.Should().Be(corrections, "a packet without the check checks nothing");

		rig.Idle(30);

		player.Attributes[Health].BaseValue
			.Should().Be(80, "the entity's state went with that send, and isn't sent again");
	}

	[Fact]
	[Trait("Networking", "Prediction")]
	public void A_run_from_elsewhere_of_a_graph_clients_run_nothing_of_is_taken_in_too()
	{
		var rig = new Rig(NetworkConditions.Good);
		Player player = rig.JoinWithPlayer();
		long corrections = rig.Mirror.Corrections;

		rig.ServerPlayer.Events.Raise(new EventData
		{
			EventTags = new TagContainer(rig.ServerAssets.Tags, [rig.ServerAssets.Tag("event.shove")]),
			Target = rig.ServerPlayer,
		});
		rig.Idle(20);

		player.Abilities.TryGetAbility(rig.Assets.Brace, out AbilityHandle? brace, player).Should().BeTrue();
		brace!.IsActive.Should().BeTrue();

		rig.Idle(40);

		brace.IsActive.Should().BeFalse("it ends here as it does there");
		rig.Mirror.Corrections.Should().Be(corrections + 1, "taking the run in is the one correction");
	}

	[Fact]
	[Trait("Networking", "Prediction")]
	public void A_tag_from_what_the_owner_does_not_hear_of_reaches_it_and_goes_as_it_does_on_the_server()
	{
		var rig = new Rig(NetworkConditions.Good);
		Player player = rig.JoinWithPlayer();
		long corrections = rig.Mirror.Corrections;

		rig.ServerPlayer.EffectsManager.ApplyEffect(new Effect(rig.ServerAssets.Mark, new EffectOwnership(null, null)));
		rig.Idle(20);

		player.Tags.AllTags.HasTag(rig.Assets.Tag("state.marked")).Should().BeTrue();
		rig.Mirror.Corrections.Should().Be(corrections + 1, "with the tag, every packet agrees again");

		rig.Idle(40);

		player.Tags.AllTags.HasTag(rig.Assets.Tag("state.marked")).Should().BeFalse();
		rig.Mirror.Corrections.Should().Be(corrections + 2);
	}

	[Fact]
	[Trait("Networking", "Prediction")]
	public void A_press_submitted_in_a_step_the_clock_waited_through_survives_a_correction()
	{
		var rig = new Rig(NetworkConditions.Good);
		Player player = rig.JoinWithPlayer();

		rig.ServerPlayer.EffectsManager.ApplyEffect(new Effect(rig.ServerAssets.Hurt, new EffectOwnership(null, null)));
		long corrections = rig.Mirror.Corrections;

		for (int i = 0; i < 30 && rig.Mirror.Corrections == corrections; i++)
		{
			rig.Wait("dash", pressed: i == 0);
		}

		rig.Mirror.Corrections.Should().Be(corrections + 1);

		rig.Idle(1);

		player.Tags.AllTags.HasTag(rig.Assets.Tag("state.dashing")).Should().BeTrue();
	}

	[Fact]
	[Trait("Networking", "Prediction")]
	public void Prediction_holds_through_a_poor_link()
	{
		var rig = new Rig(new NetworkConditions(0.1, 0.01, 0.05f), seed: 7);
		Player player = rig.JoinWithPlayer();

		for (int i = 0; i < 6; i++)
		{
			rig.Hold("right", 40);
			rig.Press("dash");
			rig.ServerPlayer.EffectsManager.ApplyEffect(
				new Effect(rig.ServerAssets.Slow, new EffectOwnership(null, null)));
			rig.Hold("right", 20);
			rig.ServerPlayer.EffectsManager.ApplyEffect(
				new Effect(rig.ServerAssets.Hurt, new EffectOwnership(null, null)));
			rig.ServerPlayer.Events.Raise(new EventData
			{
				EventTags = new TagContainer(rig.ServerAssets.Tags, [rig.ServerAssets.Tag("event.hit")]),
				Target = rig.ServerPlayer,
			});
		}

		rig.Idle(120);

		player.Attributes[Health].BaseValue.Should().Be(rig.ServerPlayer.Attributes[Health].BaseValue);
		player.Position.X.Should().BeApproximately(rig.ServerPlayer.Position.X, 1e-3f);
		rig.Mirror.Confirmations.Should().BeGreaterThan(rig.Mirror.Corrections);
		rig.Mirror.Corrections.Should().BeLessThan(40);
	}

	private sealed class Assets
	{
		public TagsManager Tags { get; } = new(_tagNames);

		public NetworkAssetRegistry Registry { get; } = new();

		// Half a second: -0.5 to the speed, and the state.slowed tag.
		public EffectData Slow { get; }

		// Instant: -10 to the health.
		public EffectData Hurt { get; }

		// Half a second: -1 to the health every tenth of a second, and a fire.
		public EffectData Burning { get; }

		// A quarter of a second: +4 to the speed, the state.dashing tag and a trail.
		public EffectData Dashing { get; }

		// Bound to dash: dashes, with the cue.dash executed as it starts.
		public AbilityData Dash { get; }

		// Bound to chant, for the server alone: glows for half a second.
		public AbilityData Chant { get; }

		// Triggered by event.hit: glows for half a second.
		public AbilityData Flinch { get; }

		// Bound to spark: sparks once, summons something only the server may, then waits half a second, with a glow
		// and a variable set in a Server Only region meanwhile.
		public AbilityData Sparks { get; }

		// Half a second, for the server alone to know of: the state.marked tag.
		public EffectData Mark { get; }

		// Triggered by event.shove: waits half a second, with nothing for a client to run.
		public AbilityData Brace { get; }

		public List<string> Log { get; } = [];

		// Makes a running dash throw as it's captured.
		public bool FailsCapture { get; set; }

		public Assets()
		{
			Slow = new EffectData(
				"Slow",
				new DurationData(
					DurationType.HasDuration,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(0.5f))),
				[Flat(Speed, -50)],
				effectComponents: [new ModifierTagsEffectComponent(Container("state.slowed"))]);

			Hurt = new EffectData(
				"Hurt",
				new DurationData(DurationType.Instant),
				[Flat(Health, -10)]);

			Burning = new EffectData(
				"Burning",
				new DurationData(
					DurationType.HasDuration,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(0.5f))),
				[Flat(Health, -1)],
				periodicData: new PeriodicData(new ScalableFloat(0.1f), true, PeriodInhibitionRemovedPolicy.NeverReset),
				cues: [new CueData(Container("cue.fire"), 0, 1, CueMagnitudeType.EffectLevel)]);

			Dashing = new EffectData(
				"Dashing",
				new DurationData(
					DurationType.HasDuration,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(0.25f))),
				[Flat(Speed, 400)],
				effectComponents: [new ModifierTagsEffectComponent(Container("state.dashing"))],
				cues: [new CueData(Container("cue.trail"), 0, 1, CueMagnitudeType.EffectLevel)]);

			Mark = new EffectData(
				"Mark",
				new DurationData(
					DurationType.HasDuration,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(0.5f))),
				effectComponents: [new ModifierTagsEffectComponent(Container("state.marked"))],
				visibility: EffectVisibility.ServerOnly);

			Dash = new AbilityData(
				"Dash",
				behaviorFactory: () => new DashBehavior(this),
				inputBinding: AbilityInputBinding.ForAction("dash"));

			Graph glow = Glow();
			Chant = new AbilityData(
				"Chant",
				behaviorFactory: () => new GraphAbilityBehavior(glow),
				inputBinding: AbilityInputBinding.ForAction("chant"),
				serverOnly: true);
			Flinch = new AbilityData(
				"Flinch",
				abilityTriggerData: AbilityTriggerData.ForEvent(Tag("event.hit")),
				behaviorFactory: () => new GraphAbilityBehavior(glow));

			Graph spark = Spark();
			Sparks = new AbilityData(
				"Sparks",
				behaviorFactory: () => new GraphAbilityBehavior(spark),
				inputBinding: AbilityInputBinding.ForAction("spark"));

			Graph wait = Wait();
			Brace = new AbilityData(
				"Brace",
				abilityTriggerData: AbilityTriggerData.ForEvent(Tag("event.shove")),
				behaviorFactory: () => new GraphAbilityBehavior(wait));

			Registry.Register(AssetKind.Scene, "uid://player", "Player");
			Registry.Register(AssetKind.Effect, "uid://slow", Slow);
			Registry.Register(AssetKind.Effect, "uid://hurt", Hurt);
			Registry.Register(AssetKind.Effect, "uid://dashing", Dashing);
			Registry.Register(AssetKind.Effect, "uid://burning", Burning);
			Registry.Register(AssetKind.Ability, "uid://dash", Dash);
			Registry.Register(AssetKind.Ability, "uid://chant", Chant);
			Registry.Register(AssetKind.Ability, "uid://flinch", Flinch);
			Registry.Register(AssetKind.Ability, "uid://sparks", Sparks);
			Registry.Register(AssetKind.Ability, "uid://brace", Brace);
			Registry.Seal();
		}

		public Tag Tag(string name)
		{
			return Forge.Tags.Tag.RequestTag(Tags, name);
		}

		private static Modifier Flat(string attribute, int amount)
		{
			return new Modifier(
				attribute,
				ModifierOperation.FlatBonus,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(amount)));
		}

		private static Graph Wait()
		{
			var graph = new Graph();
			var wait = new TimerNode();
			graph.AddNode(wait);
			graph.VariableDefinitions.DefineProperty(
				"length", new VariantResolver(new Variant128(0.5), typeof(double)));
			wait.BindInput(TimerNode.DurationInput, "length");
			graph.AddConnection(new Connection(
				graph.EntryNode.OutputPorts[EntryNode.OutputPort],
				wait.InputPorts[StateNode<StateNodeContext>.InputPort]));
			return graph;
		}

		private Graph Glow()
		{
			var graph = new Graph();
			var glow = new GlowNode(Log, 0.5);
			graph.AddNode(glow);
			graph.AddConnection(new Connection(
				graph.EntryNode.OutputPorts[EntryNode.OutputPort],
				glow.InputPorts[StateNode<StateNodeContext>.InputPort]));
			return graph;
		}

		private Graph Spark()
		{
			var graph = new Graph();
			var spark = new SparkNode(Log);
			var summon = new SummonNode(Log);
			var wait = new TimerNode();
			var muster = new GlowNode(Log, 0.25) { Region = NodeRegion.ServerOnly };
			var count = new SetVariableNode { Region = NodeRegion.ServerOnly };
			graph.AddNode(spark);
			graph.AddNode(summon);
			graph.AddNode(wait);
			graph.AddNode(muster);
			graph.AddNode(count);
			graph.VariableDefinitions.DefineProperty(
				"length", new VariantResolver(new Variant128(0.5), typeof(double)));
			graph.VariableDefinitions.DefineProperty("seven", new VariantResolver(new Variant128(7), typeof(int)));
			graph.VariableDefinitions.DefineVariable("mustered", 0);
			wait.BindInput(TimerNode.DurationInput, "length");
			count.BindInput(SetVariableNode.SourceInput, "seven");
			count.BindOutput(SetVariableNode.TargetOutput, "mustered");
			graph.AddConnection(new Connection(
				graph.EntryNode.OutputPorts[EntryNode.OutputPort],
				spark.InputPorts[ActionNode.InputPort]));
			graph.AddConnection(new Connection(
				spark.OutputPorts[ActionNode.OutputPort],
				summon.InputPorts[ActionNode.InputPort]));
			graph.AddConnection(new Connection(
				summon.OutputPorts[ActionNode.OutputPort],
				wait.InputPorts[StateNode<StateNodeContext>.InputPort]));
			graph.AddConnection(new Connection(
				summon.OutputPorts[ActionNode.OutputPort],
				muster.InputPorts[StateNode<StateNodeContext>.InputPort]));
			graph.AddConnection(new Connection(
				summon.OutputPorts[ActionNode.OutputPort],
				count.InputPorts[ActionNode.InputPort]));
			return graph;
		}

		private TagContainer Container(string tag)
		{
			return new TagContainer(Tags, [Tag(tag)]);
		}
	}

	// Dashes for the length of its effect, then ends: a coded behavior that takes part in snapshots.
	private sealed class DashBehavior(Assets assets) : ISnapshotAbilityBehavior
	{
		private const int Ticks = 15;

		private AbilityBehaviorContext? _context;

		private int _ticks;

		public void OnStarted(AbilityBehaviorContext context)
		{
			_context = context;
			_ticks = 0;
			context.Owner.CuesManager.ExecuteCue(assets.Tag("cue.dash"), context.Owner, null);
			context.Owner.EffectsManager.ApplyEffect(
				new Effect(assets.Dashing, new EffectOwnership(context.Owner, context.Owner)));
		}

		public void OnEnded(AbilityBehaviorContext context)
		{
			_context = null;
		}

		public void OnFixedUpdate(double deltaTime)
		{
			if (++_ticks >= Ticks)
			{
				_context?.InstanceHandle.End();
			}
		}

		public object? CaptureSnapshot()
		{
			return assets.FailsCapture
				? throw new InvalidOperationException("The dash can't be captured.")
				: (_ticks, _context);
		}

		public void RestoreSnapshot(object? snapshot)
		{
			(_ticks, _context) = ((int, AbilityBehaviorContext?))snapshot!;
		}

		public void DropRun()
		{
			_context = null;
		}
	}

	private sealed class GlowContext : StateNodeContext
	{
		public double Elapsed { get; set; }
	}

	// A presentation state that shows something for as long as it lasts, counted in fixed steps from activation.
	private sealed class GlowNode(List<string> log, double length) : StateNode<GlowContext>
	{
		public override NodeNetworkKind NetworkKind => NodeNetworkKind.Presentation;

		protected override void OnActivate(GraphContext graphContext)
		{
			GlowContext context = graphContext.GetNodeContext<GlowContext>(NodeID);
			context.Elapsed = 0;
			log.Add($"glow on {context.ActivationReason}");
		}

		protected override void OnDeactivate(GraphContext graphContext)
		{
			log.Add("glow off");
		}

		protected override void OnFixedUpdate(double deltaTime, GraphContext graphContext)
		{
			GlowContext context = graphContext.GetNodeContext<GlowContext>(NodeID);
			context.Elapsed += deltaTime;

			if (context.Elapsed >= length - 1e-9)
			{
				DeactivateNode(graphContext);
			}
		}

		protected override object? OnPresentationActivated(GraphContext graphContext)
		{
			log.Add("glow shown");
			return null;
		}

		protected override void OnPresentationDeactivated(object? presentation)
		{
			log.Add("glow hidden");
		}
	}

	// A presentation action that shows a spark once, and notes when it runs again for a correction instead.
	private sealed class SparkNode(List<string> log) : ActionNode
	{
		public override NodeNetworkKind NetworkKind => NodeNetworkKind.Presentation;

		protected override void Execute(GraphContext graphContext)
		{
			log.Add(graphContext.IsCorrecting ? "spark again while correcting" : "spark");
		}
	}

	// Something only the server may do, such as spawning a networked entity.
	private sealed class SummonNode(List<string> log) : ActionNode
	{
		public override NodeNetworkKind NetworkKind => NodeNetworkKind.Authority;

		protected override void Execute(GraphContext graphContext)
		{
			log.Add("summon");
		}
	}

	// Walks along X at the speed attribute, a hundredth of a unit per point each second, while right is held.
	private sealed class WalkingMover(Player player) : IEntityMover
	{
		public BodyState Body { get; private set; } =
			new(Vector3.Zero, Quaternion.Identity, Vector3.Zero, Grounded: true);

		public void Move(double fixedDeltaTime)
		{
			float speed = player.Input!.IsHeld("right") ? player.Attributes[Speed].CurrentValue / 100f : 0f;
			var velocity = new Vector3(speed, 0, 0);
			Body = Body with { Position = Body.Position + (velocity * (float)fixedDeltaTime), Velocity = velocity };
		}

		public void Push(Vector3 offset)
		{
			Body = Body with { Position = Body.Position + offset };
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
		private readonly Dictionary<long, Vector3> _positions = [];

		public NetworkEntityId NetworkId { get; set; }

		public DeterministicRandom? Random { get; init; }

		public EntityAttributes Attributes { get; }

		public EntityTags Tags { get; }

		public EffectsManager EffectsManager { get; }

		public CuesManager CuesManager { get; }

		public EntityAbilities Abilities { get; }

		public EventManager Events { get; } = new();

		public Variables SharedVariables { get; } = new();

		public EntityInput? Input { get; }

		public WalkingMover Mover { get; }

		public Vector3 Position => Mover.Body.Position;

		IEntityMover? IForgeEntity.Mover => Mover;

		public Player(Assets assets, CuesManager cues)
		{
			Attributes = new EntityAttributes(this, new TestAttributeSet());
			Tags = new EntityTags(new TagContainer(assets.Tags));
			EffectsManager = new EffectsManager(this, cues);
			CuesManager = cues;
			Abilities = new EntityAbilities(this);
			Input = new EntityInput(_actions);
			Mover = new WalkingMover(this);
			Abilities.GrantAbilityPermanently(assets.Dash, 1, LevelComparison.None, this);
			Abilities.GrantAbilityPermanently(assets.Chant, 1, LevelComparison.None, this);
			Abilities.GrantAbilityPermanently(assets.Flinch, 1, LevelComparison.None, this);
			Abilities.GrantAbilityPermanently(assets.Sparks, 1, LevelComparison.None, this);
			Abilities.GrantAbilityPermanently(assets.Brace, 1, LevelComparison.None, this);
		}

		public Vector3 PositionAt(long tick)
		{
			return _positions[tick];
		}

		public void Remember(long tick)
		{
			_positions[tick] = Position;
		}
	}

	// Records the cues it's shown: the executed one as cue.dash, the persistent one by its tag.
	private sealed class CueLog(Assets assets, string persistent = "cue.trail") : ICueHandler
	{
		public List<string> Executed { get; } = [];

		public List<string> Applied { get; } = [];

		public List<string> Removed { get; } = [];

		public void Watch(CuesManager cues)
		{
			cues.RegisterCue(assets.Tag("cue.dash"), this);
			cues.RegisterCue(assets.Tag(persistent), this);
		}

		public void OnExecute(IForgeEntity? target, CueParameters? parameters)
		{
			Executed.Add("cue.dash");
		}

		public void OnApply(IForgeEntity? target, CueParameters? parameters)
		{
			Applied.Add(persistent);
		}

		public void OnRemove(IForgeEntity? target, bool interrupted)
		{
			Removed.Add(persistent);
		}

		public void OnUpdate(IForgeEntity? target, CueParameters? parameters)
		{
		}
	}

	private sealed class Rig : IReplicationHost
	{
		private readonly ConditionedTransport _serverLink;
		private readonly ConditionedTransport _clientLink;
		private readonly ClockDilation _dilation = new();
		private InputFrame _frame;
		private double _time;

		public Assets ServerAssets { get; } = new();

		public Assets Assets { get; } = new();

		public NetworkSession Server { get; }

		public NetworkSession Client { get; }

		public CuesManager ServerCues { get; } = new();

		public CuesManager ClientCues { get; }

		public CueLog ClientCueLog { get; }

		public CueLog ClientFireLog { get; }

		public Player ServerPlayer { get; private set; } = null!;

		public Player? ClientPlayer { get; private set; }

		// Where the client was told to make its player.
		public BodyState? SpawnedAt { get; private set; }

		public ReplicationClient Mirror => Client.Mirror!;

		public Rig(NetworkConditions conditions, int seed = 1)
		{
			var network = new LoopbackNetwork();
			_serverLink = new ConditionedTransport(network.Server, conditions, new DeterministicRandom((ulong)seed));
			_clientLink = new ConditionedTransport(
				network.Connect(),
				conditions,
				new DeterministicRandom((ulong)seed * 31));
			Server = new NetworkSession(
				_serverLink,
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
			Client.Mirror!.Host = this;
			ClientCues = new CuesManager { DeferDispatch = true };
			ClientCueLog = new CueLog(Assets);
			ClientCueLog.Watch(ClientCues);
			ClientFireLog = new CueLog(Assets, "cue.fire");
			ClientFireLog.Watch(ClientCues);
		}

		public Player JoinWithPlayer(Vector3 movedBeforeSent = default, string? holding = null)
		{
			_frame = holding is null ? default : new InputFrame { Held = 1UL << _actions.IndexOf(holding) };

			for (int i = 0; i < 120 && Client.State != SessionState.Ready; i++)
			{
				Step();
			}

			Client.State.Should().Be(SessionState.Ready);
			NetworkEntityId id = Server.Replication!.AllocateId();
			ServerPlayer = new Player(ServerAssets, ServerCues)
			{
				NetworkId = id,
				Random = SeededAt(id, Server.Clock.Tick),
			};

			Server.Replication.Register(ServerPlayer, 0, Server.Peers[0].Id);
			ServerPlayer.Mover.Push(movedBeforeSent);

			for (int i = 0; i < 120 && ClientPlayer is null; i++)
			{
				Step();
			}

			ClientPlayer.Should().NotBeNull();

			if (holding is null)
			{
				Idle(60);
			}
			else
			{
				Hold(holding, 60);
			}

			return ClientPlayer!;
		}

		public void Hold(string action, int ticks)
		{
			for (int i = 0; i < ticks; i++)
			{
				int slot = _actions.IndexOf(action);
				ulong bit = 1UL << slot;
				_frame = new InputFrame { Held = bit, Pressed = (_frame.Held & bit) == 0 ? bit : 0 };
				Step();
			}

			_frame = default;
		}

		public void Press(string action)
		{
			int slot = _actions.IndexOf(action);
			ulong bit = 1UL << slot;
			_frame = new InputFrame { Held = bit, Pressed = bit };
			Step();
			_frame = new InputFrame { Released = bit };
			Step();
			_frame = default;
		}

		// A step the client's clock waits through, simulating no tick, with the action held.
		public void Wait(string action, bool pressed)
		{
			ulong bit = 1UL << _actions.IndexOf(action);
			_frame = new InputFrame { Held = bit, Pressed = pressed ? bit : 0 };
			Step(waits: true);
		}

		public void Idle(int ticks)
		{
			_frame = default;

			for (int i = 0; i < ticks; i++)
			{
				Step();
			}
		}

		// One frame on both machines: what's in flight arrives, the client puts its player right, steers its clock and
		// predicts the ticks the step simulates, and the server sends what the last tick left and simulates the next.
		public void Step(bool waits = false)
		{
			_time += TickLength;
			_serverLink.Update(_time);
			_clientLink.Update(_time);
			Client.Update(_time);

			DilatedStep step = Client.State == SessionState.Ready
				? _dilation.Steer(Client.Clock.Tick, Client.ClockSync, _time, Client.ServerInputDepth)
				: new DilatedStep(Client.Clock.Tick, 1);

			if (waits)
			{
				step = new DilatedStep(Client.Clock.Tick, 0);
			}

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
					long tick = Client.Clock.Tick;
					Mirror.Predict(tick, TickLength);
					player.Remember(tick);
					Client.SendInput(new InputCommand(tick, player.Input!.Current));
				}

				Client.Clock.Advance();
			}

			ClientPlayer?.Abilities.PresentAbilities();
			ClientCues.PresentCues();

			Server.Update(_time);
			Server.Replication!.Send(Server.Clock.Tick - 1);
			Server.TakeInput(Server.Clock.Tick);

			if (ServerPlayer is not null)
			{
				EntitySimulation.Tick(ServerPlayer, Server.Clock.Tick, TickLength);
				ServerPlayer.Remember(Server.Clock.Tick);
			}

			Server.Clock.Advance();
		}

		public IForgeEntity? SpawnPredicted(
			NetworkEntityId id,
			int sceneIndex,
			PeerId owner,
			BodyState? body,
			SpawnKey? key)
		{
			SpawnedAt = body;
			return Spawn(id, sceneIndex, owner);
		}

		public IForgeEntity? Spawn(NetworkEntityId id, int sceneIndex, PeerId owner)
		{
			var player = new Player(Assets, ClientCues) { NetworkId = id, Random = SeededAt(id, Client.Clock.Tick) };
			ClientPlayer = player;
			return player;
		}

		public void Despawn(IForgeEntity entity)
		{
			ClientPlayer = null;
		}

		// Rolls from the entity's id, begun at the tick it's made at, as a game seeds a networked entity.
		private static DeterministicRandom SeededAt(NetworkEntityId id, long tick)
		{
			var random = new DeterministicRandom(id.Value);
			random.BeginTick(tick);
			return random;
		}
	}
}
