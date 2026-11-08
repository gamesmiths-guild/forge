// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

// A resolver that reads one variable as it is, which a graph's network table follows from a node's input to the
// variable behind it.
internal interface IVariableReader
{
	StringKey VariableName { get; }

	VariableScope Scope { get; }
}
