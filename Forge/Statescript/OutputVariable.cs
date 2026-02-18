// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// Specifies which <see cref="Variables"/> bag an <see cref="OutputVariable"/> writes to at runtime.
/// </summary>
public enum VariableScope
{
	/// <summary>
	/// The variable is written to the per-graph-instance <see cref="GraphContext.GraphVariables"/> bag.
	/// </summary>
	Graph = 0,

	/// <summary>
	/// The variable is written to the entity-level <see cref="GraphContext.SharedVariables"/> bag.
	/// </summary>
	Shared = 1,
}

/// <summary>
/// Declares that a node writes a named variable at runtime. The actual name is bound after construction via
/// <see cref="Node.BindOutput"/>.
/// </summary>
/// <param name="Label">A human-readable label for this output (e.g., "Target", "Result"). Used by editor tooling
/// and documentation.</param>
/// <param name="ValueType">The <see cref="Type"/> the node writes. Tooling uses this for display and validation.
/// </param>
/// <param name="Scope">Which <see cref="Variables"/> bag this output writes to.</param>
public readonly record struct OutputVariable(string Label, Type ValueType, VariableScope Scope = VariableScope.Graph)
{
	/// <summary>
	/// Gets the bound variable name. This is set via <see cref="Node.BindOutput"/> after the node is constructed.
	/// Before binding, this value is <see langword="default"/>.
	/// </summary>
	public StringKey BoundName { get; internal init; }
}
