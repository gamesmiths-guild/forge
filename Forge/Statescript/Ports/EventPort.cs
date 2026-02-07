// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Ports;

/// <summary>
/// Defines an event output port that can emit messages to connected input ports in the Statescript system.
/// </summary>
[Serializable]
public class EventPort : OutputPort
{
	/// <summary>
	/// Initializes a new instance of the <see cref="EventPort"/> class.
	/// </summary>
	public EventPort()
	{
		PortID = Guid.NewGuid();
	}
}
