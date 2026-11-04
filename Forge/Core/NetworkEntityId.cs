// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Core;

/// <summary>
/// The id an entity is known by on every machine of a session. The server assigns one to each networked entity as it
/// spawns, the spawn message carries it, and from then on every reference to the entity on the wire, and every checksum
/// that names it, uses the id rather than anything local.
/// </summary>
/// <param name="Value">The id, positive for a networked entity.</param>
public readonly record struct NetworkEntityId(uint Value)
{
	/// <summary>
	/// No id: an entity that exists only on this machine.
	/// </summary>
	public static readonly NetworkEntityId None = new(0);

	/// <summary>
	/// Gets a value indicating whether this names a networked entity.
	/// </summary>
	public bool IsValid => Value != 0;

	/// <inheritdoc/>
	public override string ToString()
	{
		return IsValid ? $"#{Value}" : "local";
	}
}
