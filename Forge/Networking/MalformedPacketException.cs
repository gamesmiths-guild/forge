// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Networking;

/// <summary>
/// Thrown when a packet can't be what its sender would have written: it ends early, or holds a value no writer
/// produces. A receiver drops the packet; nothing read before the exception is trusted.
/// </summary>
public sealed class MalformedPacketException : Exception
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MalformedPacketException"/> class.
	/// </summary>
	public MalformedPacketException()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MalformedPacketException"/> class.
	/// </summary>
	/// <param name="message">What was wrong with the packet.</param>
	public MalformedPacketException(string message)
		: base(message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MalformedPacketException"/> class.
	/// </summary>
	/// <param name="message">What was wrong with the packet.</param>
	/// <param name="innerException">What found it.</param>
	public MalformedPacketException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
