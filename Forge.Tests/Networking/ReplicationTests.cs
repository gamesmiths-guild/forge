// Copyright © Gamesmiths Guild.

using System.Numerics;
using System.Reflection;
using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Networking;
using Gamesmiths.Forge.Networking.Messages;
using Gamesmiths.Forge.Networking.Replication;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Networking;

/// <summary>
/// The server replicates its entities to every client over the loopback: a client's mirror holds what the server's
/// entity has, the union of what a client hasn't acknowledged survives loss and reordering, a late joiner and an entity
/// coming back into relevancy are sent whole, executed cues play once and expire, and a client's input drives what it
/// owns on the server.
/// </summary>
public class ReplicationTests
{
	private const int TickRate = 60;

	private static readonly string[] _tagNames =
		["state.burning", "state.wet", "state.stunned", "cue.fire", "cue.hit", "cue.regen", "input.menu"];

	private static readonly InputActionSet _actions = new(["fire"], [new InputVector("l", "r", "u", "d")]);

	[Fact]
	[Trait("Networking", "Replication")]
	public void A_spawned_entity_reaches_a_client_whole()
	{
		var rig = new Rig();
		Client client = rig.Join();
		Entity entity = rig.Spawn(new PeerId(7));
		entity.Body = new BodyState(new Vector3(1.25f, 0, -3.5f), Quaternion.Identity, Vector3.UnitX, true);
		Rig.Apply(entity, rig.Burning());
		int expected = entity.Set.Attribute1.CurrentValue;

		rig.Step(6);

		Entity mirror = client.Only();
		mirror.NetworkId.Should().Be(entity.NetworkId);
		mirror.Set.Attribute1.CurrentValue.Should().Be(expected, "the current value arrives, modifiers included");
		mirror.Tags.AllTags.HasTag(client.Tag("state.burning")).Should().BeTrue();
		mirror.EffectsManager.GetActiveEffects().Should().BeEmpty("the registry doesn't list the effect");
		client.Fire.IsApplied.Should().BeTrue("the persistent cue is state");
		client.Session.Mirror!.OwnerOf(mirror).Should().Be(new PeerId(7));
		client.Session.Mirror.BodyOf(mirror)!.Latest!.Value.Position.Should().Be(new Vector3(1.25f, 0, -3.5f));
		rig.Server.TagsManager.GetNetworkUsage().Select(x => x.Key.ToString())
			.Should().Contain("state.burning", "the server counts the tags it sends");
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void Only_the_tags_of_entries_that_fit_a_packet_are_counted()
	{
		var rig = new Rig();
		rig.Join();
		rig.Server.Replication!.BudgetBytes = 1;
		Rig.Apply(rig.Spawn(), rig.Burning());
		Rig.Apply(rig.Spawn(), rig.Burning());

		for (int i = 0; i < 60 && rig.Server.TagsManager.GetNetworkUsage().Count == 0; i++)
		{
			rig.Step();
		}

		rig.Server.TagsManager.GetNetworkUsage().Single(x => x.Key.ToString() == "state.burning").Value
			.Should().Be(1, "the budget left the second entity out of the packet");
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void A_held_packet_brings_its_bodies_at_once_and_the_rest_once_its_tick_is_shown()
	{
		var rig = new Rig();
		Client client = rig.Join();
		ReplicationClient replication = client.Session.Mirror!;
		replication.HoldsUntilShown = true;
		Entity entity = rig.Spawn();
		entity.Body = new BodyState(Vector3.UnitX, Quaternion.Identity, Vector3.Zero, true);
		rig.Step(6);

		client.Spawned.Should().BeEmpty("the spawn waits for its tick to be shown");
		replication.Show(replication.LastServerTick);
		Entity mirror = client.Only();
		replication.BodyOf(mirror)!.Latest!.Value.Position.Should().Be(Vector3.UnitX, "its bodies waited with it");

		long beforeBurning = replication.LastServerTick;
		Rig.Apply(entity, rig.Burning());
		entity.Body = new BodyState(new Vector3(2, 0, 0), Quaternion.Identity, Vector3.Zero, true);
		rig.Step(4);

		replication.BodyOf(mirror)!.Latest!.Value.Position.Should().Be(new Vector3(2, 0, 0));
		replication.Show(beforeBurning);
		mirror.Tags.AllTags.HasTag(client.Tag("state.burning")).Should().BeFalse();
		client.Fire.IsApplied.Should().BeFalse();

		replication.Show(replication.LastServerTick);
		mirror.Tags.AllTags.HasTag(client.Tag("state.burning")).Should().BeTrue();
		client.Fire.IsApplied.Should().BeTrue();

		rig.Server.Replication!.Unregister(entity);
		rig.Step(4);
		client.Spawned.Should().ContainSingle("the despawn waits too");

		replication.Show(replication.LastServerTick);
		client.Spawned.Should().BeEmpty();
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void Turning_holding_off_applies_what_was_held()
	{
		var rig = new Rig();
		Client client = rig.Join();
		ReplicationClient replication = client.Session.Mirror!;
		replication.HoldsUntilShown = true;
		Entity entity = rig.Spawn();
		Rig.Apply(entity, rig.Burning());
		rig.Step(6);
		client.Spawned.Should().BeEmpty();

		replication.HoldsUntilShown = false;

		client.Only().Tags.AllTags.HasTag(client.Tag("state.burning"))
			.Should().BeTrue("the held packets were acknowledged and won't come again");
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void A_change_reaches_the_client_and_its_end_too()
	{
		var rig = new Rig();
		Client client = rig.Join();
		Entity entity = rig.Spawn();
		rig.Step(6);

		ActiveEffectHandle handle = Rig.Apply(entity, rig.Burning())!;
		rig.Step(4);
		Entity mirror = client.Only();
		mirror.Tags.AllTags.HasTag(client.Tag("state.burning")).Should().BeTrue();
		client.Fire.IsApplied.Should().BeTrue();

		entity.EffectsManager.RemoveEffect(handle);
		rig.Step(4);

		mirror.Tags.AllTags.HasTag(client.Tag("state.burning")).Should().BeFalse();
		mirror.Set.Attribute1.CurrentValue.Should().Be(entity.Set.Attribute1.CurrentValue);
		client.Fire.IsApplied.Should().BeFalse();
		client.Fire.LastInterrupted.Should().BeTrue("the server took the effect away before its time");
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void A_cue_that_ended_on_its_own_ends_uninterrupted_on_the_client()
	{
		var rig = new Rig();
		Client client = rig.Join();
		Entity entity = rig.Spawn();
		rig.ServerCues.ApplyCue(rig.ServerTag("cue.fire"), entity, null);
		rig.Step(6);

		rig.ServerCues.RemoveCue(rig.ServerTag("cue.fire"), entity, interrupted: false);
		rig.Step(6);

		client.Fire.IsApplied.Should().BeFalse();
		client.Fire.LastInterrupted.Should().BeFalse();
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void A_cue_sent_again_until_it_s_acknowledged_is_no_update_on_the_client()
	{
		var rig = new Rig();
		Client client = rig.Join(up: new NetworkConditions(0.1));
		Entity entity = rig.Spawn();
		rig.Step(20);

		rig.ServerCues.ApplyCue(rig.ServerTag("cue.fire"), entity, new CueParameters(5, 0.5f, null));
		rig.Step(20);
		client.Fire.ApplyCount.Should().Be(1);
		client.Fire.UpdateCount.Should().Be(0);

		rig.ServerCues.UpdateCue(rig.ServerTag("cue.fire"), entity, new CueParameters(6, 0.5f, null));
		rig.Step(20);
		client.Fire.UpdateCount.Should().Be(1);
		client.Fire.LastParameters!.Value.Magnitude.Should().Be(6);
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void State_seen_before_the_first_tick_still_reaches_a_client()
	{
		var rig = new Rig();
		Entity placed = rig.Place(new NetworkEntityId(1000));
		Rig.Apply(placed, rig.Soaked());
		rig.Server.Replication!.Send(-1);

		Client client = rig.Join(null, new NetworkEntityId(1000));
		rig.Step(6);

		IForgeEntity mirror = client.Session.Mirror!.Find(new NetworkEntityId(1000))!;
		mirror.Tags.AllTags.HasTag(client.Tag("state.wet")).Should().BeTrue();
		client.Regen.IsApplied.Should().BeTrue();
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void A_mirror_with_other_attributes_is_still_announced()
	{
		var rig = new Rig();
		Client client = rig.Join();
		var spawned = new List<IForgeEntity>();
		client.Session.Mirror!.Spawned += spawned.Add;

		var writer = new BitWriter();
		writer.WriteVarUInt(100_000);
		writer.WriteVarInt(100);
		writer.WriteVarUInt(50);
		writer.WriteBits(1, 2);
		writer.WriteBool(false);
		writer.WriteBits(0, rig.Server.Registry.BitsFor(AssetKind.Scene));
		writer.WriteVarUInt(0);
		writer.WriteVarUInt(1);
		rig.Network.Server.Send(client.Peer, Protocol.StateChannel, reliable: false, writer.ToArray());

		Validation.Enabled = true;

		try
		{
			Action step = () => rig.Step();

			step.Should().Throw<ValidationException>();
			spawned.Should().ContainSingle();
		}
		finally
		{
			Validation.Enabled = false;
		}
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void A_cue_applied_before_registering_reaches_the_client_and_its_end_too()
	{
		var rig = new Rig();
		Client client = rig.Join();
		var entity = new Entity(rig.ServerTagsManager, rig.ServerCues)
		{
			NetworkId = rig.Server.Replication!.AllocateId(),
		};
		ActiveEffectHandle handle = Rig.Apply(entity, rig.Burning())!;

		rig.Server.Replication.Register(entity, 0, PeerId.None);
		rig.Step(6);
		client.Fire.IsApplied.Should().BeTrue();

		entity.EffectsManager.RemoveEffect(handle);
		rig.Step(6);
		client.Fire.IsApplied.Should().BeFalse();
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void A_restore_on_the_server_reaches_the_client()
	{
		var rig = new Rig();
		Client client = rig.Join();
		Entity entity = rig.Spawn();
		EntitySnapshot before = EntitySimulation.CaptureSnapshot(entity);
		Rig.Apply(entity, rig.Burning());
		EntitySnapshot burning = EntitySimulation.CaptureSnapshot(entity);
		rig.Step(6);
		client.Fire.IsApplied.Should().BeTrue();

		EntitySimulation.RestoreSnapshot(entity, before);
		rig.Step(6);
		client.Fire.IsApplied.Should().BeFalse("the restore took the effect and its cue away");

		EntitySimulation.RestoreSnapshot(entity, burning);
		rig.Step(6);
		client.Fire.IsApplied.Should().BeTrue("the restore brought them back");
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void A_server_stops_observing_cues_when_nothing_of_it_needs_them()
	{
		var rig = new Rig();
		var shared = new CuesManager();
		var first = new Entity(rig.ServerTagsManager, shared) { NetworkId = rig.Server.Replication!.AllocateId() };
		var second = new Entity(rig.ServerTagsManager, shared) { NetworkId = rig.Server.Replication.AllocateId() };
		rig.Server.Replication.Register(first, 0, PeerId.None);
		rig.Server.Replication.Register(second, 0, PeerId.None);
		ObserverCount(shared).Should().Be(1);

		rig.Server.Replication.Unregister(first);
		ObserverCount(shared).Should().Be(1, "the other entity still uses it");

		rig.Server.Replication.Unregister(second);
		ObserverCount(shared).Should().Be(0);

		rig.Server.Replication.Register(first, 0, PeerId.None);
		rig.Server.Disconnect();
		ObserverCount(shared).Should().Be(0, "the entity outlives the session");
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void Under_loss_and_reordering_every_client_ends_where_the_server_is()
	{
		var rig = new Rig(down: new NetworkConditions(0.05, 0.04, 0.2f), seed: 3);
		Client client = rig.Join(up: new NetworkConditions(0.05, 0.04, 0.2f));
		rig.Server.Replication!.BudgetBytes = 48;
		var entities = new List<Entity>();

		for (int i = 0; i < 8; i++)
		{
			entities.Add(rig.Spawn());
		}

		var random = new DeterministicRandom(11);
		var soaked = new Dictionary<Entity, ActiveEffectHandle>();
		int lowest = int.MaxValue;

		for (int step = 0; step < 300; step++)
		{
			Entity changed = entities[random.NextInt(entities.Count)];
			Rig.Apply(changed, Rig.Raise());
			ToggleSoaked(changed, soaked, rig, step % 7 == 0);
			changed.Body = new BodyState(new Vector3(step * 0.01f, 0, 0), Quaternion.Identity, default, true);
			rig.Step();

			foreach (Entity mirror in client.Spawned)
			{
				lowest = Math.Min(lowest, mirror.Set.Attribute100.CurrentValue);
			}
		}

		rig.Step(120);

		client.Session.Mirror!.PacketsLate.Should().BePositive("jitter reordered packets");
		rig.ServerLink!.LostPackets.Should().BeGreaterThan(10);

		foreach (Entity entity in entities)
		{
			Entity mirror = client.Find(entity);
			mirror.Set.Attribute100.CurrentValue.Should().Be(entity.Set.Attribute100.CurrentValue);
			SameTags(mirror, entity, client, rig).Should().BeTrue();
			client.Session.Mirror.BodyOf(mirror)!.Latest.Should().Be(Rounded(entity.Body!.Value));
		}

		lowest.Should().BeGreaterThanOrEqualTo(100, "a value seen is never one older than a value seen before it");
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void A_late_joiner_gets_the_world_as_it_is()
	{
		var rig = new Rig();
		Client early = rig.Join();
		Entity entity = rig.Spawn();
		ActiveEffectHandle burning = Rig.Apply(entity, rig.Burning())!;
		Rig.Apply(entity, rig.Soaked());
		rig.Step(10);
		entity.EffectsManager.RemoveEffect(burning);
		rig.Step(10);

		Client late = rig.Join();
		rig.Step(6);

		Entity mirror = late.Only();
		mirror.Set.Attribute1.CurrentValue.Should().Be(entity.Set.Attribute1.CurrentValue);
		mirror.Tags.AllTags.HasTag(late.Tag("state.wet")).Should().BeTrue();
		mirror.Tags.AllTags.HasTag(late.Tag("state.burning")).Should().BeFalse();
		late.Fire.ApplyCount.Should().Be(0, "a cue that ended before the join never shows");
		late.Regen.IsApplied.Should().BeTrue();
		early.Fire.ApplyCount.Should().Be(1);
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void An_entity_out_of_range_is_despawned_and_comes_back_whole()
	{
		var rig = new Rig();
		Client client = rig.Join();
		rig.Server.Replication!.Relevancy = new DistanceRelevancy(10);
		Entity player = rig.Spawn(client.Peer);
		player.Body = new BodyState(Vector3.Zero, Quaternion.Identity, default, true);
		Entity other = rig.Spawn();
		other.Body = new BodyState(new Vector3(5, 0, 0), Quaternion.Identity, default, true);
		rig.Step(6);
		client.Spawned.Should().HaveCount(2);

		other.Body = new BodyState(new Vector3(50, 0, 0), Quaternion.Identity, default, true);
		rig.Step(6);
		client.Spawned.Should().ContainSingle();
		client.Despawns.Should().Be(1);

		Rig.Apply(other, rig.Soaked());
		other.Body = new BodyState(new Vector3(3, 0, 0), Quaternion.Identity, default, true);
		rig.Step(6);

		Entity mirror = client.Find(other);
		mirror.Tags.AllTags.HasTag(client.Tag("state.wet")).Should().BeTrue("what changed while away arrives whole");
		client.Session.Mirror!.BodyOf(mirror)!.Latest!.Value.Position.Should().Be(new Vector3(3, 0, 0));
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void Distance_is_from_what_each_client_owns()
	{
		var rig = new Rig();
		Client near = rig.Join();
		Client far = rig.Join();
		rig.Server.Replication!.Relevancy = new DistanceRelevancy(10);
		rig.Spawn(near.Peer).Body = new BodyState(Vector3.Zero, Quaternion.Identity, default, true);
		rig.Spawn(far.Peer).Body = new BodyState(new Vector3(100, 0, 0), Quaternion.Identity, default, true);
		rig.Spawn().Body = new BodyState(new Vector3(5, 0, 0), Quaternion.Identity, default, true);

		rig.Step(6);

		near.Spawned.Should().HaveCount(2);
		far.Spawned.Should().ContainSingle();
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void A_late_acknowledgement_of_a_despawn_leaves_the_entity_that_came_back_alone()
	{
		// Each round takes the tag away at a different moment after the entity comes back. The acknowledgements of the
		// despawn's copies are still on their way, and none may cost the client that removal.
		for (int round = 0; round < 24; round++)
		{
			var rig = new Rig();
			Client client = rig.Join(up: new NetworkConditions(0.1));
			var relevancy = new SwitchedRelevancy();
			rig.Server.Replication!.Relevancy = relevancy;
			Entity entity = rig.Spawn();
			ActiveEffectHandle handle = Rig.Apply(entity, rig.Soaked())!;
			rig.Step(20);

			relevancy.Relevant = false;
			rig.Step(8);
			relevancy.Relevant = true;
			rig.Step(round);
			entity.EffectsManager.RemoveEffect(handle);
			rig.Step(40);

			client.Find(entity).Tags.AllTags.HasTag(client.Tag("state.wet")).Should().BeFalse($"round {round}");
		}
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void An_executed_cue_plays_once_and_one_too_old_never_plays()
	{
		var rig = new Rig(down: new NetworkConditions(0.01), seed: 5);
		Client client = rig.Join();
		Entity entity = rig.Spawn();
		rig.Step(6);

		rig.ServerCues.ExecuteCue(rig.ServerTag("cue.hit"), entity, new CueParameters(25, 0.25f, null));
		rig.Step(8);
		client.Hit.ExecuteCount.Should().Be(1);
		client.Hit.LastParameters!.Value.Magnitude.Should().Be(25);

		rig.ServerLink!.Conditions = new NetworkConditions(0.01, 0, 1);
		rig.ServerCues.ExecuteCue(rig.ServerTag("cue.hit"), entity, null);
		rig.Step((int)(ReplicationServer.EventExpirySeconds * TickRate) + 4);
		rig.ServerLink.Conditions = new NetworkConditions(0.01);
		rig.Step(8);

		client.Hit.ExecuteCount.Should().Be(1, "the hit expired while the link was down");
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void A_late_joiner_never_plays_what_a_placed_entity_did_long_before_it_went()
	{
		var rig = new Rig();
		Entity placed = rig.Place(new NetworkEntityId(1000));
		rig.Step(6);

		rig.ServerCues.ExecuteCue(rig.ServerTag("cue.hit"), placed, null);
		rig.Server.Replication!.Unregister(placed);
		rig.Step((int)(ReplicationServer.EventExpirySeconds * TickRate) + 4);

		Client late = rig.Join(null, new NetworkEntityId(1000));
		rig.Step(6);

		late.Despawns.Should().Be(1);
		late.Hit.ExecuteCount.Should().Be(0, "the hit was long over when it joined");
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void Unregistering_despawns_on_every_client_even_a_placed_entity_on_a_late_joiner()
	{
		var rig = new Rig();
		Entity placed = rig.Place(new NetworkEntityId(1000));
		Client client = rig.Join(null, new NetworkEntityId(1000));
		Entity spawned = rig.Spawn();
		rig.Step(6);
		client.Spawned.Should().ContainSingle();

		rig.Server.Replication!.Unregister(spawned).Should().BeTrue();
		rig.Server.Replication.Unregister(placed).Should().BeTrue();
		rig.Step(6);
		client.Despawns.Should().Be(2);

		Client late = rig.Join(null, new NetworkEntityId(1000));
		rig.Step(6);
		late.Despawns.Should().Be(1, "the late joiner placed the entity the server let go of");
		late.Spawned.Should().BeEmpty();
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void A_placed_entity_learns_its_owner_from_the_server()
	{
		var rig = new Rig();
		rig.Place(new NetworkEntityId(1000), new PeerId(7));
		Client client = rig.Join(null, new NetworkEntityId(1000));

		rig.Step(6);

		IForgeEntity mirror = client.Session.Mirror!.Find(new NetworkEntityId(1000))!;
		client.Session.Mirror.OwnerOf(mirror).Should().Be(new PeerId(7));
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void A_packet_over_budget_carries_what_fits_and_the_rest_follows_by_priority()
	{
		var rig = new Rig();
		Client client = rig.Join();
		rig.Server.Replication!.BudgetBytes = 40;

		for (int i = 0; i < 12; i++)
		{
			rig.Spawn().Body = new BodyState(new Vector3(i, 0, 0), Quaternion.Identity, default, true);
		}

		rig.Step(2);
		client.Spawned.Count.Should().BeInRange(1, 11, "one packet can't hold every entity");

		rig.Step(40);
		client.Spawned.Should().HaveCount(12);
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void A_body_that_rested_then_moved_is_held_where_it_stood_until_it_moved()
	{
		var rig = new Rig();
		Client client = rig.Join();
		Entity entity = rig.Spawn();
		entity.Body = new BodyState(Vector3.Zero, Quaternion.Identity, default, true);
		rig.Step(6);
		long restedAt = client.Session.Mirror!.LastServerTick;

		rig.Step(60);
		entity.Body = new BodyState(new Vector3(6, 0, 0), Quaternion.Identity, default, true);
		rig.Step(2);

		RemoteBody body = client.Session.Mirror.BodyOf(client.Only())!;
		body.TrySample(restedAt + 50, out BodyState state).Should().BeTrue();
		state.Position.Should().Be(Vector3.Zero, "it stood still until the send before it moved");
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void A_body_the_budget_keeps_leaving_out_is_never_shown_stopping()
	{
		var rig = new Rig();
		Client client = rig.Join();
		rig.Server.Replication!.BudgetBytes = 1;
		var entities = new List<Entity>();

		for (int i = 0; i < 3; i++)
		{
			entities.Add(rig.Spawn());
		}

		for (int step = 0; step < 120; step++)
		{
			foreach (Entity entity in entities)
			{
				entity.Body = new BodyState(new Vector3(step * 0.1f, 0, 0), Quaternion.Identity, default, true);
			}

			rig.Step();
		}

		RemoteBody body = client.Session.Mirror!.BodyOf(client.Find(entities[0]))!;
		long newest = body.LatestTick;

		for (long tick = newest - 60; tick < newest; tick++)
		{
			body.TrySample(tick, out BodyState earlier);
			body.TrySample(tick + 1, out BodyState later);
			later.Position.X.Should().BeGreaterThan(earlier.Position.X, $"it never stopped, and at {tick} it did");
		}
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void A_body_that_went_away_is_gone_from_the_client_too()
	{
		var rig = new Rig();
		Client client = rig.Join();
		Entity entity = rig.Spawn();
		entity.Body = new BodyState(Vector3.Zero, Quaternion.Identity, default, true);
		rig.Step(6);
		client.Session.Mirror!.BodyOf(client.Only()).Should().NotBeNull();

		entity.Body = null;
		rig.Step(6);

		client.Session.Mirror.BodyOf(client.Only()).Should().BeNull();
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void A_client_s_input_drives_what_it_owns_on_the_server()
	{
		var rig = new Rig();
		Client client = rig.Join();
		Entity mine = rig.Spawn(client.Peer);
		Entity theirs = rig.Spawn(new PeerId(99));
		mine.Input = new EntityInput(_actions);
		theirs.Input = new EntityInput(_actions);
		var taken = new List<InputDelivery>();
		rig.Server.InputTaken += (_, _, delivery) => taken.Add(delivery);
		var events = new TagContainer(rig.ServerTagsManager, [rig.ServerTag("input.menu")]);
		var raised = new List<EventData>();
		mine.Events.Subscribe(rig.ServerTag("input.menu"), raised.Add);

		long tick = rig.Server.Clock.Tick + 3;
		client.Session.SendInput(
			new InputCommand(
				tick,
				new InputFrame { Held = 1 },
				[InputCommand.EventIndex(client.Tags, client.Tag("input.menu"))]));
		rig.Step(3);
		rig.Server.TakeInput(tick, events);
		EntitySimulation.BeginTick(mine, tick);
		EntitySimulation.BeginTick(theirs, tick);

		mine.Input.Current.Held.Should().Be(1);
		theirs.Input.Current.Held.Should().Be(0);
		taken.Should().Equal(InputDelivery.Received);
		raised.Should().ContainSingle();
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void An_input_event_can_spawn_and_despawn_entities()
	{
		var rig = new Rig();
		Client client = rig.Join();
		Entity mine = rig.Spawn(client.Peer);
		Entity gone = rig.Spawn(client.Peer);
		mine.Input = new EntityInput(_actions);
		gone.Input = new EntityInput(_actions);
		var events = new TagContainer(rig.ServerTagsManager, [rig.ServerTag("input.menu")]);
		mine.Events.Subscribe(rig.ServerTag("input.menu"), _ =>
		{
			rig.Spawn(client.Peer);
			rig.Server.Replication!.Unregister(gone);
		});

		long tick = rig.Server.Clock.Tick + 3;
		client.Session.SendInput(
			new InputCommand(
				tick,
				new InputFrame { Held = 1 },
				[InputCommand.EventIndex(client.Tags, client.Tag("input.menu"))]));
		rig.Step(3);
		rig.Server.TakeInput(tick, events);
		EntitySimulation.BeginTick(gone, tick);

		rig.Server.Replication!.Entities.Should().HaveCount(2);
		gone.Input.Current.Held.Should().Be(0, "it was gone by the time its turn came");
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void A_state_packet_that_makes_no_sense_is_dropped_and_counted()
	{
		var rig = new Rig();
		Client client = rig.Join();
		rig.Step(2);

		rig.Network.Server.Send(client.Peer, Protocol.StateChannel, reliable: false, [5, 0x80]);
		rig.Step();

		client.Session.MalformedPackets.Should().Be(1);
		client.Session.State.Should().Be(SessionState.Ready);
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void A_state_packet_that_cannot_be_read_counts_for_nothing()
	{
		var rig = new Rig();
		Client client = rig.Join();
		Entity entity = rig.Spawn();
		rig.Step(4);
		uint sequence = client.Session.Mirror!.LastSequence;
		long tick = client.Session.Mirror.LastServerTick;

		// A far newer sequence and tick, then a truncated entry.
		var writer = new BitWriter();
		writer.WriteVarUInt(100_000);
		writer.WriteVarInt(100_000);
		writer.WriteVarUInt(entity.NetworkId.Value);
		rig.Network.Server.Send(client.Peer, Protocol.StateChannel, reliable: false, writer.ToArray());
		rig.Step();

		client.Session.MalformedPackets.Should().Be(1);
		client.Session.Mirror.LastSequence.Should().BeLessThan(100_000);
		client.Session.Mirror.LastServerTick.Should().BeLessThan(100_000);
		client.Session.Mirror.LastSequence.Should().BeGreaterThanOrEqualTo(sequence);
		client.Session.Mirror.LastServerTick.Should().BeGreaterThanOrEqualTo(tick);

		Rig.Apply(entity, rig.Soaked());
		rig.Step(6);
		client.Only().Tags.AllTags.HasTag(client.Tag("state.wet")).Should().BeTrue("later packets still count");
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void A_state_packet_with_a_negative_tick_is_dropped_and_counted()
	{
		var rig = new Rig();
		Client client = rig.Join();
		rig.Step(2);

		var writer = new BitWriter();
		writer.WriteVarUInt(100_000);
		writer.WriteVarInt(-1);
		writer.WriteVarUInt(0);
		rig.Network.Server.Send(client.Peer, Protocol.StateChannel, reliable: false, writer.ToArray());
		rig.Step();

		client.Session.MalformedPackets.Should().Be(1);
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void An_attribute_range_upside_down_is_dropped_and_counted()
	{
		var rig = new Rig();
		Client client = rig.Join();
		rig.Step(2);

		var writer = new BitWriter();
		writer.WriteVarUInt(100_000);
		writer.WriteVarInt(100);
		writer.WriteVarUInt(999);
		writer.WriteBits(0, 2);
		writer.WriteBool(true);
		writer.WriteVarUInt(1);
		writer.WriteVarUInt(0);
		writer.WriteVarInt(1);
		writer.WriteBool(true);
		writer.WriteVarInt(5);
		writer.WriteVarInt(1);
		rig.Network.Server.Send(client.Peer, Protocol.StateChannel, reliable: false, writer.ToArray());
		rig.Step();

		client.Session.MalformedPackets.Should().Be(1);
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void A_body_quantizes_to_the_wire_s_steps_and_back()
	{
		var rotation = Quaternion.Normalize(new Quaternion(0.1f, -0.7f, 0.2f, 0.6f));
		var state = new BodyState(new Vector3(12.3456f, -0.0004f, 7f), rotation, new Vector3(1.234f, 0, -9.876f), true);

		QuantizedBody quantized = BodyCodec.Quantize(state, BodyPrecision.Default);
		var writer = new BitWriter();
		BodyCodec.Write(writer, quantized, BodyPrecision.Default);
		QuantizedBody read = BodyCodec.Read(new BitReader(writer.ToArray()), BodyPrecision.Default);
		BodyState back = BodyCodec.Dequantize(read, BodyPrecision.Default);

		read.Should().Be(quantized);
		back.Position.X.Should().BeApproximately(12.346f, 1e-4f);
		back.Position.Y.Should().Be(0);
		back.Velocity.Z.Should().BeApproximately(-9.88f, 1e-4f);
		back.Grounded.Should().BeTrue();
		Math.Abs(Quaternion.Dot(back.Rotation, rotation)).Should().BeGreaterThan(0.9999f);
		BodyCodec.Quantize(back, BodyPrecision.Default).Should().Be(quantized, "a quantized state quantizes to itself");
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void A_coarser_precision_rounds_a_body_to_its_steps_in_fewer_bytes()
	{
		var precision = new BodyPrecision(100, 10, 6);
		var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 1.1f);
		var state = new BodyState(new Vector3(12.3456f, -0.004f, 7f), rotation, new Vector3(1.234f, 0, -9.876f), true);

		var writer = new BitWriter();
		BodyCodec.Write(writer, BodyCodec.Quantize(state, precision), precision);
		BodyState back = BodyCodec.Dequantize(BodyCodec.Read(new BitReader(writer.ToArray()), precision), precision);
		var finer = new BitWriter();
		BodyCodec.Write(finer, BodyCodec.Quantize(state, BodyPrecision.Default), BodyPrecision.Default);

		back.Position.X.Should().BeApproximately(12.35f, 1e-4f);
		back.Position.Y.Should().Be(0);
		back.Velocity.X.Should().BeApproximately(1.2f, 1e-4f);
		Math.Abs(Quaternion.Dot(back.Rotation, rotation)).Should().BeGreaterThan(0.999f);
		back.Rotation.X.Should().Be(0, "zero is a level at any precision");
		writer.ToArray().Length.Should().BeLessThan(finer.ToArray().Length);
	}

	[Theory]
	[Trait("Networking", "Replication")]
	[InlineData(0, 100, 9)]
	[InlineData(1000, 0, 9)]
	[InlineData(1000, 100, 1)]
	[InlineData(1000, 100, 17)]
	public void A_session_takes_no_precision_it_cannot_write(int positionSteps, int velocitySteps, int rotationBits)
	{
		var rig = new Rig();

		Action set = () => rig.Server.BodyPrecision = new BodyPrecision(positionSteps, velocitySteps, rotationBits);

		set.Should().Throw<ArgumentException>();
		rig.Server.BodyPrecision.Should().Be(BodyPrecision.Default);
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void A_body_turned_only_about_one_axis_stays_upright_through_the_wire()
	{
		var yaw = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.8f);

		Quaternion still = Rounded(new BodyState(Vector3.Zero, Quaternion.Identity, Vector3.Zero, true)).Rotation;
		Quaternion turned = Rounded(new BodyState(Vector3.Zero, yaw, Vector3.Zero, true)).Rotation;

		still.Should().Be(Quaternion.Identity, "a capsule tilted by a step leans into what it touches");
		turned.X.Should().Be(0);
		turned.Z.Should().Be(0);
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void A_remote_body_shows_the_states_on_either_side_of_a_tick()
	{
		var body = new RemoteBody();
		body.TrySample(5, out _).Should().BeFalse();

		body.Add(10, new BodyState(Vector3.Zero, Quaternion.Identity, default, true));
		body.Add(20, new BodyState(new Vector3(10, 0, 0), Quaternion.Identity, default, false));
		body.Add(15, new BodyState(new Vector3(99, 0, 0), Quaternion.Identity, default, false));

		body.Count.Should().Be(2, "a state older than the newest arrived late");
		body.TrySample(14, out BodyState between).Should().BeTrue();
		between.Position.X.Should().BeApproximately(4, 1e-5f);
		body.TrySample(2, out BodyState before).Should().BeTrue();
		before.Position.Should().Be(Vector3.Zero);
		body.TrySample(30, out BodyState after).Should().BeTrue();
		after.Position.X.Should().Be(10);
	}

	[Theory]
	[Trait("Networking", "Interpolation")]
	[InlineData(0, 0, 2, 2.6)]
	[InlineData(0.02, 0, 2.8, 5)]
	[InlineData(0, 0.1, 3.5, 4.8)]
	public void The_interpolation_delay_follows_how_the_packets_arrive(
		double jitter,
		float loss,
		double atLeast,
		double atMost)
	{
		var rig = new Rig(new NetworkConditions(0.01, jitter, loss));
		Client client = rig.Join();
		InterpolationDelay delay = client.Session.Mirror!.InterpolationDelay;

		for (int i = 0; i < 600; i++)
		{
			rig.Step();
			delay.Update(i / (double)TickRate);
		}

		delay.SendInterval.Should().Be(ReplicationServer.DefaultSendInterval);
		delay.Ticks.Should().BeInRange(atLeast, atMost);
	}

	[Fact]
	[Trait("Networking", "Interpolation")]
	public void The_interpolation_delay_rises_at_once_falls_slowly_and_keeps_to_its_bounds()
	{
		var rig = new Rig(new NetworkConditions(0.01, 0, 0.2f));
		Client client = rig.Join();
		InterpolationDelay delay = client.Session.Mirror!.InterpolationDelay;
		delay.MaxSeconds = 3.0 / TickRate;
		double time = 0;

		for (int i = 0; i < 300; i++)
		{
			rig.Step();
			time += 1.0 / TickRate;
			delay.Update(time);
		}

		delay.Ticks.Should().Be(3, "the loss calls for 4, and the longest is 3");

		delay.MaxSeconds = 1;
		time += 1.0 / TickRate;
		delay.Update(time);
		delay.Ticks.Should().BeGreaterThan(3.5, "it rises at once");

		rig.ServerLink!.Conditions = new NetworkConditions(0.01);
		double before = delay.Ticks;

		for (int i = 0; i < 30; i++)
		{
			rig.Step();
			time += 1.0 / TickRate;
			delay.Update(time);
		}

		delay.Ticks.Should().BeLessThan(before).And.BeGreaterThan(before - 2.1, "it falls by 4 ticks a second at most");
	}

	[Fact]
	[Trait("Networking", "LagCompensation")]
	public void The_server_rewinds_the_bodies_a_client_was_shown_but_not_its_own()
	{
		var rig = new Rig();
		Client client = rig.Join();
		Entity target = rig.Spawn();
		Entity mine = rig.Spawn(client.Peer);
		Entity exempt = rig.Spawn();
		rig.Server.Replication!.Find(exempt)!.LagCompensated = false;

		for (int i = 0; i < 20; i++)
		{
			long now = rig.Server.Clock.Tick;
			target.Body = new BodyState(new Vector3(now, 0, 0), Quaternion.Identity, default, true);
			mine.Body = target.Body;
			exempt.Body = target.Body;
			rig.Step();
		}

		long tick = rig.Server.Clock.Tick + 3;
		client.Session.SendInput(new InputCommand(tick, default) { ViewDelay = 6 });
		rig.Step(3);
		rig.Server.TakeInput(tick);

		LagCompensation compensation = rig.Server.Replication.LagCompensation;
		compensation.ViewDelay(client.Peer).Should().Be(6);
		compensation.ShownTickOf(client.Peer, tick).Should().Be(tick - 6);
		compensation.ShownTickOf(client.Peer, tick, 0.5).Should().Be(tick - 3);

		var bodies = new List<RewoundBody>();
		double shown = rig.Server.Clock.Tick - 8.5;
		compensation.Rewound(client.Peer, shown, bodies);
		bodies
			.Should().ContainSingle("its own body and an exempt one aren't rewound").Which.Entity.Should().Be(target);
		bodies[0].Body.Position.X
			.Should().BeApproximately((float)shown, 1e-4f, "it's between the sends on either side");

		client.Session.SendInput(new InputCommand(tick + 1, default) { ViewDelay = 100 });
		rig.Step();
		rig.Server.TakeInput(tick + 1);
		compensation.ViewDelay(client.Peer)
			.Should().Be(TickRate * 0.5, "an instigator further behind is rewound by the cap");
	}

	[Fact]
	[Trait("Networking", "LagCompensation")]
	public void A_placed_entity_its_instigator_owns_is_rewound_and_a_body_that_went_away_is_not()
	{
		var rig = new Rig();
		Client client = rig.Join();
		Entity placed = rig.Place(new NetworkEntityId(1000), client.Peer);
		placed.Body = new BodyState(Vector3.One, Quaternion.Identity, default, true);
		rig.Step(6);
		LagCompensation compensation = rig.Server.Replication!.LagCompensation;
		var bodies = new List<RewoundBody>();

		compensation.Rewound(client.Peer, rig.Server.Clock.Tick - 2, bodies);
		bodies.Should().ContainSingle("its owner's client shows it behind, as any mirror");

		placed.Body = null;
		rig.Step(2);

		compensation.TryRewind(placed, rig.Server.Clock.Tick - 4, out _).Should().BeFalse("its clients let go of it");
	}

	[Fact]
	[Trait("Networking", "LagCompensation")]
	public void The_server_keeps_bodies_as_far_back_as_it_rewinds()
	{
		var rig = new Rig();
		rig.Server.Replication!.SendInterval = 1;
		rig.Server.Replication.LagCompensation.MaxRewindSeconds = 2;
		Entity target = rig.Spawn();

		for (int i = 0; i < 150; i++)
		{
			target.Body = new BodyState(new Vector3(rig.Server.Clock.Tick, 0, 0), Quaternion.Identity, default, true);
			rig.Step();
		}

		double shown = rig.Server.Clock.Tick - 100.5;
		rig.Server.Replication.LagCompensation.TryRewind(target, shown, out BodyState body).Should().BeTrue();
		body.Position.X.Should().BeApproximately((float)shown, 1e-4f);
	}

	[Fact]
	[Trait("Networking", "LagCompensation")]
	public void A_rewound_effect_an_entity_had_as_it_was_registered_counts()
	{
		var rig = new Rig();
		var target = new Entity(rig.ServerTagsManager, rig.ServerCues)
		{
			NetworkId = rig.Server.Replication!.AllocateId(),
		};
		EffectData hit = HitData(rig);
		ActiveEffectHandle guard = Rig.Apply(target, GuardData(rig, hit, false, EffectLagCompensation.Rewound))!;
		rig.Server.Replication.Register(target, 0, PeerId.None);
		rig.Step(10);

		target.EffectsManager.RemoveEffect(guard);

		Lands(rig.Server.Replication.LagCompensation, target, hit, rig.Server.Clock.Tick - 2)
			.Should().BeFalse("the guard was up at the tick the instigator saw");
	}

	[Fact]
	[Trait("Networking", "LagCompensation")]
	public void A_check_a_blocked_hit_sets_off_leaves_the_rewound_tags_as_they_were()
	{
		var rig = new Rig();
		Entity target = rig.Spawn();
		EffectData hit = HitData(rig);
		rig.Step(10);
		long before = rig.Server.Clock.Tick;
		ActiveEffectHandle stun = Rig.Apply(target, GuardData(rig, hit, false, EffectLagCompensation.Rewound))!;
		Rig.Apply(target, GuardData(rig, hit, true, EffectLagCompensation.Current));
		int blocked = 0;
		target.EffectsManager.OnEffectApplicationBlocked += (_, _) =>
		{
			blocked++;
			Rig.Apply(target, Rig.Raise());
		};

		Lands(rig.Server.Replication!.LagCompensation, target, hit, before - 1);
		blocked.Should().Be(1, "the immunity turned the hit away, and its handler checked another application");
		target.EffectsManager.RemoveEffect(stun);

		target.Tags.AllTags.HasTag(rig.ServerTag("state.stunned")).Should().BeFalse("each check put back what it took");
	}

	[Theory]
	[Trait("Networking", "LagCompensation")]
	[InlineData(0, 0.25, 1)]
	[InlineData(0.125, 0.25, 0.5)]
	[InlineData(0.3, 0.25, 0)]
	[InlineData(0.01, 0, 0)]
	[InlineData(0, 0, 1)]
	[InlineData(5, double.PositiveInfinity, 1)]
	public void An_action_s_compensation_fades_as_it_runs(double elapsed, double fade, double remaining)
	{
		LagCompensation.Remaining(elapsed, fade).Should().Be(remaining);
	}

	[Theory]
	[Trait("Networking", "LagCompensation")]
	[InlineData(false)]
	[InlineData(true)]
	public void A_rewound_effect_decides_whether_a_hit_lands_as_the_instigator_saw_it(bool immunity)
	{
		var rig = new Rig();
		Entity target = rig.Spawn();
		EffectData hit = HitData(rig);
		EffectData guard = GuardData(rig, hit, immunity, EffectLagCompensation.Rewound);
		LagCompensation compensation = rig.Server.Replication!.LagCompensation;
		int changes = 0;
		target.Tags.OnTagsChanged += _ => changes++;

		rig.Step(10);
		long applied = rig.Server.Clock.Tick;
		ActiveEffectHandle handle = Rig.Apply(target, guard)!;

		Lands(compensation, target, hit, applied - 2).Should().BeTrue("the guard came up after the tick the instigator saw");
		Lands(compensation, target, hit, applied).Should().BeFalse();
		Rig.Apply(target, hit).Should().BeNull("outside a scope the guard is as it is");

		rig.Step(4);
		long removed = rig.Server.Clock.Tick;
		target.EffectsManager.RemoveEffect(handle);
		int settled = changes;

		Lands(compensation, target, hit, removed - 0.5).Should().BeFalse("the guard was still up at the tick the instigator saw");
		Lands(compensation, target, hit, removed).Should().BeTrue();
		target.Tags.AllTags.HasTag(rig.ServerTag("state.stunned")).Should().BeFalse();
		changes.Should().Be(settled, "the tags are put as they were without raising anything");
	}

	[Fact]
	[Trait("Networking", "LagCompensation")]
	public void An_effect_that_counts_as_it_is_protects_from_a_hit_aimed_before_it()
	{
		var rig = new Rig();
		Entity target = rig.Spawn();
		rig.Step(10);
		EffectData hit = HitData(rig);
		Rig.Apply(target, GuardData(rig, hit, false, EffectLagCompensation.Current));

		Lands(rig.Server.Replication!.LagCompensation, target, hit, rig.Server.Clock.Tick - 5).Should().BeFalse();
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void A_remote_body_jumps_between_states_further_apart_than_the_snap_distance()
	{
		var body = new RemoteBody();
		body.Add(10, new BodyState(Vector3.Zero, Quaternion.Identity, default, true));
		body.Add(12, new BodyState(new Vector3(1, 0, 0), Quaternion.Identity, default, true));
		body.Add(14, new BodyState(new Vector3(20, 0, 0), Quaternion.Identity, default, true));

		body.TrySample(11, out BodyState walking, 2).Should().BeTrue();
		walking.Position.X.Should().BeApproximately(0.5f, 1e-5f);
		body.TrySample(13.9, out BodyState respawning, 2).Should().BeTrue();
		respawning.Position.X.Should().Be(1, "a respawn isn't shown sliding to the spawn point");
		body.TrySample(14, out BodyState respawned, 2).Should().BeTrue();
		respawned.Position.X.Should().Be(20);

		body.Add(16, new BodyState(new Vector3(21, 0, 0), Quaternion.Identity, default, true));
		body.TrySample(14, out BodyState stillRespawned, 2).Should().BeTrue();
		stillRespawned.Position.X.Should().Be(20, "at its own tick a state is where the body is");
	}

	[Fact]
	[Trait("Networking", "Replication")]
	public void An_acknowledgement_covers_the_newest_packet_and_the_32_before_it()
	{
		var ack = new StateAckMessage(40, 0b101);

		ack.Acknowledges(40).Should().BeTrue();
		ack.Acknowledges(39).Should().BeTrue();
		ack.Acknowledges(38).Should().BeFalse();
		ack.Acknowledges(37).Should().BeTrue();
		ack.Acknowledges(41).Should().BeFalse();
		ack.Acknowledges(7).Should().BeFalse();
	}

	// Hit: an instant -1 to Attribute100's base value, turned away from a stunned target.
	private static EffectData HitData(Rig rig)
	{
		return new EffectData(
			"Hit",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					"TestAttributeSet.Attribute100",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(-1)))
			],
			effectComponents:
			[
				new TargetTagRequirementsEffectComponent(
					new TagRequirements(
						IgnoreTags: new TagContainer(rig.ServerTagsManager, [rig.ServerTag("state.stunned")])))
			]);
	}

	// Guard: grants state.stunned, or turns hits away as an immunity.
	private static EffectData GuardData(Rig rig, EffectData hit, bool immunity, EffectLagCompensation lagCompensation)
	{
		IEffectComponent component = immunity
			? new ImmunityEffectComponent([new EffectQuery(hit)])
			: new ModifierTagsEffectComponent(
				new TagContainer(rig.ServerTagsManager, [rig.ServerTag("state.stunned")]));

		return new EffectData(
			"Guard",
			new DurationData(DurationType.Infinite),
			effectComponents: [component],
			lagCompensation: lagCompensation);
	}

	private static bool Lands(LagCompensation compensation, Entity target, EffectData hit, double shownTick)
	{
		compensation.Begin(new PeerId(5), shownTick);

		try
		{
			bool landed = false;
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
			void OnApplied(EffectEvaluatedData _)
			{
				landed = true;
			}
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter

			target.EffectsManager.OnEffectApplied += OnApplied;
			Rig.Apply(target, hit);
			target.EffectsManager.OnEffectApplied -= OnApplied;
			return landed;
		}
		finally
		{
			compensation.End();
		}
	}

	private static int ObserverCount(CuesManager cues)
	{
		FieldInfo field = typeof(CuesManager).GetField("_observers", BindingFlags.NonPublic | BindingFlags.Instance)!;
		return ((System.Collections.ICollection)field.GetValue(cues)!).Count;
	}

	private static void ToggleSoaked(Entity entity, Dictionary<Entity, ActiveEffectHandle> soaked, Rig rig, bool on)
	{
		if (on && !soaked.ContainsKey(entity))
		{
			soaked[entity] = Rig.Apply(entity, rig.Soaked())!;
		}
		else if (!on && soaked.Remove(entity, out ActiveEffectHandle? handle))
		{
			entity.EffectsManager.RemoveEffect(handle);
		}
	}

	private static bool SameTags(Entity mirror, Entity entity, Client client, Rig rig)
	{
		return Array.TrueForAll(_tagNames, x =>
			mirror.Tags.AllTags.HasTag(client.Tag(x)) == entity.Tags.AllTags.HasTag(rig.ServerTag(x)));
	}

	private static BodyState Rounded(BodyState state)
	{
		return BodyCodec.Dequantize(BodyCodec.Quantize(state, BodyPrecision.Default), BodyPrecision.Default);
	}

	private static NetworkAssetRegistry Registry()
	{
		var registry = new NetworkAssetRegistry();
		registry.Register(AssetKind.Scene, "uid://creature", "Creature");
		registry.Seal();
		return registry;
	}

	private static (CuesManager Cues, RecordingCueHandler Fire, RecordingCueHandler Hit, RecordingCueHandler Regen)
		CreateCues(TagsManager tags)
	{
		var cues = new CuesManager();
		var fire = new RecordingCueHandler();
		var hit = new RecordingCueHandler();
		var regen = new RecordingCueHandler();
		cues.RegisterCue(Tag.RequestTag(tags, "cue.fire"), fire);
		cues.RegisterCue(Tag.RequestTag(tags, "cue.hit"), hit);
		cues.RegisterCue(Tag.RequestTag(tags, "cue.regen"), regen);
		return (cues, fire, hit, regen);
	}

	private sealed class Rig
	{
		private readonly List<Client> _clients = [];
		private readonly int _seed;
		private double _time;

		public LoopbackNetwork Network { get; } = new();

		public NetworkSession Server { get; }

		public ConditionedTransport? ServerLink { get; }

		public TagsManager ServerTagsManager { get; }

		public CuesManager ServerCues { get; }

		public Rig(NetworkConditions? down = null, int seed = 1)
		{
			_seed = seed;
			INetworkTransport transport = Network.Server;

			if (down is NetworkConditions conditions)
			{
				ServerLink = new ConditionedTransport(transport, conditions, new DeterministicRandom((ulong)seed));
				transport = ServerLink;
			}

			ServerTagsManager = new TagsManager(_tagNames);
			ServerCues = CreateCues(ServerTagsManager).Cues;
			Server = new NetworkSession(
				transport,
				new SimulationClock(TickRate, 100),
				Registry(),
				ServerTagsManager,
				_actions);
		}

		public static ActiveEffectHandle? Apply(Entity entity, EffectData data)
		{
			return entity.EffectsManager.ApplyEffect(new Effect(data, new EffectOwnership(entity, entity)));
		}

		// Raise: an instant +1 to Attribute100's base value.
		public static EffectData Raise()
		{
			return new EffectData(
				"Raise",
				new DurationData(DurationType.Instant),
				[
					new Modifier(
						"TestAttributeSet.Attribute100",
						ModifierOperation.FlatBonus,
						new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(1)))
				]);
		}

		public Tag ServerTag(string name)
		{
			return Tag.RequestTag(ServerTagsManager, name);
		}

		public Client Join(NetworkConditions? up = null, params NetworkEntityId[] placed)
		{
			var client = new Client(Network.Connect(), up, (ulong)(_seed * 31) + (ulong)_clients.Count);
			_clients.Add(client);

			// Placed entities are bound before the session is ready, as a host binds its scene's.
			foreach (NetworkEntityId id in placed)
			{
				client.Place(id);
			}

			for (int i = 0; i < 30 && client.Session.State != SessionState.Ready; i++)
			{
				Step();
			}

			client.Session.State.Should().Be(SessionState.Ready);
			return client;
		}

		public Entity Spawn(PeerId? owner = null)
		{
			var entity = new Entity(ServerTagsManager, ServerCues)
			{
				NetworkId = Server.Replication!.AllocateId(),
			};
			Server.Replication.Register(entity, 0, owner ?? PeerId.None);
			return entity;
		}

		public Entity Place(NetworkEntityId id, PeerId? owner = null)
		{
			var entity = new Entity(ServerTagsManager, ServerCues) { NetworkId = id };
			Server.Replication!.Register(entity, -1, owner ?? PeerId.None);
			return entity;
		}

		// Burning: +10 to Attribute1, the state.burning tag, and the cue.fire persistent cue.
		public EffectData Burning()
		{
			return Buff("Burning", "state.burning", "cue.fire", "TestAttributeSet.Attribute1", 10);
		}

		// Soaked: +3 to Attribute2, the state.wet tag, and the cue.regen persistent cue.
		public EffectData Soaked()
		{
			return Buff("Soaked", "state.wet", "cue.regen", "TestAttributeSet.Attribute2", 3);
		}

		// One tick on every machine: what's in flight arrives, clients acknowledge, the server takes the acks and
		// sends.
		public void Step(int ticks = 1)
		{
			for (int i = 0; i < ticks; i++)
			{
				_time += 1.0 / TickRate;
				ServerLink?.Update(_time);

				foreach (Client client in _clients)
				{
					client.Update(_time);
				}

				Server.Update(_time);
				Server.Replication!.Send(Server.Clock.Tick);
				Server.Clock.Advance();
			}
		}

		private EffectData Buff(string name, string tag, string cue, string attribute, int amount)
		{
			return new EffectData(
				name,
				new DurationData(DurationType.Infinite),
				[
					new Modifier(
						attribute,
						ModifierOperation.FlatBonus,
						new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(amount)))
				],
				effectComponents:
				[
					new ModifierTagsEffectComponent(new TagContainer(ServerTagsManager, [ServerTag(tag)]))
				],
				cues:
				[
					new CueData(
						new TagContainer(ServerTagsManager, [ServerTag(cue)]),
						0,
						10,
						CueMagnitudeType.EffectLevel)
				]);
		}
	}

	private sealed class Client : IReplicationHost
	{
		private readonly ConditionedTransport? _link;

		public NetworkSession Session { get; }

		public PeerId Peer { get; }

		public TagsManager Tags { get; }

		public CuesManager Cues { get; }

		public RecordingCueHandler Fire { get; }

		public RecordingCueHandler Hit { get; }

		public RecordingCueHandler Regen { get; }

		public List<Entity> Spawned { get; } = [];

		public int Despawns { get; private set; }

		public Client(INetworkTransport transport, NetworkConditions? up, ulong seed)
		{
			if (up is NetworkConditions conditions)
			{
				_link = new ConditionedTransport(transport, conditions, new DeterministicRandom(seed));
				transport = _link;
			}

			Tags = new TagsManager(_tagNames);
			(Cues, Fire, Hit, Regen) = CreateCues(Tags);
			Session = new NetworkSession(transport, new SimulationClock(TickRate), Registry(), Tags, _actions);
			Session.Mirror!.Host = this;
			Peer = transport.LocalPeer;
		}

		public Tag Tag(string name)
		{
			return Forge.Tags.Tag.RequestTag(Tags, name);
		}

		public Entity Only()
		{
			return Spawned.Should().ContainSingle().Subject;
		}

		public Entity Find(Entity serverEntity)
		{
			return (Entity)Session.Mirror!.Find(serverEntity.NetworkId)!;
		}

		public void Place(NetworkEntityId id)
		{
			Session.Mirror!.BindPlaced(new Entity(Tags, Cues) { NetworkId = id });
		}

		public void Update(double time)
		{
			_link?.Update(time);
			Session.Update(time);
		}

		public IForgeEntity? Spawn(NetworkEntityId id, int sceneIndex, PeerId owner)
		{
			var entity = new Entity(Tags, Cues) { NetworkId = id };
			Spawned.Add(entity);
			return entity;
		}

		public void Despawn(IForgeEntity entity)
		{
			Spawned.Remove((Entity)entity);
			Despawns++;
		}
	}

	private sealed class Entity : IForgeEntity
	{
		public NetworkEntityId NetworkId { get; set; }

		public TestAttributeSet Set { get; } = new();

		public EntityAttributes Attributes { get; }

		public EntityTags Tags { get; }

		public EffectsManager EffectsManager { get; }

		public CuesManager CuesManager { get; }

		public EntityAbilities Abilities { get; }

		public EventManager Events { get; } = new();

		public Variables SharedVariables { get; } = new();

		public EntityInput? Input { get; set; }

		public BodyState? Body { get; set; }

		public IEntityMover? Mover => Body is null ? null : new FixedMover(this);

		public Entity(TagsManager tags, CuesManager cues)
		{
			Attributes = new EntityAttributes(this, Set);
			Tags = new EntityTags(new TagContainer(tags));
			EffectsManager = new EffectsManager(this, cues);
			CuesManager = cues;
			Abilities = new EntityAbilities(this);
		}
	}

	private sealed class SwitchedRelevancy : IRelevancy
	{
		public bool Relevant { get; set; } = true;

		public bool IsRelevant(PeerId peer, ReplicatedEntity entity, ReplicationServer server)
		{
			return Relevant;
		}
	}

	private sealed class FixedMover(Entity entity) : IEntityMover
	{
		public void Move(double fixedDeltaTime)
		{
		}

		public BodyState CaptureState()
		{
			return entity.Body!.Value;
		}

		public void RestoreState(in BodyState state)
		{
			entity.Body = state;
		}
	}
}
