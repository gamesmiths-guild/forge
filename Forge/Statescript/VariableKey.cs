// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript;

// A variable of a Variables bag: its kind and name, since each kind has names of its own.
internal readonly record struct VariableKey(VariableKind Kind, StringKey Name);
