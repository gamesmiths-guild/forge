// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// A state node that remains active for a configured duration, then deactivates. The duration is read from a graph
/// variable or property by name, allowing it to be a fixed value, driven by an entity attribute, or any other
/// <see cref="Properties.IPropertyResolver"/>.
/// </summary>
/// <remarks>
/// <para>The duration property must resolve to a <see langword="double"/> value representing seconds.</para>
/// <para>The node accumulates elapsed time in its <see cref="TimerNodeContext"/> during
/// <see cref="StateNode{T}.OnUpdate"/> calls. When the elapsed time reaches or exceeds the duration, the node
/// deactivates itself.</para>
/// </remarks>
/// <param name="durationPropertyName">The name of the graph variable or property that provides the timer duration in
/// seconds.</param>
public class TimerNode(StringKey durationPropertyName) : StateNode<TimerNodeContext>
{
	private readonly StringKey _durationPropertyName = durationPropertyName;

	/// <inheritdoc/>
	public override string Description => "Remains active for a configured duration, then deactivates.";

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

		if (!graphContext.GraphVariables.TryGet(_durationPropertyName, graphContext, out double duration))
		{
			return;
		}

		if (nodeContext.ElapsedTime >= duration)
		{
			DeactivateNode(graphContext);
		}
	}
}
