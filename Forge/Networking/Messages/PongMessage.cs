// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Networking.Messages;

/// <summary>
/// The server's answer to a ping, which also tells the client how its input is arriving.
/// </summary>
/// <param name="Sequence">The ping's number.</param>
/// <param name="ServerTick">The tick the server was about to simulate when the ping arrived.</param>
/// <param name="AckedInputTick">The newest input tick the server has from the client, or -1 before any, so the
/// client can drop it from its window.</param>
/// <param name="InputDepth">How many input ticks the server holds ahead of the one it last ran: zero means the client
/// is late, a steady few is right, many means it runs too far ahead.</param>
public readonly record struct PongMessage(ushort Sequence, long ServerTick, long AckedInputTick, int InputDepth)
{
	/// <summary>
	/// Writes the message, after its <see cref="ControlMessage.Pong"/> byte.
	/// </summary>
	/// <param name="writer">Where to write.</param>
	public void Write(BitWriter writer)
	{
		writer.WriteByte((byte)ControlMessage.Pong);
		writer.WriteUInt16(Sequence);
		writer.WriteVarInt(ServerTick);
		writer.WriteVarInt(AckedInputTick);
		writer.WriteVarUInt((ulong)Math.Clamp(InputDepth, 0, byte.MaxValue));
	}

	/// <summary>
	/// Reads the message, after its <see cref="ControlMessage.Pong"/> byte.
	/// </summary>
	/// <param name="reader">Where to read.</param>
	/// <returns>The message.</returns>
	public static PongMessage Read(BitReader reader)
	{
		return new PongMessage(
			reader.ReadUInt16(),
			reader.ReadVarInt(),
			reader.ReadVarInt(),
			(int)Math.Min(reader.ReadVarUInt(), byte.MaxValue));
	}
}
