// Copyright © Gamesmiths Guild.

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

	private sealed class ConfigurableAttributeSet : AttributeSet
	{
		public EntityAttribute Scaled { get; }

		public ConfigurableAttributeSet(int decimalPlaces)
		{
			Scaled = InitializeAttribute(nameof(Scaled), 0, 0, int.MaxValue, decimalPlaces: decimalPlaces);
		}
	}
}
