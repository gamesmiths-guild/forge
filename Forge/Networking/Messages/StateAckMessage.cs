// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Networking.Messages;

/// <summary>
/// A client telling the server which state packets it received: the newest sequence, and which of the 32 before it
/// arrived, one bit each, so a lost acknowledgement costs nothing.
/// </summary>
/// <param name="Latest">The newest sequence received.</param>
/// <param name="Mask">Bit <c>i</c> set means sequence <c>Latest - 1 - i</c> was received.</param>
public readonly record struct StateAckMessage(uint Latest, uint Mask)
{
	/// <summary>
	/// Writes the message.
	/// </summary>
	/// <param name="writer">The writer.</param>
	public void Write(BitWriter writer)
	{
		writer.WriteVarUInt(Latest);
		writer.WriteUInt32(Mask);
	}

	/// <summary>
	/// Reads a message.
	/// </summary>
	/// <param name="reader">The reader.</param>
	/// <returns>The message.</returns>
	/// <exception cref="MalformedPacketException">Thrown when the message can't be one a writer wrote.</exception>
	public static StateAckMessage Read(BitReader reader)
	{
		ulong latest = reader.ReadVarUInt();

		if (latest > uint.MaxValue)
		{
			throw new MalformedPacketException($"{latest} is not a packet sequence.");
		}

		return new StateAckMessage((uint)latest, reader.ReadUInt32());
	}

	/// <summary>
	/// Whether a sequence is acknowledged by the message.
	/// </summary>
	/// <param name="sequence">The sequence.</param>
	/// <returns><see langword="true"/> if the client received it.</returns>
	public bool Acknowledges(uint sequence)
	{
		if (sequence == Latest)
		{
			return true;
		}

		if (sequence > Latest || Latest - sequence > 32)
		{
			return false;
		}

		return (Mask & (1u << (int)(Latest - sequence - 1))) != 0;
	}
}
