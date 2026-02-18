// Copyright © Gamesmiths Guild.
#pragma warning disable SA1649, SA1402 // File name should match first type name

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.Action;
using Gamesmiths.Forge.Statescript.Nodes.Condition;
using Gamesmiths.Forge.Statescript.Nodes.State;

namespace Gamesmiths.Forge.Tests.Helpers;

/// <summary>
/// Convenience helpers for creating and binding framework nodes in tests.
/// </summary>
internal static class NodeBindings
{
	public static TimerNode CreateTimerNode(StringKey durationPropertyName)
	{
		var node = new TimerNode();
		node.BindInput(TimerNode.DurationInput, durationPropertyName);
		return node;
	}

	public static ExpressionNode CreateExpressionNode(StringKey conditionPropertyName)
	{
		var node = new ExpressionNode();
		node.BindInput(ExpressionNode.ConditionInput, conditionPropertyName);
		return node;
	}

	public static SetVariableNode CreateSetVariableNode(
		StringKey sourcePropertyName,
		StringKey targetVariableName,
		VariableScope scope = VariableScope.Graph)
	{
		var node = new SetVariableNode();
		node.BindInput(SetVariableNode.SourceInput, sourcePropertyName);
		node.BindOutput(SetVariableNode.TargetOutput, targetVariableName, scope);
		return node;
	}
}

internal sealed class TrackingActionNode(string? name = null, List<string>? executionLog = null) : ActionNode
{
	private readonly string? _name = name;
	private readonly List<string>? _executionLog = executionLog;

	public int ExecutionCount { get; private set; }

	protected override void Execute(GraphContext graphContext)
	{
		ExecutionCount++;

		if (_name is not null)
		{
			_executionLog?.Add(_name);
		}
	}
}

internal sealed class FixedConditionNode(bool result) : ConditionNode
{
	private readonly bool _result = result;

	protected override bool Test(GraphContext graphContext)
	{
		return _result;
	}
}

internal sealed class ThresholdConditionNode : ConditionNode
{
	private readonly string _variableName;
	private readonly string? _thresholdVariableName;
	private readonly int _fixedThreshold;

	public ThresholdConditionNode(string variableName, string thresholdVariableName)
	{
		_variableName = variableName;
		_thresholdVariableName = thresholdVariableName;
	}

	public ThresholdConditionNode(string variableName, int threshold)
	{
		_variableName = variableName;
		_fixedThreshold = threshold;
	}

	protected override bool Test(GraphContext graphContext)
	{
		graphContext.GraphVariables.TryGetVar(_variableName, out int value);

		var threshold = _fixedThreshold;
		if (_thresholdVariableName is not null)
		{
			graphContext.GraphVariables.TryGetVar(_thresholdVariableName, out threshold);
		}

		return value > threshold;
	}
}

internal sealed class IncrementCounterNode(string variableName) : ActionNode
{
	private readonly string _variableName = variableName;

	protected override void Execute(GraphContext graphContext)
	{
		graphContext.GraphVariables.TryGetVar(_variableName, out int currentValue);
		graphContext.GraphVariables.SetVar(_variableName, currentValue + 1);
	}
}

internal sealed class ReadVariableNode<T>(string variableName) : ActionNode
	where T : unmanaged
{
	private readonly string _variableName = variableName;

	public T LastReadValue { get; private set; }

	protected override void Execute(GraphContext graphContext)
	{
		graphContext.GraphVariables.TryGetVar(_variableName, out T value);
		LastReadValue = value;
	}
}

internal sealed class CaptureActivationContextNode : ActionNode
{
	public object? CapturedActivationContext { get; private set; }

	protected override void Execute(GraphContext graphContext)
	{
		CapturedActivationContext = graphContext.ActivationContext;
	}
}

internal sealed class TryGetActivationContextNode<T> : ActionNode
	where T : class
{
	public bool Found { get; private set; }

	public T? CapturedContext { get; private set; }

	protected override void Execute(GraphContext graphContext)
	{
		Found = graphContext.TryGetActivationContext(out T? data);
		CapturedContext = data;
	}
}

internal sealed class CaptureGraphContextNode : ActionNode
{
	public GraphContext? CapturedGraphContext { get; private set; }

	protected override void Execute(GraphContext graphContext)
	{
		CapturedGraphContext = graphContext;
	}
}
