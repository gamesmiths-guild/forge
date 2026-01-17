// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Effects;

/// <summary>
/// Defines the requirements for triggering different types of cues.
/// </summary>
[Flags]
public enum CueTriggerRequirement
{
	/// <summary>
	/// No specific trigger requirements.
	/// </summary>
	None = 0,

	/// <summary>
	/// Requirement for triggering cues on a persistent effect application.
	/// </summary>
	OnApply = 1 << 0,

	/// <summary>
	/// Requirement for triggering cues update when effect updates.
	/// </summary>
	OnUpdate = 1 << 1,

	/// <summary>
	/// Requirement for triggering cues on effect execution.
	/// </summary>
	OnExecute = 1 << 2,
}
