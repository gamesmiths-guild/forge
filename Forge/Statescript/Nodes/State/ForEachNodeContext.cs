// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// The context for a <see cref="ForEachNode"/>. Holds the snapshot of the iterated array taken on activation, plus the
/// element variable the node writes each iteration into.
/// </summary>
public class ForEachNodeContext : IterationNodeContext
{
	/// <summary>
	/// Gets or sets the snapshot of a value-typed source array. <see langword="null"/> when the source is
	/// object-backed or resolved nothing.
	/// </summary>
	public Variant128[]? Values { get; set; }

	/// <summary>
	/// Gets or sets the snapshot of an object-backed source array. <see langword="null"/> when the source is
	/// value-typed or resolved nothing.
	/// </summary>
	public object?[]? ObjectValues { get; set; }

	/// <summary>
	/// Gets or sets the variable bag holding the element variable, or <see langword="null"/> when no element variable
	/// can be written this run.
	/// </summary>
	public Variables? ElementVariables { get; set; }

	/// <summary>
	/// Gets or sets the name of the element variable to write each iteration.
	/// </summary>
	public StringKey ElementVariableName { get; set; }
}
