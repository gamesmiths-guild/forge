// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Networking;

/// <summary>
/// The constants both ends of a session agree on before anything else: the protocol version, the channels, and the
/// hashes the handshake compares.
/// </summary>
public static class Protocol
{
	/// <summary>
	/// The version of the wire protocol. Bumped whenever a message's layout changes, so an old build is refused instead
	/// of misreading a new one.
	/// </summary>
	public const ushort Version = 6;

	/// <summary>
	/// The channel for connection control: the handshake, pings, and later the disconnect. Its messages are
	/// reliable, except pings, which are worthless late.
	/// </summary>
	public const byte ControlChannel = 0;

	/// <summary>
	/// The channel a client's input windows go up on, unreliable.
	/// </summary>
	public const byte InputChannel = 1;

	/// <summary>
	/// The channel the server's state goes down on, unreliable.
	/// </summary>
	public const byte StateChannel = 2;

	/// <summary>
	/// Hashes the tags in the order of their net indices, so a client whose project has other tags is refused rather
	/// than having its input events read as other tags.
	/// </summary>
	/// <param name="tagsManager">The tags.</param>
	/// <returns>A hash the same on every machine with the same tags.</returns>
	public static ulong HashOf(TagsManager tagsManager)
	{
		int count = tagsManager.NetIndexCount;
		ulong hash = Fnv64.Add(Fnv64.Offset, (ulong)count);

		for (int i = 0; i < count; i++)
		{
			hash = Fnv64.Add(hash, tagsManager.GetTagKeyFromNetIndex((ushort)i).ToString());
		}

		return hash;
	}

	/// <summary>
	/// Hashes an input action set, so a client whose project lists the actions in another order, or lists other
	/// actions, is refused rather than having its presses read as other presses.
	/// </summary>
	/// <param name="actions">The set.</param>
	/// <returns>A hash the same on every machine with the same set.</returns>
	public static ulong HashOf(InputActionSet actions)
	{
		ulong hash = Fnv64.Offset;
		hash = Fnv64.Add(hash, (ulong)actions.Actions.Count);

		foreach (string action in actions.Actions)
		{
			hash = Fnv64.Add(hash, action);
		}

		hash = Fnv64.Add(hash, (ulong)actions.Vectors.Count);

		foreach (InputVector vector in actions.Vectors)
		{
			hash = Fnv64.Add(hash, vector.NegativeX);
			hash = Fnv64.Add(hash, vector.PositiveX);
			hash = Fnv64.Add(hash, vector.NegativeY);
			hash = Fnv64.Add(hash, vector.PositiveY);
		}

		return hash;
	}
}
