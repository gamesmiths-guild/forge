// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Networking;

/// <summary>
/// Identifies a machine in a session. The server is always <see cref="Server"/>; a client's id is whatever the
/// transport assigned it when it connected, and stays the same for the client's whole connection.
/// </summary>
/// <param name="Value">The transport's number for the peer, positive for a connected peer.</param>
public readonly record struct PeerId(int Value)
{
	/// <summary>
	/// No peer.
	/// </summary>
	public static readonly PeerId None = new(0);

	/// <summary>
	/// The server, which every session has exactly one of. A listen host is the server and plays on it.
	/// </summary>
	public static readonly PeerId Server = new(1);

	/// <summary>
	/// Gets a value indicating whether this is the server.
	/// </summary>
	public bool IsServer => Value == Server.Value;

	/// <summary>
	/// Gets a value indicating whether this names a peer at all.
	/// </summary>
	public bool IsValid => Value > 0;

	/// <inheritdoc/>
	public override string ToString()
	{
		if (IsServer)
		{
			return "server";
		}
		else if (IsValid)
		{
			return $"peer {Value}";
		}
		else
		{
			return "no peer";
		}
	}
}
