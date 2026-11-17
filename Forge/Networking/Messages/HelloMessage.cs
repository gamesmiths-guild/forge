// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Networking.Replication;

namespace Gamesmiths.Forge.Networking.Messages;

/// <summary>
/// A client's first message: the protocol version it speaks, the hashes of its asset registry, tags and input action
/// set, and the precision it rounds bodies to, for the server to compare with its own.
/// </summary>
/// <param name="Version">The protocol version.</param>
/// <param name="RegistryHash">The asset registry's hash.</param>
/// <param name="TagsHash">The tags' hash.</param>
/// <param name="InputHash">The input action set's hash.</param>
/// <param name="BodyPrecision">The precision body states are rounded to.</param>
public readonly record struct HelloMessage(
	ushort Version,
	ulong RegistryHash,
	ulong TagsHash,
	ulong InputHash,
	BodyPrecision BodyPrecision)
{
	/// <summary>
	/// Writes the message, after its <see cref="ControlMessage.Hello"/> byte.
	/// </summary>
	/// <param name="writer">Where to write.</param>
	public void Write(BitWriter writer)
	{
		writer.WriteByte((byte)ControlMessage.Hello);
		writer.WriteUInt16(Version);
		writer.WriteUInt64(RegistryHash);
		writer.WriteUInt64(TagsHash);
		writer.WriteUInt64(InputHash);
		writer.WriteVarInt(BodyPrecision.PositionSteps);
		writer.WriteVarInt(BodyPrecision.VelocitySteps);
		writer.WriteVarInt(BodyPrecision.RotationBits);
	}

	/// <summary>
	/// Reads the message, after its <see cref="ControlMessage.Hello"/> byte.
	/// </summary>
	/// <param name="reader">Where to read.</param>
	/// <returns>The message. One of another protocol version carries the version alone, since what follows it may
	/// differ.</returns>
	public static HelloMessage Read(BitReader reader)
	{
		ushort version = reader.ReadUInt16();

		if (version != Protocol.Version)
		{
			return new HelloMessage(version, 0, 0, 0, default);
		}

		return new HelloMessage(
			version,
			reader.ReadUInt64(),
			reader.ReadUInt64(),
			reader.ReadUInt64(),
			new BodyPrecision((int)reader.ReadVarInt(), (int)reader.ReadVarInt(), (int)reader.ReadVarInt()));
	}
}
