// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Effects.Periodic;

namespace Gamesmiths.Forge.Tests.Effects;

public sealed class ModifierAggregationValidationTests : IDisposable
{
	private const string TargetAttribute = "TestAttributeSet.Attribute100";

	public ModifierAggregationValidationTests()
	{
		Validation.Enabled = true;
	}

	public void Dispose()
	{
		Validation.Enabled = false;
		GC.SuppressFinalize(this);
	}

	[Theory]
	[Trait("Aggregation", null)]
	[InlineData(AggregationMode.Max)]
	[InlineData(AggregationMode.Min)]
	public void An_instant_effect_cannot_aggregate_its_modifiers(AggregationMode aggregationMode)
	{
		Action act = () => _ = CreateEffectData(new DurationData(DurationType.Instant), null, aggregationMode);

		act.Should().Throw<ValidationException>();
	}

	[Theory]
	[Trait("Aggregation", null)]
	[InlineData(AggregationMode.Max)]
	[InlineData(AggregationMode.Min)]
	public void A_periodic_effect_cannot_aggregate_its_modifiers(AggregationMode aggregationMode)
	{
		Action act = () => _ = CreateEffectData(
			new DurationData(DurationType.Infinite),
			new PeriodicData(new ScalableFloat(1), true, PeriodInhibitionRemovedPolicy.NeverReset),
			aggregationMode);

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("Aggregation", null)]
	public void An_executed_effect_is_accepted_with_the_default_aggregation_mode()
	{
		Action act = () => _ = CreateEffectData(new DurationData(DurationType.Instant), null, AggregationMode.Sum);

		act.Should().NotThrow();
	}

	[Theory]
	[Trait("Aggregation", null)]
	[InlineData(AggregationMode.Sum)]
	[InlineData(AggregationMode.Max)]
	[InlineData(AggregationMode.Min)]
	public void A_duration_effect_accepts_any_aggregation_mode(AggregationMode aggregationMode)
	{
		Action act = () => _ = CreateEffectData(new DurationData(DurationType.Infinite), null, aggregationMode);

		act.Should().NotThrow();
	}

	private static EffectData CreateEffectData(
		DurationData durationData,
		PeriodicData? periodicData,
		AggregationMode aggregationMode)
	{
		return new EffectData(
			"Aggregated effect",
			durationData,
			[
				new Modifier(
					TargetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10)),
					AggregationMode: aggregationMode)
			],
			periodicData: periodicData);
	}
}
