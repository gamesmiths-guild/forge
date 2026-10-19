// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Tests.Core;

public class DeterministicRandomTests
{
	private const int Samples = 2000;

	[Fact]
	[Trait("Random", "Deterministic")]
	public void The_sequence_for_a_seed_and_tick_never_changes()
	{
		// Pinned: two machines only agree on a roll if every build of Forge produces this exact sequence.
		var random = new DeterministicRandom(12345UL);

		random.NextInt64().Should().Be(6860419412842801741);
		random.NextInt64().Should().Be(1199458347604198499);
		random.NextInt64().Should().Be(8885192424992434628);

		random.BeginTick(7);

		random.NextInt64().Should().Be(5229388492309676965);
		random.NextInt64().Should().Be(6537658947463323429);
		random.NextInt64().Should().Be(5233143330264529661);

		random.BeginTick(7);

		random.NextInt(0, 100).Should().Be(31);
		random.NextInt(0, 100).Should().Be(58);
		random.NextInt(0, 100).Should().Be(23);
		random.NextInt(0, 100).Should().Be(62);
	}

	[Fact]
	[Trait("Random", "Deterministic")]
	public void Two_generators_with_the_same_seed_draw_the_same_numbers()
	{
		var first = new DeterministicRandom(42UL);
		var second = new DeterministicRandom(42UL);

		Draw(first, 32).Should().Equal(Draw(second, 32));
	}

	[Fact]
	[Trait("Random", "Deterministic")]
	public void Different_seeds_draw_different_numbers()
	{
		Draw(new DeterministicRandom(1UL), 8).Should().NotEqual(Draw(new DeterministicRandom(2UL), 8));
	}

	[Fact]
	[Trait("Random", "Deterministic")]
	public void Beginning_the_same_tick_again_repeats_its_draws()
	{
		var random = new DeterministicRandom(99UL);

		random.BeginTick(500);
		long[] first = Draw(random, 16);

		random.BeginTick(500);
		long[] second = Draw(random, 16);

		second.Should().Equal(first);
	}

	[Fact]
	[Trait("Random", "Deterministic")]
	public void A_tick_draws_the_same_numbers_however_many_were_drawn_before_it()
	{
		var frugal = new DeterministicRandom(7UL);
		frugal.BeginTick(1);
		Draw(frugal, 1);
		frugal.BeginTick(2);
		long[] afterFewDraws = Draw(frugal, 8);

		var greedy = new DeterministicRandom(7UL);
		greedy.BeginTick(1);
		Draw(greedy, 300);
		greedy.BeginTick(2);
		long[] afterManyDraws = Draw(greedy, 8);

		afterManyDraws.Should().Equal(
			afterFewDraws,
			"a roll one machine made and another did not must not shift later ticks");
	}

	[Fact]
	[Trait("Random", "Deterministic")]
	public void Neighbouring_ticks_draw_different_numbers()
	{
		var random = new DeterministicRandom(3UL);

		random.BeginTick(10);
		long[] tickTen = Draw(random, 8);

		random.BeginTick(11);
		long[] tickEleven = Draw(random, 8);

		tickEleven.Should().NotEqual(tickTen);
	}

	[Theory]
	[Trait("Random", "Deterministic")]
	[InlineData(0, 10)]
	[InlineData(-50, 50)]
	[InlineData(int.MinValue, int.MaxValue)]
	[InlineData(5, 6)]
	public void Exclusive_int_ranges_stay_in_range(int minValue, int maxValue)
	{
		var random = new DeterministicRandom(11UL);

		for (int i = 0; i < Samples; i++)
		{
			int value = random.NextInt(minValue, maxValue);
			value.Should().BeGreaterThanOrEqualTo(minValue).And.BeLessThan(maxValue);
		}
	}

	[Theory]
	[Trait("Random", "Deterministic")]
	[InlineData(0, 2)]
	[InlineData(-3, 3)]
	[InlineData(int.MinValue, int.MaxValue)]
	public void Inclusive_int_ranges_stay_in_range(int minValue, int maxValue)
	{
		var random = new DeterministicRandom(13UL);

		for (int i = 0; i < Samples; i++)
		{
			int value = random.NextIntInclusive(minValue, maxValue);
			value.Should().BeGreaterThanOrEqualTo(minValue).And.BeLessThanOrEqualTo(maxValue);
		}
	}

	[Fact]
	[Trait("Random", "Deterministic")]
	public void An_inclusive_range_reaches_both_ends()
	{
		var random = new DeterministicRandom(17UL);
		var seen = new HashSet<int>();

		for (int i = 0; i < Samples; i++)
		{
			seen.Add(random.NextIntInclusive(0, 2));
		}

		seen.Should().BeEquivalentTo([0, 1, 2]);
	}

	[Fact]
	[Trait("Random", "Deterministic")]
	public void Unsigned_and_bounded_draws_stay_in_range()
	{
		var random = new DeterministicRandom(19UL);

		for (int i = 0; i < Samples; i++)
		{
			random.NextInt().Should().BeGreaterThanOrEqualTo(0);
			random.NextInt(7).Should().BeInRange(0, 6);
			random.NextInt64().Should().BeGreaterThanOrEqualTo(0);
			random.NextInt64(1000).Should().BeInRange(0, 999);
			random.NextInt64(-5, 5).Should().BeInRange(-5, 4);
			random.NextInt64Inclusive(-5, 5).Should().BeInRange(-5, 5);
		}
	}

	[Fact]
	[Trait("Random", "Deterministic")]
	public void The_full_long_range_is_supported()
	{
		var random = new DeterministicRandom(23UL);

		random.Invoking(x => x.NextInt64Inclusive(long.MinValue, long.MaxValue)).Should().NotThrow();
		random.Invoking(x => x.NextInt64(long.MinValue, long.MaxValue)).Should().NotThrow();
	}

	[Fact]
	[Trait("Random", "Deterministic")]
	public void Invalid_ranges_return_the_minimum_like_the_system_random()
	{
		var random = new DeterministicRandom(29UL);

		random.NextInt(0).Should().Be(0);
		random.NextInt(-4).Should().Be(0);
		random.NextInt(5, 5).Should().Be(5);
		random.NextInt(7, 3).Should().Be(7);
		random.NextIntInclusive(7, 3).Should().Be(7);
		random.NextIntInclusive(4, 4).Should().Be(4);
		random.NextInt64(0).Should().Be(0);
		random.NextInt64(9, 9).Should().Be(9);
		random.NextInt64Inclusive(9, 1).Should().Be(9);
	}

	[Fact]
	[Trait("Random", "Deterministic")]
	public void Floating_point_draws_stay_in_the_unit_range()
	{
		var random = new DeterministicRandom(31UL);

		for (int i = 0; i < Samples; i++)
		{
			random.NextSingle().Should().BeGreaterThanOrEqualTo(0f).And.BeLessThan(1f);
			random.NextDouble().Should().BeGreaterThanOrEqualTo(0d).And.BeLessThan(1d);
			random.NextSingleInclusive().Should().BeInRange(0f, 1f);
			random.NextDoubleInclusive().Should().BeInRange(0d, 1d);
		}
	}

	[Fact]
	[Trait("Random", "Deterministic")]
	public void Bytes_fill_the_whole_buffer_and_repeat_for_the_same_seed()
	{
		byte[] first = new byte[13];
		byte[] second = new byte[13];

		new DeterministicRandom(37UL).NextBytes(first);
		new DeterministicRandom(37UL).NextBytes(second);

		second.Should().Equal(first);
		first.Should().Contain(x => x != 0, "thirteen bytes of zeros from a working generator would be a broken fill");
	}

	private static long[] Draw(DeterministicRandom random, int count)
	{
		long[] values = new long[count];

		for (int i = 0; i < count; i++)
		{
			values[i] = random.NextInt64();
		}

		return values;
	}
}
