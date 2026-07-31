// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Tests.Effects;

public sealed class RequirementsComponentsValidationTests : IDisposable
{
	private const string HealthAttribute = "VitalAttributeSet.CurrentHealth";

	public RequirementsComponentsValidationTests()
	{
		Validation.Enabled = true;
	}

	public void Dispose()
	{
		Validation.Enabled = false;
		GC.SuppressFinalize(this);
	}

	[Fact]
	[Trait("AttributeRequirements", null)]
	public void An_attribute_requirement_without_any_bound_is_rejected()
	{
		Action act = () => _ = CreateEffectData([new AttributeRequirement(HealthAttribute)]);

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("AttributeRequirements", null)]
	public void An_unbounded_requirement_is_rejected_even_alongside_valid_ones()
	{
		Action act = () => _ = CreateEffectData(
		[
			new AttributeRequirement(HealthAttribute, MaxValue: 50),
			new AttributeRequirement("VitalAttributeSet.Vitality")
		]);

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("AttributeRequirements", null)]
	public void A_requirement_with_only_a_minimum_is_accepted()
	{
		Action act = () => _ = CreateEffectData([new AttributeRequirement(HealthAttribute, MinValue: 1)]);

		act.Should().NotThrow();
	}

	[Fact]
	[Trait("AttributeRequirements", null)]
	public void A_requirement_with_only_a_maximum_is_accepted()
	{
		Action act = () => _ = CreateEffectData([new AttributeRequirement(HealthAttribute, MaxValue: 50)]);

		act.Should().NotThrow();
	}

	[Fact]
	[Trait("AttributeRequirements", null)]
	public void A_requirement_with_both_bounds_is_accepted()
	{
		Action act = () => _ = CreateEffectData(
			[new AttributeRequirement(HealthAttribute, MinValue: 1, MaxValue: 50)]);

		act.Should().NotThrow();
	}

	[Fact]
	[Trait("AttributeRequirements", null)]
	public void A_component_with_no_requirements_at_all_is_accepted()
	{
		Action act = () => _ = new EffectData(
			"Empty Requirements",
			new DurationData(DurationType.Infinite),
			effectComponents: [new AttributeRequirementsEffectComponent()]);

		act.Should().NotThrow();
	}

	[Fact]
	[Trait("AttributeRequirements", null)]
	public void The_unbounded_check_covers_the_removal_and_ongoing_buckets()
	{
		Action removal = () => _ = new EffectData(
			"Unbounded Removal",
			new DurationData(DurationType.Infinite),
			effectComponents:
			[
				new AttributeRequirementsEffectComponent(
					removalRequirements: [new AttributeRequirement(HealthAttribute)])
			]);

		Action ongoing = () => _ = new EffectData(
			"Unbounded Ongoing",
			new DurationData(DurationType.Infinite),
			effectComponents:
			[
				new AttributeRequirementsEffectComponent(
					ongoingRequirements: [new AttributeRequirement(HealthAttribute)])
			]);

		removal.Should().Throw<ValidationException>();
		ongoing.Should().Throw<ValidationException>();
	}

	// Inhibition acts on an active effect, and instant effects never become one, so ongoing requirements on an instant
	// effect are dead configuration. Every requirements component is checked for it.
	[Theory]
	[Trait("OngoingOnInstant", null)]
	[InlineData("target-tags")]
	[InlineData("source-tags")]
	[InlineData("target-attributes")]
	[InlineData("source-attributes")]
	public void Ongoing_requirements_on_an_instant_effect_are_rejected(string component)
	{
		Action act = () => _ = new EffectData(
			"Instant With Ongoing",
			new DurationData(DurationType.Instant),
			effectComponents: [CreateComponentWithOngoingRequirements(component)]);

		act.Should().Throw<ValidationException>();
	}

	[Theory]
	[Trait("OngoingOnInstant", null)]
	[InlineData("target-tags")]
	[InlineData("source-tags")]
	[InlineData("target-attributes")]
	[InlineData("source-attributes")]
	public void Ongoing_requirements_on_a_duration_effect_are_accepted(string component)
	{
		Action act = () => _ = new EffectData(
			"Infinite With Ongoing",
			new DurationData(DurationType.Infinite),
			effectComponents: [CreateComponentWithOngoingRequirements(component)]);

		act.Should().NotThrow();
	}

	// An unconfigured Godot resource still hands over three TagRequirements, just empty ones. Those must not trip the
	// ongoing check, or every instant effect authored in the editor would fail validation.
	[Fact]
	[Trait("OngoingOnInstant", null)]
	public void Empty_ongoing_requirements_on_an_instant_effect_are_accepted()
	{
		Action act = () => _ = new EffectData(
			"Instant With Empty Ongoing",
			new DurationData(DurationType.Instant),
			effectComponents:
			[
				new TargetTagRequirementsEffectComponent(
					ongoingTagRequirements: default(TagRequirements)),
				new SourceTagRequirementsEffectComponent(
					ongoingTagRequirements: default(TagRequirements)),
				new AttributeRequirementsEffectComponent(ongoingRequirements: []),
				new SourceAttributeRequirementsEffectComponent(ongoingRequirements: [])
			]);

		act.Should().NotThrow();
	}

	[Fact]
	[Trait("OngoingOnInstant", null)]
	public void Application_requirements_on_an_instant_effect_are_accepted()
	{
		Action act = () => _ = new EffectData(
			"Instant Gate",
			new DurationData(DurationType.Instant),
			effectComponents:
			[
				new AttributeRequirementsEffectComponent(
					applicationRequirements: [new AttributeRequirement(HealthAttribute, MaxValue: 25)])
			]);

		act.Should().NotThrow();
	}

	private static IEffectComponent CreateComponentWithOngoingRequirements(string component)
	{
		AttributeRequirement[] attributeRequirements = [new AttributeRequirement(HealthAttribute, MinValue: 1)];

		return component switch
		{
			"target-tags" => new TargetTagRequirementsEffectComponent(
				ongoingTagRequirements: new TagRequirements(RequiredTags: EmptyContainerWith("color.red"))),
			"source-tags" => new SourceTagRequirementsEffectComponent(
				ongoingTagRequirements: new TagRequirements(RequiredTags: EmptyContainerWith("color.red"))),
			"target-attributes" => new AttributeRequirementsEffectComponent(
				ongoingRequirements: attributeRequirements),
			"source-attributes" => new SourceAttributeRequirementsEffectComponent(
				ongoingRequirements: attributeRequirements),
			_ => throw new ArgumentOutOfRangeException(nameof(component)),
		};
	}

	private static TagContainer EmptyContainerWith(string tagKey)
	{
		var tagsManager = new TagsManager([tagKey]);
		return tagsManager.RequestTagContainer([tagKey]);
	}

	private static EffectData CreateEffectData(AttributeRequirement[] applicationRequirements)
	{
		return new EffectData(
			"Gated Effect",
			new DurationData(DurationType.Infinite),
			effectComponents: [new AttributeRequirementsEffectComponent(applicationRequirements)]);
	}
}
