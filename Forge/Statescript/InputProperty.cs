// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// Declares that a node reads a named value at runtime. The value may come from a graph variable, a shared variable,
/// or a read-only property backed by any <see cref="Properties.IPropertyResolver"/> whose
/// <see cref="Properties.IPropertyResolver.ValueType"/> is assignable to <see cref="ExpectedType"/>. The actual name
/// is bound after construction via <see cref="Node.BindInput"/>.
/// </summary>
/// <param name="Label">A human-readable label for this input (e.g., "Duration", "Condition"). Used by editor tooling
/// and documentation.</param>
/// <param name="ExpectedType">The <see cref="Type"/> the node expects to read. Tooling uses this to filter which
/// resolvers or variables are valid binding targets.</param>
public readonly record struct InputProperty(string Label, Type ExpectedType)
{
	/// <summary>
	/// Gets the bound variable or property name. This is set via <see cref="Node.BindInput"/> after the node is
	/// constructed. Before binding, this value is <see langword="default"/>.
	/// </summary>
	public StringKey BoundName { get; internal init; }
}
