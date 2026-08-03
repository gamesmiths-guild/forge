// Copyright © Gamesmiths Guild.

using System.Globalization;
using FluentAssertions;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Attributes;

public class AttributeDisplayValueTests(TagsAndCuesFixture tagsAndCuesFixture)
	: IClassFixture<TagsAndCuesFixture>
{
	private const string SpeedAttribute = "ScaledAttributeSet.Speed";
	private const float Tolerance = 0.0001f;

	private readonly TagsAndCuesFixture _fixture = tagsAndCuesFixture;

	[Fact]
	[Trait("Display value", null)]
	public void An_attribute_declares_no_decimal_places_by_default()
	{
		var set = new ScaledAttributeSet();

		set.Ammo.DecimalPlaces.Should().Be(0);
		set.Ammo.DisplayScale.Should().Be(1);
		set.Ammo.DisplayValue.Should().Be(set.Ammo.CurrentValue);
	}

	[Fact]
	[Trait("Display value", null)]
	public void A_scaled_attribute_presents_its_stored_value_divided_by_the_scale()
	{
		var set = new ScaledAttributeSet();

		set.Speed.DecimalPlaces.Should().Be(2);
		set.Speed.DisplayScale.Should().Be(100);

		// The stored integer is untouched; only the reading of it changes.
		set.Speed.CurrentValue.Should().Be(475);
		set.Speed.DisplayValue.Should().BeApproximately(4.75f, Tolerance);
	}

	[Fact]
	[Trait("Display value", null)]
	public void ToDisplayValue_converts_any_raw_value_the_attribute_carries()
	{
		var set = new ScaledAttributeSet();

		set.Speed.ToDisplayValue(set.Speed.Max).Should().BeApproximately(100f, Tolerance);
		set.Speed.ToDisplayValue(set.Speed.Min).Should().Be(0f);
		set.Speed.ToDisplayValue(-50).Should().BeApproximately(-0.5f, Tolerance);
	}

	[Theory]
	[Trait("Display value", null)]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(475)]
	[InlineData(-475)]
	[InlineData(1_000_000)]
	public void ToRawValue_is_the_inverse_of_ToDisplayValue(int rawValue)
	{
		var set = new ScaledAttributeSet();

		set.Speed.ToRawValue(set.Speed.ToDisplayValue(rawValue)).Should().Be(rawValue);
	}

	[Theory]
	[Trait("Display value", null)]
	[InlineData(0.125f, 13)]
	[InlineData(-0.125f, -13)]
	[InlineData(0.121f, 12)]
	public void ToRawValue_rounds_halves_away_from_zero(float displayValue, int expectedRawValue)
	{
		var set = new ScaledAttributeSet();

		set.Speed.ToRawValue(displayValue).Should().Be(expectedRawValue);
	}

	[Fact]
	[Trait("Display value", null)]
	public void ToRawValue_converts_units_without_clamping_to_the_attributes_bounds()
	{
		var set = new ScaledAttributeSet();

		// Clamping belongs wherever the value lands, not to a unit conversion.
		set.Speed.Max.Should().Be(10_000);
		set.Speed.ToRawValue(500f).Should().Be(50_000);
	}

	[Fact]
	[Trait("Display value", null)]
	public void ToDisplayString_uses_exactly_the_declared_decimal_places()
	{
		var set = new ScaledAttributeSet();

		set.Speed.ToDisplayString(475, CultureInfo.InvariantCulture).Should().Be("4.75");
		set.Speed.ToDisplayString(400, CultureInfo.InvariantCulture).Should().Be("4.00");
		set.Ammo.ToDisplayString(7, CultureInfo.InvariantCulture).Should().Be("7");
	}

	[Fact]
	[Trait("Scope guard", null)]
	public void The_scale_is_presentation_only_and_modifiers_keep_working_in_raw_units()
	{
		var target = new TestEntity(_fixture.TagsManager, _fixture.CuesManager);
		var set = new ScaledAttributeSet();
		target.Attributes.AddAttributeSet(set);

		var effectData = new EffectData(
			"Swiftness",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					SpeedAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(50)))
			]);

		target.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(target, target)));

		// A flat bonus of 50 raw units is a bonus of 0.5 in display units. Nothing in the pipeline knows about the
		// scale, which is exactly the point.
		set.Speed.CurrentValue.Should().Be(525);
		set.Speed.Modifier.Should().Be(50);
		set.Speed.DisplayValue.Should().BeApproximately(5.25f, Tolerance);
		set.Speed.ToDisplayValue(set.Speed.Modifier).Should().BeApproximately(0.5f, Tolerance);
	}

	[Fact]
	[Trait("Scope guard", null)]
	public void Bounds_are_enforced_in_raw_units()
	{
		var set = new ScaledAttributeSet();

		set.SetSpeed(999_999);

		set.Speed.CurrentValue.Should().Be(10_000);
		set.Speed.DisplayValue.Should().BeApproximately(100f, Tolerance);
	}

	private sealed class ScaledAttributeSet : AttributeSet
	{
		/// <summary>
		/// Gets movement speed stored in hundredths: 475 reads as 4.75 units per second.
		/// </summary>
		public EntityAttribute Speed { get; }

		/// <summary>
		/// Gets a plain counting attribute, to prove the default is no scaling at all.
		/// </summary>
		public EntityAttribute Ammo { get; }

		public ScaledAttributeSet()
		{
			Speed = InitializeAttribute(nameof(Speed), 475, 0, 10_000, decimalPlaces: 2);
			Ammo = InitializeAttribute(nameof(Ammo), 7, 0, 100);
		}

		public void SetSpeed(int rawValue)
		{
			SetAttributeBaseValue(Speed, rawValue);
		}
	}
}
