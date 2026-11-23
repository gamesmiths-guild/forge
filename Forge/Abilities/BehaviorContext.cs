// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// The state of a <see cref="RollbackBehavior{TContext}"/>'s run: what an entity snapshot captures of it and a restore
/// puts back, as a graph's node contexts are.
/// </summary>
/// <remarks>
/// Derive one with the fields the run needs to go on. Fields of the behavior itself, outside its context, don't roll
/// back.
/// </remarks>
public class BehaviorContext
{
	/// <summary>
	/// Gets the context the run was started with, or <see langword="null"/> before it starts.
	/// </summary>
	public AbilityBehaviorContext? Ability { get; internal set; }

	// A member-by-member copy for a snapshot, which every context type gets without code of its own.
	internal BehaviorContext Copy()
	{
		var copy = (BehaviorContext)MemberwiseClone();
		copy.OnCopied();
		return copy;
	}

	/// <summary>
	/// Called on a copy of this context that a snapshot has just made, member by member, either to keep or to restore.
	/// Until this returns, the copy shares every object the original holds. Give it a copy of its own of each one the
	/// run changes in place, such as a list it adds to.
	/// </summary>
	/// <remarks>
	/// Values need nothing, and neither do references the run only reads or replaces whole: an entity, an effect, an
	/// array it never writes into. Those stay shared, so a restored context points at the same objects. Overrides call
	/// the base first.
	/// </remarks>
	protected virtual void OnCopied()
	{
	}
}
