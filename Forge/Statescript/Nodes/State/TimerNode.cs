// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// A state node that remains active for a configured duration, then deactivates. The duration is read from a bound
/// input property, allowing it to be a fixed variable value, driven by an entity attribute, or any other
/// <see cref="Properties.IPropertyResolver"/> that produces a <see langword="double"/>.
/// </summary>
/// <remarks>
/// <para>The duration input must resolve to a <see langword="double"/> value representing seconds.</para>
/// <para>The node accumulates elapsed time in its <see cref="TimerNodeContext"/> during
/// <see cref="StateNode{T}.OnUpdate"/> calls. When the elapsed time reaches or exceeds the duration, the node
/// deactivates itself.</para>
/// </remarks>
public class TimerNode : StateNode<TimerNodeContext>
{
	/// <summary>
	/// Input property index for the timer duration.
	/// </summary>
	public const byte DurationInput = 0;

	/// <inheritdoc/>
	public override string Description => "Remains active for a configured duration, then deactivates.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Duration", typeof(double)));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		TimerNodeContext nodeContext = graphContext.GetNodeContext<TimerNodeContext>(NodeID);
		nodeContext.ElapsedTime = 0;
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
	}

	/// <inheritdoc/>
	protected override void OnUpdate(double deltaTime, GraphContext graphContext)
	{
		TimerNodeContext nodeContext = graphContext.GetNodeContext<TimerNodeContext>(NodeID);

		nodeContext.ElapsedTime += deltaTime;

		if (!graphContext.TryResolve(InputProperties[DurationInput].BoundName, out double duration))
		{
			return;
		}

		if (nodeContext.ElapsedTime >= duration)
		{
			DeactivateNode(graphContext);
		}
	}
}
