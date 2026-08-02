// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// The points in an effect's lifetime at which a <see cref="RaiseEventEffectComponent"/> raises its event. The values
/// combine, so one component can cover several of them.
/// </summary>
[Flags]
public enum EffectEventTrigger : byte
{
	/// <summary>
	/// Never raises. A component configured with this does nothing, and <see cref="EffectData"/> rejects it.
	/// </summary>
	None = 0,

	/// <summary>
	/// Raises when the effect is applied, from <see cref="IEffectComponent.OnEffectApplied"/>. Every effect reaches
	/// this, including instant ones, and it fires again for each successfully applied stack.
	/// </summary>
	Applied = 1 << 0,

	/// <summary>
	/// Raises on each execution, from <see cref="IEffectComponent.OnEffectExecuted"/>. Only instant and periodic
	/// effects execute, so a duration effect using this must be periodic.
	/// </summary>
	Executed = 1 << 1,

	/// <summary>
	/// Raises when the effect is fully removed, from <see cref="IEffectComponent.OnActiveEffectUnapplied"/>. Losing a
	/// single stack does not count, and an instant effect never becomes active, so it never reaches this.
	/// </summary>
	Removed = 1 << 2,

	/// <summary>
	/// Raises when the effect loses a stack it survives, from <see cref="IEffectComponent.OnActiveEffectUnapplied"/>.
	/// This is the counterpart of <see cref="Applied"/> firing again for each stack gained; the stack that takes the
	/// count to zero is a full removal and reports <see cref="Removed"/> instead. Only stackable effects reach it.
	/// </summary>
	StackRemoved = 1 << 3,
}
