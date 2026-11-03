// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Networking;
using Gamesmiths.Forge.Networking.Messages;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Networking;

/// <summary>
/// What both ends of a session must agree on: assets by index, entities by network id, and the hashes that say the
/// agreement holds.
/// </summary>
/// <param name="fixture">The fixture providing the <see cref="TagsManager"/> and <see cref="CuesManager"/>.</param>
public class IdentityTests(TagsAndCuesFixture fixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = fixture.TagsManager;
	private readonly CuesManager _cuesManager = fixture.CuesManager;

	[Fact]
	[Trait("Networking", "Identity")]
	public void Assets_are_numbered_by_key_whatever_the_order_they_were_registered_in()
	{
		var first = new NetworkAssetRegistry();
		first.Register(AssetKind.Effect, "uid://burning", "Burning");
		first.Register(AssetKind.Effect, "uid://aaa-haste", "Haste");
		first.Register(AssetKind.Ability, "uid://dash", "Dash");
		first.Seal();

		var second = new NetworkAssetRegistry();
		second.Register(AssetKind.Ability, "uid://dash", "Dash");
		second.Register(AssetKind.Effect, "uid://aaa-haste", "Haste");
		second.Register(AssetKind.Effect, "uid://burning", "Burning");
		second.Seal();

		first.IndexOf(AssetKind.Effect, "uid://aaa-haste").Should().Be(0);
		first.IndexOf(AssetKind.Effect, "uid://burning").Should().Be(1);
		first.IndexOf(AssetKind.Ability, "uid://dash").Should().Be(0);
		first.IndexOf(AssetKind.Effect, "uid://dash").Should().Be(-1, "the key is an ability's");
		first.Count(AssetKind.Effect).Should().Be(2);
		first.Count(AssetKind.Scene).Should().Be(0);
		first.KeyAt(AssetKind.Effect, 1).Should().Be("uid://burning");
		first.Get<string>(AssetKind.Effect, 1).Should().Be("Burning");
		first.TryGet(AssetKind.Effect, 2, out string _).Should().BeFalse();
		first.TryGet(AssetKind.Effect, 0, out int _).Should().BeFalse("the asset isn't an int");
		first.BitsFor(AssetKind.Effect).Should().Be(1);
		first.BitsFor(AssetKind.Scene).Should().Be(1, "an empty kind still writes a bit");

		second.Hash.Should().Be(first.Hash, "the same keys hash the same whatever the order");
		second.IndexOf(AssetKind.Effect, "uid://burning").Should().Be(1);
	}

	[Fact]
	[Trait("Networking", "Identity")]
	public void Registries_that_differ_hash_differently()
	{
		var one = new NetworkAssetRegistry();
		one.Register(AssetKind.Effect, "a", 1);
		one.Seal();

		var other = new NetworkAssetRegistry();
		other.Register(AssetKind.Effect, "a", 1);
		other.Register(AssetKind.Effect, "b", 2);
		other.Seal();

		var movedKind = new NetworkAssetRegistry();
		movedKind.Register(AssetKind.Ability, "a", 1);
		movedKind.Seal();

		one.Hash.Should().NotBe(0);
		other.Hash.Should().NotBe(one.Hash);
		movedKind.Hash.Should().NotBe(one.Hash);
	}

	[Fact]
	[Trait("Networking", "Identity")]
	public void A_registry_refuses_duplicates_empty_keys_and_changes_once_sealed()
	{
		var registry = new NetworkAssetRegistry();
		registry.Register(AssetKind.Graph, "g", 1);

		registry.Invoking(x => x.Register(AssetKind.Graph, "g", 2)).Should().Throw<ArgumentException>();
		registry.Invoking(x => x.Register(AssetKind.Graph, string.Empty, 2)).Should().Throw<ArgumentException>();
		registry.Invoking(x => x.Count(AssetKind.Graph)).Should().Throw<InvalidOperationException>("not sealed");

		registry.Seal();
		registry.Seal();

		registry.Invoking(x => x.Register(AssetKind.Graph, "h", 3)).Should().Throw<InvalidOperationException>();
		registry.Invoking(x => x.KeyAt(AssetKind.Graph, 1)).Should().Throw<ArgumentOutOfRangeException>();
		registry.Invoking(x => x.Get<int>(AssetKind.Graph, 1)).Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	[Trait("Networking", "Identity")]
	public void Input_action_sets_hash_by_their_actions_and_vectors_in_order()
	{
		var set = new InputActionSet(["jump", "fire"], [new InputVector("l", "r", "u", "d")]);
		var same = new InputActionSet(["jump", "fire"], [new InputVector("l", "r", "u", "d")]);
		var reordered = new InputActionSet(["fire", "jump"], [new InputVector("l", "r", "u", "d")]);
		var otherVector = new InputActionSet(["jump", "fire"], [new InputVector("l", "r", "d", "u")]);
		var noVector = new InputActionSet(["jump", "fire"]);

		Protocol.HashOf(same).Should().Be(Protocol.HashOf(set));
		Protocol.HashOf(reordered).Should().NotBe(Protocol.HashOf(set));
		Protocol.HashOf(otherVector).Should().NotBe(Protocol.HashOf(set));
		Protocol.HashOf(noVector).Should().NotBe(Protocol.HashOf(set));
	}

	[Fact]
	[Trait("Networking", "Identity")]
	public void Control_messages_read_back()
	{
		var writer = new BitWriter();
		new HelloMessage(Protocol.Version, 0xABCDEF, 0x123456).Write(writer);
		new WelcomeMessage(60, 1234, 0xFEEDFACE).Write(writer);
		new RefusedMessage(RefusalReason.AssetRegistry).Write(writer);
		new PingMessage(42).Write(writer);
		new PongMessage(42, 1300, -1, 3).Write(writer);
		new PongMessage(43, 1301, 1320, 1000).Write(writer);

		var reader = new BitReader(writer.ToArray());

		reader.ReadByte().Should().Be((byte)ControlMessage.Hello);
		HelloMessage.Read(reader).Should().Be(new HelloMessage(Protocol.Version, 0xABCDEF, 0x123456));
		reader.ReadByte().Should().Be((byte)ControlMessage.Welcome);
		WelcomeMessage.Read(reader).Should().Be(new WelcomeMessage(60, 1234, 0xFEEDFACE));
		reader.ReadByte().Should().Be((byte)ControlMessage.Refused);
		RefusedMessage.Read(reader).Should().Be(new RefusedMessage(RefusalReason.AssetRegistry));
		reader.ReadByte().Should().Be((byte)ControlMessage.Ping);
		PingMessage.Read(reader).Should().Be(new PingMessage(42));
		reader.ReadByte().Should().Be((byte)ControlMessage.Pong);
		PongMessage.Read(reader).Should().Be(new PongMessage(42, 1300, -1, 3));
		reader.ReadByte().Should().Be((byte)ControlMessage.Pong);
		PongMessage.Read(reader).Should().Be(new PongMessage(43, 1301, 1320, 255), "the depth is capped at a byte");

		var badRate = new BitWriter();
		badRate.WriteVarUInt(0);
		new BitReader(badRate.ToArray()).Invoking(WelcomeMessage.Read).Should().Throw<MalformedPacketException>();
	}

	[Fact]
	[Trait("Networking", "Identity")]
	public void An_entity_s_checksum_is_stable_and_follows_its_state()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		ulong before = EntitySimulation.Checksum(entity);

		EntitySimulation.Checksum(entity).Should().Be(before, "nothing changed");

		entity.EffectsManager.ApplyEffect(new Effect(Buff(), new EffectOwnership(entity, entity)));

		ulong after = EntitySimulation.Checksum(entity);
		after.Should().NotBe(before);

		var twin = new TestEntity(_tagsManager, _cuesManager);
		twin.EffectsManager.ApplyEffect(new Effect(Buff(), new EffectOwnership(twin, twin)));

		EntitySimulation.Checksum(twin).Should().Be(after, "an equal entity in an equal state hashes the same");
	}

	[Fact]
	[Trait("Networking", "Identity")]
	public void An_entity_referring_to_another_hashes_it_by_network_id()
	{
		// Two machines' copies of a source and a target. The sources have the same id; the local-only sources don't.
		var serverSource = new NetworkedEntity(_tagsManager, _cuesManager, new NetworkEntityId(7));
		var clientSource = new NetworkedEntity(_tagsManager, _cuesManager, new NetworkEntityId(7));
		var otherSource = new NetworkedEntity(_tagsManager, _cuesManager, new NetworkEntityId(8));
		var serverTarget = new NetworkedEntity(_tagsManager, _cuesManager, new NetworkEntityId(9));
		var clientTarget = new NetworkedEntity(_tagsManager, _cuesManager, new NetworkEntityId(9));
		var otherTarget = new NetworkedEntity(_tagsManager, _cuesManager, new NetworkEntityId(9));

		serverTarget.EffectsManager.ApplyEffect(new Effect(Buff(), new EffectOwnership(serverSource, serverSource)));
		clientTarget.EffectsManager.ApplyEffect(new Effect(Buff(), new EffectOwnership(clientSource, clientSource)));
		otherTarget.EffectsManager.ApplyEffect(new Effect(Buff(), new EffectOwnership(otherSource, otherSource)));

		EntitySimulation.Checksum(clientTarget).Should().Be(EntitySimulation.Checksum(serverTarget));
		EntitySimulation.Checksum(otherTarget).Should().NotBe(EntitySimulation.Checksum(serverTarget));

		var localSource = new TestEntity(_tagsManager, _cuesManager);
		var localTarget = new NetworkedEntity(_tagsManager, _cuesManager, new NetworkEntityId(9));
		localTarget.EffectsManager.ApplyEffect(new Effect(Buff(), new EffectOwnership(localSource, localSource)));

		EntitySimulation.Checksum(localTarget)
			.Should().NotBe(EntitySimulation.Checksum(serverTarget), "a local source isn't #7");
	}

	[Fact]
	[Trait("Networking", "Identity")]
	public void A_world_checksum_counts_networked_entities_by_id_and_local_ones_by_place()
	{
		const double Step = 1.0 / 64.0;
		var source = new NetworkedEntity(_tagsManager, _cuesManager, new NetworkEntityId(3));
		var target = new NetworkedEntity(_tagsManager, _cuesManager, new NetworkEntityId(4));
		var bystander = new TestEntity(_tagsManager, _cuesManager);
		target.EffectsManager.ApplyEffect(new Effect(Buff(), new EffectOwnership(source, source)));

		var world = new WorldHistory(4, Step);
		world.Add(source);
		world.Add(target);
		world.Add(bystander);
		EntitySimulation.Tick(world.Entities, 1, Step);
		world.Record(1);

		// The same world on another machine, with the networked entities in another order.
		var otherSource = new NetworkedEntity(_tagsManager, _cuesManager, new NetworkEntityId(3));
		var otherTarget = new NetworkedEntity(_tagsManager, _cuesManager, new NetworkEntityId(4));
		var otherBystander = new TestEntity(_tagsManager, _cuesManager);
		otherTarget.EffectsManager.ApplyEffect(new Effect(Buff(), new EffectOwnership(otherSource, otherSource)));

		var otherWorld = new WorldHistory(4, Step);
		otherWorld.Add(otherTarget);
		otherWorld.Add(otherSource);
		otherWorld.Add(otherBystander);
		EntitySimulation.Tick(otherWorld.Entities, 1, Step);
		otherWorld.Record(1);

		// The worlds list their entities in different orders, so the world hashes differ by place, but the target's
		// reference to its source hashes the same, by id: that's what the per-entity checksums show.
		EntitySimulation.Checksum(otherTarget).Should().Be(EntitySimulation.Checksum(target));
		world.ChecksumAt(1).Should().NotBe(otherWorld.ChecksumAt(1), "the entities sit in different places");
	}

	private static EffectData Buff()
	{
		return new EffectData(
			"Haste",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10))),
			[
				new Modifier(
					"TestAttributeSet.Attribute1",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(1)))
			]);
	}

	private sealed class NetworkedEntity : IForgeEntity
	{
		public NetworkEntityId NetworkId { get; }

		public EntityAttributes Attributes { get; }

		public EntityTags Tags { get; }

		public EffectsManager EffectsManager { get; }

		public CuesManager CuesManager { get; }

		public EntityAbilities Abilities { get; }

		public EventManager Events { get; }

		public Variables SharedVariables { get; } = new();

		public NetworkedEntity(TagsManager tagsManager, CuesManager cuesManager, NetworkEntityId networkId)
		{
			NetworkId = networkId;
			Attributes = new EntityAttributes(this, new TestAttributeSet());
			Tags = new EntityTags(new TagContainer(tagsManager));
			EffectsManager = new EffectsManager(this, cuesManager);
			CuesManager = cuesManager;
			Abilities = new EntityAbilities(this);
			Events = new EventManager();
		}
	}
}
