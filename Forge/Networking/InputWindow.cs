// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Networking;

/// <summary>
/// A client's recent input commands, sent together over an unreliable channel so a lost packet costs nothing: the
/// next packet carries the same ticks again. The server keeps the first copy of each tick it sees and ignores the
/// rest.
/// </summary>
/// <remarks>
/// The window holds the last <see cref="Capacity"/> ticks, and drops the ones the server has acknowledged. Each packet
/// writes the newest tick, how many ticks it carries, then the frames oldest first, each against the one before it,
/// so a run of identical frames costs a bit each.
/// </remarks>
public sealed class InputWindow
{
	/// <summary>
	/// The default number of ticks a packet carries, eight: at 60 Hz, a tick survives 130 ms of lost packets.
	/// </summary>
	public const int DefaultCapacity = 8;

	/// <summary>
	/// The most ticks a packet can carry.
	/// </summary>
	public const int MaxCapacity = 15;

	private readonly List<InputCommand> _commands = [];

	/// <summary>
	/// Gets the action set the frames belong to.
	/// </summary>
	public InputActionSet Actions { get; }

	/// <summary>
	/// Gets how many ticks a packet carries at most.
	/// </summary>
	public int Capacity { get; }

	/// <summary>
	/// Gets the commands the next packet carries, oldest first.
	/// </summary>
	public IReadOnlyList<InputCommand> Commands => _commands;

	/// <summary>
	/// Gets the newest tick in the window, or <see langword="null"/> when it's empty.
	/// </summary>
	public long? NewestTick => _commands.Count == 0 ? null : _commands[^1].Tick;

	/// <summary>
	/// Initializes a new instance of the <see cref="InputWindow"/> class.
	/// </summary>
	/// <param name="actions">The action set the frames belong to.</param>
	/// <param name="capacity">How many ticks a packet carries, from 1 to <see cref="MaxCapacity"/>.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when the capacity is outside that range.</exception>
	public InputWindow(InputActionSet actions, int capacity = DefaultCapacity)
	{
		if (capacity is < 1 or > MaxCapacity)
		{
			throw new ArgumentOutOfRangeException(
				nameof(capacity),
				capacity,
				$"A window carries 1 to {MaxCapacity} ticks.");
		}

		Actions = actions;
		Capacity = capacity;
	}

	/// <summary>
	/// Reads a packet <see cref="Write"/> wrote.
	/// </summary>
	/// <param name="reader">Where to read.</param>
	/// <param name="actions">The action set the frames belong to.</param>
	/// <param name="commands">Where to put the commands, oldest first. Cleared first.</param>
	/// <exception cref="MalformedPacketException">Thrown when the packet can't be a window.</exception>
	public static void Read(BitReader reader, InputActionSet actions, List<InputCommand> commands)
	{
		commands.Clear();
		long newestTick = reader.ReadVarInt();
		int count = (int)reader.ReadBits(4);
		InputFrame previous = default;

		for (int i = 0; i < count; i++)
		{
			InputFrame frame = InputCodec.ReadFrame(reader, previous, actions);
			ushort[] events = InputCodec.ReadEvents(reader);
			commands.Add(new InputCommand(0, frame, events));
			previous = frame;
		}

		if (count == 0)
		{
			return;
		}

		// The ticks are counted back from the newest, through the gaps between consecutive commands.
		long[] gaps = new long[count];

		for (int i = 1; i < count; i++)
		{
			ulong gap = reader.ReadVarUInt();

			if (gap > int.MaxValue)
			{
				throw new MalformedPacketException("A gap between input ticks is too large.");
			}

			gaps[i] = (long)gap + 1;
		}

		long tick = newestTick;

		for (int i = count - 1; i >= 0; i--)
		{
			commands[i] = commands[i] with { Tick = tick };
			tick -= gaps[i];
		}
	}

	/// <summary>
	/// Adds the command for a tick. A tick already in the window is replaced; one older than the newest is refused,
	/// since the server reads ticks in order.
	/// </summary>
	/// <param name="command">The command.</param>
	/// <exception cref="ArgumentException">Thrown when the tick is older than the newest in the window.</exception>
	public void Add(in InputCommand command)
	{
		if (_commands.Count > 0 && command.Tick < _commands[^1].Tick)
		{
			throw new ArgumentException(
				$"Tick {command.Tick} is older than the window's newest, {_commands[^1].Tick}.",
				nameof(command));
		}

		if (_commands.Count > 0 && command.Tick == _commands[^1].Tick)
		{
			_commands[^1] = command;
		}
		else
		{
			_commands.Add(command);
		}

		while (_commands.Count > Capacity)
		{
			_commands.RemoveAt(0);
		}
	}

	/// <summary>
	/// Drops every tick up to and including the one the server acknowledged, so the window carries only what the
	/// server may still be missing.
	/// </summary>
	/// <param name="tick">The newest tick the server has.</param>
	public void Acknowledge(long tick)
	{
		int keep = 0;

		while (keep < _commands.Count && _commands[keep].Tick <= tick)
		{
			keep++;
		}

		_commands.RemoveRange(0, keep);
	}

	/// <summary>
	/// Forgets every command, as when the clock is set to another tick.
	/// </summary>
	public void Clear()
	{
		_commands.Clear();
	}

	/// <summary>
	/// Writes the window as one packet: the newest tick, the count, then each command oldest first.
	/// </summary>
	/// <param name="writer">Where to write.</param>
	public void Write(BitWriter writer)
	{
		writer.WriteVarInt(NewestTick ?? 0);
		writer.WriteBits((ulong)_commands.Count, 4);

		InputFrame previous = default;

		foreach (InputCommand command in _commands)
		{
			// Consecutive ticks are the rule; the gap is written so a window that skipped some still reads right.
			InputCodec.WriteFrame(writer, command.Frame, previous, Actions);
			InputCodec.WriteEvents(writer, command.Events);
			previous = command.Frame;
		}

		for (int i = 1; i < _commands.Count; i++)
		{
			writer.WriteVarUInt((ulong)(_commands[i].Tick - _commands[i - 1].Tick - 1));
		}
	}
}
