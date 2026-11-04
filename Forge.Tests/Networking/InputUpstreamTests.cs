// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Networking;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Networking;

/// <summary>
/// Input travels from a client to the server as redundant windows of per-tick commands: frames read back bit for bit,
/// the window carries the last few ticks and drops acknowledged ones, and the server's playout buffer takes each tick
/// in order, keeps the first copy it sees, fills what never arrived, and tells lost from late.
/// </summary>
/// <param name="fixture">The fixture providing the <see cref="TagsManager"/>.</param>
public class InputUpstreamTests(TagsAndCuesFixture fixture) : IClassFixture<TagsAndCuesFixture>
{
	private static readonly InputActionSet _actions = new(
		[
			"jump",
			"dash",
			"fire",
			"menu"
		],
		[
			new InputVector("left", "right", "forward", "back"),
			new InputVector("look_left", "look_right", "look_up", "look_down")
		]);

	private readonly TagsManager _tagsManager = fixture.TagsManager;

	public static TheoryData<InputFrame> Frames => new(
		default,
		new InputFrame { Held = 0b0101 },
		new InputFrame { Held = 0b0001, Pressed = 0b0001 },
		new InputFrame { Held = 0, Pressed = 0b0010, Released = 0b0010 },
		new InputFrame { Held = ulong.MaxValue, Pressed = 1UL << 63, Released = 1UL << 40 },
		default(InputFrame).WithVector(0, new Vector2(1f, -1f)),
		default(InputFrame).WithVector(0, new Vector2(0.3f, 0.7f)).WithVector(1, new Vector2(-0.01f, 0.99f)),
		default(InputFrame).WithAim(new Vector3(1.2345f, -6.789f, 100.001f), new Vector3(0.577f, -0.577f, 0.577f)),
		new InputFrame { Held = 0b1000 }
			.WithVector(1, new Vector2(-1f, 0f))
			.WithAim(new Vector3(-1234.567f, 0f, 0.0004f), Vector3.UnitZ),
		new InputFrame { Held = 1 }
			.WithAim(new Vector3(float.PositiveInfinity, float.NaN, -1e9f), new Vector3(float.NegativeInfinity, 0f, 1f)));

	[Theory]
	[Trait("Networking", "Input")]
	[MemberData(nameof(Frames), DisableDiscoveryEnumeration = true)]
	public void A_frame_reads_back_bit_for_bit(InputFrame frame)
	{
		var writer = new BitWriter();
		InputCodec.WriteFrame(writer, frame, default, _actions);

		InputCodec.ReadFrame(new BitReader(writer.ToArray()), default, _actions).Should().Be(frame);
	}

	[Fact]
	[Trait("Networking", "Input")]
	public void A_frame_equal_to_the_previous_costs_one_bit()
	{
		InputFrame frame = new InputFrame { Held = 0b11 }.WithAim(new Vector3(1, 2, 3), Vector3.UnitX);
		var writer = new BitWriter();
		InputCodec.WriteFrame(writer, frame, frame, _actions);

		writer.BitLength.Should().Be(1);
		InputCodec.ReadFrame(new BitReader(writer.ToArray()), frame, _actions).Should().Be(frame);
	}

	[Fact]
	[Trait("Networking", "Input")]
	public void A_frame_differing_from_the_previous_reads_back_against_it()
	{
		InputFrame previous = new InputFrame { Held = 0b11 }
			.WithVector(0, new Vector2(0.5f, 0.5f))
			.WithAim(new Vector3(10, 0, 10), Vector3.UnitX);
		InputFrame sameAim = previous with { Held = 0b01, Released = 0b10 };
		InputFrame movedAim = previous.WithAim(new Vector3(10.004f, 0, 9.998f), new Vector3(0.9f, 0.1f, 0));
		InputFrame noAim = new InputFrame { Held = 0b11 }.WithVector(0, new Vector2(0.5f, 0.5f));

		var writer = new BitWriter();
		InputCodec.WriteFrame(writer, sameAim, previous, _actions);
		int sameAimBits = writer.BitLength;
		InputCodec.WriteFrame(writer, movedAim, sameAim, _actions);
		int movedAimBits = writer.BitLength - sameAimBits;
		InputCodec.WriteFrame(writer, noAim, movedAim, _actions);

		var reader = new BitReader(writer.ToArray());
		InputFrame first = InputCodec.ReadFrame(reader, previous, _actions);
		InputFrame second = InputCodec.ReadFrame(reader, first, _actions);
		InputFrame third = InputCodec.ReadFrame(reader, second, _actions);

		first.Should().Be(sameAim);
		second.Should().Be(movedAim);
		third.Should().Be(noAim);
		sameAimBits.Should().BeLessThan(40, "an unchanged aim and vectors cost a few bits");
		movedAimBits.Should().BeLessThan(80, "a small aim move costs small deltas");
	}

	[Fact]
	[Trait("Networking", "Input")]
	public void Events_read_back_and_too_many_are_malformed()
	{
		var writer = new BitWriter();
		InputCodec.WriteEvents(writer, []);
		InputCodec.WriteEvents(writer, [3, 65535, 0]);

		var reader = new BitReader(writer.ToArray());
		InputCodec.ReadEvents(reader).Should().BeEmpty();
		InputCodec.ReadEvents(reader).Should().Equal(3, 65535, 0);

		FluentActions.Invoking(() => InputCodec.WriteEvents(new BitWriter(), new ushort[17]))
			.Should().Throw<ArgumentException>();

		var tooMany = new BitWriter();
		tooMany.WriteBits(17, 5);
		new BitReader(tooMany.ToArray()).Invoking(InputCodec.ReadEvents).Should().Throw<MalformedPacketException>();
	}

	[Fact]
	[Trait("Networking", "Input")]
	public void An_aim_moving_further_than_any_aim_can_is_malformed()
	{
		InputFrame low = default(InputFrame).WithAim(new Vector3(-1e9f), -Vector3.One);
		InputFrame high = default(InputFrame).WithAim(new Vector3(1e9f), Vector3.One);
		var writer = new BitWriter();
		InputCodec.WriteFrame(writer, high, low, _actions);

		InputCodec.ReadFrame(new BitReader(writer.ToArray()), low, _actions)
			.Should().Be(high, "the furthest an aim can move reads back");

		var forged = new BitWriter();
		forged.WriteBool(false);
		forged.WriteVarUInt(0);
		forged.WriteBool(false);

		for (int i = 0; i < _actions.Vectors.Count; i++)
		{
			forged.WriteBool(false);
		}

		forged.WriteBool(true);
		forged.WriteBool(false);
		forged.WriteVarInt(long.MaxValue);

		new BitReader(forged.ToArray()).Invoking(x => InputCodec.ReadFrame(x, low, _actions))
			.Should().Throw<MalformedPacketException>();
	}

	[Fact]
	[Trait("Networking", "Input")]
	public void The_window_keeps_the_newest_ticks_and_drops_acknowledged_ones()
	{
		var window = new InputWindow(_actions, capacity: 3);

		for (long tick = 1; tick <= 5; tick++)
		{
			window.Add(new InputCommand(tick, new InputFrame { Held = (ulong)tick }));
		}

		window.Commands.Select(x => x.Tick).Should().Equal(3, 4, 5);
		window.NewestTick.Should().Be(5);

		window.Add(new InputCommand(5, new InputFrame { Held = 55 }));
		window.Commands[^1].Frame.Held.Should().Be(55, "the same tick is replaced");
		window.Invoking(x => x.Add(new InputCommand(4, default))).Should().Throw<ArgumentException>();

		window.Acknowledge(4);
		window.Commands.Select(x => x.Tick).Should().Equal(5);

		window.Acknowledge(10);
		window.Commands.Should().BeEmpty();
		window.NewestTick.Should().BeNull();
	}

	[Fact]
	[Trait("Networking", "Input")]
	public void A_window_reads_back_with_its_ticks_frames_and_events()
	{
		var window = new InputWindow(_actions);
		window.Add(new InputCommand(100, new InputFrame { Held = 1 }));
		window.Add(new InputCommand(101, new InputFrame { Held = 1 }, [7]));
		window.Add(new InputCommand(103, new InputFrame { Held = 3, Pressed = 2 }.WithAim(Vector3.One, Vector3.UnitY)));
		window.Add(new InputCommand(104, new InputFrame { Held = 3 }.WithAim(Vector3.One, Vector3.UnitY), [1, 2]));

		var writer = new BitWriter();
		window.Write(writer);

		var read = new List<InputCommand>();
		InputWindow.Read(new BitReader(writer.ToArray()), _actions, read);

		read.Select(x => x.Tick).Should().Equal(100, 101, 103, 104);
		read.Should().Equal(window.Commands, (actual, expected) => actual.Frame == expected.Frame);
		read[1].Events.Should().Equal(7);
		read[2].Events.Should().BeEmpty();
		read[3].Events.Should().Equal(1, 2);
		writer.ByteLength.Should().BeLessThan(40, "four ticks, two of them repeats, fit in a few dozen bytes");
	}

	[Fact]
	[Trait("Networking", "Input")]
	public void An_empty_window_reads_back_empty()
	{
		var writer = new BitWriter();
		new InputWindow(_actions).Write(writer);

		var read = new List<InputCommand> { new(1, default) };
		InputWindow.Read(new BitReader(writer.ToArray()), _actions, read);

		read.Should().BeEmpty();
	}

	[Fact]
	[Trait("Networking", "Input")]
	public void The_playout_buffer_keeps_the_first_copy_of_a_tick_and_takes_ticks_in_order()
	{
		var buffer = new InputPlayoutBuffer(_actions);

		buffer.Add(new InputCommand(10, new InputFrame { Held = 10 })).Should().BeTrue();
		buffer.Add(new InputCommand(11, new InputFrame { Held = 11 })).Should().BeTrue();
		buffer.Add(new InputCommand(10, new InputFrame { Held = 99 })).Should().BeFalse("tick 10 is already here");
		buffer.Depth.Should().Be(2);
		buffer.NewestTick.Should().Be(11);

		buffer.Take(10, out InputCommand first).Should().Be(InputDelivery.Received);
		first.Frame.Held.Should().Be(10);
		buffer.Add(new InputCommand(10, default)).Should().BeFalse("tick 10 was taken");
		buffer.Add(new InputCommand(9, default)).Should().BeFalse("tick 9 is before the one taken");
		buffer.Depth.Should().Be(1);
		buffer.LastTakenTick.Should().Be(10);

		buffer.Take(11, out InputCommand second).Should().Be(InputDelivery.Received);
		second.Frame.Held.Should().Be(11);
		buffer.ReceivedTicks.Should().Be(2);
	}

	[Fact]
	[Trait("Networking", "Input")]
	public void A_missing_tick_is_filled_with_the_last_frame_held_and_told_lost_or_late()
	{
		var buffer = new InputPlayoutBuffer(_actions);
		buffer.Add(new InputCommand(1, new InputFrame { Held = 0b11, Pressed = 0b11 }));
		buffer.Add(new InputCommand(3, new InputFrame { Held = 0b01, Released = 0b10 }));

		buffer.Take(1, out _).Should().Be(InputDelivery.Received);

		buffer.Take(2, out InputCommand lost).Should().Be(InputDelivery.Lost, "tick 3 is here, so 2 was lost");
		lost.Frame.Should().Be(new InputFrame { Held = 0b11 }, "the last frame holds on without its presses");
		lost.Events.Should().BeEmpty();

		buffer.Take(3, out _).Should().Be(InputDelivery.Received);

		buffer.Take(4, out InputCommand late).Should().Be(InputDelivery.Late, "nothing after 3 has arrived");
		late.Frame.Should().Be(new InputFrame { Held = 0b01 });
		buffer.Take(5, out InputCommand stillLate).Should().Be(InputDelivery.Late);
		stillLate.Frame.Should().Be(new InputFrame { Held = 0b01 });

		buffer.LostTicks.Should().Be(1);
		buffer.LateTicks.Should().Be(2);
	}

	[Fact]
	[Trait("Networking", "Input")]
	public void Ticks_skipped_over_are_dropped_and_ones_too_far_ahead_refused()
	{
		var buffer = new InputPlayoutBuffer(_actions);
		buffer.Add(new InputCommand(1, default));
		buffer.Add(new InputCommand(2, default));
		buffer.Add(new InputCommand(5, default));

		buffer.Take(4, out _).Should().Be(InputDelivery.Lost);
		buffer.Depth.Should().Be(1, "ticks 1 and 2 were skipped over");
		buffer.Add(new InputCommand(4 + InputPlayoutBuffer.MaxTicksAhead + 1, default)).Should().BeFalse();
		buffer.Add(new InputCommand(4 + InputPlayoutBuffer.MaxTicksAhead, default)).Should().BeTrue();
	}

	[Fact]
	[Trait("Networking", "Input")]
	public void A_command_s_input_events_are_raised_on_the_entity_as_forge_events()
	{
		var entity = new TestEntity(_tagsManager, fixture.CuesManager);
		var tag = Tag.RequestTag(_tagsManager, "simple.tag");
		var other = Tag.RequestTag(_tagsManager, "other.tag");
		var raised = new List<EventData>();
		entity.Events.Subscribe(tag, raised.Add);
		entity.Events.Subscribe(other, raised.Add);

		ushort tagIndex = InputCommand.EventIndex(_tagsManager, tag);
		ushort otherIndex = InputCommand.EventIndex(_tagsManager, other);
		var command = new InputCommand(1, default, [tagIndex, otherIndex, ushort.MaxValue, tagIndex]);

		command.RaiseEvents(entity, new TagContainer(_tagsManager, [Tag.RequestTag(_tagsManager, "simple")]));

		raised.Should().HaveCount(2, "a tag that isn't an input event, and an index that names no tag, are skipped");
		raised.Should().OnlyContain(x => x.EventTags.HasTag(tag));
		raised[0].Source.Should().BeSameAs(entity);
		raised[0].Target.Should().BeSameAs(entity);
	}

	[Fact]
	[Trait("Networking", "Input")]
	public void Under_a_poor_link_a_redundant_window_loses_no_tick_when_the_client_leads_enough()
	{
		const int TickRate = 60;
		const long Lead = 24;
		var network = new LoopbackNetwork();
		INetworkTransport server = network.Server;
		var client = new ConditionedTransport(network.Connect(), NetworkConditions.Poor, new DeterministicRandom(5));
		var window = new InputWindow(_actions);
		var buffer = new InputPlayoutBuffer(_actions);
		var writer = new BitWriter();
		var taken = new List<(long Tick, InputDelivery Delivery, InputFrame Frame)>();
		var sentFrames = new Dictionary<long, InputFrame>();

		// The client runs Lead ticks ahead of the server: 400 ms, which covers 200 ms one way plus 50 ms of jitter
		// with room to spare. Each step it adds its tick's command and sends its window; the server takes its own tick.
		for (long serverTick = 0; serverTick < 600; serverTick++)
		{
			double time = serverTick / (double)TickRate;
			long clientTick = serverTick + Lead;
			InputFrame frame =
				new InputFrame { Held = (ulong)(clientTick % 7), Pressed = clientTick % 13 == 0 ? 1UL : 0 }
					.WithAim(new Vector3(clientTick * 0.01f, 0, 0), Vector3.UnitZ);
			sentFrames[clientTick] = frame;
			window.Add(new InputCommand(clientTick, frame));

			writer.Clear();
			window.Write(writer);
			client.Update(time);
			client.Send(PeerId.Server, 1, reliable: false, writer.WrittenBytes);

			while (server.TryReceive(out TransportEvent received))
			{
				if (received.Kind == TransportEventKind.Packet)
				{
					buffer.Receive(new BitReader(received.Payload));
				}
			}

			if (buffer.NewestTick is long newest)
			{
				window.Acknowledge(newest);
			}

			if (serverTick >= Lead)
			{
				InputDelivery delivery = buffer.Take(serverTick, out InputCommand command);
				taken.Add((serverTick, delivery, command.Frame));
			}
		}

		client.LostPackets.Should().BeGreaterThan(10, "a poor link loses packets");
		taken.Should().OnlyContain(x => x.Delivery == InputDelivery.Received, "every tick survived the redundancy");
		taken.Should().OnlyContain(x => x.Frame == sentFrames[x.Tick], "every tick ran with the frame the client sent");
		buffer.Depth.Should().BeInRange(1, (int)Lead, "the server sits a lead behind the client");
	}

	[Fact]
	[Trait("Networking", "Input")]
	public void A_client_with_too_little_lead_is_late_and_a_garbled_packet_is_refused()
	{
		var network = new LoopbackNetwork();
		INetworkTransport server = network.Server;
		var client = new ConditionedTransport(
			network.Connect(),
			new NetworkConditions(0.5),
			new DeterministicRandom(1));
		var window = new InputWindow(_actions);
		var buffer = new InputPlayoutBuffer(_actions);
		var writer = new BitWriter();
		int late = 0;

		for (long tick = 0; tick < 20; tick++)
		{
			window.Add(new InputCommand(tick + 2, default));
			writer.Clear();
			window.Write(writer);
			client.Update(tick / 60.0);
			client.Send(PeerId.Server, 1, reliable: false, writer.WrittenBytes);

			while (server.TryReceive(out TransportEvent received))
			{
				if (received.Kind == TransportEventKind.Packet)
				{
					buffer.Receive(new BitReader(received.Payload));
				}
			}

			late += buffer.Take(tick, out _) == InputDelivery.Late ? 1 : 0;
		}

		late.Should().Be(20, "nothing arrives within 20 ticks on a 500 ms link");

		byte[] garbage = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];
		buffer.Invoking(x => x.Receive(new BitReader(garbage))).Should().Throw<MalformedPacketException>();
	}
}
