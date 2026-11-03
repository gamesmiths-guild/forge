// Copyright © Gamesmiths Guild.

using System.Reflection;
using FluentAssertions;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Effects.Periodic;
using Gamesmiths.Forge.Effects.Stacking;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Core;
using Gamesmiths.Forge.Tests.Helpers;

// A leak test has to collect to prove something was let go of.
#pragma warning disable S1215

namespace Gamesmiths.Forge.Tests.Performance;

/// <summary>
/// Nothing an effect holds while it is active may outlive it. An active effect is on its shared <see cref="Effect"/>'s
/// list of live applications and subscribes to the attributes it reads; once it ends, both must be gone, and an entity
/// that is dropped must be collectable even though effects it applied, and effects applied to it, are still around.
/// </summary>
/// <param name="fixture">The fixture providing the <see cref="TagsManager"/> and <see cref="CuesManager"/>.</param>
public class EffectLeakTests(TagsAndCuesFixture fixture) : IClassFixture<TagsAndCuesFixture>
{
	// A power of two, so durations authored as floats are whole numbers of steps.
	private const double Step = 1.0 / 64.0;

	private const int Applications = 2_000;

	private readonly TagsManager _tagsManager = fixture.TagsManager;
	private readonly CuesManager _cuesManager = fixture.CuesManager;

	[Fact]
	[Trait("Performance", "Leak")]
	public void Expired_effects_leave_the_shared_effect_with_no_applications()
	{
		var source = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);
		var effect = new Effect(TimedBuff(0.5f), new EffectOwnership(source, source));

		for (int i = 0; i < Applications; i++)
		{
			target.EffectsManager.ApplyEffect(effect);
		}

		// Every live application is on the effect's list, so a level or set-by-caller change reaches it.
		ApplicationCount(effect).Should().Be(Applications);
		target.EffectsManager.GetActiveEffects().Should().HaveCount(Applications);

		Tick(target, seconds: 1);

		target.EffectsManager.GetActiveEffects().Should().BeEmpty();
		ApplicationCount(effect).Should().Be(0);
	}

	[Fact]
	[Trait("Performance", "Leak")]
	public void A_level_change_reaches_every_live_application_and_none_that_ended()
	{
		var source = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);
		var effect = new Effect(LeveledBuff(), new EffectOwnership(source, source));

		var handles = new List<ActiveEffectHandle>();

		for (int i = 0; i < 10; i++)
		{
			handles.Add(target.EffectsManager.ApplyEffect(effect)!);
		}

		// Ten applications of +1 per level.
		target.PlayerAttributeSet.Attribute1.CurrentValue.Should().Be(1 + 10);

		target.EffectsManager.RemoveEffect(handles[3]);
		target.EffectsManager.RemoveEffect(handles[7]);
		effect.LevelUp();

		// Eight live applications of +2; the two removed ones neither count nor were reached.
		target.PlayerAttributeSet.Attribute1.CurrentValue.Should().Be(1 + (8 * 2));
		ApplicationCount(effect).Should().Be(8);
	}

	[Fact]
	[Trait("Performance", "Leak")]
	public void Removed_effects_leave_no_subscriptions_on_the_attributes_they_read()
	{
		var source = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);
		EntityAttribute backing = source.PlayerAttributeSet.Attribute5;
		int before = SubscriberCount(backing, nameof(EntityAttribute.OnValueChanged));

		var effect = new Effect(AttributeBasedBuff(), new EffectOwnership(source, source));

		for (int i = 0; i < Applications; i++)
		{
			target.EffectsManager.ApplyEffect(effect);
		}

		SubscriberCount(backing, nameof(EntityAttribute.OnValueChanged))
			.Should().BeGreaterThan(before, "a non-snapshot attribute-based magnitude watches its backing attribute");

		target.EffectsManager.RemoveEffects(new EffectQuery(EffectDefinition: effect.EffectData));

		target.EffectsManager.GetActiveEffects().Should().BeEmpty();
		SubscriberCount(backing, nameof(EntityAttribute.OnValueChanged)).Should().Be(before);
	}

	[Fact]
	[Trait("Performance", "Leak")]
	public void A_stack_applied_and_expired_many_times_leaves_nothing_behind()
	{
		var source = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);
		var effect = new Effect(StackingBurn(0.25f), new EffectOwnership(source, source));

		for (int round = 0; round < 50; round++)
		{
			for (int i = 0; i < 40; i++)
			{
				target.EffectsManager.ApplyEffect(effect);
			}

			Tick(target, seconds: 0.5);
		}

		target.EffectsManager.GetActiveEffects().Should().BeEmpty();
		ApplicationCount(effect).Should().Be(0);
	}

	[Fact]
	[Trait("Performance", "Leak")]
	public void A_target_dropped_after_its_effects_ended_is_collected()
	{
		var source = new TestEntity(_tagsManager, _cuesManager);
		var effect = new Effect(TimedBuff(0.5f), new EffectOwnership(source, source));

		WeakReference reference = ApplyThenDrop(effect);

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		reference.IsAlive.Should().BeFalse(
			"the shared effect and its source are still alive, and neither may keep a former target reachable");
		GC.KeepAlive(effect);
		GC.KeepAlive(source);
	}

	/// <summary>
	/// An entity dropped while a shared effect is still active on it stays reachable: the <see cref="Effect"/>'s list
	/// of live applications holds the application, which holds its target. <see cref="EntitySimulation.Teardown"/> is
	/// what frees it, by ending its effects.
	/// </summary>
	[Fact]
	[Trait("Performance", "Leak")]
	public void A_target_torn_down_with_effects_still_active_is_collected()
	{
		var source = new TestEntity(_tagsManager, _cuesManager);
		var effect = new Effect(TimedBuff(1_000), new EffectOwnership(source, source));

		WeakReference reference = ApplyTearDownAndDrop(effect);

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		reference.IsAlive.Should().BeFalse(
			"a torn-down entity must not be pinned by the shared effect it was given");
		ApplicationCount(effect).Should().Be(0);
		GC.KeepAlive(effect);
		GC.KeepAlive(source);
	}

	[Fact]
	[Trait("Performance", "Leak")]
	public void An_owner_override_moves_the_application_to_the_new_owner_s_effect()
	{
		var first = new TestEntity(_tagsManager, _cuesManager);
		var second = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);
		EffectData buff = OverridingBuff(Flat("TestAttributeSet.Attribute1", 1));
		var firstEffect = new Effect(buff, new EffectOwnership(first, first));
		var secondEffect = new Effect(buff, new EffectOwnership(second, second));

		ActiveEffectHandle handle = target.EffectsManager.ApplyEffect(firstEffect)!;
		target.EffectsManager.ApplyEffect(secondEffect);

		handle.Effect.Should().BeSameAs(secondEffect);
		ApplicationCount(firstEffect).Should().Be(0);
		ApplicationCount(secondEffect).Should().Be(1);

		secondEffect.LevelUp();
		handle.Level.Should().Be(2, "the new owner's effect reaches the application it took over");

		target.EffectsManager.RemoveEffect(handle);

		ApplicationCount(firstEffect).Should().Be(0);
		ApplicationCount(secondEffect).Should().Be(0);
	}

	[Fact]
	[Trait("Performance", "Leak")]
	public void An_owner_override_watches_the_new_owner_s_attributes_instead_of_the_old_one_s()
	{
		var first = new TestEntity(_tagsManager, _cuesManager);
		var second = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);
		int firstBefore = SubscriberCount(
			first.PlayerAttributeSet.Attribute5,
			nameof(EntityAttribute.OnValueChanged));
		int secondBefore = SubscriberCount(
			second.PlayerAttributeSet.Attribute5,
			nameof(EntityAttribute.OnValueChanged));
		EffectData buff = OverridingBuff(AttributeBasedBuff().Modifiers[0]);

		ActiveEffectHandle handle =
			target.EffectsManager.ApplyEffect(new Effect(buff, new EffectOwnership(first, first)))!;
		target.EffectsManager.ApplyEffect(new Effect(buff, new EffectOwnership(second, second)));

		SubscriberCount(first.PlayerAttributeSet.Attribute5, nameof(EntityAttribute.OnValueChanged))
			.Should().Be(firstBefore, "the old owner's attribute no longer feeds the effect");
		SubscriberCount(second.PlayerAttributeSet.Attribute5, nameof(EntityAttribute.OnValueChanged))
			.Should().BeGreaterThan(secondBefore);

		target.EffectsManager.RemoveEffect(handle);

		SubscriberCount(second.PlayerAttributeSet.Attribute5, nameof(EntityAttribute.OnValueChanged))
			.Should().Be(secondBefore);
	}

	private static void Tick(TestEntity entity, double seconds)
	{
		int ticks = (int)(seconds / Step) + 1;

		for (long tick = 1; tick <= ticks; tick++)
		{
			EntitySimulation.Tick(entity, tick, Step);
		}
	}

	// The effect counts the applications on its list in a private field.
	private static int ApplicationCount(Effect effect)
	{
		FieldInfo? field = typeof(Effect).GetField(
			"_applicationCount",
			BindingFlags.Instance | BindingFlags.NonPublic);

		field.Should().NotBeNull("Effect should count its live applications in _applicationCount");

		return (int)field!.GetValue(effect)!;
	}

	// The compiler backs a field-like event with a private field of the same name holding the multicast delegate.
	private static int SubscriberCount(object owner, string eventName)
	{
		FieldInfo? field = owner.GetType().GetField(
			eventName,
			BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

		field.Should().NotBeNull($"{owner.GetType().Name} should back {eventName} with a field");

		return field!.GetValue(owner) is Delegate handlers ? handlers.GetInvocationList().Length : 0;
	}

	// A level that isn't snapshotted, so each application also subscribes to the effect's level.
	private static EffectData TimedBuff(float seconds)
	{
		return new EffectData(
			"Haste",
			Seconds(seconds),
			[Flat("TestAttributeSet.Attribute1", 1)],
			snapshotLevel: false);
	}

	// +1 per level on Attribute1, following the effect's level.
	private static EffectData LeveledBuff()
	{
		return new EffectData(
			"Leveled Haste",
			Seconds(1_000),
			[
				new Modifier(
					"TestAttributeSet.Attribute1",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(1, new Curve([new CurveKey(1, 1), new CurveKey(2, 2)]))))
			],
			snapshotLevel: false);
	}

	private static EffectData AttributeBasedBuff()
	{
		return new EffectData(
			"Scaled Haste",
			Seconds(1_000),
			[
				new Modifier(
					"TestAttributeSet.Attribute1",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.AttributeBased,
						attributeBasedFloat: new AttributeBasedFloat(
							new AttributeCaptureDefinition(
								"TestAttributeSet.Attribute5",
								AttributeCaptureSource.Owner,
								Snapshot: false),
							AttributeCalculationType.CurrentValue,
							new ScalableFloat(1),
							new ScalableFloat(0),
							new ScalableFloat(0))))
			]);
	}

	private static EffectData OverridingBuff(Modifier modifier)
	{
		return new EffectData(
			"Contested Haste",
			Seconds(1_000),
			[modifier],
			new StackingData(
				new ScalableInt(3),
				new ScalableInt(1),
				StackPolicy.AggregateByTarget,
				StackLevelPolicy.SegregateLevels,
				StackMagnitudePolicy.Sum,
				StackOverflowPolicy.AllowApplication,
				StackExpirationPolicy.ClearEntireStack,
				StackOwnerDenialPolicy.AlwaysAllow,
				StackOwnerOverridePolicy.Override,
				StackOwnerOverrideStackCountPolicy.IncreaseStacks,
				ApplicationRefreshPolicy: StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication,
				ApplicationResetPeriodPolicy: StackApplicationResetPeriodPolicy.NeverReset),
			snapshotLevel: false);
	}

	private static EffectData StackingBurn(float seconds)
	{
		return new EffectData(
			"Burning",
			Seconds(seconds),
			[Flat("TestAttributeSet.Attribute90", -1)],
			new StackingData(
				new ScalableInt(3),
				new ScalableInt(1),
				StackPolicy.AggregateByTarget,
				StackLevelPolicy.AggregateLevels,
				StackMagnitudePolicy.Sum,
				StackOverflowPolicy.AllowApplication,
				StackExpirationPolicy.ClearEntireStack,
				ApplicationRefreshPolicy: StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication,
				ApplicationResetPeriodPolicy: StackApplicationResetPeriodPolicy.NeverReset),
			new PeriodicData(new ScalableFloat(0.125f), false, PeriodInhibitionRemovedPolicy.NeverReset));
	}

	private static DurationData Seconds(float seconds)
	{
		return new DurationData(
			DurationType.HasDuration,
			new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(seconds)));
	}

	private static Modifier Flat(string attribute, float value)
	{
		return new Modifier(
			attribute,
			ModifierOperation.FlatBonus,
			new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(value)));
	}

	// Kept out of line, so the target never lives in the caller's frame where the JIT could keep it reachable.
	private WeakReference ApplyThenDrop(Effect effect)
	{
		var target = new TestEntity(_tagsManager, _cuesManager);

		for (int i = 0; i < 100; i++)
		{
			target.EffectsManager.ApplyEffect(effect);
		}

		Tick(target, seconds: 1);
		target.EffectsManager.GetActiveEffects().Should().BeEmpty();

		return new WeakReference(target);
	}

	private WeakReference ApplyTearDownAndDrop(Effect effect)
	{
		var target = new TestEntity(_tagsManager, _cuesManager);

		for (int i = 0; i < 100; i++)
		{
			target.EffectsManager.ApplyEffect(effect);
		}

		target.EffectsManager.GetActiveEffects().Should().HaveCount(100);
		EntitySimulation.Teardown(target);
		target.EffectsManager.GetActiveEffects().Should().BeEmpty();

		return new WeakReference(target);
	}
}
