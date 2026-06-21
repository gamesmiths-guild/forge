// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Statescript.Providers;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class EventPayloadOutputResolverTests
{
	[Fact]
	[Trait("Resolver", "EventPayloadOutput")]
	public void Writer_writes_payload_fields_to_bound_variables()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineVariable("amountVar", 0);
		var resolver = new EventPayloadOutputResolver(
			new TestEventPayloadProvider(),
			new Dictionary<string, EventOutputBinding>
			{
				[TestEventPayloadProvider.AmountKey] = new EventOutputBinding("amountVar", VariableScope.Graph),
			});

		EventPayloadWriter writer = resolver.Resolve(context);
		writer.Write(new TestEventPayload(7), context);

		context.GraphVariables.TryGetVar("amountVar", out int amount).Should().BeTrue();
		amount.Should().Be(7);
	}

	[Fact]
	[Trait("Resolver", "EventPayloadOutput")]
	public void Writer_skips_outputs_with_no_binding()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineVariable("amountVar", 99);
		var resolver = new EventPayloadOutputResolver(
			new TestEventPayloadProvider(),
			new Dictionary<string, EventOutputBinding>());

		resolver.Resolve(context).Write(new TestEventPayload(7), context);

		context.GraphVariables.TryGetVar("amountVar", out int amount).Should().BeTrue();
		amount.Should().Be(99);
	}

	[Fact]
	[Trait("Resolver", "EventPayloadOutput")]
	public void Resolver_value_type_is_the_payload_writer()
	{
		var resolver = new EventPayloadOutputResolver(
			new TestEventPayloadProvider(),
			new Dictionary<string, EventOutputBinding>());

		resolver.ValueType.Should().Be(typeof(EventPayloadWriter));
	}

	[Fact]
	[Trait("Resolver", "EventPayloadOutput")]
	public void Writer_widens_float_outputs_to_double()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineVariable("floatVar", 0.0);
		var resolver = new EventPayloadOutputResolver(
			new FloatPayloadProvider(),
			new Dictionary<string, EventOutputBinding>
			{
				["Value"] = new EventOutputBinding("floatVar", VariableScope.Graph),
			});

		resolver.Resolve(context).Write(11.0f, context);

		context.GraphVariables.TryGetVar("floatVar", out double value).Should().BeTrue();
		value.Should().Be(11.0);
	}

	private sealed class FloatPayloadProvider : EventPayloadProvider<float>
	{
		public override IReadOnlyList<EventPayloadOutput> Outputs => [new EventPayloadOutput("Value", typeof(float))];

		public override float CreatePayload(GraphContext graphContext, EventPayloadInputs inputs)
		{
			return 0f;
		}

		public override void WriteOutputs(float payload, EventPayloadOutputs outputs)
		{
			outputs.Set("Value", payload);
		}
	}
}
