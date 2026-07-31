// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Periodic;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Effects;

public sealed class AbilityComponentsValidationTests : IClassFixture<TagsAndCuesFixture>, IDisposable
{
	private readonly TagsManager _tagsManager;

	public AbilityComponentsValidationTests(TagsAndCuesFixture tagsAndCuesFixture)
	{
		_tagsManager = tagsAndCuesFixture.TagsManager;
		Validation.Enabled = true;
	}

	public void Dispose()
	{
		Validation.Enabled = false;
		GC.SuppressFinalize(this);
	}

	[Fact]
	[Trait("BlockAbilityTags", null)]
	public void Block_ability_tags_on_an_instant_effect_is_rejected()
	{
		Action act = () => _ = new EffectData(
			"Instant Blocker",
			new DurationData(DurationType.Instant),
			effectComponents: [new BlockAbilityTagsEffectComponent(TagsOf("color.red"))]);

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("BlockAbilityTags", null)]
	public void Block_ability_tags_on_a_duration_effect_is_accepted()
	{
		Action act = () => _ = new EffectData(
			"Infinite Blocker",
			new DurationData(DurationType.Infinite),
			effectComponents: [new BlockAbilityTagsEffectComponent(TagsOf("color.red"))]);

		act.Should().NotThrow();
	}

	[Fact]
	[Trait("CancelAbilityTags", null)]
	public void On_execution_policy_on_a_non_periodic_duration_effect_is_rejected()
	{
		Action act = () => _ = new EffectData(
			"Non Periodic Canceler",
			new DurationData(DurationType.Infinite),
			effectComponents:
			[
				new CancelAbilityTagsEffectComponent(
					TagsOf("color.red"),
					null,
					CancelAbilityTagsPolicy.OnExecution)
			]);

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("CancelAbilityTags", null)]
	public void On_execution_policy_on_a_periodic_effect_is_accepted()
	{
		Action act = () => _ = new EffectData(
			"Periodic Canceler",
			new DurationData(DurationType.Infinite),
			periodicData: new PeriodicData(new ScalableFloat(1f), true, PeriodInhibitionRemovedPolicy.ResetPeriod),
			effectComponents:
			[
				new CancelAbilityTagsEffectComponent(
					TagsOf("color.red"),
					null,
					CancelAbilityTagsPolicy.OnExecution)
			]);

		act.Should().NotThrow();
	}

	[Fact]
	[Trait("CancelAbilityTags", null)]
	public void On_execution_policy_on_an_instant_effect_is_accepted()
	{
		Action act = () => _ = new EffectData(
			"Instant Canceler",
			new DurationData(DurationType.Instant),
			effectComponents:
			[
				new CancelAbilityTagsEffectComponent(
					TagsOf("color.red"),
					null,
					CancelAbilityTagsPolicy.OnExecution)
			]);

		act.Should().NotThrow();
	}

	[Fact]
	[Trait("CancelAbilityTags", null)]
	public void Cancel_ability_tags_without_any_filter_is_rejected()
	{
		Action act = () => _ = new EffectData(
			"Unfiltered Canceler",
			new DurationData(DurationType.Instant),
			effectComponents: [new CancelAbilityTagsEffectComponent()]);

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("CancelAbilityTags", null)]
	public void Cancel_ability_tags_with_only_without_tags_is_accepted()
	{
		Action act = () => _ = new EffectData(
			"Without Only Canceler",
			new DurationData(DurationType.Instant),
			effectComponents: [new CancelAbilityTagsEffectComponent(null, TagsOf("color.blue"))]);

		act.Should().NotThrow();
	}

	private TagContainer TagsOf(params string[] tagKeys)
	{
		return new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, tagKeys));
	}
}
