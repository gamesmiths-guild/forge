// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Specifies the instancing policy for abilities, determining how ability instances are created and managed.
/// </summary>
/// <remarks>This enumeration defines the instancing behavior for abilities, which can affect their lifecycle and
/// usage: <list type="bullet"> <item> <term><see cref="PerEntity"/></term> <description>Each entity gets its own
/// instance of the ability, ensuring that the ability's state is isolated per entity.</description> </item> <item>
/// <term><see cref="PerExecution"/></term> <description>A new instance of the ability is created for each execution,
/// allowing for stateless or transient behavior.</description> </item> </list> Choose the appropriate policy based on
/// whether the ability requires persistent state per entity or should be stateless and transient.</remarks>
public enum AbilityInstancingPolicy : byte
{
	/// <summary>
	/// Abilities are instantiated per entity, meaning each entity has its own instance of the ability.
	/// </summary>
	PerEntity = 0,

	/// <summary>
	/// Abilities are instantiated per execution, meaning a new instance is created each time the ability is used.
	/// </summary>
	PerExecution = 1,
}
