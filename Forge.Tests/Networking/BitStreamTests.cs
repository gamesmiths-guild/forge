// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Networking;

namespace Gamesmiths.Forge.Tests.Networking;

/// <summary>
/// Everything a <see cref="BitWriter"/> writes, a <see cref="BitReader"/> reads back the same, at any bit offset, and
/// the reader refuses what no writer produces.
/// </summary>
public class BitStreamTests
{
	[Theory]
	[Trait("Networking", "Encoding")]
	[InlineData(0UL, 1)]
	[InlineData(1UL, 1)]
	[InlineData(5UL, 3)]
	[InlineData(0xFFUL, 8)]
	[InlineData(0x1234UL, 13)]
	[InlineData(0xDEADBEEFUL, 32)]
	[InlineData(ulong.MaxValue, 64)]
	[InlineData(0x0123456789ABCDEFUL, 64)]
	public void Fixed_width_values_round_trip(ulong value, int bits)
	{
		var writer = new BitWriter();
		writer.WriteBits(value, bits);

		var reader = new BitReader(writer.ToArray());

		reader.ReadBits(bits).Should().Be(value);
		writer.BitLength.Should().Be(bits);
		reader.BitsRemaining.Should().BeLessThan(8);
	}

	[Fact]
	[Trait("Networking", "Encoding")]
	public void Values_straddling_byte_boundaries_round_trip_in_order()
	{
		var writer = new BitWriter(capacity: 1);
		writer.WriteBits(0b101, 3);
		writer.WriteBool(true);
		writer.WriteBits(0xABCD, 16);
		writer.WriteBits(0b0, 1);
		writer.WriteUInt64(0xFEDCBA9876543210);
		writer.WriteBits(0b1111111, 7);

		var reader = new BitReader(writer.ToArray());

		reader.ReadBits(3).Should().Be(0b101);
		reader.ReadBool().Should().BeTrue();
		reader.ReadBits(16).Should().Be(0xABCD);
		reader.ReadBool().Should().BeFalse();
		reader.ReadUInt64().Should().Be(0xFEDCBA9876543210);
		reader.ReadBits(7).Should().Be(0b1111111);
		writer.BitLength.Should().Be(3 + 1 + 16 + 1 + 64 + 7);
	}

	[Fact]
	[Trait("Networking", "Encoding")]
	public void Bits_above_the_width_are_ignored()
	{
		var writer = new BitWriter();
		writer.WriteBits(0xFF, 4);
		writer.WriteBits(0b1010, 4);

		var reader = new BitReader(writer.ToArray());

		reader.ReadBits(4).Should().Be(0xF);
		reader.ReadBits(4).Should().Be(0b1010);
		writer.ByteLength.Should().Be(1);
	}

	[Theory]
	[Trait("Networking", "Encoding")]
	[InlineData(0UL, 1)]
	[InlineData(127UL, 1)]
	[InlineData(128UL, 2)]
	[InlineData(16_383UL, 2)]
	[InlineData(16_384UL, 3)]
	[InlineData(uint.MaxValue, 5)]
	[InlineData(ulong.MaxValue, 10)]
	public void Variable_width_unsigned_integers_cost_a_byte_per_seven_bits(ulong value, int expectedBytes)
	{
		var writer = new BitWriter();
		writer.WriteVarUInt(value);

		writer.ByteLength.Should().Be(expectedBytes);
		new BitReader(writer.ToArray()).ReadVarUInt().Should().Be(value);
	}

	[Theory]
	[Trait("Networking", "Encoding")]
	[InlineData(0L, 1)]
	[InlineData(-1L, 1)]
	[InlineData(63L, 1)]
	[InlineData(-64L, 1)]
	[InlineData(64L, 2)]
	[InlineData(-65L, 2)]
	[InlineData(long.MaxValue, 10)]
	[InlineData(long.MinValue, 10)]
	public void Variable_width_signed_integers_cost_by_magnitude(long value, int expectedBytes)
	{
		var writer = new BitWriter();
		writer.WriteVarInt(value);

		writer.ByteLength.Should().Be(expectedBytes);
		new BitReader(writer.ToArray()).ReadVarInt().Should().Be(value);
	}

	[Theory]
	[Trait("Networking", "Encoding")]
	[InlineData(0f)]
	[InlineData(1f)]
	[InlineData(-2.5f)]
	[InlineData(float.MaxValue)]
	[InlineData(float.Epsilon)]
	[InlineData(float.NegativeInfinity)]
	public void Floats_round_trip_bit_for_bit(float value)
	{
		var writer = new BitWriter();
		writer.WriteFloat(value);

		new BitReader(writer.ToArray()).ReadFloat().Should().Be(value);
	}

	[Fact]
	[Trait("Networking", "Encoding")]
	public void A_nan_float_round_trips_as_a_nan()
	{
		var writer = new BitWriter();
		writer.WriteFloat(float.NaN);

		float.IsNaN(new BitReader(writer.ToArray()).ReadFloat()).Should().BeTrue();
	}

	[Theory]
	[Trait("Networking", "Encoding")]
	[InlineData(0f, 0.001f, 0f)]
	[InlineData(1.2344f, 0.001f, 1.234f)]
	[InlineData(-7.8886f, 0.001f, -7.889f)]
	[InlineData(0.0004f, 0.001f, 0f)]
	[InlineData(0.0006f, 0.001f, 0.001f)]
	[InlineData(12_345.678f, 0.01f, 12_345.68f)]
	[InlineData(3f, 0.5f, 3f)]
	public void Stepped_floats_round_to_the_step_and_read_back_what_was_sent(float value, float step, float expected)
	{
		var writer = new BitWriter();
		writer.WriteQuantized(value, step);

		float read = new BitReader(writer.ToArray()).ReadQuantized(step);

		read.Should().BeApproximately(expected, step / 100);
		BitWriter.Quantize(value, step).Should().Be(read, "the sender simulates exactly what the receiver gets");
	}

	[Fact]
	[Trait("Networking", "Encoding")]
	public void A_stepped_float_costs_by_its_magnitude_in_steps()
	{
		var small = new BitWriter();
		small.WriteQuantized(0.05f, 0.001f);

		var large = new BitWriter();
		large.WriteQuantized(500f, 0.001f);

		small.ByteLength.Should().Be(1, "50 steps fit in one byte");
		large.ByteLength.Should().Be(3, "500,000 steps need three");
	}

	[Fact]
	[Trait("Networking", "Encoding")]
	public void A_stepped_nan_is_sent_as_zero()
	{
		var writer = new BitWriter();
		writer.WriteQuantized(float.NaN, 0.001f);

		new BitReader(writer.ToArray()).ReadQuantized(0.001f).Should().Be(0f);
	}

	[Theory]
	[Trait("Networking", "Encoding")]
	[InlineData(float.PositiveInfinity, long.MaxValue - 1023)]
	[InlineData(float.NegativeInfinity, -(long.MaxValue - 1023))]
	[InlineData(float.MaxValue, long.MaxValue - 1023)]
	public void A_stepped_float_past_the_step_range_is_clamped(float value, long expectedSteps)
	{
		var writer = new BitWriter();
		writer.WriteQuantized(value, 0.001f);

		new BitReader(writer.ToArray()).ReadVarInt().Should().Be(expectedSteps);
	}

	[Theory]
	[Trait("Networking", "Encoding")]
	[InlineData(0f, 8)]
	[InlineData(1f, 8)]
	[InlineData(0.5f, 8)]
	[InlineData(-1f, 16)]
	[InlineData(0.3f, 16)]
	[InlineData(0.123456f, 32)]
	public void Ranged_floats_read_back_within_the_range_s_precision(float value, int bits)
	{
		const float Min = -1f;
		const float Max = 1f;
		double precision = (Max - Min) / ((1UL << bits) - 1);

		var writer = new BitWriter();
		writer.WriteQuantized(value, Min, Max, bits);

		writer.BitLength.Should().Be(bits);
		new BitReader(writer.ToArray()).ReadQuantized(Min, Max, bits)
			.Should().BeApproximately(value, (float)precision);
	}

	[Fact]
	[Trait("Networking", "Encoding")]
	public void A_ranged_float_outside_the_range_is_clamped()
	{
		var writer = new BitWriter();
		writer.WriteQuantized(5f, 0f, 1f, 8);
		writer.WriteQuantized(-5f, 0f, 1f, 8);

		var reader = new BitReader(writer.ToArray());

		reader.ReadQuantized(0f, 1f, 8).Should().Be(1f);
		reader.ReadQuantized(0f, 1f, 8).Should().Be(0f);
	}

	[Fact]
	[Trait("Networking", "Encoding")]
	public void A_ranged_nan_is_written_as_the_minimum()
	{
		var writer = new BitWriter();
		writer.WriteQuantized(float.NaN, -1f, 1f, 8);

		new BitReader(writer.ToArray()).ReadQuantized(-1f, 1f, 8).Should().Be(-1f);
	}

	[Fact]
	[Trait("Networking", "Encoding")]
	public void A_ranged_float_keeps_its_endpoints_and_full_precision()
	{
		var writer = new BitWriter();
		writer.WriteQuantized(0.1f, -1f, 0.1f, 32);
		writer.WriteQuantized(0.1f, -1f, 1f, 32);

		var reader = new BitReader(writer.ToArray());

		reader.ReadQuantized(-1f, 0.1f, 32).Should().Be(0.1f, "the maximum reads back as itself, not past it");
		reader.ReadQuantized(-1f, 1f, 32).Should().Be(0.1f);
	}

	[Theory]
	[Trait("Networking", "Encoding")]
	[InlineData("")]
	[InlineData(null)]
	[InlineData("tag")]
	[InlineData("enemy.undead.zombie")]
	[InlineData("ação com acentos e 日本語")]
	public void Strings_round_trip_as_utf8(string? value)
	{
		var writer = new BitWriter();
		writer.WriteBits(0b11, 2);
		writer.WriteString(value);
		writer.WriteBool(true);

		var reader = new BitReader(writer.ToArray());

		reader.ReadBits(2).Should().Be(0b11);
		reader.ReadString().Should().Be(value ?? string.Empty);
		reader.ReadBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Networking", "Encoding")]
	public void Aligning_pads_to_the_next_byte_once()
	{
		var writer = new BitWriter();
		writer.WriteBits(0b1, 3);
		writer.Align();
		writer.Align();
		writer.WriteByte(0xAB);

		writer.BitLength.Should().Be(16);

		var reader = new BitReader(writer.ToArray());
		reader.ReadBits(3).Should().Be(0b1);
		reader.Align();
		reader.Align();
		reader.ReadByte().Should().Be(0xAB);
	}

	[Fact]
	[Trait("Networking", "Encoding")]
	public void The_writer_grows_past_its_capacity_and_clears_for_reuse()
	{
		var writer = new BitWriter(capacity: 2);

		for (int i = 0; i < 100; i++)
		{
			writer.WriteUInt32((uint)i);
		}

		writer.ByteLength.Should().Be(400);

		var reader = new BitReader(writer.ToArray());

		for (int i = 0; i < 100; i++)
		{
			reader.ReadUInt32().Should().Be((uint)i);
		}

		writer.Clear();
		writer.BitLength.Should().Be(0);
		writer.WriteBits(0b1, 1);
		writer.ToArray().Should().Equal(0b1);
	}

	[Fact]
	[Trait("Networking", "Encoding")]
	public void Reading_past_the_end_throws_a_malformed_packet()
	{
		var writer = new BitWriter();
		writer.WriteBits(0b101, 3);
		var reader = new BitReader(writer.ToArray());

		reader.ReadBits(8).Should().Be(0b101, "the last byte's unwritten bits are zero");
		reader.Invoking(x => x.ReadBool()).Should().Throw<MalformedPacketException>();
	}

	[Fact]
	[Trait("Networking", "Encoding")]
	public void A_variable_width_integer_that_never_ends_is_malformed()
	{
		byte[] endless = new byte[12];
		Array.Fill(endless, (byte)0xFF);

		new BitReader(endless).Invoking(x => x.ReadVarUInt()).Should().Throw<MalformedPacketException>();
	}

	[Fact]
	[Trait("Networking", "Encoding")]
	public void A_variable_width_integer_past_64_bits_is_malformed()
	{
		byte[] highest = [0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x01];
		byte[] overflowing = [0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x02];

		new BitReader(highest).ReadVarUInt().Should().Be(1UL << 63);
		new BitReader(overflowing).Invoking(x => x.ReadVarUInt()).Should().Throw<MalformedPacketException>();
	}

	[Fact]
	[Trait("Networking", "Encoding")]
	public void A_string_longer_than_allowed_is_malformed()
	{
		var writer = new BitWriter();
		writer.WriteString(new string('x', 20));

		new BitReader(writer.ToArray()).Invoking(x => x.ReadString(maxBytes: 10))
			.Should().Throw<MalformedPacketException>();
	}

	[Theory]
	[Trait("Networking", "Encoding")]
	[InlineData(0UL, 1)]
	[InlineData(1UL, 1)]
	[InlineData(2UL, 2)]
	[InlineData(255UL, 8)]
	[InlineData(256UL, 9)]
	[InlineData(ulong.MaxValue, 64)]
	public void Bits_for_a_maximum_value_is_the_smallest_width_that_holds_it(ulong maxValue, int expected)
	{
		BitWriter.BitsFor(maxValue).Should().Be(expected);
	}

	[Theory]
	[Trait("Networking", "Encoding")]
	[InlineData(-1)]
	[InlineData(65)]
	public void A_width_outside_zero_to_sixty_four_is_rejected(int bits)
	{
		new BitWriter().Invoking(x => x.WriteBits(0, bits)).Should().Throw<ArgumentOutOfRangeException>();
		new BitReader(new byte[16]).Invoking(x => x.ReadBits(bits)).Should().Throw<ArgumentOutOfRangeException>();
	}
}
