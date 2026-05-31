// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Nodes.Action;

/// <summary>
/// An action node that writes a value to a graph variable. The value is read from a bound input property (which can
/// resolve from a variable, an entity attribute, or any other <see cref="Properties.IPropertyResolver"/>) and written
/// to a bound output variable.
/// </summary>
/// <remarks>
/// <para>The source (input) and target (output) are bound by name at graph construction time via
/// <see cref="Node.BindInput"/> and <see cref="Node.BindOutput"/>. At runtime, the node resolves the input and writes
/// the result to the matching target variable kind (variant, variant array, object-backed value, or object-backed
/// array).</para>
/// </remarks>
public class SetVariableNode : ActionNode
{
	/// <summary>
	/// Input property index for the source value.
	/// </summary>
	public const byte SourceInput = 0;

	/// <summary>
	/// Output variable index for the target variable.
	/// </summary>
	public const byte TargetOutput = 0;

	private enum TargetValueKind
	{
		Variant = 0,
		VariantArray = 1,
		Object = 2,
		ObjectArray = 3,
	}

	/// <inheritdoc/>
	public override string Description => "Sets a graph variable to the value of a property.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Source", typeof(object)));
		outputVariables.Add(new OutputVariable("Target", typeof(object)));
	}

	/// <inheritdoc/>
	protected override void Execute(GraphContext graphContext)
	{
		OutputVariable target = OutputVariables[TargetOutput];
		TargetBinding targetBinding = GetTargetBinding(graphContext, target);
		StringKey sourceName = InputProperties[SourceInput].BoundName;

		switch (targetBinding.Kind)
		{
			case TargetValueKind.Variant:
				if (graphContext.TryResolveVariant(sourceName, out Variant128 value))
				{
					targetBinding.Variables.SetVariant(target.BoundName, value);
				}

				return;

			case TargetValueKind.VariantArray:
				if (graphContext.TryResolveArray(sourceName, out Variant128[]? values))
				{
					targetBinding.Variables.DefineArrayVariable(target.BoundName, values);
				}

				return;

			case TargetValueKind.Object:
				if (targetBinding.ValueType is not null
					&& graphContext.TryResolveObject(
						sourceName,
						targetBinding.ValueType,
						out object? objectValue))
				{
					targetBinding.Variables.SetObject(target.BoundName, objectValue);
				}

				return;

			case TargetValueKind.ObjectArray:
				if (targetBinding.ValueType is not null
					&& graphContext.TryResolveObjectArray(
						sourceName,
						targetBinding.ValueType,
						out object?[]? objectArray))
				{
					targetBinding.Variables.DefineObjectArrayVariable(
						target.BoundName,
						targetBinding.ValueType,
						objectArray);
				}

				return;

			default:
				throw new InvalidOperationException($"Unsupported target kind '{targetBinding.Kind}'.");
		}
	}

	private static Variables GetTargetVariables(GraphContext graphContext, OutputVariable target)
	{
		return target.Scope == VariableScope.Shared
			? graphContext.SharedVariables
				?? throw new InvalidOperationException(
					$"Cannot write to shared variable '{target.BoundName}': SharedVariables is not available in this " +
					"context.")
			: graphContext.GraphVariables;
	}

	private static TargetBinding ResolveTargetBinding(Variables variables, OutputVariable target)
	{
		if (variables.TryGetObjectArrayElementType(target.BoundName, out Type? objectArrayElementType))
		{
			return new TargetBinding(variables, TargetValueKind.ObjectArray, objectArrayElementType);
		}

		if (variables.GetArrayLength(target.BoundName) >= 0)
		{
			return new TargetBinding(variables, TargetValueKind.VariantArray);
		}

		if (variables.TryGetObjectVariableType(target.BoundName, out Type? objectType))
		{
			return new TargetBinding(variables, TargetValueKind.Object, objectType);
		}

		if (variables.TryGetVariant(target.BoundName, out _))
		{
			return new TargetBinding(variables, TargetValueKind.Variant);
		}

		throw new InvalidOperationException(
			$"Cannot write to '{target.BoundName}': no compatible target variable exists in scope '{target.Scope}'.");
	}

	private TargetBinding GetTargetBinding(GraphContext graphContext, OutputVariable target)
	{
		Variables variables = GetTargetVariables(graphContext, target);
		SetVariableNodeContext nodeContext = graphContext.GetOrCreateNodeContext<SetVariableNodeContext>(NodeID);

		if (nodeContext.HasCachedBinding && ReferenceEquals(nodeContext.CachedVariables, variables))
		{
			return nodeContext.CachedBinding;
		}

		TargetBinding binding = ResolveTargetBinding(variables, target);
		nodeContext.CachedVariables = variables;
		nodeContext.CachedBinding = binding;
		nodeContext.HasCachedBinding = true;
		return binding;
	}

	private sealed class SetVariableNodeContext : INodeContext
	{
		public Variables? CachedVariables { get; set; }

		public TargetBinding CachedBinding { get; set; }

		public bool HasCachedBinding { get; set; }
	}

	private readonly record struct TargetBinding(Variables Variables, TargetValueKind Kind, Type? ValueType = null);
}
