// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;

namespace Gamesmiths.Forge.Tests.Effects;

public sealed class AdditionalEffectsValidationTests : IDisposable
{
	public AdditionalEffectsValidationTests()
	{
		Validation.Enabled = true;
	}

	public void Dispose()
	{
		Validation.Enabled = false;
		GC.SuppressFinalize(this);
	}

	[Fact]
	[Trait("Completion", null)]
	public void Completion_effects_on_an_instant_effect_are_rejected()
	{
		Action act = () => _ = CreateApplierData(
			DurationType.Instant,
			new AdditionalEffectsEffectComponent(onCompleteAlways: [Completion(DurationType.Infinite)]));

		act.Should().Throw<ValidationException>();
	}

	[Theory]
	[Trait("Completion", null)]
	[InlineData(DurationType.Infinite)]
	[InlineData(DurationType.HasDuration)]
	public void Completion_effects_on_a_non_instant_effect_are_accepted(DurationType durationType)
	{
		Action act = () => _ = CreateApplierData(
			durationType,
			new AdditionalEffectsEffectComponent(
				onCompleteAlways: [Completion(DurationType.Infinite)],
				onCompleteNormal: [Completion(DurationType.Infinite)],
				onCompletePrematurely: [Completion(DurationType.Infinite)]));

		act.Should().NotThrow();
	}

	[Fact]
	[Trait("Application", null)]
	public void Application_effects_on_an_instant_effect_are_accepted()
	{
		// Only the completion half needs an effect that becomes active; application fires for instant effects too.
		Action act = () => _ = CreateApplierData(
			DurationType.Instant,
			new AdditionalEffectsEffectComponent([new ConditionalEffect(CreateAppliedData(DurationType.Instant))]));

		act.Should().NotThrow();
	}

	[Fact]
	[Trait("RemoveOnEnd", null)]
	public void Remove_on_end_with_an_instant_applier_is_rejected()
	{
		Action act = () => _ = CreateApplierData(
			DurationType.Instant,
			new AdditionalEffectsEffectComponent(
			[
				new ConditionalEffect(
					CreateAppliedData(DurationType.Infinite),
					RemovalPolicy: ConditionalEffectRemovalPolicy.RemoveOnEnd)
			]));

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("RemoveOnEnd", null)]
	public void Remove_on_end_with_an_instant_applied_effect_is_rejected()
	{
		Action act = () => _ = CreateApplierData(
			DurationType.Infinite,
			new AdditionalEffectsEffectComponent(
			[
				new ConditionalEffect(
					CreateAppliedData(DurationType.Instant),
					RemovalPolicy: ConditionalEffectRemovalPolicy.RemoveOnEnd)
			]));

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("RemoveOnEnd", null)]
	public void An_instant_applied_effect_is_accepted_when_it_is_left_alone()
	{
		Action act = () => _ = CreateApplierData(
			DurationType.Infinite,
			new AdditionalEffectsEffectComponent([new ConditionalEffect(CreateAppliedData(DurationType.Instant))]));

		act.Should().NotThrow();
	}

	[Theory]
	[Trait("RemoveOnEnd", null)]
	[InlineData(DurationType.Infinite)]
	[InlineData(DurationType.HasDuration)]
	public void Remove_on_end_between_two_non_instant_effects_is_accepted(DurationType durationType)
	{
		Action act = () => _ = CreateApplierData(
			durationType,
			new AdditionalEffectsEffectComponent(
			[
				new ConditionalEffect(
					CreateAppliedData(durationType),
					RemovalPolicy: ConditionalEffectRemovalPolicy.RemoveOnEnd)
			]));

		act.Should().NotThrow();
	}

	[Fact]
	[Trait("Application", null)]
	public void A_component_with_nothing_configured_at_all_is_accepted()
	{
		Action act = () => _ = CreateApplierData(DurationType.Instant, new AdditionalEffectsEffectComponent());

		act.Should().NotThrow();
	}

	[Theory]
	[Trait("Completion", null)]
	[InlineData("always")]
	[InlineData("normal")]
	[InlineData("prematurely")]
	public void Remove_on_end_on_a_completion_effect_is_rejected(string completionSet)
	{
		// The end a completion effect would be taken back at is the one applying it, so the policy reads as configured
		// while meaning nothing.
		ConditionalEffect[] entries =
		[
			new ConditionalEffect(
				CreateAppliedData(DurationType.Infinite),
				RemovalPolicy: ConditionalEffectRemovalPolicy.RemoveOnEnd)
		];

		Action act = () => _ = CreateApplierData(
			DurationType.Infinite,
			new AdditionalEffectsEffectComponent(
				onCompleteAlways: completionSet == "always" ? entries : null,
				onCompleteNormal: completionSet == "normal" ? entries : null,
				onCompletePrematurely: completionSet == "prematurely" ? entries : null));

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("Completion", null)]
	public void A_completion_effect_with_a_source_condition_and_a_target_is_accepted()
	{
		Action act = () => _ = CreateApplierData(
			DurationType.Infinite,
			new AdditionalEffectsEffectComponent(
				onCompleteAlways:
				[
					new ConditionalEffect(
						CreateAppliedData(DurationType.Instant),
						Target: EffectApplicationTarget.Source)
				]));

		act.Should().NotThrow();
	}

	private static DurationData CreateDurationData(DurationType durationType)
	{
		return durationType == DurationType.HasDuration
			? new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10f)))
			: new DurationData(durationType);
	}

	private static EffectData CreateAppliedData(DurationType durationType)
	{
		return new EffectData("Applied Effect", CreateDurationData(durationType));
	}

	private static ConditionalEffect Completion(DurationType durationType)
	{
		return new ConditionalEffect(CreateAppliedData(durationType));
	}

	private static EffectData CreateApplierData(DurationType durationType, IEffectComponent component)
	{
		return new EffectData("Applier Effect", CreateDurationData(durationType), effectComponents: [component]);
	}
}
