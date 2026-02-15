// Copyright © Gamesmiths Guild.
#pragma warning disable SA1649, SA1402 // File name should match first type name

using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;

namespace Gamesmiths.Forge.Tests.Helpers;

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
