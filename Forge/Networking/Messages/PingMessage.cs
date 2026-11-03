// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Networking.Messages;

/// <summary>
/// A client asking the server for its tick.
/// </summary>
/// <param name="Sequence">The ping's number, for the answer to carry back.</param>
public readonly record struct PingMessage(ushort Sequence)
{
	/// <summary>
	/// Writes the message, after its <see cref="ControlMessage.Ping"/> byte.
	/// </summary>
	/// <param name="writer">Where to write.</param>
	public void Write(BitWriter writer)
	{
		writer.WriteByte((byte)ControlMessage.Ping);
		writer.WriteUInt16(Sequence);
	}

	/// <summary>
	/// Reads the message, after its <see cref="ControlMessage.Ping"/> byte.
	/// </summary>
	/// <param name="reader">Where to read.</param>
	/// <returns>The message.</returns>
	public static PingMessage Read(BitReader reader)
	{
		return new PingMessage(reader.ReadUInt16());
	}
}
