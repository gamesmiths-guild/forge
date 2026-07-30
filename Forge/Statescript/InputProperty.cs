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
/// <param name="IsOptional">Whether leaving this input unbound is a meaningful authoring choice. Set this only when the
/// node reads <see cref="BoundName"/> emptiness as a distinct state that no binding can reproduce — for example an
/// entity input whose absence means "no source" rather than "the owner", or a cue parameter whose absence suppresses
/// the whole parameter set. Tooling uses this to offer an explicit "none" option and to leave a fresh slot unbound
/// instead of seeding it with a default resolver. Inputs whose unbound behavior equals some selectable resolver (a
/// level that falls back to the context level, a magnitude that defaults to zero) must leave this
/// <see langword="false"/>, so the editor does not present two spellings of the same value.</param>
public readonly record struct InputProperty(string Label, Type ExpectedType, bool IsOptional = false)
{
	/// <summary>
	/// Gets the bound variable or property name. This is set via <see cref="Node.BindInput"/> after the node is
	/// constructed. Before binding, this value is <see cref="StringKey.Empty"/>.
	/// </summary>
	public StringKey BoundName { get; internal init; } = StringKey.Empty;
}
