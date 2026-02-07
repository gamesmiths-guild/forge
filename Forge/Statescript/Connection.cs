// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Ports;

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// Represents a connection between an output port and an input port.
/// </summary>
/// <param name="OutputPort">The output port.</param>
/// <param name="InputPort">The input port.</param>
[Serializable]
public record struct Connection(OutputPort OutputPort, InputPort InputPort);
