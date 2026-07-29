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

	[Fact]
	[Trait("Resolver", "EventPayloadOutput")]
	public void Provider_declares_its_payload_members_without_an_override()
	{
		IReadOnlyList<EventPayloadOutput> outputs = new AutoPayloadProvider().Outputs;

		// Declaration order, so editor dropdowns match the payload source.
		outputs.Select(x => x.Name).Should().Equal("Damage", "IsCritical", "Force");
		outputs.Select(x => x.ValueType).Should().Equal(typeof(int), typeof(bool), typeof(float));
	}

	[Fact]
	[Trait("Resolver", "EventPayloadOutput")]
	public void Provider_writes_its_payload_members_without_an_override()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineVariable("damageVar", 0);
		context.GraphVariables.DefineVariable("criticalVar", false);
		context.GraphVariables.DefineVariable("forceVar", 0.0);

		var resolver = new EventPayloadOutputResolver(
			new AutoPayloadProvider(),
			new Dictionary<string, EventOutputBinding>
			{
				["Damage"] = new EventOutputBinding("damageVar", VariableScope.Graph),
				["IsCritical"] = new EventOutputBinding("criticalVar", VariableScope.Graph),
				["Force"] = new EventOutputBinding("forceVar", VariableScope.Graph),
			});

		resolver.Resolve(context).Write(new AutoPayload(42, true, 2.5f), context);

		context.GraphVariables.TryGetVar("damageVar", out int damage).Should().BeTrue();
		damage.Should().Be(42);
		context.GraphVariables.TryGetVar("criticalVar", out bool isCritical).Should().BeTrue();
		isCritical.Should().BeTrue();

		// Floating-point graph variables are double-backed, so the reflected writer widens like a hand-written one.
		context.GraphVariables.TryGetVar("forceVar", out double force).Should().BeTrue();
		force.Should().Be(2.5);
	}

	[Fact]
	[Trait("Resolver", "EventPayloadOutput")]
	public void Provider_omits_payload_members_it_could_not_write()
	{
		// A struct Variant128 cannot hold is skipped, so the editor never offers a binding that would never update.
		new UnwritableMemberPayloadProvider().Outputs.Select(x => x.Name).Should().Equal("Damage");
	}

	[Fact]
	[Trait("Resolver", "EventPayloadOutput")]
	public void Explicitly_declared_outputs_still_win_over_the_reflected_ones()
	{
		new TestEventPayloadProvider().Outputs.Should()
			.ContainSingle().Which.Name.Should().Be(TestEventPayloadProvider.AmountKey);
	}

	private sealed record AutoPayload(int Damage, bool IsCritical, float Force);

	private readonly record struct Unsupported(int A, int B, int C, int D, int E);

	private sealed record UnwritableMemberPayload(int Damage, Unsupported Extra);

	private sealed class AutoPayloadProvider : EventPayloadProvider<AutoPayload>
	{
		public override AutoPayload CreatePayload(GraphContext graphContext, EventPayloadInputs inputs)
		{
			return new AutoPayload(0, false, 0f);
		}
	}

	private sealed class UnwritableMemberPayloadProvider : EventPayloadProvider<UnwritableMemberPayload>
	{
		public override UnwritableMemberPayload CreatePayload(GraphContext graphContext, EventPayloadInputs inputs)
		{
			return new UnwritableMemberPayload(0, default);
		}
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
