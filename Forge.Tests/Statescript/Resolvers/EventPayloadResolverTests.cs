// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class EventPayloadResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;

	[Fact]
	[Trait("Resolver", "EventPayload")]
	public void Resolver_value_type_is_the_payload_raiser()
	{
		var resolver = new EventPayloadResolver(new TestEventPayloadProvider());

		resolver.ValueType.Should().Be(typeof(EventPayloadRaiser));
	}

	[Fact]
	[Trait("Resolver", "EventPayload")]
	public void Raiser_raises_a_typed_event_with_the_payload_built_from_declared_inputs()
	{
		var context = new GraphContext();
		EventPayloadRaiser raiser = new EventPayloadResolver(
			new TestEventPayloadProvider(),
			new Dictionary<string, IPropertyResolver>
			{
				[TestEventPayloadProvider.AmountKey] = new VariantResolver(new Variant128(42), typeof(int)),
			}).Resolve(context);

		var manager = new EventManager();
		var eventTag = Tag.RequestTag(_tagsManager, "test.cue1");
		TestEventPayload? captured = null;
		manager.Subscribe<TestEventPayload>(eventTag, data => captured = data.Payload);

		raiser.Raise(manager, eventTag.GetSingleTagContainer()!, null, null, 0f, context);

		captured.Should().NotBeNull();
		captured!.Amount.Should().Be(42);
	}

	[Fact]
	[Trait("Resolver", "EventPayload")]
	public void Raiser_uses_default_input_value_when_no_resolver_is_bound()
	{
		var context = new GraphContext();
		EventPayloadRaiser raiser = new EventPayloadResolver(new TestEventPayloadProvider()).Resolve(context);

		var manager = new EventManager();
		var eventTag = Tag.RequestTag(_tagsManager, "test.cue1");
		TestEventPayload? captured = null;
		manager.Subscribe<TestEventPayload>(eventTag, data => captured = data.Payload);

		raiser.Raise(manager, eventTag.GetSingleTagContainer()!, null, null, 0f, context);

		captured.Should().NotBeNull();
		captured!.Amount.Should().Be(0);
	}
}
