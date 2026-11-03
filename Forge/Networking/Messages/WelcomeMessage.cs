// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Networking.Messages;

/// <summary>
/// The server accepting a client.
/// </summary>
/// <param name="TickRate">How many ticks make a second in this session.</param>
/// <param name="ServerTick">The tick the server is about to simulate as it sends this.</param>
/// <param name="SessionSeed">The seed the session's randomness derives from.</param>
public readonly record struct WelcomeMessage(int TickRate, long ServerTick, ulong SessionSeed)
{
	/// <summary>
	/// Writes the message, after its <see cref="ControlMessage.Welcome"/> byte.
	/// </summary>
	/// <param name="writer">Where to write.</param>
	public void Write(BitWriter writer)
	{
		writer.WriteByte((byte)ControlMessage.Welcome);
		writer.WriteVarUInt((ulong)TickRate);
		writer.WriteVarInt(ServerTick);
		writer.WriteUInt64(SessionSeed);
	}

	/// <summary>
	/// Reads the message, after its <see cref="ControlMessage.Welcome"/> byte.
	/// </summary>
	/// <param name="reader">Where to read.</param>
	/// <returns>The message.</returns>
	/// <exception cref="MalformedPacketException">Thrown when the tick rate isn't one.</exception>
	public static WelcomeMessage Read(BitReader reader)
	{
		ulong tickRate = reader.ReadVarUInt();

		if (tickRate is 0 or > 1000)
		{
			throw new MalformedPacketException($"{tickRate} is not a tick rate.");
		}

		return new WelcomeMessage((int)tickRate, reader.ReadVarInt(), reader.ReadUInt64());
	}
}
