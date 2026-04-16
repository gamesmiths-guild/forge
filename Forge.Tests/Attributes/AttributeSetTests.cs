// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Attributes;

public class AttributeSetTests
{
	[Fact]
	[Trait("Initialization", null)]
	public void Non_initialized_attribute_are_null()
	{
		var set = new SimpleAttributeSet();

		EntityAttribute attribute = set.NonInitializedAttribute;

		attribute.Should().Be(null);
	}

	[Fact]
	[Trait("Initialization", null)]
	public void Initialized_attribute_has_the_configured_default_values()
	{
		var set = new SimpleAttributeSet();
		EntityAttribute attribute = set.InitializedAttribute;

		attribute.BaseValue.Should().Be(5);
		attribute.Modifier.Should().Be(0);
		attribute.Overflow.Should().Be(0);
		attribute.Min.Should().Be(0);
		attribute.Max.Should().Be(10);
		attribute.CurrentValue.Should().Be(5);
	}

	[Fact]
	[Trait("Initialization", null)]
	public void Vital_attribute_set_attributes_initialize_with_configured_values()
	{
		var set = new VitalAttributeSet();

		set.Vitality.BaseValue.Should().Be(10);
		set.Vitality.Modifier.Should().Be(0);
		set.Vitality.Overflow.Should().Be(0);
		set.Vitality.Min.Should().Be(0);
		set.Vitality.Max.Should().Be(99);
		set.Vitality.CurrentValue.Should().Be(10);

		set.MaxHealth.BaseValue.Should().Be(100);
		set.MaxHealth.Modifier.Should().Be(0);
		set.MaxHealth.Overflow.Should().Be(0);
		set.MaxHealth.Min.Should().Be(0);
		set.MaxHealth.Max.Should().Be(1000);
		set.MaxHealth.CurrentValue.Should().Be(100);

		set.CurrentHealth.BaseValue.Should().Be(100);
		set.CurrentHealth.Modifier.Should().Be(0);
		set.CurrentHealth.Overflow.Should().Be(0);
		set.CurrentHealth.Min.Should().Be(0);
		set.CurrentHealth.Max.Should().Be(100);
		set.CurrentHealth.CurrentValue.Should().Be(100);
	}

	[Fact]
	[Trait("SetValue", null)]
	public void SetMaxValue_updates_the_max_value_of_the_attribute()
	{
		var set = new VitalAttributeSet();

		set.CurrentHealth.Max.Should().Be(100);

		set.UpdateMaxHealth(200);

		set.CurrentHealth.Max.Should().Be(200);
	}

	[Fact]
	[Trait("SetValue", null)]
	public void SetMaxValue_clamps_base_value_when_it_exceeds_new_max()
	{
		var set = new VitalAttributeSet();

		set.CurrentHealth.BaseValue.Should().Be(100);
		set.CurrentHealth.Max.Should().Be(100);

		set.UpdateMaxHealth(50);

		set.CurrentHealth.Max.Should().Be(50);
		set.CurrentHealth.BaseValue.Should().Be(50);
		set.CurrentHealth.CurrentValue.Should().Be(50);
	}

	[Fact]
	[Trait("SetValue", null)]
	public void SetMaxValue_does_not_change_base_value_when_it_is_within_new_max()
	{
		var set = new VitalAttributeSet();

		set.CurrentHealth.BaseValue.Should().Be(100);
		set.CurrentHealth.Max.Should().Be(100);

		set.UpdateMaxHealth(150);

		set.CurrentHealth.Max.Should().Be(150);
		set.CurrentHealth.BaseValue.Should().Be(100);
		set.CurrentHealth.CurrentValue.Should().Be(100);
	}

	[Fact]
	[Trait("SetValue", null)]
	public void SetMinValue_updates_the_min_value_of_the_attribute()
	{
		var set = new VitalAttributeSet();

		set.CurrentHealth.Min.Should().Be(0);

		set.UpdateMinHealth(10);

		set.CurrentHealth.Min.Should().Be(10);
	}

	[Fact]
	[Trait("SetValue", null)]
	public void SetMinValue_clamps_base_value_when_it_is_below_new_min()
	{
		var set = new VitalAttributeSet();

		set.UpdateMaxHealth(200);
		set.UpdateBaseHealth(50);

		set.CurrentHealth.BaseValue.Should().Be(50);
		set.CurrentHealth.Min.Should().Be(0);

		set.UpdateMinHealth(75);

		set.CurrentHealth.Min.Should().Be(75);
		set.CurrentHealth.BaseValue.Should().Be(75);
		set.CurrentHealth.CurrentValue.Should().Be(75);
	}

	[Fact]
	[Trait("SetValue", null)]
	public void SetMinValue_does_not_change_base_value_when_it_is_above_new_min()
	{
		var set = new VitalAttributeSet();

		set.CurrentHealth.BaseValue.Should().Be(100);
		set.CurrentHealth.Min.Should().Be(0);

		set.UpdateMinHealth(10);

		set.CurrentHealth.Min.Should().Be(10);
		set.CurrentHealth.BaseValue.Should().Be(100);
		set.CurrentHealth.CurrentValue.Should().Be(100);
	}

	private sealed class SimpleAttributeSet : AttributeSet
	{
		public EntityAttribute InitializedAttribute { get; }

#pragma warning disable S3459 // Unassigned members should be removed
		public EntityAttribute NonInitializedAttribute { get; }
#pragma warning restore S3459 // Unassigned members should be removed

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
		public SimpleAttributeSet()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
		{
			InitializedAttribute = InitializeAttribute(nameof(InitializedAttribute), 5, 0, 10);
		}
	}
}
