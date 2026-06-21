// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Providers;

/// <summary>
/// Binds a declared event-payload output to the graph variable the event-listener node writes it to.
/// </summary>
/// <param name="VariableName">The target variable name.</param>
/// <param name="Scope">The scope (graph or shared) the variable lives in.</param>
public readonly record struct EventOutputBinding(StringKey VariableName, VariableScope Scope);
