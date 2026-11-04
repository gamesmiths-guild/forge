// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Networking;

namespace Gamesmiths.Forge.Tests.Networking;

/// <summary>
/// The loopback network delivers like a transport should, and the conditioner delays, loses and reorders the way a
/// real link does, repeatably.
/// </summary>
public class TransportTests
{
	private const byte Control = 0;

	private const byte Input = 1;

	[Fact]
	[Trait("Networking", "Transport")]
	public void A_client_connecting_is_seen_by_both_ends()
	{
		var network = new LoopbackNetwork();
		INetworkTransport server = network.Server;

		server.IsServer.Should().BeTrue();
		server.LocalPeer.Should().Be(PeerId.Server);
		server.Peers.Should().BeEmpty();

		INetworkTransport client = network.Connect();

		client.IsServer.Should().BeFalse();
		client.LocalPeer.IsValid.Should().BeTrue();
		client.LocalPeer.Should().NotBe(PeerId.Server);
		client.Peers.Should().Equal(PeerId.Server);
		server.Peers.Should().Equal(client.LocalPeer);

		Next(server).Should().Be(TransportEvent.Connected(client.LocalPeer));
		Next(client).Should().Be(TransportEvent.Connected(PeerId.Server));
		server.TryReceive(out _).Should().BeFalse();
	}

	[Fact]
	[Trait("Networking", "Transport")]
	public void Packets_arrive_in_order_with_their_channel_and_sender()
	{
		var network = new LoopbackNetwork();
		INetworkTransport server = network.Server;
		INetworkTransport client = network.Connect();
		Drain(server);
		Drain(client);

		client.Send(PeerId.Server, Control, reliable: true, [1, 2, 3]);
		client.Send(PeerId.Server, Input, reliable: false, [4]);
		server.Send(client.LocalPeer, Control, reliable: true, [5, 6]);

		TransportEvent first = Next(server);
		first.Kind.Should().Be(TransportEventKind.Packet);
		first.Peer.Should().Be(client.LocalPeer);
		first.Channel.Should().Be(Control);
		first.Payload.ToArray().Should().Equal(1, 2, 3);

		TransportEvent second = Next(server);
		second.Channel.Should().Be(Input);
		second.Payload.ToArray().Should().Equal(4);
		server.TryReceive(out _).Should().BeFalse();

		TransportEvent toClient = Next(client);
		toClient.Peer.Should().Be(PeerId.Server);
		toClient.Payload.ToArray().Should().Equal(5, 6);
	}

	[Fact]
	[Trait("Networking", "Transport")]
	public void A_payload_is_copied_when_sent()
	{
		var network = new LoopbackNetwork();
		INetworkTransport client = network.Connect();
		Drain(network.Server);
		byte[] payload = [7, 8];

		client.Send(PeerId.Server, Input, reliable: false, payload);
		payload[0] = 0;

		Next(network.Server).Payload.ToArray().Should().Equal(7, 8);
	}

	[Fact]
	[Trait("Networking", "Transport")]
	public void Clients_only_reach_the_server()
	{
		var network = new LoopbackNetwork();
		INetworkTransport first = network.Connect();
		INetworkTransport second = network.Connect();
		Drain(first);
		Drain(second);

		first.Send(second.LocalPeer, Input, reliable: false, [1]);

		second.TryReceive(out _).Should().BeFalse();
	}

	[Fact]
	[Trait("Networking", "Transport")]
	public void Disconnecting_from_either_end_tells_both()
	{
		var network = new LoopbackNetwork();
		INetworkTransport server = network.Server;
		INetworkTransport first = network.Connect();
		INetworkTransport second = network.Connect();
		Drain(server);
		Drain(first);
		Drain(second);

		first.Disconnect(PeerId.Server);
		server.Disconnect(second.LocalPeer);

		Next(server).Should().Be(TransportEvent.Disconnected(first.LocalPeer));
		Next(server).Should().Be(TransportEvent.Disconnected(second.LocalPeer));
		Next(first).Should().Be(TransportEvent.Disconnected(PeerId.Server));
		Next(second).Should().Be(TransportEvent.Disconnected(PeerId.Server));
		server.Peers.Should().BeEmpty();
		first.Peers.Should().BeEmpty();

		first.Send(PeerId.Server, Input, reliable: false, [1]);
		server.TryReceive(out _).Should().BeFalse("a disconnected client's packets go nowhere");
	}

	[Fact]
	[Trait("Networking", "Transport")]
	public void A_conditioned_packet_arrives_once_the_latency_has_passed()
	{
		var network = new LoopbackNetwork();
		INetworkTransport server = network.Server;
		var client = new ConditionedTransport(
			network.Connect(),
			new NetworkConditions(0.1),
			new DeterministicRandom(1));
		Drain(server);

		client.Send(PeerId.Server, Input, reliable: false, [1]);

		client.HeldPackets.Should().Be(1);
		server.TryReceive(out _).Should().BeFalse();

		client.Update(0.05);
		server.TryReceive(out _).Should().BeFalse("half the latency has passed");

		client.Update(0.1);
		Next(server).Payload.ToArray().Should().Equal(1);
		client.HeldPackets.Should().Be(0);
	}

	[Fact]
	[Trait("Networking", "Transport")]
	public void A_perfect_link_delivers_at_once()
	{
		var network = new LoopbackNetwork();
		var client = new ConditionedTransport(
			network.Connect(),
			NetworkConditions.Perfect,
			new DeterministicRandom(1));
		Drain(network.Server);

		client.Send(PeerId.Server, Input, reliable: false, [1]);

		client.HeldPackets.Should().Be(0);
		Next(network.Server).Payload.ToArray().Should().Equal(1);
	}

	[Fact]
	[Trait("Networking", "Transport")]
	public void Loss_takes_unreliable_packets_and_never_reliable_ones()
	{
		var network = new LoopbackNetwork();
		INetworkTransport server = network.Server;
		var client = new ConditionedTransport(
			network.Connect(),
			new NetworkConditions(0, LossRate: 0.5f),
			new DeterministicRandom(7));
		Drain(server);

		for (int i = 0; i < 200; i++)
		{
			client.Send(PeerId.Server, Input, reliable: false, [1]);
			client.Send(PeerId.Server, Control, reliable: true, [2]);
		}

		client.Update(0);
		int unreliable = 0;
		int reliable = 0;

		while (server.TryReceive(out TransportEvent received))
		{
			if (received.Channel == Input)
			{
				unreliable++;
			}
			else
			{
				reliable++;
			}
		}

		reliable.Should().Be(200);
		unreliable.Should().BeInRange(60, 140, "about half of 200 are lost");
		client.LostPackets.Should().Be(200 - unreliable);
	}

	[Fact]
	[Trait("Networking", "Transport")]
	public void Jitter_reorders_unreliable_packets_but_not_reliable_ones()
	{
		var network = new LoopbackNetwork();
		INetworkTransport server = network.Server;
		var client = new ConditionedTransport(
			network.Connect(),
			new NetworkConditions(0.1, JitterSeconds: 0.09),
			new DeterministicRandom(3));
		Drain(server);

		for (byte i = 0; i < 50; i++)
		{
			client.Send(PeerId.Server, Input, reliable: false, [i]);
			client.Send(PeerId.Server, Control, reliable: true, [i]);
		}

		client.Update(1);
		var unreliable = new List<byte>();
		var reliable = new List<byte>();

		while (server.TryReceive(out TransportEvent received))
		{
			(received.Channel == Input ? unreliable : reliable).Add(received.Payload.Span[0]);
		}

		unreliable.Should().HaveCount(50).And.NotBeInAscendingOrder("jitter reorders unreliable packets");
		reliable.Should().HaveCount(50).And.BeInAscendingOrder("a reliable channel keeps its order");
	}

	[Fact]
	[Trait("Networking", "Transport")]
	public void Held_packets_go_out_earliest_first_as_time_passes()
	{
		var network = new LoopbackNetwork();
		INetworkTransport server = network.Server;
		var client = new ConditionedTransport(
			network.Connect(),
			new NetworkConditions(0.1),
			new DeterministicRandom(1));
		Drain(server);

		client.Send(PeerId.Server, Input, reliable: false, [1]);
		client.Update(0.05);
		client.Send(PeerId.Server, Input, reliable: false, [2]);
		client.Update(0.1);

		Next(server).Payload.ToArray().Should().Equal(1);
		server.TryReceive(out _).Should().BeFalse("the second packet is due around 0.15");

		client.Update(0.125);
		server.TryReceive(out _).Should().BeFalse();

		client.Update(0.2);
		Next(server).Payload.ToArray().Should().Equal(2);
	}

	[Fact]
	[Trait("Networking", "Transport")]
	public void The_same_seed_gives_the_same_delivery()
	{
		static List<byte> Run()
		{
			var network = new LoopbackNetwork();
			var client = new ConditionedTransport(
				network.Connect(),
				NetworkConditions.Poor,
				new DeterministicRandom(42));
			Drain(network.Server);

			for (byte i = 0; i < 100; i++)
			{
				client.Send(PeerId.Server, Input, reliable: false, [i]);
				client.Update(i * 0.016);
			}

			client.Update(10);
			var received = new List<byte>();

			while (network.Server.TryReceive(out TransportEvent packet))
			{
				received.Add(packet.Payload.Span[0]);
			}

			return received;
		}

		List<byte> first = Run();

		first.Should().NotBeEmpty().And.HaveCountLessThan(100, "a poor link loses some");
		Run().Should().Equal(first);
	}

	[Fact]
	[Trait("Networking", "Transport")]
	public void Disconnecting_drops_what_was_held_for_the_peer()
	{
		var network = new LoopbackNetwork();
		INetworkTransport server = network.Server;
		var client = new ConditionedTransport(
			network.Connect(),
			new NetworkConditions(0.1),
			new DeterministicRandom(1));
		Drain(server);

		client.Send(PeerId.Server, Input, reliable: false, [1]);
		client.Disconnect(PeerId.Server);
		client.Update(1);

		client.HeldPackets.Should().Be(0);
		Next(server).Kind.Should().Be(TransportEventKind.PeerDisconnected);
		server.TryReceive(out _).Should().BeFalse();
	}

	private static TransportEvent Next(INetworkTransport transport)
	{
		transport.TryReceive(out TransportEvent transportEvent).Should().BeTrue("something should have arrived");
		return transportEvent;
	}

	private static void Drain(INetworkTransport transport)
	{
		while (transport.TryReceive(out _))
		{
			// Nothing, just empty the queue
		}
	}
}
