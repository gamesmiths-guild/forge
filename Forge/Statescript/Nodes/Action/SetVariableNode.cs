// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Nodes.Action;

/// <summary>
/// An action node that writes a value to a graph variable. The value is read from a source property (which can be a
/// fixed variable, an entity attribute, or any other <see cref="Properties.IPropertyResolver"/>) and written to a
/// target variable (which must be backed by a <see cref="Properties.VariantResolver"/>).
/// </summary>
/// <remarks>
/// <para>The source and target are bound by property name at graph construction time. At runtime, the node resolves the
/// source property and writes its <see cref="Variant128"/> value to the target variable.</para>
/// <para>The source property is an <em>input</em>, it is only read. The target variable is an <em>output</em>, it is
/// only written.</para>
/// </remarks>
/// <param name="sourcePropertyName">The name of the graph property to read from.</param>
/// <param name="targetVariableName">The name of the graph variable to write to.</param>
public class SetVariableNode(StringKey sourcePropertyName, StringKey targetVariableName) : ActionNode
{
	private readonly StringKey _sourcePropertyName = sourcePropertyName;

	private readonly StringKey _targetVariableName = targetVariableName;

	/// <inheritdoc/>
	protected override void Execute(GraphContext graphContext)
	{
		if (!graphContext.GraphVariables.TryGetVariant(_sourcePropertyName, graphContext, out Variant128 value))
		{
			return;
		}

		graphContext.GraphVariables.SetVariant(_targetVariableName, value);
	}
}
