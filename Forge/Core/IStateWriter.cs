// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript;

namespace Gamesmiths.Forge.Core;

// Walks an entity snapshot value by value, for a checksum to hash it or a description to list it. A scope opened as a
// set holds values and scopes told apart by their names rather than by their order.
internal interface IStateWriter
{
	void Open(string name);

	void Open(string name, int index);

	void OpenSet(string name);

	void Close();

	void Write(string name, long value);

	void Write(string name, double value);

	void Write(string name, string? value);

	void Write(string name, in Variant128 value);

	// An entity is written as its place in the world the snapshot belongs to, so two runs that spawned equal entities
	// write the same.
	void WriteEntity(string name, IForgeEntity? entity);
}
