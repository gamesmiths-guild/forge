// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// A coded ability behavior that a client can predict and roll back: everything its run needs to go on lives in a
/// <typeparamref name="TContext"/>, which entity snapshots capture and restores put back, member by member.
/// </summary>
/// <remarks>
/// <para>Each run starts with a new context. Keep the run's state in <see cref="Context"/>, count time in fixed steps
/// in <see cref="OnFixedUpdate"/>, and draw randomness from the entity's seeded random, so the ticks a correction runs
/// again come out as they did. Change gameplay only through effects, tags, attributes and events, and show things only
/// through cues.</para>
/// <para>Fields of the behavior outside its context don't roll back.</para>
/// </remarks>
/// <typeparam name="TContext">The run's state.</typeparam>
public abstract class RollbackBehavior<TContext> : ISnapshotAbilityBehavior
	where TContext : BehaviorContext, new()
{
	/// <inheritdoc/>
	public abstract void OnEnded(AbilityBehaviorContext context);

	/// <summary>
	/// Gets the state of the running run.
	/// </summary>
	protected TContext Context { get; private set; } = new();

	/// <summary>
	/// Called when a run starts. The base gives the run a new <see cref="Context"/>; overrides call it first.
	/// </summary>
	/// <param name="context">The context for the started run.</param>
	public virtual void OnStarted(AbilityBehaviorContext context)
	{
		Context = new TContext { Ability = context };
	}

	/// <inheritdoc/>
	public virtual void OnFixedUpdate(double deltaTime)
	{
	}

	/// <inheritdoc/>
	public virtual void OnUpdate(double deltaTime)
	{
	}

	/// <inheritdoc/>
	public virtual void OnPresent()
	{
	}

	/// <inheritdoc/>
	public object? CaptureSnapshot()
	{
		return Context.Copy();
	}

	/// <inheritdoc/>
	public void RestoreSnapshot(object? snapshot)
	{
		Context = (TContext)((TContext)snapshot!).Copy();
	}

	/// <inheritdoc/>
	public virtual void DropRun()
	{
		Context = new TContext();
	}

	/// <inheritdoc/>
	public virtual void OnRestoreDetach()
	{
	}

	/// <inheritdoc/>
	public virtual void OnRestoreAttach()
	{
	}
}
