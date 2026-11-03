// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Networking.Messages;

/// <summary>
/// Why the server refused a client.
/// </summary>
public enum RefusalReason : byte
{
	/// <summary>
	/// The client runs another version of the protocol.
	/// </summary>
	ProtocolVersion = 1,

	/// <summary>
	/// The client's assets differ from the server's, so an index would mean different things.
	/// </summary>
	AssetRegistry = 2,

	/// <summary>
	/// The client's input actions differ from the server's, so a press would be read as another press.
	/// </summary>
	InputActions = 3,

	/// <summary>
	/// The session has no room.
	/// </summary>
	Full = 4,
}

/// <summary>
/// The server refusing a client.
/// </summary>
/// <param name="Reason">Why.</param>
public readonly record struct RefusedMessage(RefusalReason Reason)
{
	/// <summary>
	/// Writes the message, after its <see cref="ControlMessage.Refused"/> byte.
	/// </summary>
	/// <param name="writer">Where to write.</param>
	public void Write(BitWriter writer)
	{
		writer.WriteByte((byte)ControlMessage.Refused);
		writer.WriteByte((byte)Reason);
	}

	/// <summary>
	/// Reads the message, after its <see cref="ControlMessage.Refused"/> byte.
	/// </summary>
	/// <param name="reader">Where to read.</param>
	/// <returns>The message.</returns>
	public static RefusedMessage Read(BitReader reader)
	{
		return new RefusedMessage((RefusalReason)reader.ReadByte());
	}
}
