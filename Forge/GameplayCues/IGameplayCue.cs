// Copyright © 2024 Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.GameplayCues;

/// <summary>
/// Interface for implementing Gameplay Cues that handle visual and audio feedback for gameplay events.
/// </summary>
public interface IGameplayCue
{
	/// <summary>
	/// Called when the gameplay cue is executed.
	/// </summary>
	/// <param name="target">The cue's target.</param>
	/// <param name="parameters">Optional parameters for the gameplay cue execution.</param>
	void OnExecute(IForgeEntity? target, GameplayCueParameters? parameters);

	/// <summary>
	/// Called when the gameplay cue is applied.
	/// </summary>
	/// <param name="target">The cue's target.</param>
	/// <param name="parameters">Optional parameters for the gameplay cue execution.</param>
	void OnApply(IForgeEntity? target, GameplayCueParameters? parameters);

	/// <summary>
	/// Called when the gameplay cue is removed.
	/// </summary>
	/// <param name="target">The cue's target.</param>
	/// <param name="interrupted">Whether this removal is the result of an interruption.</param>
	void OnRemove(IForgeEntity? target, bool interrupted);

	/// <summary>
	/// Called when the gameplay cue is already active but needs to be updated without calling the
	/// <see cref="OnApply"/>.
	/// </summary>
	/// <param name="target">The cue's target.</param>
	/// <param name="parameters">Optional parameters for the gameplay cue execution.</param>
	void WhileActive(IForgeEntity? target, GameplayCueParameters? parameters);
}
