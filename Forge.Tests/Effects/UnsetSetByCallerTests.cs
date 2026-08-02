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
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Effects;

/// <summary>
/// What happens when a <see cref="SetByCallerFloat"/> magnitude names a tag nobody ever set. The common way to reach it
/// is an effect applied by <see cref="AdditionalEffectsEffectComponent"/> without
/// <c>copyDataFromOriginalEffect</c>, which builds the child with an empty magnitude dictionary.
/// </summary>
/// <param name="tagsAndCuesFixture">The fixture providing tags and cues managers.</param>
public sealed class UnsetSetByCallerTests(TagsAndCuesFixture tagsAndCuesFixture)
	: IClassFixture<TagsAndCuesFixture>, IDisposable
{
	private const string TargetAttribute = "TestAttributeSet.Attribute90";

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	public void Dispose()
	{
		Validation.Enabled = false;
	}

	[Fact]
	[Trait("Validation", null)]
	public void An_unset_magnitude_reports_a_validation_failure_naming_the_tag_and_the_effect()
	{
		Validation.Enabled = true;

		var target = new TestEntity(_tagsManager, _cuesManager);
		var unsetTag = Tag.RequestTag(_tagsManager, "tag");

		Action act = () => target.EffectsManager.ApplyEffect(
			new Effect(CreateSetByCallerData(unsetTag), new EffectOwnership(target, target)));

		// The dictionary indexer on its own would only have reported the missing key.
		act.Should().Throw<ValidationException>()
			.WithMessage("*SetByCaller*")
			.WithMessage("*Unset Effect*")
			.WithMessage("*'tag'*")
			.WithMessage("*copyDataFromOriginalEffect*");
	}

	[Fact]
	[Trait("Validation", null)]
	public void An_unset_magnitude_resolves_to_zero_when_validation_is_off()
	{
		Validation.Enabled = false;

		var target = new TestEntity(_tagsManager, _cuesManager);
		var unsetTag = Tag.RequestTag(_tagsManager, "tag");

		target.EffectsManager.ApplyEffect(
			new Effect(CreateSetByCallerData(unsetTag), new EffectOwnership(target, target)));

		// A release build degrades to a zero magnitude rather than throwing out of the application.
		TestUtils.TestAttribute(target, TargetAttribute, [90, 90, 0, 0]);
	}

	[Fact]
	[Trait("Validation", null)]
	public void A_magnitude_set_after_a_failed_read_is_still_picked_up()
	{
		Validation.Enabled = false;

		var target = new TestEntity(_tagsManager, _cuesManager);
		var unsetTag = Tag.RequestTag(_tagsManager, "tag");

		var effect = new Effect(CreateSetByCallerData(unsetTag), new EffectOwnership(target, target));

		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(target, TargetAttribute, [90, 90, 0, 0]);

		// The failed read is not cached as a zero snapshot, so setting the magnitude afterwards still works.
		effect.SetSetByCallerMagnitude(unsetTag, -10);
		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, TargetAttribute, [80, 80, 0, 0]);
	}

	[Fact]
	[Trait("AdditionalEffects", null)]
	public void An_applied_effect_without_copied_data_is_the_case_that_reaches_it()
	{
		Validation.Enabled = true;

		var target = new TestEntity(_tagsManager, _cuesManager);
		var tallyTag = Tag.RequestTag(_tagsManager, "tag");

		// The applier publishes the tag, but the child is built fresh rather than linked, so it never sees it.
		var applierData = new EffectData(
			"Applier",
			new DurationData(DurationType.Instant),
			effectComponents:
			[
				new AdditionalEffectsEffectComponent(
					[new ConditionalEffect(CreateSetByCallerData(tallyTag))],
					copyDataFromOriginalEffect: false)
			]);

		var applier = new Effect(applierData, new EffectOwnership(target, target));
		applier.SetSetByCallerMagnitude(tallyTag, -10);

		Action act = () => target.EffectsManager.ApplyEffect(applier);

		act.Should().Throw<ValidationException>().WithMessage("*copyDataFromOriginalEffect*");
	}

	[Fact]
	[Trait("AdditionalEffects", null)]
	public void Copying_the_data_across_is_the_fix()
	{
		Validation.Enabled = true;

		var target = new TestEntity(_tagsManager, _cuesManager);
		var tallyTag = Tag.RequestTag(_tagsManager, "tag");

		var applierData = new EffectData(
			"Applier",
			new DurationData(DurationType.Instant),
			effectComponents:
			[
				new AdditionalEffectsEffectComponent(
					[new ConditionalEffect(CreateSetByCallerData(tallyTag))],
					copyDataFromOriginalEffect: true)
			]);

		var applier = new Effect(applierData, new EffectOwnership(target, target));
		applier.SetSetByCallerMagnitude(tallyTag, -10);

		target.EffectsManager.ApplyEffect(applier);

		TestUtils.TestAttribute(target, TargetAttribute, [80, 80, 0, 0]);
	}

	[Fact]
	[Trait("Accumulator", null)]
	public void An_accumulator_seeds_its_own_tag_so_a_linked_payout_never_reaches_this()
	{
		Validation.Enabled = true;

		var target = new TestEntity(_tagsManager, _cuesManager);
		var tallyTag = Tag.RequestTag(_tagsManager, "tag");

		// Removed before it ever executes: the seed in OnActiveEffectAdded is the only thing that has written the tag.
		var drainData = new EffectData(
			"Drain",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10))),
			periodicData: new PeriodicData(new ScalableFloat(1), false, PeriodInhibitionRemovedPolicy.NeverReset),
			effectComponents:
			[
				new AttributeAccumulatorEffectComponent(TargetAttribute, tallyTag),
				new AdditionalEffectsEffectComponent(
					onCompleteAlways: [new ConditionalEffect(CreateSetByCallerData(tallyTag))],
					copyDataFromOriginalEffect: true)
			]);

		ActiveEffectHandle handle = target.EffectsManager.ApplyEffect(
			new Effect(drainData, new EffectOwnership(target, target)))!;

		Action act = () => target.EffectsManager.RemoveEffect(handle, true);

		act.Should().NotThrow();
		TestUtils.TestAttribute(target, TargetAttribute, [90, 90, 0, 0]);
	}

	private static EffectData CreateSetByCallerData(Tag tag)
	{
		return new EffectData(
			"Unset Effect",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					TargetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.SetByCaller,
						setByCallerFloat: new SetByCallerFloat(tag)))
			]);
	}
}
