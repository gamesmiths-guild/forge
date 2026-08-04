// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Tags;

public class EntityTagsEventTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Event", null)]
	public void An_effect_granting_a_modifier_tag_raises_the_event_with_the_updated_tags()
	{
		TestEntity target = CreateEntity();

		int raised = 0;
		TagContainer? reported = null;

		target.Tags.OnTagsChanged += tags =>
		{
			raised++;
			reported = tags;
		};

		ApplyModifierTagsEffect(target, "color.red");

		raised.Should().Be(1);
		reported.Should().BeSameAs(target.Tags.AllTags);
		reported!.HasTagExact(Tag.RequestTag(_tagsManager, "color.red")).Should().BeTrue();
	}

	[Fact]
	[Trait("Event", null)]
	public void Removing_the_effect_raises_the_event_with_the_tag_already_gone()
	{
		TestEntity target = CreateEntity();
		ActiveEffectHandle? handle = ApplyModifierTagsEffect(target, "color.red");

		bool hadTagWhenRaised = true;
		target.Tags.OnTagsChanged += tags =>
			hadTagWhenRaised = tags.HasTagExact(Tag.RequestTag(_tagsManager, "color.red"));

		target.EffectsManager.RemoveEffect(handle!);

		hadTagWhenRaised.Should().BeFalse();
	}

	[Fact]
	[Trait("Event", null)]
	public void A_second_effect_granting_the_same_tag_raises_nothing()
	{
		TestEntity target = CreateEntity();
		ApplyModifierTagsEffect(target, "color.red");

		int raised = 0;
		target.Tags.OnTagsChanged += _ => raised++;

		// The reference count goes up but AllTags does not change, so there is nothing to report.
		ActiveEffectHandle? second = ApplyModifierTagsEffect(target, "color.red");
		raised.Should().Be(0);

		// Nor when the first of the two goes away, since the tag is still held by the other.
		target.EffectsManager.RemoveEffect(second!);
		raised.Should().Be(0);
	}

	[Fact]
	[Trait("Event", null)]
	public void The_event_only_reports_changes_on_its_own_entity()
	{
		TestEntity watched = CreateEntity();
		TestEntity other = CreateEntity();

		int raised = 0;
		watched.Tags.OnTagsChanged += _ => raised++;

		ApplyModifierTagsEffect(other, "color.red");

		raised.Should().Be(0);
	}

	private TestEntity CreateEntity()
	{
		return new TestEntity(_tagsManager, _cuesManager);
	}

	private ActiveEffectHandle? ApplyModifierTagsEffect(TestEntity target, params string[] tagKeys)
	{
		var effectData = new EffectData(
			"Tag Granting Effect",
			new DurationData(DurationType.Infinite),
			effectComponents:
			[
				new ModifierTagsEffectComponent(
					new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, tagKeys)))
			]);

		return target.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(target, target)));
	}
}
