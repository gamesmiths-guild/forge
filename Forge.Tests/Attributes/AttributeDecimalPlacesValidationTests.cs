// Copyright © Gamesmiths Guild.

using System.Globalization;
using FluentAssertions;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Tests.Attributes;

public sealed class AttributeDecimalPlacesValidationTests : IDisposable
{
	public AttributeDecimalPlacesValidationTests()
	{
		Validation.Enabled = true;
	}

	public void Dispose()
	{
		Validation.Enabled = false;
		GC.SuppressFinalize(this);
	}

	[Theory]
	[Trait("Decimal places", null)]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(9)]
	public void A_scale_that_fits_in_an_int_is_accepted(int decimalPlaces)
	{
		Action act = () => _ = new ConfigurableAttributeSet(decimalPlaces);

		act.Should().NotThrow();
	}

	[Theory]
	[Trait("Decimal places", null)]
	[InlineData(-1)]
	[InlineData(10)]
	public void A_scale_that_does_not_fit_in_an_int_is_rejected(int decimalPlaces)
	{
		// Ten places would need a scale of 10^10, which overflows the int every attribute value is stored in.
		Action act = () => _ = new ConfigurableAttributeSet(decimalPlaces);

		act.Should().Throw<ValidationException>();
	}

	[Theory]
	[Trait("Decimal places", null)]
	[InlineData(-1)]
	[InlineData(10)]
	public void The_static_helpers_reject_the_same_out_of_range_scale(int decimalPlaces)
	{
		// The helpers take the places as an argument rather than reading them off an attribute, so they have to make
		// the same check themselves.
		Action act = () => _ = Quantization.ToDisplayValue(1, decimalPlaces);

		act.Should().Throw<ValidationException>();
	}

	[Theory]
	[Trait("Decimal places", null)]
	[InlineData(-1)]
	[InlineData(10)]
	public void An_out_of_range_scale_still_leaves_a_usable_attribute_when_validation_is_off(int decimalPlaces)
	{
		// Validation is what should catch this, but it is off by default in a shipped build, so the value has to be
		// brought into range rather than left to contradict itself: DecimalPlaces feeds a "F{n}" format string, and
		// "F-1" throws.
		Validation.Enabled = false;

		var set = new ConfigurableAttributeSet(decimalPlaces);

		set.Scaled.DecimalPlaces.Should().BeInRange(0, Quantization.MaxDecimalPlaces);
		set.Scaled.DisplayScale.Should().Be(Quantization.GetScale(set.Scaled.DecimalPlaces));

		set.Scaled.Invoking(x => x.ToDisplayString(1, CultureInfo.InvariantCulture)).Should().NotThrow();
	}

	[Theory]
	[Trait("Decimal places", null)]
	[InlineData(-1)]
	[InlineData(10)]
	public void The_static_formatter_survives_an_out_of_range_scale_when_validation_is_off(int decimalPlaces)
	{
		Validation.Enabled = false;

		Action act = () => _ = Quantization.ToDisplayString(1, decimalPlaces, CultureInfo.InvariantCulture);

		act.Should().NotThrow();
	}

	private sealed class ConfigurableAttributeSet : AttributeSet
	{
		public EntityAttribute Scaled { get; }

		public ConfigurableAttributeSet(int decimalPlaces)
		{
			Scaled = InitializeAttribute(nameof(Scaled), 0, 0, int.MaxValue, decimalPlaces: decimalPlaces);
		}
	}
}
