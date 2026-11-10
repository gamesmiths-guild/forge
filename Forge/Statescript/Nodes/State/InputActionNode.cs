// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript.Ports;

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// A state node that watches a button of an entity's input while active, reporting when it is pressed and released.
/// </summary>
/// <remarks>
/// <para>This is the "wait for a button before continuing" node. <see cref="WhilePressedPort"/> gives hold-to-channel,
/// <see cref="OnReleasedPort"/> with a timer gives a charged shot, and deactivating on the first press inside a timer's
/// subgraph gives a combo window.</para>
/// <para>The events are edges the node itself saw, so a button already held when the node activates is not a press:
/// a combo window must not fire on a button nobody pressed inside it, and the button that started the ability is
/// usually still down when the window opens. The subgraph is the other half of that rule - it follows the button's
/// state rather than its edges, so an ability activated by the very button it channels on starts channelling at once.
/// </para>
/// <para>The button is read from the <see cref="IForgeEntity.Input"/> of the <see cref="InputSourceInput"/> entity,
/// the ability's owner when unbound, once per fixed step. The node sees exactly what every machine simulating the
/// entity sees, a tap shorter than a step included. An entity without an input, or an input that does not carry the
/// action, is not read. An engine integration can derive from this node and report its own device's state through
/// <see cref="ReadButtonState"/>.</para>
/// <para>Bound, <see cref="InputSourcesInput"/> is watched instead: every entity in it, such as everyone standing in an
/// area, as one button: down while any of them holds it, pressed when one presses it while none does, and released
/// when the last lets go.</para>
/// </remarks>
/// <param name="actionName">The input action to watch.</param>
/// <param name="deactivateOnPressed">Whether the node deactivates itself on the first press, for the "wait for one
/// button and move on" shape.</param>
public class InputActionNode(string actionName = "", bool deactivateOnPressed = false)
	: StateNode<InputActionNodeContext>
{
	/// <summary>
	/// Input property index for the entity whose input is watched. Unbound means the ability's owner.
	/// </summary>
	public const byte InputSourceInput = 0;

	/// <summary>
	/// Input property index for the entities whose input is watched as one button. Bound, it is watched instead of
	/// <see cref="InputSourceInput"/>.
	/// </summary>
	public const byte InputSourcesInput = 1;

	/// <summary>
	/// Output port index for the event emitted when the button goes down.
	/// </summary>
	public const byte OnPressedPort = 4;

	/// <summary>
	/// Output port index for the event emitted when the button comes up.
	/// </summary>
	public const byte OnReleasedPort = 5;

	/// <summary>
	/// Output port index for the subgraph that is active while the button is down.
	/// </summary>
	public const byte WhilePressedPort = 6;

	private readonly string _actionName = actionName ?? string.Empty;
	private readonly bool _deactivateOnPressed = deactivateOnPressed;
	private readonly List<EntityInput> _inputs = [];

	/// <inheritdoc/>
	public override string Description =>
		"Watches a button while active, emitting press and release events and routing a held subgraph.";

	/// <summary>
	/// Gets a value indicating whether <see cref="InputSourcesInput"/> is bound, so the node watches those entities
	/// rather than <see cref="InputSourceInput"/>.
	/// </summary>
	protected bool WatchesInputSources => InputProperties[InputSourcesInput].BoundName != StringKey.Empty;

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		base.DefinePorts(inputPorts, outputPorts);
		outputPorts.Add(CreatePort<EventPort>(OnPressedPort, "OnPressed"));
		outputPorts.Add(CreatePort<EventPort>(OnReleasedPort, "OnReleased"));
		outputPorts.Add(CreatePort<SubgraphPort>(WhilePressedPort, "WhilePressed"));
	}

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Input Source", typeof(IForgeEntity), IsOptional: true));
		inputProperties.Add(new InputProperty("Input Sources", typeof(IForgeEntity[]), IsOptional: true));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		graphContext.GetNodeContext<InputActionNodeContext>(NodeID).LastPressed = null;
		ReadInputs(graphContext);
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		graphContext.GetNodeContext<InputActionNodeContext>(NodeID).LastPressed = null;
	}

	/// <inheritdoc/>
	protected override void OnFixedUpdate(double deltaTime, GraphContext graphContext)
	{
		ReadInputs(graphContext);
	}

	/// <summary>
	/// Resolves the entity whose input is watched.
	/// </summary>
	/// <param name="graphContext">The graph's context.</param>
	/// <returns>The <see cref="InputSourceInput"/> entity, the ability's owner when unbound, or
	/// <see langword="null"/> when there is neither.</returns>
	protected IForgeEntity? ResolveInputSource(GraphContext graphContext)
	{
		return AbilityNodeUtilities.ResolveEntityOrOwner(graphContext, InputProperties[InputSourceInput].BoundName);
	}

	/// <summary>
	/// Reports whether the button is down, read from a source that keeps no presses or releases of its own, such as a
	/// device polled every frame. A change since the last read is reported as a press or a release; the first read is
	/// the state the node started in.
	/// </summary>
	/// <param name="graphContext">The graph's context.</param>
	/// <param name="down">Whether the button is down.</param>
	protected void ReadButtonState(GraphContext graphContext, bool down)
	{
		InputActionNodeContext nodeContext = graphContext.GetNodeContext<InputActionNodeContext>(NodeID);

		if (nodeContext.LastPressed == down)
		{
			return;
		}

		bool hadValue = nodeContext.LastPressed.HasValue;
		nodeContext.LastPressed = down;

		if (!hadValue)
		{
			// The first read is the state the node started in rather than a change, so only the subgraph follows it.
			if (down)
			{
				EmitMessage(graphContext, WhilePressedPort);
			}

			return;
		}

		if (down)
		{
			Press(graphContext, nodeContext);
		}
		else
		{
			Release(graphContext, nodeContext);
		}
	}

	private static bool WasRead(InputActionNodeContext nodeContext, EntityInput input)
	{
		foreach (KeyValuePair<EntityInput, ulong> read in nodeContext.Reads)
		{
			if (read.Key == input && read.Value == input.FrameNumber)
			{
				return true;
			}
		}

		return false;
	}

	private void ReadInputs(GraphContext graphContext)
	{
		_inputs.Clear();

		if (!WatchesInputSources)
		{
			if (ResolveInputSource(graphContext)?.Input is EntityInput input)
			{
				_inputs.Add(input);
			}
		}
		else if (graphContext.TryResolveObjectArray(
			InputProperties[InputSourcesInput].BoundName,
			typeof(IForgeEntity),
			out object?[]? sources))
		{
			foreach (object? source in sources)
			{
				if (source is IForgeEntity { Input: EntityInput input })
				{
					_inputs.Add(input);
				}
			}
		}

		ReadInput(graphContext, _inputs);
	}

	// A tick's input can hold a release and a press of the same button, in an order the frame cannot tell, so the state
	// before the tick decides: a button that was down came up first, one that was up went down first.
	private void ReadInput(GraphContext graphContext, List<EntityInput> inputs)
	{
		InputActionNodeContext nodeContext = graphContext.GetNodeContext<InputActionNodeContext>(NodeID);
		bool carried = false;
		bool held = false;
		bool pressed = false;
		bool pressedAgain = false;
		bool heldThrough = false;
		bool unread = false;

		foreach (EntityInput input in inputs)
		{
			int action = input.Actions.IndexOf(_actionName);

			if (action < 0)
			{
				continue;
			}

			InputFrame frame = input.Current;
			bool isHeld = frame.IsHeld(action);
			carried = true;
			held |= isHeld;

			// The edges of a frame already read, such as the one an input-bound ability activated on, happened before
			// the node started watching.
			if (WasRead(nodeContext, input))
			{
				heldThrough |= isHeld;
				continue;
			}

			unread = true;
			pressed |= frame.WasPressed(action);
			pressedAgain |= frame.WasReleased(action) && isHeld;
			heldThrough |= isHeld && !frame.WasPressed(action) && !frame.WasReleased(action);
		}

		if (!carried)
		{
			// Watched entities that all left hold nothing.
			if (WatchesInputSources && nodeContext.LastPressed == true)
			{
				nodeContext.Reads.Clear();
				nodeContext.LastPressed = false;
				Release(graphContext, nodeContext);
			}

			return;
		}

		nodeContext.Reads.Clear();

		foreach (EntityInput input in inputs)
		{
			nodeContext.Reads.Add(new KeyValuePair<EntityInput, ulong>(input, input.FrameNumber));
		}

		if (nodeContext.LastPressed is not bool down)
		{
			nodeContext.LastPressed = held;

			if (held)
			{
				EmitMessage(graphContext, WhilePressedPort);
			}

			return;
		}

		if (!unread)
		{
			return;
		}

		// A button another source still holds, or held through the tick, stays down.
		if (down && ((pressedAgain && !heldThrough) || !held))
		{
			nodeContext.LastPressed = down = false;

			if (!Release(graphContext, nodeContext))
			{
				return;
			}
		}

		if (!down && (pressed || held))
		{
			nodeContext.LastPressed = down = true;

			if (!Press(graphContext, nodeContext))
			{
				return;
			}
		}

		// A tap: pressed and released within the tick.
		if (down && !held)
		{
			nodeContext.LastPressed = false;
			Release(graphContext, nodeContext);
		}
	}

	// Each returns whether the node is still active to carry on.
	private bool Press(GraphContext graphContext, InputActionNodeContext nodeContext)
	{
		if (_deactivateOnPressed)
		{
			DeactivateNodeAndEmitMessage(graphContext, OnPressedPort);
			return false;
		}

		EmitMessage(graphContext, OnPressedPort, WhilePressedPort);
		return nodeContext.Active;
	}

	private bool Release(GraphContext graphContext, InputActionNodeContext nodeContext)
	{
		var whilePressedPort = (SubgraphPort)OutputPorts[WhilePressedPort];
		whilePressedPort.EmitDisableSubgraphMessage(graphContext);

		// Disabling the subgraph can end this node or the graph, and then there is no release left to report.
		if (!nodeContext.Active)
		{
			return false;
		}

		EmitMessage(graphContext, OnReleasedPort);
		return nodeContext.Active;
	}
}
