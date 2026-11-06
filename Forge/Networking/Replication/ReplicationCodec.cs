// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;

namespace Gamesmiths.Forge.Networking.Replication;

// What an entity's entry in a state packet is.
internal enum EntryKind : byte
{
	State = 0,
	Spawn = 1,
	Despawn = 2,
}

// The pieces of a state packet shared by the server that writes it and the client that reads it.
internal static class ReplicationCodec
{
	// The most of anything one entry claims, so a bad packet can't ask for an unbounded read.
	internal const int MaxCount = 4096;

	// Normalized magnitudes are sent in a thousandth.
	private const int NormalizedBits = 10;

	private const float NormalizedLevels = (1 << NormalizedBits) - 1;

	internal static void WriteParameters(BitWriter writer, CueParameters? parameters)
	{
		if (parameters is not CueParameters value)
		{
			writer.WriteBool(false);
			return;
		}

		writer.WriteBool(true);
		writer.WriteVarInt(value.Magnitude);
		double normalized = float.IsNaN(value.NormalizedMagnitude) ? 0 : Math.Clamp(value.NormalizedMagnitude, 0, 1);
		writer.WriteBits((ulong)Math.Round(normalized * NormalizedLevels), NormalizedBits);
		writer.WriteVarUInt(value.Source?.NetworkId.Value ?? 0);
	}

	internal static CueParameters? ReadParameters(BitReader reader, Func<NetworkEntityId, IForgeEntity?> entities)
	{
		if (!reader.ReadBool())
		{
			return null;
		}

		long magnitude = reader.ReadVarInt();

		if (magnitude is < int.MinValue or > int.MaxValue)
		{
			throw new MalformedPacketException($"{magnitude} is not a cue magnitude.");
		}

		float normalized = reader.ReadBits(NormalizedBits) / NormalizedLevels;
		uint source = ReadEntityId(reader);

		return new CueParameters(
			(int)magnitude,
			normalized,
			source == 0 ? null : entities(new NetworkEntityId(source)));
	}

	internal static uint ReadEntityId(BitReader reader)
	{
		ulong id = reader.ReadVarUInt();

		if (id > uint.MaxValue)
		{
			throw new MalformedPacketException($"{id} is not an entity id.");
		}

		return (uint)id;
	}

	internal static int ReadCount(BitReader reader, int max = MaxCount)
	{
		ulong count = reader.ReadVarUInt();

		if (count > (ulong)max)
		{
			throw new MalformedPacketException($"A packet claims {count} of something, more than the {max} allowed.");
		}

		return (int)count;
	}

	internal static int ReadInt(BitReader reader)
	{
		long value = reader.ReadVarInt();

		if (value is < int.MinValue or > int.MaxValue)
		{
			throw new MalformedPacketException($"{value} doesn't fit an integer.");
		}

		return (int)value;
	}

	internal static ushort ReadTagIndex(BitReader reader, int netIndexCount)
	{
		ulong index = reader.ReadVarUInt();

		if (index >= (ulong)netIndexCount)
		{
			throw new MalformedPacketException($"{index} is not a tag net index.");
		}

		return (ushort)index;
	}
}
