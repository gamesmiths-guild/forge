// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Providers;

namespace Gamesmiths.Forge.Tests.Helpers;

/// <summary>
/// A bidirectional test event-payload provider: it builds a <see cref="TestEventPayload"/> from a declared
/// <c>Amount</c> input and writes the payload's amount back to a declared <c>Amount</c> output. Used by both the
/// raise-event and event-listener node tests.
/// </summary>
internal sealed class TestEventPayloadProvider : EventPayloadProvider<TestEventPayload>
{
	/// <summary>
	/// The name of the declared input and output this provider uses.
	/// </summary>
	public const string AmountKey = "Amount";

	/// <inheritdoc/>
	public override IReadOnlyList<EventPayloadInput> Inputs => [new EventPayloadInput(AmountKey, typeof(int))];

	/// <inheritdoc/>
	public override IReadOnlyList<EventPayloadOutput> Outputs => [new EventPayloadOutput(AmountKey, typeof(int))];

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
