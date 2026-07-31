// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Effects;

public class SourceTagRequirementsComponentTests(TagsAndCuesFixture tagsAndCuesFixture)
	: IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Application", null)]
	public void Application_is_gated_on_the_source_tags_not_the_targets()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		var plainAttacker = new TestEntity(_tagsManager, _cuesManager);
		var venomousAttacker = new TestEntity(_tagsManager, _cuesManager);

		ApplyTagEffect(venomousAttacker, TagsOf("enemy.beast.wolf"));

		// "Poison" only lands if the attacker is a wolf.
		TagRequirements requirements = new(RequiredTags: TagsOf("enemy.beast.wolf"));

		target.EffectsManager
			.ApplyEffect(CreateSourceGatedEffect(plainAttacker, applicationTagRequirements: requirements))
			.Should().BeNull();

		target.EffectsManager
			.ApplyEffect(CreateSourceGatedEffect(venomousAttacker, applicationTagRequirements: requirements))
			.Should().NotBeNull();
	}

	[Fact]
	[Trait("Application", null)]
	public void The_targets_own_tags_do_not_satisfy_a_source_requirement()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		var attacker = new TestEntity(_tagsManager, _cuesManager);

		// The target carries the tag; the source does not.
		ApplyTagEffect(target, TagsOf("enemy.beast.wolf"));

		target.EffectsManager
			.ApplyEffect(CreateSourceGatedEffect(
				attacker,
				applicationTagRequirements: new TagRequirements(RequiredTags: TagsOf("enemy.beast.wolf"))))
			.Should().BeNull();
	}

	[Fact]
	[Trait("Ongoing", null)]
	public void Ongoing_source_tags_toggle_inhibition()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		var caster = new TestEntity(_tagsManager, _cuesManager);

		// The aura is suppressed while its caster is silenced.
		ActiveEffectHandle? handle = target.EffectsManager.ApplyEffect(CreateSourceGatedEffect(
			caster,
			ongoingTagRequirements: new TagRequirements(IgnoreTags: TagsOf("color.red"))));

		handle.Should().NotBeNull();
		handle!.IsInhibited.Should().BeFalse();

		ActiveEffectHandle? silence = ApplyTagEffect(caster, TagsOf("color.red"));
		handle.IsInhibited.Should().BeTrue();

		caster.EffectsManager.RemoveEffect(silence!, true);
		handle.IsInhibited.Should().BeFalse();
	}

	[Fact]
	[Trait("Removal", null)]
	public void Removal_source_tags_force_removal()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		var caster = new TestEntity(_tagsManager, _cuesManager);

		ActiveEffectHandle? handle = target.EffectsManager.ApplyEffect(CreateSourceGatedEffect(
			caster,
			removalTagRequirements: new TagRequirements(RequiredTags: TagsOf("color.red"))));

		handle.Should().NotBeNull();
		handle!.IsValid.Should().BeTrue();

		ApplyTagEffect(caster, TagsOf("color.red"));
		handle.IsValid.Should().BeFalse();
	}

	[Fact]
	[Trait("OwnershipEntity", null)]
	public void Owner_mode_reads_the_owner_instead_of_the_source()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var source = new TestEntity(_tagsManager, _cuesManager);

		ApplyTagEffect(owner, TagsOf("color.red"));

		TagRequirements requirements = new(RequiredTags: TagsOf("color.red"));

		// Reading the source, whose tags lack color.red, denies the application.
		target.EffectsManager
			.ApplyEffect(CreateEffect(
				new EffectOwnership(owner, source),
				applicationTagRequirements: requirements))
			.Should().BeNull();

		// Reading the owner, who carries it, allows it.
		target.EffectsManager
			.ApplyEffect(CreateEffect(
				new EffectOwnership(owner, source),
				applicationTagRequirements: requirements,
				ownershipEntity: OwnershipEntity.Owner))
			.Should().NotBeNull();
	}

	[Fact]
	[Trait("NullSource", null)]
	public void A_null_source_fails_required_tags_but_satisfies_ignored_tags()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);

		target.EffectsManager
			.ApplyEffect(CreateEffect(
				new EffectOwnership(null, null),
				applicationTagRequirements: new TagRequirements(RequiredTags: TagsOf("color.red"))))
			.Should().BeNull();

		target.EffectsManager
			.ApplyEffect(CreateEffect(
				new EffectOwnership(null, null),
				applicationTagRequirements: new TagRequirements(IgnoreTags: TagsOf("color.red"))))
			.Should().NotBeNull();
	}

	private static ActiveEffectHandle? ApplyTagEffect(IForgeEntity entity, TagContainer tags)
	{
		var tagEffectData = new EffectData(
			"Tag Effect",
			new DurationData(DurationType.Infinite),
			effectComponents: [new ModifierTagsEffectComponent(tags)]);

		return entity.EffectsManager.ApplyEffect(
			new Effect(tagEffectData, new EffectOwnership(entity, entity)));
	}

	private static Effect CreateEffect(
		EffectOwnership ownership,
		TagRequirements? applicationTagRequirements = null,
		TagRequirements? removalTagRequirements = null,
		TagRequirements? ongoingTagRequirements = null,
		OwnershipEntity ownershipEntity = OwnershipEntity.Source)
	{
		var effectData = new EffectData(
			"Source Gated Effect",
			new DurationData(DurationType.Infinite),
			effectComponents:
			[
				new SourceTagRequirementsEffectComponent(
					applicationTagRequirements,
					removalTagRequirements,
					ongoingTagRequirements,
					ownershipEntity)
			]);

		return new Effect(effectData, ownership);
	}

	private static Effect CreateSourceGatedEffect(
		IForgeEntity source,
		TagRequirements? applicationTagRequirements = null,
		TagRequirements? removalTagRequirements = null,
		TagRequirements? ongoingTagRequirements = null)
	{
		return CreateEffect(
			new EffectOwnership(source, source),
			applicationTagRequirements,
			removalTagRequirements,
			ongoingTagRequirements);
	}

	private TagContainer TagsOf(params string[] tagKeys)
	{
		return new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, tagKeys));
	}
}
