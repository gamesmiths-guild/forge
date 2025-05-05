// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Attribute = Gamesmiths.Forge.Core.Attribute;

namespace Gamesmiths.Forge.Tests.Core;

public class AttributeSetTests
{
	[Fact]
	[Trait("Initialization", null)]
	public void Non_initialized_attribute_are_null()
	{
		var set = new SimpleAttributeSet();

		Attribute attribute = set.NonInitializedAttribute;

		attribute.Should().Be(null);
	}

	[Fact]
	public void Initialized_attribute_has_the_configured_default_values()
	{
		var set = new SimpleAttributeSet();
		Attribute attribute = set.InitializedAttribute;

		attribute.BaseValue.Should().Be(5);
		attribute.Modifier.Should().Be(0);
		attribute.Overflow.Should().Be(0);
		attribute.Min.Should().Be(0);
		attribute.Max.Should().Be(10);
		attribute.CurrentValue.Should().Be(5);
	}

	[Fact]
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

	private sealed class SimpleAttributeSet : AttributeSet
	{
		public Attribute InitializedAttribute { get; }

#pragma warning disable S3459 // Unassigned members should be removed
		public Attribute NonInitializedAttribute { get; }
#pragma warning restore S3459 // Unassigned members should be removed

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
		public SimpleAttributeSet()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
		{
			InitializedAttribute = InitializeAttribute(nameof(InitializedAttribute), 5, 0, 10);
		}
	}

	private sealed class VitalAttributeSet : AttributeSet
	{
		public Attribute Vitality { get; }

		public Attribute MaxHealth { get; }

		public Attribute CurrentHealth { get; }

		public VitalAttributeSet()
		{
			Vitality = InitializeAttribute(nameof(Vitality), 10, 0, 99);
			MaxHealth = InitializeAttribute(nameof(MaxHealth), Vitality.CurrentValue * 10, 0, 1000);
			CurrentHealth = InitializeAttribute(nameof(CurrentHealth), 100, 0, MaxHealth.CurrentValue);
		}

		protected override void AttributeOnValueChanged(Attribute attribute, int change)
		{
			base.AttributeOnValueChanged(attribute, change);

			if (attribute == Vitality)
			{
				// Do health to vit calculations here.
				SetAttributeMaxValue(MaxHealth, Vitality.CurrentValue * 10);
			}

			if (attribute == MaxHealth)
			{
				SetAttributeMaxValue(CurrentHealth, MaxHealth.CurrentValue);
			}

			if (attribute == CurrentHealth)
			{
				if (change < 0)
				{
					Console.WriteLine($"Damage: {change}");

					if (CurrentHealth.CurrentValue <= 0)
					{
						Console.WriteLine("Death");
					}
				}
				else
				{
					Console.WriteLine($"Healing: {change}");
				}
			}
		}
	}
}
