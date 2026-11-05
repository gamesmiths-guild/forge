// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Networking;
using Gamesmiths.Forge.Networking.Messages;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Tests.Networking;

/// <summary>
/// A server and its clients over the loopback: the handshake accepts a matching client and refuses a mismatched one,
/// pings give the client the server's tick, input reaches the server's playout buffers under a poor link, and leaving
/// is seen on both ends.
/// </summary>
public class SessionTests
{
	private const int TickRate = 60;

	private static readonly InputActionSet _actions = new(["jump", "fire"], [new InputVector("l", "r", "u", "d")]);

	private static readonly TagsManager _tags = new(["input.menu", "event.hit"]);

	[Fact]
	[Trait("Networking", "Session")]
	public void A_matching_client_is_welcomed_with_the_session_s_tick_and_seed()
	{
		var network = new LoopbackNetwork();
		NetworkSession server = Server(network, seed: 0xC0FFEE, startTick: 500);
		var joined = new List<NetworkPeer>();
		server.PeerJoined += joined.Add;

		NetworkSession client = Client(network.Connect());
		client.State.Should().Be(SessionState.Connecting);

		client.Update(0);
		client.State.Should().Be(SessionState.Handshaking, "the hello went out as the transport connected");

		server.Update(0);
		joined.Should().ContainSingle().Which.Id.Should().Be(client.Transport.LocalPeer);
		server.Peers.Should().ContainSingle().Which.IsReady.Should().BeTrue();
		server.FindPeer(client.Transport.LocalPeer).Should().NotBeNull();

		client.Update(0.01);
		client.State.Should().Be(SessionState.Ready);
		client.SessionSeed.Should().Be(0xC0FFEE);
		client.Clock.Tick.Should().Be(500, "the client's clock starts at the server's tick");
	}

	[Theory]
	[Trait("Networking", "Session")]
	[InlineData(RefusalReason.AssetRegistry)]
	[InlineData(RefusalReason.Tags)]
	[InlineData(RefusalReason.InputActions)]
	public void A_mismatched_client_is_refused_and_leaves(RefusalReason expected)
	{
		var network = new LoopbackNetwork();
		NetworkSession server = Server(network);
		var refused = new List<(PeerId, RefusalReason)>();
		server.PeerRefused += (peer, reason) => refused.Add((peer, reason));

		NetworkAssetRegistry registry = expected == RefusalReason.AssetRegistry ? Registry("other") : Registry();
		InputActionSet actions = expected == RefusalReason.InputActions
			? new InputActionSet(["fire", "jump"])
			: _actions;
		TagsManager tags = expected == RefusalReason.Tags ? new TagsManager(["input.menu", "event.hurt"]) : _tags;
		INetworkTransport transport = network.Connect();
		var client = new NetworkSession(transport, new SimulationClock(TickRate), registry, tags, actions);

		client.Update(0);
		server.Update(0);

		refused.Should().Equal((transport.LocalPeer, expected));
		server.Peers.Should().BeEmpty();
		network.Server.Peers.Should().ContainSingle("the server lets the refusal reach the client first");

		client.Update(0.01);
		server.Update(0.02);

		client.State.Should().Be(SessionState.Refused);
		client.Refusal.Should().Be(expected);
		network.Server.Peers.Should().BeEmpty("the client left once it knew why");
	}

	[Fact]
	[Trait("Networking", "Session")]
	public void A_refused_client_that_stays_is_disconnected_after_the_linger()
	{
		var network = new LoopbackNetwork();
		NetworkSession server = Server(network);
		INetworkTransport raw = network.Connect();
		var writer = new BitWriter();
		new HelloMessage(Protocol.Version + 1, 0, 0, 0).Write(writer);
		raw.Send(PeerId.Server, Protocol.ControlChannel, reliable: true, writer.WrittenBytes);

		server.Update(0);
		server.Update(NetworkSession.RefusalLinger - 0.01);
		network.Server.Peers.Should().ContainSingle();

		server.Update(NetworkSession.RefusalLinger);
		network.Server.Peers.Should().BeEmpty();
	}

	[Fact]
	[Trait("Networking", "Session")]
	public void A_client_that_never_says_hello_is_disconnected_after_the_timeout()
	{
		var network = new LoopbackNetwork();
		NetworkSession server = Server(network);
		network.Connect();

		server.Update(0);
		server.Update(NetworkSession.HandshakeTimeout - 0.01);
		network.Server.Peers.Should().ContainSingle();

		server.Update(NetworkSession.HandshakeTimeout);
		network.Server.Peers.Should().BeEmpty();
	}

	[Fact]
	[Trait("Networking", "Session")]
	public void A_hello_that_waited_out_a_stall_on_the_server_is_still_read()
	{
		var network = new LoopbackNetwork();
		NetworkSession server = Server(network);
		NetworkSession client = Client(network.Connect());
		server.Update(0);
		client.Update(0);

		server.Update(NetworkSession.HandshakeTimeout + 1);

		server.Peers.Should().ContainSingle("the hello was waiting when the server came back");
	}

	[Fact]
	[Trait("Networking", "Session")]
	public void A_full_session_refuses_the_next_client()
	{
		var network = new LoopbackNetwork();
		NetworkSession server = Server(network, maxClients: 1);
		NetworkSession first = Client(network.Connect());
		NetworkSession second = Client(network.Connect());

		first.Update(0);
		second.Update(0);
		server.Update(0);
		first.Update(0.01);
		second.Update(0.01);

		first.State.Should().Be(SessionState.Ready);
		second.State.Should().Be(SessionState.Refused);
		second.Refusal.Should().Be(RefusalReason.Full);
		server.Peers.Should().ContainSingle();
		server.Update(0.02);
		network.Server.Peers.Should().ContainSingle("the refused client left");
	}

	[Fact]
	[Trait("Networking", "Session")]
	public void A_client_whose_clock_runs_at_another_rate_leaves()
	{
		var network = new LoopbackNetwork();
		NetworkSession server = Server(network);
		var client = new NetworkSession(network.Connect(), new SimulationClock(30), Registry(), _tags, _actions);

		client.Update(0);
		server.Update(0);
		client.Update(0.01);
		server.Update(0.02);

		client.State.Should().Be(SessionState.Refused);
		server.Peers.Should().BeEmpty();
	}

	[Fact]
	[Trait("Networking", "Session")]
	public void Pings_give_the_client_the_server_s_tick_and_acknowledge_its_input()
	{
		var network = new LoopbackNetwork();
		NetworkSession server = Server(network, startTick: 1000);
		var link = new ConditionedTransport(network.Connect(), new NetworkConditions(0.05), new DeterministicRandom(1));
		NetworkSession client = Client(link);
		double time = 0;

		for (int step = 0; step < 60; step++)
		{
			time = step / (double)TickRate;
			link.Update(time);
			client.Update(time);
			server.Update(time);

			if (client.State == SessionState.Ready)
			{
				client.SendInput(new InputCommand(client.Clock.Tick + 10, new InputFrame { Held = 1 }));
				client.Clock.Advance();
			}

			server.Clock.Advance();
		}

		client.State
			.Should().Be(SessionState.Ready);
		client.ClockSync.HasEstimate
			.Should().BeTrue();
		client.ClockSync.BestRoundTripTime
			.Should().BeApproximately(0.05, 0.02, "only the client's sends are delayed");
		client.ClockSync.EstimateServerTick(time)
			.Should().BeApproximately(server.Clock.Tick, 4);
		client.AckedInputTick
			.Should().BeGreaterThan(1000, "the server acknowledged input");
		client.Input.Commands.Count
			.Should().BeLessThan(InputWindow.DefaultCapacity, "acknowledged ticks left the window");
		server.Peers[0].Pings
			.Should().BeGreaterThan(3);
		server.Peers[0].Input.NewestTick
			.Should().BeGreaterThanOrEqualTo(client.AckedInputTick, "the ack is a pong old");
	}

	[Fact]
	[Trait("Networking", "Session")]
	public void Input_recorded_before_the_welcome_is_dropped_when_the_clock_moves()
	{
		var network = new LoopbackNetwork();
		NetworkSession server = Server(network, startTick: 100);
		NetworkSession client = Client(network.Connect());
		client.Clock.Set(500);
		client.SendInput(new InputCommand(500, default));

		client.Update(0);
		server.Update(0);
		client.Update(0.01);
		client.Clock.Tick.Should().Be(100);

		client.Invoking(x => x.SendInput(new InputCommand(100, default))).Should().NotThrow();
		client.Input.Commands.Should().ContainSingle().Which.Tick.Should().Be(100);
		client.AckedInputTick.Should().Be(-1);
	}

	[Fact]
	[Trait("Networking", "Session")]
	public void A_client_that_just_joined_can_t_send_input_far_ahead_of_the_server()
	{
		var network = new LoopbackNetwork();
		NetworkSession server = Server(network, startTick: 100);
		NetworkSession client = Client(network.Connect());
		client.Update(0);
		server.Update(0);
		client.Update(0.01);

		client.SendInput(new InputCommand(100 + (2 * TickRate) + 1, default));
		server.Update(0.02);

		server.Peers[0].Input.MaxTicksAhead.Should().Be(2 * TickRate, "the lead is two seconds of ticks");
		server.Peers[0].Input.Depth.Should().Be(0, "nothing has been taken yet, but the join tick bounds it");
		server.Peers[0].Input.NewestTick.Should().BeNull();
	}

	[Fact]
	[Trait("Networking", "Session")]
	public void A_faster_session_allows_as_long_an_input_lead()
	{
		var network = new LoopbackNetwork();
		var server = new NetworkSession(network.Server, new SimulationClock(2000), Registry(), _tags, _actions);
		var client = new NetworkSession(network.Connect(), new SimulationClock(2000), Registry(), _tags, _actions);
		client.Update(0);
		server.Update(0);
		client.Update(0.01);

		client.SendInput(new InputCommand(400, default));
		server.Update(0.02);

		server.Peers[0].Input.MaxTicksAhead.Should().Be(4000);
		server.Peers[0].Input.NewestTick.Should().Be(400, "a 200 ms lead at 2000 ticks a second is accepted");
	}

	[Fact]
	[Trait("Networking", "Session")]
	public void Input_reaches_the_server_s_playout_buffer_under_a_poor_link()
	{
		const long Lead = 24;
		var network = new LoopbackNetwork();
		NetworkSession server = Server(network);
		var link = new ConditionedTransport(network.Connect(), NetworkConditions.Poor, new DeterministicRandom(9));
		NetworkSession client = Client(link);
		var deliveries = new List<InputDelivery>();
		var frames = new Dictionary<long, InputFrame>();

		for (long serverTick = 0; serverTick < 400; serverTick++)
		{
			double time = serverTick / (double)TickRate;
			link.Update(time);
			client.Update(time);
			server.Update(time);

			if (client.State == SessionState.Ready)
			{
				long clientTick = serverTick + Lead;
				InputFrame frame = new InputFrame { Held = (ulong)(clientTick % 3) }
					.WithAim(new Vector3(clientTick * 0.02f, 1, 0), Vector3.UnitX);
				frames[clientTick] = frame;
				client.SendInput(new InputCommand(clientTick, frame));
			}

			// The handshake itself crosses the poor link, so the first command is for a tick well after zero.
			if (server.Peers.Count == 1 && frames.TryGetValue(serverTick, out InputFrame value))
			{
				InputDelivery delivery = server.Peers[0].Input.Take(serverTick, out InputCommand command);
				deliveries.Add(delivery);
				command.Frame.Should().Be(value);
			}

			server.Clock.Advance();
		}

		deliveries.Should().NotBeEmpty().And.OnlyContain(x => x == InputDelivery.Received);
		link.LostPackets.Should().BeGreaterThan(5);
		server.Peers[0].MalformedPackets.Should().Be(0);
	}

	[Fact]
	[Trait("Networking", "Session")]
	public void Leaving_is_seen_on_both_ends()
	{
		var network = new LoopbackNetwork();
		NetworkSession server = Server(network);
		var left = new List<NetworkPeer>();
		server.PeerLeft += left.Add;
		NetworkSession first = Client(network.Connect());
		NetworkSession second = Client(network.Connect());
		first.Update(0);
		second.Update(0);
		server.Update(0);
		first.Update(0.01);
		second.Update(0.01);

		first.Disconnect();
		server.Update(0.02);
		first.Update(0.02);

		left.Should().ContainSingle().Which.Id.Should().Be(first.Transport.LocalPeer);
		first.State.Should().Be(SessionState.Disconnected);
		server.Peers.Should().ContainSingle();

		server.Disconnect();
		server.Update(0.03);
		second.Update(0.03);

		second.State.Should().Be(SessionState.Disconnected);
		server.Peers.Should().BeEmpty();
		left.Should().HaveCount(2);
	}

	[Fact]
	[Trait("Networking", "Session")]
	public void A_server_leaving_disconnects_clients_it_hasn_t_welcomed()
	{
		var network = new LoopbackNetwork();
		NetworkSession server = Server(network);
		INetworkTransport refused = network.Connect();
		var writer = new BitWriter();
		new HelloMessage(Protocol.Version + 1, 0, 0, 0).Write(writer);
		refused.Send(PeerId.Server, Protocol.ControlChannel, reliable: true, writer.WrittenBytes);
		server.Update(0);
		network.Connect();

		server.Disconnect();

		network.Server.Peers.Should().BeEmpty("a refused client and one the session hadn't seen yet are gone too");
	}

	[Fact]
	[Trait("Networking", "Session")]
	public void Packets_that_can_t_be_read_are_dropped_and_counted()
	{
		var network = new LoopbackNetwork();
		NetworkSession server = Server(network);
		INetworkTransport raw = network.Connect();
		NetworkSession client = Client(raw);
		client.Update(0);
		server.Update(0);
		client.Update(0.01);

		raw.Send(
			PeerId.Server,
			Protocol.ControlChannel,
			reliable: true,
			[0xFF, 0xFF]);
		raw.Send(
			PeerId.Server,
			Protocol.ControlChannel,
			reliable: true,
			[(byte)ControlMessage.Hello]);
		raw.Send(
			PeerId.Server,
			Protocol.InputChannel,
			reliable: false,
			[0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF]);
		raw.Send(
			PeerId.Server,
			Protocol.StateChannel,
			reliable: false,
			[1, 2, 3]);
		server.Update(0.02);

		server.MalformedPackets
			.Should().Be(4, "two control packets, an input packet and a state acknowledgement made no sense");
		server.Peers[0].MalformedPackets
			.Should().Be(4);
		server.Peers
			.Should().ContainSingle("a bad packet doesn't end the connection");
		client.State
			.Should().Be(SessionState.Ready);
	}

	[Fact]
	[Trait("Networking", "Session")]
	public void A_session_needs_a_sealed_registry()
	{
		var network = new LoopbackNetwork();

		FluentActions.Invoking(() => new NetworkSession(
			network.Server,
			new SimulationClock(TickRate),
			new NetworkAssetRegistry(),
			_tags,
			_actions)).Should().Throw<ArgumentException>();
	}

	private static NetworkAssetRegistry Registry(string extra = "")
	{
		var registry = new NetworkAssetRegistry();
		registry.Register(AssetKind.Effect, "uid://burning", "Burning");
		registry.Register(AssetKind.Ability, "uid://dash", "Dash");

		if (extra.Length > 0)
		{
			registry.Register(AssetKind.Effect, extra, extra);
		}

		registry.Seal();
		return registry;
	}

	private static NetworkSession Server(
		LoopbackNetwork network,
		ulong seed = 1,
		long startTick = 0,
		int maxClients = 0)
	{
		return new NetworkSession(
			network.Server,
			new SimulationClock(TickRate, startTick),
			Registry(),
			_tags,
			_actions,
			seed,
			maxClients);
	}

	private static NetworkSession Client(INetworkTransport transport)
	{
		return new NetworkSession(transport, new SimulationClock(TickRate), Registry(), _tags, _actions);
	}
}
