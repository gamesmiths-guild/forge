// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Attributes;

namespace Gamesmiths.Forge.Tests.Helpers;

public sealed class VitalAttributeSet : AttributeSet
{
	public EntityAttribute Vitality { get; }

	public EntityAttribute MaxHealth { get; }

	public EntityAttribute CurrentHealth { get; }

	public VitalAttributeSet()
	{
		Vitality = InitializeAttribute(nameof(Vitality), 10, 0, 99);
		MaxHealth = InitializeAttribute(nameof(MaxHealth), Vitality.CurrentValue * 10, 0, 1000);
		CurrentHealth = InitializeAttribute(nameof(CurrentHealth), 100, 0, MaxHealth.CurrentValue);
	}

	public void UpdateMaxHealth(int newMax)
	{
		// These three Update methods exist exclusively for unit testing. In production, attribute changes should
		// always flow through effects and modifiers to ensure changes can be replicated, and done/undone through
		// effect application and removal.
		SetAttributeMaxValue(CurrentHealth, newMax);
	}

	public void UpdateMinHealth(int newMin)
	{
		SetAttributeMinValue(CurrentHealth, newMin);
	}

	public void UpdateBaseHealth(int newBase)
	{
		SetAttributeBaseValue(CurrentHealth, newBase);
	}

	protected override void AttributeOnValueChanged(EntityAttribute attribute, int change)
	{
		base.AttributeOnValueChanged(attribute, change);

		if (attribute == Vitality)
		{
			// Do health to vit calculations here. Those participate in the flow of attribute changes, so they
			// should be replicated fine.
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
