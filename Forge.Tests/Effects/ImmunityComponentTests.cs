// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Effects;

public class ImmunityComponentTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private const string TargetAttribute = "TestAttributeSet.Attribute1";

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Theory]
	[Trait("Blocking", null)]
	[InlineData("color.red", "color.blue")]
	[InlineData("enemy.beast.wolf", "enemy.undead.ghoul")]
	[InlineData("item.equipment.weapon.axe", "item.consumable.potion.health")]
	public void Immunity_blocks_matching_effects_and_allows_the_rest(string blockedTagKey, string allowedTagKey)
	{
		TestEntity target = CreateEntity();

		ApplyImmunity(target, [MakeQuery(blockedTagKey)]);

		target.EffectsManager.ApplyEffect(CreateTaggedEffect(target, blockedTagKey)).Should().BeNull();
		target.EffectsManager.ApplyEffect(CreateTaggedEffect(target, allowedTagKey)).Should().NotBeNull();
	}

	[Fact]
	[Trait("Blocking", null)]
	public void Immunity_blocks_instant_effects()
	{
		TestEntity target = CreateEntity();

		ApplyImmunity(target, [MakeQuery("color.red")]);

		target.EffectsManager.ApplyEffect(CreateTaggedEffect(target, "color.red", DurationType.Instant));

		// The instant effect never executed, so the attribute keeps its initial value.
		target.PlayerAttributeSet.Attribute1.CurrentValue.Should().Be(1);

		target.EffectsManager.ApplyEffect(CreateTaggedEffect(target, "color.blue", DurationType.Instant));

		target.PlayerAttributeSet.Attribute1.CurrentValue.Should().Be(6);
	}

	[Fact]
	[Trait("Blocking", null)]
	public void Immunity_matches_any_of_its_queries()
	{
		TestEntity target = CreateEntity();

		ApplyImmunity(target, [MakeQuery("color.red"), MakeQuery("color.blue")]);

		target.EffectsManager.ApplyEffect(CreateTaggedEffect(target, "color.red")).Should().BeNull();
		target.EffectsManager.ApplyEffect(CreateTaggedEffect(target, "color.blue")).Should().BeNull();
		target.EffectsManager.ApplyEffect(CreateTaggedEffect(target, "color.green")).Should().NotBeNull();
	}

	[Fact]
	[Trait("Blocking", null)]
	public void Immunity_matches_effect_tags_hierarchically()
	{
		TestEntity target = CreateEntity();

		ApplyImmunity(target, [MakeQuery("color.dark")]);

		target.EffectsManager.ApplyEffect(CreateTaggedEffect(target, "color.dark.red")).Should().BeNull();
		target.EffectsManager.ApplyEffect(CreateTaggedEffect(target, "color.red")).Should().NotBeNull();
	}

	[Fact]
	[Trait("Blocking", null)]
	public void Immunity_can_block_by_source_which_target_tags_cannot_reach()
	{
		TestEntity target = CreateEntity();
		TestEntity attacker = CreateEntity();
		TestEntity bystander = CreateEntity();

		ApplyImmunity(target, [new EffectQuery(EffectSource: attacker)]);

		target.EffectsManager
			.ApplyEffect(CreateTaggedEffect(attacker, "color.red")).Should().BeNull();
		target.EffectsManager
			.ApplyEffect(CreateTaggedEffect(bystander, "color.red")).Should().NotBeNull();
	}

	[Fact]
	[Trait("Inhibition", null)]
	public void Immunity_stops_blocking_while_inhibited_and_resumes_afterwards()
	{
		TestEntity target = CreateEntity();

		// The immunity is suppressed while the target carries color.blue, which it starts without.
		ActiveEffectHandle immunity = ApplyImmunity(
			target,
			[MakeQuery("color.red")],
			inhibitingTags: MakeContainer("color.blue"));

		immunity.IsInhibited.Should().BeFalse();
		target.EffectsManager.ApplyEffect(CreateTaggedEffect(target, "color.red")).Should().BeNull();

		ActiveEffectHandle inhibitor = GrantTags(target, "color.blue");

		immunity.IsInhibited.Should().BeTrue();
		ActiveEffectHandle? landed = target.EffectsManager.ApplyEffect(CreateTaggedEffect(target, "color.red"));
		landed.Should().NotBeNull();

		target.EffectsManager.RemoveEffect(inhibitor, true);

		immunity.IsInhibited.Should().BeFalse();
		target.EffectsManager.ApplyEffect(CreateTaggedEffect(target, "color.red")).Should().BeNull();

		// Immunity decides on arrival: the effect that landed while it was suppressed stays.
		landed!.IsValid.Should().BeTrue();
	}

	[Fact]
	[Trait("Removal", null)]
	public void Immunity_unregisters_when_its_effect_is_removed()
	{
		TestEntity target = CreateEntity();
		ActiveEffectHandle immunity = ApplyImmunity(target, [MakeQuery("color.red")]);

		target.EffectsManager.ApplyEffect(CreateTaggedEffect(target, "color.red")).Should().BeNull();

		target.EffectsManager.RemoveEffect(immunity, true);

		target.EffectsManager.ApplyEffect(CreateTaggedEffect(target, "color.red")).Should().NotBeNull();
	}

	[Fact]
	[Trait("Removal", null)]
	public void Immunity_unregisters_when_its_effect_expires()
	{
		TestEntity target = CreateEntity();

		ApplyImmunity(target, [MakeQuery("color.red")], duration: 10f);

		target.EffectsManager.ApplyEffect(CreateTaggedEffect(target, "color.red")).Should().BeNull();

		target.EffectsManager.UpdateEffects(10f);

		target.EffectsManager.ApplyEffect(CreateTaggedEffect(target, "color.red")).Should().NotBeNull();
	}

	[Fact]
	[Trait("Event", null)]
	public void A_blocked_application_reports_the_immunity_component_instance()
	{
		TestEntity target = CreateEntity();
		ActiveEffectHandle immunity = ApplyImmunity(target, [MakeQuery("color.red")]);

		Effect? blockedEffect = null;
		IEffectApplicationBlocker? reportedBlocker = null;

		target.EffectsManager.OnEffectApplicationBlocked += (effect, blocker) =>
		{
			blockedEffect = effect;
			reportedBlocker = blocker;
		};

		Effect incoming = CreateTaggedEffect(target, "color.red");
		target.EffectsManager.ApplyEffect(incoming);

		blockedEffect.Should().BeSameAs(incoming);
		reportedBlocker.Should().BeSameAs(immunity.GetComponent<ImmunityEffectComponent>());
	}

	[Fact]
	[Trait("Event", null)]
	public void An_allowed_application_raises_no_event()
	{
		TestEntity target = CreateEntity();
		ApplyImmunity(target, [MakeQuery("color.red")]);

		bool raised = false;
		target.EffectsManager.OnEffectApplicationBlocked += (_, _) => raised = true;

		target.EffectsManager.ApplyEffect(CreateTaggedEffect(target, "color.blue")).Should().NotBeNull();

		raised.Should().BeFalse();
	}

	[Fact]
	[Trait("Blocking", null)]
	public void An_empty_query_blocks_nothing()
	{
		TestEntity target = CreateEntity();

		// Validation is disabled here; EffectQueryComponentsValidationTests covers the assert that rejects it.
		ApplyImmunity(target, [default]);

		target.EffectsManager.ApplyEffect(CreateTaggedEffect(target, "color.red")).Should().NotBeNull();
	}

	[Fact]
	[Trait("Blocking", null)]
	public void Immunity_only_protects_its_own_target()
	{
		TestEntity immune = CreateEntity();
		TestEntity vulnerable = CreateEntity();

		ApplyImmunity(immune, [MakeQuery("color.red")]);

		immune.EffectsManager.ApplyEffect(CreateTaggedEffect(immune, "color.red")).Should().BeNull();
		vulnerable.EffectsManager.ApplyEffect(CreateTaggedEffect(vulnerable, "color.red")).Should().NotBeNull();
	}

	[Fact]
	[Trait("Blocking", null)]
	public void Each_immunity_application_blocks_on_its_own_instance()
	{
		TestEntity first = CreateEntity();
		TestEntity second = CreateEntity();

		// The same EffectData applied to two entities: removing one immunity must not disarm the other.
		EffectData immunityData = CreateImmunityData([MakeQuery("color.red")]);

		ActiveEffectHandle firstImmunity = Apply(first, immunityData)!;
		Apply(second, immunityData);

		first.EffectsManager.RemoveEffect(firstImmunity, true);

		first.EffectsManager.ApplyEffect(CreateTaggedEffect(first, "color.red")).Should().NotBeNull();
		second.EffectsManager.ApplyEffect(CreateTaggedEffect(second, "color.red")).Should().BeNull();
	}

	private static Modifier[] CreateModifiers()
	{
		return
		[
			new Modifier(
				TargetAttribute,
				ModifierOperation.FlatBonus,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(5)))
		];
	}

	private static ActiveEffectHandle? Apply(IForgeEntity target, EffectData effectData)
	{
		return target.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(target, target)));
	}

	private static EffectData CreateImmunityData(
		EffectQuery[] queries,
		TagContainer? inhibitingTags = null,
		float duration = 0f)
	{
		IEffectComponent[] components = inhibitingTags is null
			? [new ImmunityEffectComponent(queries)]
			:
			[
				new ImmunityEffectComponent(queries),
				new TargetTagRequirementsEffectComponent(
					ongoingTagRequirements: new TagRequirements(IgnoreTags: inhibitingTags))
			];

		DurationData durationData = duration > 0f
			? new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(duration)))
			: new DurationData(DurationType.Infinite);

		return new EffectData("Immunity", durationData, effectComponents: components);
	}

	private static ActiveEffectHandle ApplyImmunity(
		TestEntity target,
		EffectQuery[] queries,
		TagContainer? inhibitingTags = null,
		float duration = 0f)
	{
		return Apply(target, CreateImmunityData(queries, inhibitingTags, duration))!;
	}

	private Effect CreateTaggedEffect(
		IForgeEntity source,
		string effectTagKey,
		DurationType durationType = DurationType.Infinite)
	{
		var effectData = new EffectData(
			$"Incoming {effectTagKey}",
			new DurationData(durationType),
			CreateModifiers(),
			effectTags: MakeContainer(effectTagKey));

		return new Effect(effectData, new EffectOwnership(source, source));
	}

	private ActiveEffectHandle GrantTags(TestEntity target, params string[] tagKeys)
	{
		var tagEffectData = new EffectData(
			"Tag Granter",
			new DurationData(DurationType.Infinite),
			effectComponents: [new ModifierTagsEffectComponent(MakeContainer(tagKeys))]);

		return Apply(target, tagEffectData)!;
	}

	private TagContainer MakeContainer(params string[] tagKeys)
	{
		return new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, tagKeys));
	}

	private EffectQuery MakeQuery(params string[] tagKeys)
	{
		return new EffectQuery(EffectTagQuery: TagQuery.MakeQueryMatchAnyTags(MakeContainer(tagKeys)));
	}

	private TestEntity CreateEntity()
	{
		return new TestEntity(_tagsManager, _cuesManager);
	}
}
