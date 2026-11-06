// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Effects.Periodic;
using Gamesmiths.Forge.Effects.Stacking;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Networking;
using Gamesmiths.Forge.Networking.Messages;
using Gamesmiths.Forge.Networking.Replication;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Networking;

/// <summary>
/// Effects reach a client as what they are: a mirror holds the server's active effects and answers queries about them
/// as the server would, while nothing of them runs there. How much of them is left follows the server's tick,
/// visibility decides who hears about which, the owner gets its base values, and custom data travels by the codecs
/// registered for it.
/// </summary>
public class EffectReplicationTests
{
	private const int TickRate = 60;

	private const double TickLength = 1.0 / TickRate;

	private static readonly string[] _tagNames =
		["state.burning", "state.secret", "kind.fire", "set.power", "cue.fire", "cue.tick"];

	private static readonly InputActionSet _actions = new(["fire"]);

	[Fact]
	[Trait("Networking", "Effects")]
	public void A_mirror_answers_queries_as_the_server_does()
	{
		var rig = new Rig();
		Client client = rig.Join();
		Entity entity = rig.Spawn();
		Entity source = rig.Spawn();
		ActiveEffectHandle handle = Rig.Apply(entity, rig.Assets.Burning, source, level: 2)!;

		rig.Step(6);

		Entity mirror = client.Find(entity);
		Entity mirroredSource = client.Find(source);
		ActiveEffectHandle mirrored = mirror.EffectsManager.GetActiveEffects().Should().ContainSingle().Subject;
		mirrored.IsValid.Should().BeTrue();
		mirrored.Target.Should().BeSameAs(mirror);
		mirrored.Effect!.EffectData.Should().Be(client.Assets.Burning);
		mirrored.Effect.Ownership.Source.Should().BeSameAs(mirroredSource);
		mirrored.Effect.Ownership.Owner.Should().BeSameAs(mirroredSource);
		mirrored.Level.Should().Be(handle.Level);
		mirrored.StackCount.Should().Be(handle.StackCount);
		mirrored.TotalDuration.Should().Be(handle.TotalDuration);
		mirrored.Period.Should().Be(handle.Period);
		mirrored.IsInhibited.Should().BeFalse();

		EntityEffects(mirror, new EffectQuery(EffectDefinition: client.Assets.Burning))
			.Should().ContainSingle();
		EntityEffects(mirror, new EffectQuery(EffectDefinition: client.Assets.Blessed))
			.Should().BeEmpty();
		EntityEffects(mirror, new EffectQuery(EffectSource: mirroredSource))
			.Should().ContainSingle();
		EntityEffects(mirror, new EffectQuery(EffectSource: mirror))
			.Should().BeEmpty();
		EntityEffects(mirror, new EffectQuery(ModifyingAttribute: "TestAttributeSet.Attribute90"))
			.Should().ContainSingle();
		EntityEffects(mirror, new EffectQuery(EffectTagQuery: client.AnyTag("kind.fire")))
			.Should().ContainSingle();
		EntityEffects(mirror, new EffectQuery(GrantedTagQuery: client.AnyTag("state.burning")))
			.Should().ContainSingle();
		mirror.EffectsManager.HasAnyActiveEffect(new EffectQuery(OwningTagQuery: client.AnyTag("state.burning")))
			.Should().BeTrue();
		mirror.EffectsManager.GetEffectStackData(client.Assets.Burning)
			.Should().Equal(new EffectStackInstanceData(mirroredSource, 2, 1));
	}

	[Fact]
	[Trait("Networking", "Effects")]
	public void Nothing_of_an_effect_runs_on_a_mirror()
	{
		var rig = new Rig();
		Client client = rig.Join();
		Entity entity = rig.Spawn();
		ActiveEffectHandle handle = Rig.Apply(entity, rig.Assets.Burning)!;
		Rig.Apply(entity, rig.Assets.Blessed);

		rig.Step(130);

		Entity mirror = client.Only();
		mirror.EffectsManager.GetActiveEffects().Should().HaveCount(2);
		handle.ExecutionCount.Should().BeGreaterThanOrEqualTo(2);
		mirror.EffectsManager.GetActiveEffects().First().ExecutionCount.Should().Be(handle.ExecutionCount);
		rig.Assets.Counter.Added.Should().Be(1);
		client.Assets.Counter.Added.Should().Be(0, "a mirror's effects run no components");
		mirror.EffectsManager.GetActiveEffects().First().ComponentInstances.Should().BeEmpty();
		mirror.Set.Attribute1.CurrentValue.Should().Be(entity.Set.Attribute1.CurrentValue);
		mirror.Set.Attribute1.Modifier.Should().Be(0, "this client isn't the owner, and a mirror applies no modifiers");
		client.Tick.ExecuteCount.Should().Be(handle.ExecutionCount, "each execution reaches the client once, as a cue");
		client.Fire.ApplyCount.Should().Be(1, "the persistent cue arrives on its own, and the effect adds none");
	}

	[Fact]
	[Trait("Networking", "Effects")]
	public void What_s_left_of_an_effect_follows_the_server_with_nothing_sent_for_it()
	{
		var rig = new Rig();
		Client client = rig.Join();
		Entity entity = rig.Spawn();
		ActiveEffectHandle handle = Rig.Apply(entity, rig.Assets.Warded)!;
		rig.Step(10);

		ActiveEffectHandle mirrored = client.Only().EffectsManager.GetActiveEffects().Should().ContainSingle().Subject;
		mirrored.RemainingDuration.Should().BeApproximately(handle.RemainingDuration, 4 * TickLength);

		long packets = rig.Server.Replication!.PacketsSent;
		long bytes = rig.Server.Replication.BytesSent;
		rig.Step(60);

		long bytesPerPacket = (rig.Server.Replication.BytesSent - bytes)
			/ (rig.Server.Replication.PacketsSent - packets);
		bytesPerPacket.Should().BeLessThan(8, "an effect that's only running changes nothing");
		handle.RemainingDuration.Should().BeLessThan(4);
		mirrored.RemainingDuration.Should().BeApproximately(handle.RemainingDuration, 4 * TickLength);
	}

	[Fact]
	[Trait("Networking", "Effects")]
	public void A_late_joiner_hears_how_much_of_an_effect_is_left_and_nothing_of_one_that_ended()
	{
		var rig = new Rig();
		Client early = rig.Join();
		Entity entity = rig.Spawn();
		ActiveEffectHandle running = Rig.Apply(entity, rig.Assets.Burning)!;
		ActiveEffectHandle ended = Rig.Apply(entity, rig.Assets.Blessed)!;
		rig.Step(60);
		entity.EffectsManager.RemoveEffect(ended);
		rig.Step(60);

		var added = new List<ActiveEffectHandle>();
		var removed = new List<ActiveEffectHandle>();
		Client late = rig.Join(spawning: mirror =>
		{
			mirror.EffectsManager.OnActiveEffectAdded += added.Add;
			mirror.EffectsManager.OnActiveEffectRemoved += (handle, _) => removed.Add(handle);
		});

		rig.Step(4);

		added.Should().ContainSingle().Which.Effect!.EffectData.Should().Be(late.Assets.Burning);
		removed.Should().BeEmpty();
		ActiveEffectHandle mirrored = late.Only().EffectsManager.GetActiveEffects().Should().ContainSingle().Subject;
		running.RemainingDuration.Should().BeLessThan(3.5);
		mirrored.RemainingDuration.Should().BeApproximately(running.RemainingDuration, 4 * TickLength);
		early.Only().EffectsManager.GetActiveEffects().Should().ContainSingle();
	}

	[Fact]
	[Trait("Networking", "Effects")]
	public void A_mirror_raises_what_follows_an_effect_only_when_it_changed()
	{
		var rig = new Rig();
		Client client = rig.Join(up: new NetworkConditions(0.1));
		Entity entity = rig.Spawn();
		rig.Step(6);

		int added = 0;
		int changed = 0;
		int applied = 0;
		EffectsManager effects = client.Only().EffectsManager;
		effects.OnActiveEffectAdded += _ => added++;
		effects.OnActiveEffectChanged += _ => changed++;
		effects.OnEffectApplied += _ => applied++;

		Effect effect = Rig.Effect(rig.Assets.Burning, entity);
		ActiveEffectHandle handle = entity.EffectsManager.ApplyEffect(effect)!;
		rig.Step(30);
		added.Should().Be(1, "copies of it arrive until the slow acknowledgement does, and one is enough");
		changed.Should().Be(0);

		entity.EffectsManager.ApplyEffect(effect);
		rig.Step(30);
		handle.StackCount.Should().Be(2);
		ActiveEffectHandle mirrored = effects.GetActiveEffects().Should().ContainSingle().Subject;
		mirrored.StackCount.Should().Be(2);
		effects.GetEffectStackData(client.Assets.Burning).Single().StackCount.Should().Be(2);
		changed.Should().Be(1);
		applied.Should().Be(0, "only what follows an active effect is raised: instant effects never get here");
	}

	[Fact]
	[Trait("Networking", "Effects")]
	public void An_effect_ends_on_a_mirror_when_the_server_says_and_says_how()
	{
		var rig = new Rig();
		Client client = rig.Join();
		Entity entity = rig.Spawn();
		rig.Step(4);

		var ended = new List<(string Name, EffectRemovalReason Reason, bool WasValid)>();
		EffectsManager effects = client.Only().EffectsManager;
		effects.OnActiveEffectRemoved += (handle, reason) =>
			ended.Add((handle.Effect!.EffectData.Name, reason, handle.IsValid));

		ActiveEffectHandle removed = Rig.Apply(entity, rig.Assets.Blessed)!;
		Rig.Apply(entity, rig.Assets.Burning);
		rig.Step(6);
		ActiveEffectHandle mirroredBlessed = effects.GetActiveEffects().First();

		entity.EffectsManager.RemoveEffect(removed);
		rig.Step(6);
		ended.Should().Equal(("Blessed", EffectRemovalReason.Removed, true));
		mirroredBlessed.IsValid.Should().BeFalse("the handle goes with the effect, as it does on the server");

		rig.Step(5 * TickRate);
		entity.EffectsManager.GetActiveEffects().Should().BeEmpty();
		ended.Should().Equal(
			("Blessed", EffectRemovalReason.Removed, true),
			("Burning", EffectRemovalReason.Expired, true));
		effects.GetActiveEffects().Should().BeEmpty();
	}

	[Fact]
	[Trait("Networking", "Effects")]
	public void What_s_left_never_ends_an_effect_on_a_mirror()
	{
		var rig = new Rig(down: new NetworkConditions(0.01));
		Client client = rig.Join();
		Entity entity = rig.Spawn();
		Rig.Apply(entity, rig.Assets.Burning);
		rig.Step(10);
		ActiveEffectHandle mirrored = client.Only().EffectsManager.GetActiveEffects().Should().ContainSingle().Subject;

		rig.ServerLink!.Conditions = new NetworkConditions(0.01, 0, 1);
		rig.Step(6 * TickRate);

		mirrored.IsValid.Should().BeTrue("nothing has said it ended");
		client.Only().EffectsManager.UpdateEffects(10);
		mirrored.IsValid.Should().BeTrue("a mirror's effects don't advance");
		mirrored.ExecutionCount.Should().BeLessThanOrEqualTo(1);

		rig.ServerLink.Conditions = new NetworkConditions(0.01);
		rig.Step(10);
		mirrored.IsValid.Should().BeFalse();
	}

	[Fact]
	[Trait("Networking", "Effects")]
	public void A_mirror_refuses_whatever_would_change_what_the_server_says()
	{
		var rig = new Rig();
		Client client = rig.Join();
		Entity entity = rig.Spawn();
		Rig.Apply(entity, rig.Assets.Burning);
		rig.Step(6);

		Entity mirror = client.Only();
		ActiveEffectHandle mirrored = mirror.EffectsManager.GetActiveEffects().Single();
		double remaining = mirrored.RemainingDuration;

		mirrored.SetInhibit(true);
		mirrored.RefreshDuration();
		mirror.EffectsManager.RemoveEffect(mirrored);
		mirror.EffectsManager.RemoveEffect(mirrored, 1);
		mirror.EffectsManager.RemoveEffectData(client.Assets.Burning, forceRemoval: true);
		mirror.EffectsManager.RemoveEffects(new EffectQuery(EffectDefinition: client.Assets.Burning));
		mirror.EffectsManager.ApplyEffect(new Effect(client.Assets.Blessed, default)).Should().BeNull();

		mirror.EffectsManager.GetActiveEffects().Should().ContainSingle().Which.Should().BeSameAs(mirrored);
		mirrored.IsInhibited.Should().BeFalse();
		mirrored.StackCount.Should().Be(1);
		mirrored.RemainingDuration.Should().Be(remaining);
	}

	[Fact]
	[Trait("Networking", "Effects")]
	public void Inhibition_reaches_a_mirror()
	{
		var rig = new Rig();
		Client client = rig.Join();
		Entity entity = rig.Spawn();
		ActiveEffectHandle handle = Rig.Apply(entity, rig.Assets.Blessed)!;
		rig.Step(6);
		ActiveEffectHandle mirrored = client.Only().EffectsManager.GetActiveEffects().Single();

		handle.SetInhibit(true);
		rig.Step(6);

		mirrored.IsInhibited.Should().BeTrue();
		client.Only().Set.Attribute1.CurrentValue.Should().Be(entity.Set.Attribute1.CurrentValue);
	}

	[Fact]
	[Trait("Networking", "Effects")]
	public void A_mirror_torn_down_lets_its_effects_go_with_no_word_to_its_cues()
	{
		var rig = new Rig();
		Client client = rig.Join();
		Entity entity = rig.Spawn();
		Rig.Apply(entity, rig.Assets.Burning);
		rig.Step(6);
		Entity mirror = client.Only();
		var reasons = new List<EffectRemovalReason>();
		mirror.EffectsManager.OnActiveEffectRemoved += (_, reason) => reasons.Add(reason);

		EntitySimulation.Teardown(mirror);

		reasons.Should().Equal(EffectRemovalReason.Destroyed);
		mirror.EffectsManager.GetActiveEffects().Should().BeEmpty();
		client.Fire.RemoveCount.Should().Be(0, "a mirror's cues come and go as the server's do");

		rig.Step(6);
		client.Session.MalformedPackets.Should().Be(0);
	}

	[Fact]
	[Trait("Networking", "Effects")]
	public void A_despawned_mirror_lets_its_effects_go()
	{
		var rig = new Rig();
		Client client = rig.Join();
		Entity entity = rig.Spawn();
		Rig.Apply(entity, rig.Assets.Blessed);
		rig.Step(6);
		Entity mirror = client.Only();
		ActiveEffectHandle mirrored = mirror.EffectsManager.GetActiveEffects().Single();
		var reasons = new List<EffectRemovalReason>();
		mirror.EffectsManager.OnActiveEffectRemoved += (_, reason) => reasons.Add(reason);

		rig.Server.Replication!.Unregister(entity);
		rig.Step(6);

		reasons.Should().Equal(EffectRemovalReason.Destroyed);
		mirrored.IsValid.Should().BeFalse();
	}

	[Fact]
	[Trait("Networking", "Effects")]
	public void Visibility_decides_who_hears_about_an_effect_and_never_what_it_did()
	{
		var rig = new Rig();
		Client owner = rig.Join();
		Client other = rig.Join();
		Entity entity = rig.Spawn(owner.Peer);
		Rig.Apply(entity, rig.Assets.Blessed);
		ActiveEffectHandle secret = Rig.Apply(entity, rig.Assets.Secret)!;
		Rig.Apply(entity, rig.Assets.Hidden);

		rig.Step(6);

		Names(owner.Find(entity)).Should().Equal("Blessed", "Secret");
		Names(other.Find(entity)).Should().Equal("Blessed");

		Entity seen = other.Find(entity);
		seen.Tags.AllTags.HasTag(other.Tag("state.secret")).Should().BeTrue("tags are the entity's, whoever gave them");
		seen.Set.Attribute2.CurrentValue.Should().Be(entity.Set.Attribute2.CurrentValue);
		seen.Set.Attribute2.CurrentValue.Should().BeGreaterThan(2, "the hidden effect's modifier shows in the result");

		entity.EffectsManager.RemoveEffect(secret);
		rig.Step(6);

		Names(owner.Find(entity)).Should().Equal("Blessed");
		seen.Tags.AllTags.HasTag(other.Tag("state.secret")).Should().BeFalse();
		owner.Session.MalformedPackets.Should().Be(0);
		other.Session.MalformedPackets.Should().Be(0);
	}

	[Fact]
	[Trait("Networking", "Effects")]
	public void An_effect_that_says_nothing_goes_by_the_server_s_default()
	{
		var rig = new Rig();
		Client client = rig.Join();
		rig.Server.Replication!.DefaultEffectVisibility = EffectVisibility.ServerOnly;
		Entity entity = rig.Spawn();
		Rig.Apply(entity, rig.Assets.Blessed);
		rig.Step(6);
		Names(client.Only()).Should().BeEmpty();

		rig.Server.Replication.DefaultEffectVisibility = EffectVisibility.Public;
		Rig.Apply(entity, rig.Assets.Scaled);
		rig.Step(6);
		Names(client.Only()).Should().Equal("Scaled");
	}

	[Fact]
	[Trait("Networking", "Effects")]
	public void An_effect_the_registry_does_not_list_stays_on_the_server_and_says_so_once()
	{
		var rig = new Rig();
		Client client = rig.Join();
		Entity entity = rig.Spawn();
		var notListed = new List<string>();
		rig.Server.Replication!.EffectNotListed += data => notListed.Add(data.Name);

		ActiveEffectHandle first = Rig.Apply(entity, rig.Assets.Unlisted)!;
		rig.Step(6);
		entity.EffectsManager.RemoveEffect(first);
		rig.Step(6);
		Rig.Apply(entity, rig.Assets.Unlisted);
		rig.Step(6);

		notListed.Should().Equal("Unlisted");
		Names(client.Only()).Should().BeEmpty();
		client.Session.MalformedPackets.Should().Be(0);
	}

	[Fact]
	[Trait("Networking", "Effects")]
	public void The_owner_gets_base_values_and_everyone_else_the_result()
	{
		var rig = new Rig();
		Client owner = rig.Join();
		Client other = rig.Join();
		Entity entity = rig.Spawn(owner.Peer);
		ActiveEffectHandle blessed = Rig.Apply(entity, rig.Assets.Blessed)!;

		rig.Step(6);

		Entity mine = owner.Find(entity);
		Entity theirs = other.Find(entity);
		mine.Set.Attribute1.BaseValue.Should().Be(1);
		mine.Set.Attribute1.CurrentValue.Should().Be(11);
		mine.Set.Attribute1.Modifier.Should().Be(10);
		theirs.Set.Attribute1.BaseValue.Should().Be(11);
		theirs.Set.Attribute1.CurrentValue.Should().Be(11);
		theirs.Set.Attribute1.Modifier.Should().Be(0);

		Rig.Apply(entity, rig.Assets.Raise);
		rig.Step(6);
		mine.Set.Attribute1.BaseValue.Should().Be(2);
		mine.Set.Attribute1.CurrentValue.Should().Be(12);
		theirs.Set.Attribute1.BaseValue.Should().Be(12);

		entity.EffectsManager.RemoveEffect(blessed);
		rig.Step(6);
		mine.Set.Attribute1.BaseValue.Should().Be(2);
		mine.Set.Attribute1.CurrentValue.Should().Be(2);
		mine.Set.Attribute1.Modifier.Should().Be(0);
		theirs.Set.Attribute1.BaseValue.Should().Be(2);
	}

	[Fact]
	[Trait("Networking", "Effects")]
	public void A_base_value_that_changes_with_the_result_held_still_reaches_only_the_owner()
	{
		var rig = new Rig();
		Client owner = rig.Join();
		Client other = rig.Join();
		Entity entity = rig.Spawn(owner.Peer);
		Rig.Apply(entity, rig.Assets.Pinned);
		rig.Step(6);
		owner.Find(entity).Set.Attribute1.CurrentValue.Should().Be(50);

		Rig.Apply(entity, rig.Assets.Raise);
		rig.Step(6);

		entity.Set.Attribute1.CurrentValue.Should().Be(50);
		entity.Set.Attribute1.BaseValue.Should().Be(2);
		owner.Find(entity).Set.Attribute1.BaseValue.Should().Be(2);
		other.Find(entity).Set.Attribute1.BaseValue.Should().Be(50);
	}

	[Fact]
	[Trait("Networking", "Effects")]
	public void Set_by_caller_values_reach_a_mirror()
	{
		var rig = new Rig();
		Client client = rig.Join();
		Entity entity = rig.Spawn();
		Effect effect = Rig.Effect(rig.Assets.Scaled, entity);
		effect.SetSetByCallerMagnitude(rig.Tag("set.power"), 7);
		entity.EffectsManager.ApplyEffect(effect);
		rig.Step(6);

		ActiveEffectHandle mirrored = client.Only().EffectsManager.GetActiveEffects().Single();
		mirrored.Effect!.DataTag.Should().ContainKey(client.Tag("set.power")).WhoseValue.Should().Be(7);
		int changed = 0;
		client.Only().EffectsManager.OnActiveEffectChanged += _ => changed++;

		effect.SetSetByCallerMagnitude(rig.Tag("set.power"), 9.5f);
		rig.Step(6);

		mirrored.Effect.DataTag.Should().ContainKey(client.Tag("set.power")).WhoseValue.Should().Be(9.5f);
		client.Only().Set.Attribute3.CurrentValue.Should().Be(entity.Set.Attribute3.CurrentValue);
		changed.Should().Be(1);
	}

	[Fact]
	[Trait("Networking", "Effects")]
	public void Custom_data_travels_by_the_codec_registered_for_it()
	{
		var rig = new Rig();
		Client client = rig.Join();
		Entity entity = rig.Spawn();

		ActiveEffectHandle handle =
			entity.EffectsManager.ApplyEffect(Rig.Effect(rig.Assets.Blessed, entity), new HitData(7, "north"))!;
		rig.Step(6);

		handle.TryGetContextData(out HitData? onServer).Should().BeTrue();
		onServer.Should().Be(new HitData(7, "north"));
		ActiveEffectHandle mirrored = client.Only().EffectsManager.GetActiveEffects().Single();
		mirrored.TryGetContextData(out HitData? onClient).Should().BeTrue();
		onClient.Should().Be(new HitData(7, "north"));
		mirrored.TryGetContextData(out string? wrongType).Should().BeFalse();
		wrongType.Should().BeNull();
		client.Session.MalformedPackets.Should().Be(0);
	}

	[Fact]
	[Trait("Networking", "Effects")]
	public void Custom_data_with_no_codec_stays_on_the_server()
	{
		var rig = new Rig();
		Client client = rig.Join();
		Entity entity = rig.Spawn();

		ActiveEffectHandle handle =
			entity.EffectsManager.ApplyEffect(Rig.Effect(rig.Assets.Blessed, entity), new UnsentData(3))!;
		rig.Step(6);

		handle.TryGetContextData(out UnsentData? onServer).Should().BeTrue();
		onServer!.Value.Should().Be(3);
		ActiveEffectHandle mirrored = client.Only().EffectsManager.GetActiveEffects().Single();
		mirrored.TryGetContextData(out UnsentData? onClient).Should().BeFalse();
		onClient.Should().BeNull();
		client.Session.MalformedPackets.Should().Be(0);
	}

	[Fact]
	[Trait("Networking", "Effects")]
	public void A_client_with_other_codecs_is_refused()
	{
		var network = new LoopbackNetwork();
		var assets = new Assets();
		var other = new NetworkAssetRegistry();
		other.Register(AssetKind.Scene, "uid://creature", "Creature");
		other.Seal();
		var server = new NetworkSession(
			network.Server,
			new SimulationClock(TickRate),
			assets.Registry,
			assets.Tags,
			_actions);
		var client = new NetworkSession(
			network.Connect(),
			new SimulationClock(TickRate),
			other,
			assets.Tags,
			_actions);

		for (int i = 0; i < 4; i++)
		{
			client.Update(i * TickLength);
			server.Update(i * TickLength);
		}

		client.Refusal.Should().Be(RefusalReason.AssetRegistry);
	}

	[Fact]
	[Trait("Networking", "Effects")]
	public void An_effect_s_source_is_whatever_the_client_has_of_it()
	{
		var rig = new Rig();
		var relevancy = new SwitchedRelevancy();
		rig.Server.Replication!.Relevancy = relevancy;
		Client client = rig.Join();
		Entity source = rig.Spawn();
		relevancy.Hidden = source;
		Entity entity = rig.Spawn();
		Rig.Apply(entity, rig.Assets.Blessed, source);

		rig.Step(6);
		ActiveEffectHandle mirrored = client.Find(entity).EffectsManager.GetActiveEffects().Single();
		mirrored.Effect!.Ownership.Source.Should().BeNull("this client has nothing of it yet");

		relevancy.Hidden = null;
		rig.Step(6);
		mirrored.Effect.Ownership.Source.Should().BeSameAs(client.Find(source));
		mirrored.Effect.Ownership.Owner.Should().BeSameAs(client.Find(source));
		client.Find(entity).EffectsManager
			.GetActiveEffects(new EffectQuery(EffectSource: client.Find(source))).Should().ContainSingle();

		relevancy.Hidden = source;
		rig.Step(6);
		mirrored.Effect.Ownership.Source.Should().BeNull("it left, and what's left of it is nothing to query");
	}

	[Fact]
	[Trait("Networking", "Effects")]
	public void An_effect_that_comes_back_into_relevancy_comes_back_whole()
	{
		var rig = new Rig();
		var relevancy = new SwitchedRelevancy();
		rig.Server.Replication!.Relevancy = relevancy;
		Client client = rig.Join();
		Entity entity = rig.Spawn();
		ActiveEffectHandle handle = Rig.Apply(entity, rig.Assets.Burning)!;
		rig.Step(6);
		client.Only().EffectsManager.GetActiveEffects().Should().ContainSingle();

		relevancy.Hidden = entity;
		rig.Step(6);
		client.Spawned.Should().BeEmpty();

		relevancy.Hidden = null;
		rig.Step(6);

		ActiveEffectHandle mirrored = client.Only().EffectsManager.GetActiveEffects().Should().ContainSingle().Subject;
		mirrored.RemainingDuration.Should().BeApproximately(handle.RemainingDuration, 4 * TickLength);
		mirrored.ExecutionCount.Should().Be(handle.ExecutionCount);
	}

	[Fact]
	[Trait("Networking", "Effects")]
	public void Under_loss_and_reordering_a_mirror_ends_up_holding_what_the_server_holds()
	{
		var rig = new Rig(down: new NetworkConditions(0.05, 0.04, 0.2f), seed: 3);
		Client client = rig.Join(up: new NetworkConditions(0.05, 0.04, 0.2f));
		rig.Server.Replication!.BudgetBytes = 48;
		var entities = new List<Entity>();

		for (int i = 0; i < 6; i++)
		{
			entities.Add(rig.Spawn());
		}

		var random = new DeterministicRandom(17);
		int added = 0;
		int removed = 0;
		client.Spawning = mirror =>
		{
			mirror.EffectsManager.OnActiveEffectAdded += _ => added++;
			mirror.EffectsManager.OnActiveEffectRemoved += (_, _) => removed++;
		};

		for (int step = 0; step < 400; step++)
		{
			Entity entity = entities[random.NextInt(entities.Count)];
			ActiveEffectHandle[] active = [.. entity.EffectsManager.GetActiveEffects()];

			if (active.Length > 0 && random.NextInt(3) == 0)
			{
				entity.EffectsManager.RemoveEffect(active[random.NextInt(active.Length)], forceRemoval: true);
			}
			else if (step % 5 == 0)
			{
				Rig.Apply(entity, random.NextInt(2) == 0 ? rig.Assets.Burning : rig.Assets.Blessed);
			}

			rig.Step();
		}

		rig.Step(120);

		rig.ServerLink!.LostPackets.Should().BeGreaterThan(10);

		int held = 0;

		foreach (Entity entity in entities)
		{
			Entity mirror = client.Find(entity);
			Summary(mirror).Should().Equal(Summary(entity));
			held += mirror.EffectsManager.GetActiveEffects().Count();
		}

		(added - removed).Should().Be(held, "every effect that came went, or is still here");
	}

	[Fact]
	[Trait("Networking", "Effects")]
	public void An_effect_record_that_makes_no_sense_is_dropped_and_counted()
	{
		var rig = new Rig();
		Client client = rig.Join();
		Entity entity = rig.Spawn();
		rig.Step(4);

		rig.Network.Server.Send(client.Peer, Protocol.StateChannel, false, Record(entity, instance: 0, asset: 0));
		rig.Step();
		client.Session.MalformedPackets.Should().Be(1);

		int effects = rig.Server.Registry.Count(AssetKind.Effect);
		int bits = rig.Server.Registry.BitsFor(AssetKind.Effect);
		((1 << bits) - 1).Should().BeGreaterThanOrEqualTo(effects, "there is an index that names no effect");
		rig.Network.Server.Send(client.Peer, Protocol.StateChannel, false, Record(entity, 1, (1 << bits) - 1));
		rig.Step();
		client.Session.MalformedPackets.Should().Be(2);

		byte[] Record(Entity target, uint instance, int asset)
		{
			var writer = new BitWriter();
			writer.WriteVarUInt(100_000 + (ulong)client.Session.MalformedPackets);
			writer.WriteVarInt(100);
			writer.WriteVarUInt(target.NetworkId.Value);
			writer.WriteBits(0, 2);
			writer.WriteBool(false);
			writer.WriteBool(false);
			writer.WriteBool(false);
			writer.WriteBool(false);
			writer.WriteBool(true);
			writer.WriteVarUInt(1);
			writer.WriteVarUInt(instance);
			writer.WriteBool(false);
			writer.WriteBool(true);
			writer.WriteBits((ulong)asset, rig.Server.Registry.BitsFor(AssetKind.Effect));
			return writer.ToArray();
		}
	}

	private static IEnumerable<ActiveEffectHandle> EntityEffects(Entity entity, EffectQuery query)
	{
		return entity.EffectsManager.GetActiveEffects(query);
	}

	private static string[] Names(Entity entity)
	{
		return [.. entity.EffectsManager.GetActiveEffects().Select(x => x.Effect!.EffectData.Name)];
	}

	private static string[] Summary(Entity entity)
	{
		return
		[
			.. entity.EffectsManager.GetActiveEffects()
				.Select(x => $"{x.Effect!.EffectData.Name} x{x.StackCount} L{x.Level}")
		];
	}

	private sealed record HitData(int Damage, string Direction);

	private sealed record UnsentData(int Value);

	// What every machine builds for itself: the tags, the effects and the registry that names them.
	private sealed class Assets
	{
		public TagsManager Tags { get; } = new(_tagNames);

		public CountingComponent Counter { get; } = new();

		public NetworkAssetRegistry Registry { get; } = new();

		// 5 s, stacking, executing every second: -1 to Attribute90, the state.burning tag, a persistent and an executed cue.
		public EffectData Burning { get; }

		// Forever: +10 to Attribute1.
		public EffectData Blessed { get; }

		// 5 s, and nothing else.
		public EffectData Warded { get; }

		// Forever, for the owner only: the state.secret tag.
		public EffectData Secret { get; }

		// Forever, for nobody: +3 to Attribute2.
		public EffectData Hidden { get; }

		// Forever: +set.power to Attribute3.
		public EffectData Scaled { get; }

		// Forever: Attribute1 reads 50 whatever its base value.
		public EffectData Pinned { get; }

		// Instant: +1 to Attribute1's base value.
		public EffectData Raise { get; }

		// Forever: +1 to Attribute5, and no machine's registry has it.
		public EffectData Unlisted { get; }

		public Assets()
		{
			Burning = new EffectData(
				"Burning",
				new DurationData(
					DurationType.HasDuration,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(5))),
				[Flat("TestAttributeSet.Attribute90", -1)],
				new StackingData(
					new ScalableInt(5),
					new ScalableInt(1),
					StackPolicy.AggregateByTarget,
					StackLevelPolicy.SegregateLevels,
					StackMagnitudePolicy.DontStack,
					StackOverflowPolicy.DenyApplication,
					StackExpirationPolicy.ClearEntireStack,
					StackOwnerDenialPolicy.AlwaysAllow,
					StackOwnerOverridePolicy.KeepCurrent,
					ApplicationRefreshPolicy: StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication,
					ApplicationResetPeriodPolicy: StackApplicationResetPeriodPolicy.NeverReset,
					ExecuteOnSuccessfulApplication: false),
				new PeriodicData(new ScalableFloat(1), false, PeriodInhibitionRemovedPolicy.NeverReset),
				effectComponents:
				[
					Counter,
					new ModifierTagsEffectComponent(Container("state.burning")),
				],
				cues:
				[
					new CueData(Container("cue.fire"), 0, 10, CueMagnitudeType.EffectLevel),
					new CueData(Container("cue.tick"), 0, 10, CueMagnitudeType.EffectLevel),
				],
				effectTags: Container("kind.fire"));

			Blessed = new EffectData(
				"Blessed",
				new DurationData(DurationType.Infinite),
				[Flat("TestAttributeSet.Attribute1", 10)]);

			Warded = new EffectData(
				"Warded",
				new DurationData(
					DurationType.HasDuration,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(5))));

			Secret = new EffectData(
				"Secret",
				new DurationData(DurationType.Infinite),
				effectComponents: [new ModifierTagsEffectComponent(Container("state.secret"))],
				visibility: EffectVisibility.OwnerOnly);

			Hidden = new EffectData(
				"Hidden",
				new DurationData(DurationType.Infinite),
				[Flat("TestAttributeSet.Attribute2", 3)],
				visibility: EffectVisibility.ServerOnly);

			Scaled = new EffectData(
				"Scaled",
				new DurationData(DurationType.Infinite),
				[
					new Modifier(
						"TestAttributeSet.Attribute3",
						ModifierOperation.FlatBonus,
						new ModifierMagnitude(
							MagnitudeCalculationType.SetByCaller,
							setByCallerFloat: new SetByCallerFloat(Tag.RequestTag(Tags, "set.power"), false))),
				],
				visibility: EffectVisibility.Public);

			Pinned = new EffectData(
				"Pinned",
				new DurationData(DurationType.Infinite),
				[
					new Modifier(
						"TestAttributeSet.Attribute1",
						ModifierOperation.Override,
						new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(50))),
				]);

			Raise = new EffectData(
				"Raise",
				new DurationData(DurationType.Instant),
				[Flat("TestAttributeSet.Attribute1", 1)]);

			Unlisted = new EffectData(
				"Unlisted",
				new DurationData(DurationType.Infinite),
				[Flat("TestAttributeSet.Attribute5", 1)]);

			Registry.Register(AssetKind.Scene, "uid://creature", "Creature");
			Registry.Register(AssetKind.Effect, "uid://burning", Burning);
			Registry.Register(AssetKind.Effect, "uid://blessed", Blessed);
			Registry.Register(AssetKind.Effect, "uid://warded", Warded);
			Registry.Register(AssetKind.Effect, "uid://secret", Secret);
			Registry.Register(AssetKind.Effect, "uid://hidden", Hidden);
			Registry.Register(AssetKind.Effect, "uid://scaled", Scaled);
			Registry.Register(AssetKind.Effect, "uid://pinned", Pinned);
			Registry.Register(
				AssetKind.EffectContext,
				"hit",
				EffectContextCodec.For(
					static (writer, data) =>
					{
						writer.WriteVarInt(data.Damage);
						writer.WriteString(data.Direction);
					},
					static reader => new HitData((int)reader.ReadVarInt(), reader.ReadString())));
			Registry.Seal();
		}

		private static Modifier Flat(string attribute, int amount)
		{
			return new Modifier(
				attribute,
				ModifierOperation.FlatBonus,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(amount)));
		}

		private TagContainer Container(string tag)
		{
			return new TagContainer(Tags, [Tag.RequestTag(Tags, tag)]);
		}
	}

	private sealed class CountingComponent : IEffectComponent
	{
		public int Added { get; private set; }

		public bool OnActiveEffectAdded(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
		{
			Added++;
			return true;
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

		public CuesManager Cues { get; }

		public Rig(NetworkConditions? down = null, int seed = 1)
		{
			_seed = seed;
			INetworkTransport transport = Network.Server;

			if (down is NetworkConditions conditions)
			{
				ServerLink = new ConditionedTransport(transport, conditions, new DeterministicRandom((ulong)seed));
				transport = ServerLink;
			}

			Cues = new CuesManager();
			Server = new NetworkSession(
				transport,
				new SimulationClock(TickRate, 100),
				Assets.Registry,
				Assets.Tags,
				_actions);
		}

		public static ActiveEffectHandle? Apply(Entity entity, EffectData data, Entity? source = null, int level = 1)
		{
			IForgeEntity by = source ?? entity;
			return entity.EffectsManager.ApplyEffect(new Effect(data, new EffectOwnership(by, by), level));
		}

		public static Effect Effect(EffectData data, Entity entity)
		{
			return new Effect(data, new EffectOwnership(entity, entity));
		}

		public Tag Tag(string name)
		{
			return Forge.Tags.Tag.RequestTag(Assets.Tags, name);
		}

		public Client Join(NetworkConditions? up = null, Action<Entity>? spawning = null)
		{
			var client = new Client(Network.Connect(), up, (ulong)(_seed * 31) + (ulong)_clients.Count)
			{
				Spawning = spawning,
			};

			_clients.Add(client);

			for (int i = 0; i < 30 && client.Session.State != SessionState.Ready; i++)
			{
				Step();
			}

			client.Session.State.Should().Be(SessionState.Ready);
			return client;
		}

		public Entity Spawn(PeerId? owner = null)
		{
			var entity = new Entity(Assets.Tags, Cues) { NetworkId = Server.Replication!.AllocateId() };
			Server.Replication.Register(entity, 0, owner ?? PeerId.None);
			_entities.Add(entity);
			return entity;
		}

		// One tick on every machine: what's in flight arrives, clients acknowledge, the server simulates and sends.
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
	}

	private sealed class Client : IReplicationHost
	{
		private readonly ConditionedTransport? _link;

		public NetworkSession Session { get; }

		public PeerId Peer { get; }

		public Assets Assets { get; } = new();

		public CuesManager Cues { get; } = new();

		public RecordingCueHandler Fire { get; } = new();

		public RecordingCueHandler Tick { get; } = new();

		public List<Entity> Spawned { get; } = [];

		public Action<Entity>? Spawning { get; set; }

		public Client(INetworkTransport transport, NetworkConditions? up, ulong seed)
		{
			if (up is NetworkConditions conditions)
			{
				_link = new ConditionedTransport(transport, conditions, new DeterministicRandom(seed));
				transport = _link;
			}

			Cues.RegisterCue(Tag("cue.fire"), Fire);
			Cues.RegisterCue(Tag("cue.tick"), Tick);
			Session = new NetworkSession(
				transport,
				new SimulationClock(TickRate),
				Assets.Registry,
				Assets.Tags,
				_actions);
			Session.Mirror!.Host = this;
			Peer = transport.LocalPeer;
		}

		public Tag Tag(string name)
		{
			return Forge.Tags.Tag.RequestTag(Assets.Tags, name);
		}

		public TagQuery AnyTag(string name)
		{
			return TagQuery.MakeQueryMatchAnyTags(new TagContainer(Assets.Tags, [Tag(name)]));
		}

		public Entity Only()
		{
			return Spawned.Should().ContainSingle().Subject;
		}

		public Entity Find(Entity serverEntity)
		{
			return (Entity)Session.Mirror!.Find(serverEntity.NetworkId)!;
		}

		public void Update(double time)
		{
			_link?.Update(time);
			Session.Update(time);
		}

		public IForgeEntity? Spawn(NetworkEntityId id, int sceneIndex, PeerId owner)
		{
			var entity = new Entity(Assets.Tags, Cues) { NetworkId = id };
			Spawned.Add(entity);
			Spawning?.Invoke(entity);
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

		public TestAttributeSet Set { get; } = new();

		public EntityAttributes Attributes { get; }

		public EntityTags Tags { get; }

		public EffectsManager EffectsManager { get; }

		public CuesManager CuesManager { get; }

		public EntityAbilities Abilities { get; }

		public EventManager Events { get; } = new();

		public Variables SharedVariables { get; } = new();

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
		public Entity? Hidden { get; set; }

		public bool IsRelevant(PeerId peer, ReplicatedEntity entity, ReplicationServer server)
		{
			return entity.Entity != Hidden;
		}
	}
}
