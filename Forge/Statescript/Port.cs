// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// Base class for all Ports in the Statescript system.
/// </summary>
public abstract class Port
{
	/// <summary>
	/// Gets or sets the unique identifier for this port.
	/// </summary>
	public Guid PortID { get; set; }

	/// <summary>
	/// Gets or sets the index of this port.
	/// </summary>
	public byte Index { get; set; }
}
