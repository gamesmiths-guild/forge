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
	/// Raises when the effect is fully removed after running out of duration, from
	/// <see cref="IEffectComponent.OnActiveEffectUnapplied"/> with
	/// <see cref="EffectRemovalReason.Expired"/>. Only <see cref="Duration.DurationType.HasDuration"/> effects have a
	/// natural end to reach, so nothing else fires it.
	/// </summary>
	ExpiredNormally = 1 << 2,

	/// <summary>
	/// Raises when the effect is fully removed before it could expire, from
	/// <see cref="IEffectComponent.OnActiveEffectUnapplied"/> with <see cref="EffectRemovalReason.Removed"/>. Every
	/// removal through the <see cref="EffectsManager"/> API lands here — a dispel, a tag- or attribute-driven removal,
	/// or an outright <see cref="EffectsManager.RemoveEffect(ActiveEffectHandle, bool)"/> — as does every removal of an
	/// <see cref="Duration.DurationType.Infinite"/> effect, which has no natural end.
	/// </summary>
	RemovedPrematurely = 1 << 3,

	/// <summary>
	/// Raises when the effect loses a stack it survives, from <see cref="IEffectComponent.OnActiveEffectUnapplied"/>.
	/// This is the counterpart of <see cref="Applied"/> firing again for each stack gained; the stack that takes the
	/// count to zero is a full removal and reports <see cref="ExpiredNormally"/> or <see cref="RemovedPrematurely"/>
	/// instead. Only stackable effects reach it, and it does not separate the two reasons the way the full-removal
	/// triggers do.
	/// </summary>
	StackRemoved = 1 << 4,
}
