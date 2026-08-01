// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Periodic;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Effects;

public sealed class EffectQueryComponentsValidationTests : IClassFixture<TagsAndCuesFixture>, IDisposable
{
	private readonly TagsManager _tagsManager;

	public EffectQueryComponentsValidationTests(TagsAndCuesFixture tagsAndCuesFixture)
	{
		_tagsManager = tagsAndCuesFixture.TagsManager;
		Validation.Enabled = true;
	}

	public void Dispose()
	{
		Validation.Enabled = false;
		GC.SuppressFinalize(this);
	}

	[Fact]
	[Trait("Immunity", null)]
	public void Immunity_on_an_instant_effect_is_rejected()
	{
		Action act = () => _ = CreateEffectData(
			new DurationData(DurationType.Instant),
			new ImmunityEffectComponent([MakeQuery("color.red")]));

		act.Should().Throw<ValidationException>();
	}

	[Theory]
	[Trait("Immunity", null)]
	[InlineData(DurationType.Infinite)]
	[InlineData(DurationType.HasDuration)]
	public void Immunity_on_a_non_instant_effect_is_accepted(DurationType durationType)
	{
		Action act = () => _ = CreateEffectData(
			CreateDurationData(durationType),
			new ImmunityEffectComponent([MakeQuery("color.red")]));

		act.Should().NotThrow();
	}

	[Fact]
	[Trait("Immunity", null)]
	public void An_immunity_with_no_queries_at_all_is_accepted()
	{
		Action act = () => _ = CreateEffectData(
			new DurationData(DurationType.Infinite),
			new ImmunityEffectComponent([]));

		act.Should().NotThrow();
	}

	[Fact]
	[Trait("Immunity", null)]
	public void An_immunity_with_an_empty_query_is_rejected()
	{
		Action act = () => _ = CreateEffectData(
			new DurationData(DurationType.Infinite),
			new ImmunityEffectComponent([default]));

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("Immunity", null)]
	public void An_empty_query_is_rejected_even_alongside_valid_ones()
	{
		Action act = () => _ = CreateEffectData(
			new DurationData(DurationType.Infinite),
			new ImmunityEffectComponent([MakeQuery("color.red"), default]));

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("RemoveOther", null)]
	public void Remove_other_on_a_periodic_effect_is_rejected()
	{
		Action act = () => _ = new EffectData(
			"Periodic Remover",
			CreateDurationData(DurationType.HasDuration),
			periodicData: new PeriodicData(
				new ScalableFloat(1f),
				true,
				PeriodInhibitionRemovedPolicy.NeverReset),
			effectComponents: [new RemoveOtherEffectComponent([MakeQuery("color.red")])]);

		act.Should().Throw<ValidationException>();
	}

	[Theory]
	[Trait("RemoveOther", null)]
	[InlineData(DurationType.Instant)]
	[InlineData(DurationType.Infinite)]
	[InlineData(DurationType.HasDuration)]
	public void Remove_other_on_a_non_periodic_effect_is_accepted(DurationType durationType)
	{
		Action act = () => _ = CreateEffectData(
			CreateDurationData(durationType),
			new RemoveOtherEffectComponent([MakeQuery("color.red")]));

		act.Should().NotThrow();
	}

	[Fact]
	[Trait("RemoveOther", null)]
	public void A_remover_with_no_queries_at_all_is_accepted()
	{
		Action act = () => _ = CreateEffectData(
			new DurationData(DurationType.Instant),
			new RemoveOtherEffectComponent([]));

		act.Should().NotThrow();
	}

	[Fact]
	[Trait("RemoveOther", null)]
	public void A_remover_with_an_empty_query_is_rejected()
	{
		Action act = () => _ = CreateEffectData(
			new DurationData(DurationType.Instant),
			new RemoveOtherEffectComponent([default]));

		act.Should().Throw<ValidationException>();
	}

	[Theory]
	[Trait("Queries", null)]
	[InlineData("effectTags")]
	[InlineData("grantedTags")]
	[InlineData("sourceTags")]
	[InlineData("attribute")]
	public void A_query_with_a_single_filter_is_enough(string filter)
	{
		EffectQuery query = filter switch
		{
			"effectTags" => MakeQuery("color.red"),
			"grantedTags" => new EffectQuery(
				GrantedTagQuery: TagQuery.MakeQueryMatchAnyTags(MakeContainer("color.red"))),
			"sourceTags" => new EffectQuery(
				SourceTagRequirements: new TagRequirements(RequiredTags: MakeContainer("color.red"))),
			_ => new EffectQuery(ModifyingAttribute: "TestAttributeSet.Attribute1"),
		};

		Action act = () => _ = CreateEffectData(
			new DurationData(DurationType.Infinite),
			new ImmunityEffectComponent([query]));

		act.Should().NotThrow();
	}

	private static DurationData CreateDurationData(DurationType durationType)
	{
		return durationType == DurationType.HasDuration
			? new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10f)))
			: new DurationData(durationType);
	}

	private static EffectData CreateEffectData(DurationData durationData, IEffectComponent component)
	{
		return new EffectData("Query Component Effect", durationData, effectComponents: [component]);
	}

	private TagContainer MakeContainer(params string[] tagKeys)
	{
		return new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, tagKeys));
	}

	private EffectQuery MakeQuery(params string[] tagKeys)
	{
		return new EffectQuery(EffectTagQuery: TagQuery.MakeQueryMatchAnyTags(MakeContainer(tagKeys)));
	}
}
