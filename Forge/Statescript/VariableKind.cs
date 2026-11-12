// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript;

// The kinds of variable a Variables bag holds, each under names of its own.
internal enum VariableKind : byte
{
	Value = 0,
	Array = 1,
	Object = 2,
	ObjectArray = 3,
}
