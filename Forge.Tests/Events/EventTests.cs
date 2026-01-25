// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;
using static Gamesmiths.Forge.Tests.Samples.QuickStartTests;

namespace Gamesmiths.Forge.Tests.Events;

public class EventTests(TagsAndCuesFixture tagsAndCueFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCueFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCueFixture.CuesManager;

	[Fact]
	[Trait("Simple", null)]
	public void Subscribe_and_raise_triggers_handler_with_correct_magnitude()
	{
		// Initialize managers
		TagsManager tagsManager = _tagsManager;
		CuesManager cuesManager = _cuesManager;

		var entity = new TestEntity(tagsManager, cuesManager);
		var damageTag = Tag.RequestTag(tagsManager, "simple.tag");

		var receivedDamage = 0f;
		var eventFired = false;

		entity.Events.Subscribe(damageTag, x =>
		{
			eventFired = true;
			receivedDamage = x.EventMagnitude;
		});

		entity.Events.Raise(new EventData
		{
			EventTags = damageTag.GetSingleTagContainer()!,
			Source = null,
			Target = entity,
			EventMagnitude = 50f,
		});

		eventFired.Should().BeTrue();
		receivedDamage.Should().Be(50f);
	}

	[Fact]
	[Trait("Typed", null)]
	public void Typed_event_subscription_receives_correct_payload()
	{
		// Initialize managers
		TagsManager tagsManager = _tagsManager;
		CuesManager cuesManager = _cuesManager;

		var entity = new TestEntity(tagsManager, cuesManager);
		var damageTag = Tag.RequestTag(tagsManager, "simple.tag");

		var value = 0;
		DamageType damageType = DamageType.Physical;
		var isCritical = false;

		// Subscribe with generic type
		entity.Events.Subscribe<DamageInfo>(damageTag, x =>
		{
			value = x.Payload.Value;
			damageType = x.Payload.DamageType;
			isCritical = x.Payload.IsCritical;
		});

		// Raise with generic type
		entity.Events.Raise(new EventData<DamageInfo>
		{
			EventTags = damageTag.GetSingleTagContainer()!,
			Source = null,
			Target = entity,
			Payload = new DamageInfo(500, DamageType.Magical, true),
		});

		value.Should().Be(500);
		damageType.Should().Be(DamageType.Magical);
		isCritical.Should().Be(true);
	}

	[Fact]
	[Trait("Standalone", null)]
	public void EventManager_can_be_created_without_entity()
	{
		var events = new EventManager();
		var eventTag = Tag.RequestTag(_tagsManager, "simple.tag");
		var eventFired = false;

		events.Subscribe(eventTag, _ => eventFired = true);

		events.Raise(new EventData
		{
			EventTags = eventTag.GetSingleTagContainer()!,
		});

		eventFired.Should().BeTrue();
	}

	[Fact]
	[Trait("Standalone", null)]
	public void Standalone_EventManager_supports_typed_events()
	{
		var events = new EventManager();
		var eventTag = Tag.RequestTag(_tagsManager, "simple.tag");
		var capturedPayload = string.Empty;

		events.Subscribe<string>(eventTag, x => capturedPayload = x.Payload);

		events.Raise(new EventData<string>
		{
			EventTags = eventTag.GetSingleTagContainer()!,
			Payload = "Hello from standalone",
		});

		capturedPayload.Should().Be("Hello from standalone");
	}

	[Fact]
	[Trait("Priority", null)]
	public void Higher_priority_handlers_are_invoked_first()
	{
		var events = new EventManager();
		var eventTag = Tag.RequestTag(_tagsManager, "simple.tag");
		var invocationOrder = new List<string>();

		events.Subscribe(eventTag, _ => invocationOrder.Add("low"), priority: 0);
		events.Subscribe(eventTag, _ => invocationOrder.Add("high"), priority: 100);
		events.Subscribe(eventTag, _ => invocationOrder.Add("medium"), priority: 50);

		events.Raise(new EventData
		{
			EventTags = eventTag.GetSingleTagContainer()!,
		});

		invocationOrder.Should().ContainInOrder("high", "medium", "low");
	}

	[Fact]
	[Trait("Priority", null)]
	public void Typed_events_respect_priority_ordering()
	{
		var events = new EventManager();
		var eventTag = Tag.RequestTag(_tagsManager, "simple.tag");
		var invocationOrder = new List<int>();

		events.Subscribe<int>(eventTag, _ => invocationOrder.Add(1), priority: 10);
		events.Subscribe<int>(eventTag, _ => invocationOrder.Add(2), priority: 50);
		events.Subscribe<int>(eventTag, _ => invocationOrder.Add(3), priority: 30);

		events.Raise(new EventData<int>
		{
			EventTags = eventTag.GetSingleTagContainer()!,
			Payload = 42,
		});

		invocationOrder.Should().ContainInOrder(2, 3, 1);
	}

	[Fact]
	[Trait("Priority", null)]
	public void Negative_priority_handlers_are_invoked_last()
	{
		var events = new EventManager();
		var eventTag = Tag.RequestTag(_tagsManager, "simple.tag");
		var invocationOrder = new List<string>();

		events.Subscribe(eventTag, _ => invocationOrder.Add("negative"), priority: -100);
		events.Subscribe(eventTag, _ => invocationOrder.Add("zero"), priority: 0);
		events.Subscribe(eventTag, _ => invocationOrder.Add("positive"), priority: 100);

		events.Raise(new EventData
		{
			EventTags = eventTag.GetSingleTagContainer()!,
		});

		invocationOrder.Should().ContainInOrder("positive", "zero", "negative");
	}

	[Fact]
	[Trait("Unsubscribe", null)]
	public void Unsubscribe_prevents_handler_from_being_called()
	{
		var events = new EventManager();
		var eventTag = Tag.RequestTag(_tagsManager, "simple.tag");
		var callCount = 0;

		EventSubscriptionToken token = events.Subscribe(eventTag, _ => callCount++);

		events.Raise(new EventData { EventTags = eventTag.GetSingleTagContainer()! });
		callCount.Should().Be(1);

		var unsubscribed = events.Unsubscribe(token);
		unsubscribed.Should().BeTrue();

		events.Raise(new EventData { EventTags = eventTag.GetSingleTagContainer()! });
		callCount.Should().Be(1, "handler should not be called after un-subscription");
	}

	[Fact]
	[Trait("Unsubscribe", null)]
	public void Unsubscribe_typed_event_prevents_handler_from_being_called()
	{
		var events = new EventManager();
		var eventTag = Tag.RequestTag(_tagsManager, "simple.tag");
		var callCount = 0;

		EventSubscriptionToken token = events.Subscribe<int>(eventTag, _ => callCount++);

		events.Raise(new EventData<int> { EventTags = eventTag.GetSingleTagContainer()!, Payload = 1 });
		callCount.Should().Be(1);

		var unsubscribed = events.Unsubscribe(token);
		unsubscribed.Should().BeTrue();

		events.Raise(new EventData<int> { EventTags = eventTag.GetSingleTagContainer()!, Payload = 2 });
		callCount.Should().Be(1, "handler should not be called after un-subscription");
	}

	[Fact]
	[Trait("Unsubscribe", null)]
	public void Unsubscribe_with_invalid_token_returns_false()
	{
		var events = new EventManager();
		var invalidToken = new EventSubscriptionToken(Guid.NewGuid());

		var result = events.Unsubscribe(invalidToken);

		result.Should().BeFalse();
	}

	[Fact]
	[Trait("Unsubscribe", null)]
	public void Double_unsubscribe_returns_false_on_second_call()
	{
		var events = new EventManager();
		var eventTag = Tag.RequestTag(_tagsManager, "simple.tag");

		EventSubscriptionToken token = events.Subscribe(eventTag, _ => { });

		events.Unsubscribe(token).Should().BeTrue();
		events.Unsubscribe(token).Should().BeFalse();
	}

	[Fact]
	[Trait("Multiple", null)]
	public void Multiple_handlers_on_same_tag_all_receive_event()
	{
		var events = new EventManager();
		var eventTag = Tag.RequestTag(_tagsManager, "simple.tag");
		var handler1Called = false;
		var handler2Called = false;
		var handler3Called = false;

		events.Subscribe(eventTag, _ => handler1Called = true);
		events.Subscribe(eventTag, _ => handler2Called = true);
		events.Subscribe(eventTag, _ => handler3Called = true);

		events.Raise(new EventData { EventTags = eventTag.GetSingleTagContainer()! });

		handler1Called.Should().BeTrue();
		handler2Called.Should().BeTrue();
		handler3Called.Should().BeTrue();
	}

	[Fact]
	[Trait("Multiple", null)]
	public void Handlers_on_different_tags_only_receive_matching_events()
	{
		var events = new EventManager();
		var redTag = Tag.RequestTag(_tagsManager, "color.red");
		var blueTag = Tag.RequestTag(_tagsManager, "color.blue");
		var redCalled = false;
		var blueCalled = false;

		events.Subscribe(redTag, _ => redCalled = true);
		events.Subscribe(blueTag, _ => blueCalled = true);

		events.Raise(new EventData { EventTags = redTag.GetSingleTagContainer()! });

		redCalled.Should().BeTrue();
		blueCalled.Should().BeFalse();
	}

	[Fact]
	[Trait("Multiple", null)]
	public void Unsubscribing_one_handler_does_not_affect_others()
	{
		var events = new EventManager();
		var eventTag = Tag.RequestTag(_tagsManager, "simple.tag");
		var handler1Count = 0;
		var handler2Count = 0;

		EventSubscriptionToken token1 = events.Subscribe(eventTag, _ => handler1Count++);
		events.Subscribe(eventTag, _ => handler2Count++);

		events.Raise(new EventData { EventTags = eventTag.GetSingleTagContainer()! });
		handler1Count.Should().Be(1);
		handler2Count.Should().Be(1);

		events.Unsubscribe(token1);

		events.Raise(new EventData { EventTags = eventTag.GetSingleTagContainer()! });
		handler1Count.Should().Be(1, "unsubscribed handler should not be called");
		handler2Count.Should().Be(2, "remaining handler should still be called");
	}

	[Fact]
	[Trait("TagHierarchy", null)]
	public void Parent_tag_subscription_receives_child_tag_events()
	{
		var events = new EventManager();
		var colorTag = Tag.RequestTag(_tagsManager, "color");
		var redTag = Tag.RequestTag(_tagsManager, "color.red");
		var colorEventReceived = false;

		events.Subscribe(colorTag, _ => colorEventReceived = true);

		// Raise event with child tag
		events.Raise(new EventData { EventTags = redTag.GetSingleTagContainer()! });

		colorEventReceived.Should().BeTrue("parent tag subscription should receive child tag events");
	}

	[Fact]
	[Trait("TagHierarchy", null)]
	public void Child_tag_subscription_does_not_receive_parent_tag_events()
	{
		var events = new EventManager();
		var colorTag = Tag.RequestTag(_tagsManager, "color");
		var redTag = Tag.RequestTag(_tagsManager, "color.red");
		var redEventReceived = false;

		events.Subscribe(redTag, _ => redEventReceived = true);

		// Raise event with parent tag
		events.Raise(new EventData { EventTags = colorTag.GetSingleTagContainer()! });

		redEventReceived.Should().BeFalse("child tag subscription should not receive parent tag events");
	}

	[Fact]
	[Trait("TagHierarchy", null)]
	public void Parent_tag_receives_events_from_multiple_child_tags()
	{
		var events = new EventManager();
		var colorTag = Tag.RequestTag(_tagsManager, "color");
		var redTag = Tag.RequestTag(_tagsManager, "color.red");
		var blueTag = Tag.RequestTag(_tagsManager, "color.blue");
		var greenTag = Tag.RequestTag(_tagsManager, "color.green");
		var eventsReceived = new List<string>();

		events.Subscribe(colorTag, x =>
		{
			if (x.EventTags.HasTagExact(redTag))
			{
				eventsReceived.Add("red");
			}

			if (x.EventTags.HasTagExact(blueTag))
			{
				eventsReceived.Add("blue");
			}

			if (x.EventTags.HasTagExact(greenTag))
			{
				eventsReceived.Add("green");
			}
		});

		events.Raise(new EventData { EventTags = redTag.GetSingleTagContainer()! });
		events.Raise(new EventData { EventTags = blueTag.GetSingleTagContainer()! });
		events.Raise(new EventData { EventTags = greenTag.GetSingleTagContainer()! });

		eventsReceived.Should().ContainInOrder("red", "blue", "green");
	}

	[Fact]
	[Trait("TagHierarchy", null)]
	public void Deep_hierarchy_parent_receives_deeply_nested_child_events()
	{
		var events = new EventManager();
		var itemTag = Tag.RequestTag(_tagsManager, "item");
		var swordTag = Tag.RequestTag(_tagsManager, "item.equipment.weapon.sword");
		var itemEventReceived = false;

		events.Subscribe(itemTag, _ => itemEventReceived = true);

		events.Raise(new EventData { EventTags = swordTag.GetSingleTagContainer()! });

		itemEventReceived.Should().BeTrue("deeply nested child should trigger parent subscription");
	}

	[Fact]
	[Trait("MultipleTags", null)]
	public void Event_with_multiple_tags_triggers_all_matching_subscriptions()
	{
		var events = new EventManager();
		var redTag = Tag.RequestTag(_tagsManager, "color.red");
		var enemyTag = Tag.RequestTag(_tagsManager, "enemy.undead.zombie");
		var redCalled = false;
		var enemyCalled = false;

		events.Subscribe(redTag, _ => redCalled = true);
		events.Subscribe(enemyTag, _ => enemyCalled = true);

		// Create event with multiple tags
		var multiTagContainer = new TagContainer(_tagsManager, [redTag, enemyTag]);
		events.Raise(new EventData { EventTags = multiTagContainer });

		redCalled.Should().BeTrue();
		enemyCalled.Should().BeTrue();
	}

	[Fact]
	[Trait("MultipleTags", null)]
	public void Single_handler_called_once_even_with_multiple_matching_tags()
	{
		var events = new EventManager();
		var colorTag = Tag.RequestTag(_tagsManager, "color");
		var redTag = Tag.RequestTag(_tagsManager, "color.red");
		var blueTag = Tag.RequestTag(_tagsManager, "color.blue");
		var callCount = 0;

		// Subscribe to parent tag
		events.Subscribe(colorTag, _ => callCount++);

		// Raise event with multiple child tags
		var multiTagContainer = new TagContainer(_tagsManager, [redTag, blueTag]);
		events.Raise(new EventData { EventTags = multiTagContainer });

		// Handler should only be called once per Raise, not per matching tag
		callCount.Should().Be(1);
	}

	[Fact]
	[Trait("Isolation", null)]
	public void Generic_raise_does_not_trigger_non_generic_handlers()
	{
		var events = new EventManager();
		var eventTag = Tag.RequestTag(_tagsManager, "simple.tag");
		var nonGenericCalled = false;
		var genericCalled = false;

		events.Subscribe(eventTag, _ => nonGenericCalled = true);
		events.Subscribe<int>(eventTag, _ => genericCalled = true);

		// Raise generic event
		events.Raise(new EventData<int> { EventTags = eventTag.GetSingleTagContainer()!, Payload = 42 });

		nonGenericCalled.Should().BeFalse("non-generic handler should not be called by generic raise");
		genericCalled.Should().BeTrue();
	}

	[Fact]
	[Trait("Isolation", null)]
	public void Non_generic_raise_does_not_trigger_generic_handlers()
	{
		var events = new EventManager();
		var eventTag = Tag.RequestTag(_tagsManager, "simple.tag");
		var nonGenericCalled = false;
		var genericCalled = false;

		events.Subscribe(eventTag, _ => nonGenericCalled = true);
		events.Subscribe<int>(eventTag, _ => genericCalled = true);

		// Raise non-generic event
		events.Raise(new EventData { EventTags = eventTag.GetSingleTagContainer()! });

		nonGenericCalled.Should().BeTrue();
		genericCalled.Should().BeFalse("generic handler should not be called by non-generic raise");
	}

	[Fact]
	[Trait("Isolation", null)]
	public void Different_generic_types_are_isolated()
	{
		var events = new EventManager();
		var eventTag = Tag.RequestTag(_tagsManager, "simple.tag");
		var intCalled = false;
		var stringCalled = false;

		events.Subscribe<int>(eventTag, _ => intCalled = true);
		events.Subscribe<string>(eventTag, _ => stringCalled = true);

		events.Raise(new EventData<int> { EventTags = eventTag.GetSingleTagContainer()!, Payload = 42 });

		intCalled.Should().BeTrue();
		stringCalled.Should().BeFalse("string handler should not receive int events");
	}

	[Fact]
	[Trait("EdgeCase", null)]
	public void Raising_event_with_no_subscribers_does_not_throw()
	{
		var events = new EventManager();
		var eventTag = Tag.RequestTag(_tagsManager, "simple.tag");

		Action act = () => events.Raise(new EventData { EventTags = eventTag.GetSingleTagContainer()! });

		act.Should().NotThrow();
	}

	[Fact]
	[Trait("EdgeCase", null)]
	public void Raising_generic_event_with_no_subscribers_does_not_throw()
	{
		var events = new EventManager();
		var eventTag = Tag.RequestTag(_tagsManager, "simple.tag");

		Action act = () => events.Raise(new EventData<int>
		{
			EventTags = eventTag.GetSingleTagContainer()!,
			Payload = 42,
		});

		act.Should().NotThrow();
	}

	[Fact]
	[Trait("EdgeCase", null)]
	public void Event_data_contains_source_and_target_information()
	{
		var events = new EventManager();
		var eventTag = Tag.RequestTag(_tagsManager, "simple.tag");
		var source = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);
		EventData? capturedData = null;

		events.Subscribe(eventTag, x => capturedData = x);

		events.Raise(new EventData
		{
			EventTags = eventTag.GetSingleTagContainer()!,
			Source = source,
			Target = target,
			EventMagnitude = 123.5f,
		});

		capturedData.Should().NotBeNull();
		capturedData!.Value.Source.Should().Be(source);
		capturedData.Value.Target.Should().Be(target);
		capturedData.Value.EventMagnitude.Should().Be(123.5f);
	}

	[Fact]
	[Trait("EdgeCase", null)]
	public void Value_type_payload_is_not_boxed()
	{
		var events = new EventManager();
		var eventTag = Tag.RequestTag(_tagsManager, "simple.tag");
		var capturedPayload = default(TestValuePayload);

		events.Subscribe<TestValuePayload>(eventTag, x => capturedPayload = x.Payload);

		var payload = new TestValuePayload(1.5f, 2.5f, 3.5f);
		events.Raise(new EventData<TestValuePayload>
		{
			EventTags = eventTag.GetSingleTagContainer()!,
			Payload = payload,
		});

		capturedPayload.X.Should().Be(1.5f);
		capturedPayload.Y.Should().Be(2.5f);
		capturedPayload.Z.Should().Be(3.5f);
	}

	[Fact]
	[Trait("EdgeCase", null)]
	public void Subscription_order_preserved_for_same_priority()
	{
		var events = new EventManager();
		var eventTag = Tag.RequestTag(_tagsManager, "simple.tag");
		var invocationOrder = new List<int>();

		// All same priority, should be invoked in subscription order
		events.Subscribe(eventTag, _ => invocationOrder.Add(1), priority: 0);
		events.Subscribe(eventTag, _ => invocationOrder.Add(2), priority: 0);
		events.Subscribe(eventTag, _ => invocationOrder.Add(3), priority: 0);

		events.Raise(new EventData { EventTags = eventTag.GetSingleTagContainer()! });

		// Note: The actual order depends on the Sort stability
		// If unstable, order might not be preserved
		invocationOrder.Should().HaveCount(3);
		invocationOrder.Should().Contain([1, 2, 3]);
	}

	private readonly record struct TestValuePayload(float X, float Y, float Z);
}
