// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Cues;

/// <summary>
/// Interface for implementing handlers for visual and audio feedback for cues.
/// </summary>
public interface ICueHandler
{
	/// <summary>
	/// Called when the cue is executed.
	/// </summary>
	/// <param name="target">The cue's target.</param>
	/// <param name="parameters">Optional parameters for the cue execution.</param>
	void OnExecute(IForgeEntity? target, CueParameters? parameters);

	/// <summary>
	/// Called when the cue is applied.
	/// </summary>
	/// <param name="target">The cue's target.</param>
	/// <param name="parameters">Optional parameters for the cue execution.</param>
	void OnApply(IForgeEntity? target, CueParameters? parameters);

	/// <summary>
	/// Called when the cue is removed.
	/// </summary>
	/// <param name="target">The cue's target.</param>
	/// <param name="interrupted">Whether this removal is the result of an interruption.</param>
	void OnRemove(IForgeEntity? target, bool interrupted);

	/// <summary>
	/// Called when the cue is already active but needs to be updated without calling the
	/// <see cref="OnApply"/>.
	/// </summary>
	/// <param name="target">The cue's target.</param>
	/// <param name="parameters">Optional parameters for the cue execution.</param>
	void OnUpdate(IForgeEntity? target, CueParameters? parameters);
}
