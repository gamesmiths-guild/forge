// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Networking.Messages;

/// <summary>
/// The messages on the control channel. Each packet on it starts with one of these, as a byte.
/// </summary>
public enum ControlMessage : byte
{
	/// <summary>
	/// A client's first message: what it's running, for the server to accept or refuse. Reliable.
	/// </summary>
	Hello = 1,

	/// <summary>
	/// The server accepting a client, with the session's tick rate and current tick. Reliable.
	/// </summary>
	Welcome = 2,

	/// <summary>
	/// The server refusing a client, with why. Reliable, and the last message the client gets.
	/// </summary>
	Refused = 3,

	/// <summary>
	/// A client asking the server for its tick. Unreliable.
	/// </summary>
	Ping = 4,

	/// <summary>
	/// The server's answer to a ping, with its tick and how the client's input is arriving. Unreliable.
	/// </summary>
	Pong = 5,
}
