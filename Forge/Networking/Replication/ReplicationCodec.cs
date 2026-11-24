// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Shots;
using Gamesmiths.Forge.Statescript;

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
	private const int FlightEndBits = 2;

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

		int magnitude = ReadInt(reader);
		float normalized = reader.ReadBits(NormalizedBits) / NormalizedLevels;
		uint source = ReadEntityId(reader);

		return new CueParameters(
			magnitude,
			normalized,
			source == 0 ? null : entities(new NetworkEntityId(source)));
	}

	// A variable's value as its raw bits, so it arrives exactly as it was, whatever its type.
	internal static void WriteVariant(BitWriter writer, Variant128 value)
	{
		writer.WriteVarUInt(value.AsULong());
		writer.WriteVarUInt(value.AsUpperULong());
	}

	internal static Variant128 ReadVariant(BitReader reader)
	{
		ulong lower = reader.ReadVarUInt();
		return new Variant128(lower, reader.ReadVarUInt());
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

	// A flight as a launch or an end: its name and ticks relative to the packet's, and its path exactly, so every
	// machine flies it along the same points.
	internal static void WriteFlight(BitWriter writer, in ProjectileFlight flight, long tick, int projectile, int bits)
	{
		writer.WriteVarInt(tick - flight.Tick);
		writer.WriteVarInt(flight.Id.Sequence);
		writer.WriteBits((ulong)projectile, bits);
		WriteVector(writer, flight.Origin);
		WriteVector(writer, flight.Velocity);
		writer.WriteBool(!flight.IsFlying);

		if (flight.IsFlying)
		{
			return;
		}

		writer.WriteBits((ulong)flight.End, FlightEndBits);
		writer.WriteVarInt(flight.EndTick - flight.Tick);
		WriteVector(writer, flight.EndPoint);
		WriteVector(writer, flight.EndNormal);
	}

	internal static ProjectileFlight ReadFlight(
		BitReader reader,
		long tick,
		int bits,
		Func<int, ProjectileData> projectiles,
		IForgeEntity instigator)
	{
		long launched = tick - reader.ReadVarInt();
		long sequence = reader.ReadVarInt();

		if (sequence is > int.MaxValue or < int.MinValue)
		{
			throw new MalformedPacketException($"{sequence} is not a launch's sequence.");
		}

		ProjectileData projectile = projectiles((int)reader.ReadBits(bits));
		var flight = new ProjectileFlight(
			new ShotId(launched, (int)sequence),
			projectile,
			instigator,
			ReadVector(reader),
			ReadVector(reader));

		if (!reader.ReadBool())
		{
			return flight;
		}

		var end = (FlightEnd)reader.ReadBits(FlightEndBits);

		if (end == FlightEnd.None)
		{
			throw new MalformedPacketException("An ended flight has no end.");
		}

		return flight with
		{
			End = end,
			EndTick = launched + reader.ReadVarInt(),
			EndPoint = ReadVector(reader),
			EndNormal = ReadVector(reader),
		};
	}

	private static void WriteVector(BitWriter writer, System.Numerics.Vector3 value)
	{
		writer.WriteFloat(value.X);
		writer.WriteFloat(value.Y);
		writer.WriteFloat(value.Z);
	}

	private static System.Numerics.Vector3 ReadVector(BitReader reader)
	{
		return new System.Numerics.Vector3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat());
	}
}
