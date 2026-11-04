// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Networking.Messages;

/// <summary>
/// A client's first message: the protocol version it speaks, and the hashes of its asset registry, tags and input
/// action set, for the server to compare with its own.
/// </summary>
/// <param name="Version">The protocol version.</param>
/// <param name="RegistryHash">The asset registry's hash.</param>
/// <param name="TagsHash">The tags' hash.</param>
/// <param name="InputHash">The input action set's hash.</param>
public readonly record struct HelloMessage(ushort Version, ulong RegistryHash, ulong TagsHash, ulong InputHash)
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
	}

	/// <summary>
	/// Reads the message, after its <see cref="ControlMessage.Hello"/> byte.
	/// </summary>
	/// <param name="reader">Where to read.</param>
	/// <returns>The message.</returns>
	public static HelloMessage Read(BitReader reader)
	{
		return new HelloMessage(reader.ReadUInt16(), reader.ReadUInt64(), reader.ReadUInt64(), reader.ReadUInt64());
	}
}
