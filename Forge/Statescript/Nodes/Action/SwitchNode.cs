// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Ports;

namespace Gamesmiths.Forge.Statescript.Nodes.Action;

/// <summary>
/// Routes an incoming message to one of several case ports based on a resolved integer selector.
/// </summary>
/// <remarks>
/// <para>The selector input must resolve to an <see langword="int"/>. Selectors from <c>0</c> to
/// <c>caseCount - 1</c> emit the matching case port; anything else (including an unresolvable selector) emits the
/// default port at index <paramref name="caseCount"/>.</para>
/// </remarks>
/// <param name="caseCount">How many case ports the switch has, not counting the default port.</param>
public class SwitchNode(int caseCount = 2) : Node
{
	/// <summary>
	/// Port index for the input port.
	/// </summary>
	public const byte InputPort = 0;

	/// <summary>
	/// Input property index for the selector value.
	/// </summary>
	public const byte SelectorInput = 0;

	/// <summary>
	/// Gets the port index of the default port, emitted for out-of-range selectors. Also the number of case ports.
	/// </summary>
	public int DefaultPort { get; } = caseCount >= 1
		? caseCount
		: throw new ArgumentOutOfRangeException(nameof(caseCount), "A switch needs at least one case port.");

	/// <inheritdoc/>
	public override string Description => "Routes the incoming message to a case port picked by an integer selector.";

	/// <inheritdoc/>
	internal override IEnumerable<int> GetReachableOutputPorts(byte inputPortIndex)
	{
		for (int i = 0; i <= DefaultPort; i++)
		{
			yield return i;
		}
	}

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		inputPorts.Add(CreatePort<InputPort>(InputPort, "Input"));

		for (int i = 0; i < DefaultPort; i++)
		{
			outputPorts.Add(CreatePort<EventPort>((byte)i, $"Case {i}"));
		}

		outputPorts.Add(CreatePort<EventPort>((byte)DefaultPort, "Default"));
	}

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Selector", typeof(int)));
	}

	/// <inheritdoc/>
	protected override void HandleMessage(InputPort receiverPort, GraphContext graphContext)
	{
		int portIndex = DefaultPort;

		if (graphContext.TryResolve(InputProperties[SelectorInput].BoundName, out int selector)
			&& selector >= 0
			&& selector < DefaultPort)
		{
			portIndex = selector;
		}

		OutputPorts[portIndex].EmitMessage(graphContext);
	}
}
