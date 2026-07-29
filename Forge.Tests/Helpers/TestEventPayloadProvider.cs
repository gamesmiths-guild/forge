// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Providers;

namespace Gamesmiths.Forge.Tests.Helpers;

/// <summary>
/// A bidirectional test event-payload provider: it builds a <see cref="TestEventPayload"/> from its declared
/// <c>Amount</c> member and writes the payload's amount back to the same member. Used by both the raise-event and
/// event-listener node tests.
/// </summary>
internal sealed class TestEventPayloadProvider : EventPayloadProvider<TestEventPayload>
{
	/// <summary>
	/// The name of the declared member this provider uses.
	/// </summary>
	public const string AmountKey = "Amount";

	/// <inheritdoc/>
	public override IReadOnlyList<EventPayloadMember> Members => [new EventPayloadMember(AmountKey, typeof(int))];

	/// <inheritdoc/>
	public override TestEventPayload CreatePayload(GraphContext graphContext, EventPayloadInputs inputs)
	{
		return new TestEventPayload(inputs.Get<int>(AmountKey));
	}

	/// <inheritdoc/>
	public override void WriteOutputs(TestEventPayload payload, EventPayloadOutputs outputs)
	{
		outputs.Set(AmountKey, payload.Amount);
	}
}
