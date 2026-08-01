// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// Effect component that removes other active effects from the target when its own effect is applied. Every active
/// effect matching one of the <paramref name="removeQueries"/> is removed, or loses <paramref name="stacksToRemove"/>
/// stacks — never the effect carrying this component.
/// </summary>
/// <remarks>
/// <para>
/// This is the natural way to build a cleanse or a dispel, and unlike the tag-driven alternative it works from an
/// <see cref="Duration.DurationType.Instant"/> effect: <see cref="ModifierTagsEffectComponent"/> is rejected on instant
/// effects, so removing through <see cref="TargetTagRequirementsEffectComponent"/>'s removal requirements forces a
/// cleanse to become a short duration effect just to hold a tag long enough for the target to notice it.
/// </para>
/// <para>
/// Removal happens on application, which includes each successful stack application, and not on periodic executions.
/// A periodic owner is therefore rejected by <see cref="EffectData"/>: it would look like it dispels on every tick but
/// only ever fire once.
/// </para>
/// <para>
/// An empty <see cref="EffectQuery"/> matches every effect, so a component built from one would strip the target of
/// everything it has. That is treated as a misconfiguration rather than an intent: empty queries remove nothing and
/// <see cref="EffectData"/> asserts against them.
/// </para>
/// <para>
/// This component maintains per-effect-instance state (its own handle, so it can exclude itself). When used in
/// <see cref="EffectData"/>, each effect application will create its own instance via <see cref="CreateInstance"/> to
/// isolate state between different effect applications.
/// </para>
/// </remarks>
/// <param name="removeQueries">The queries selecting which active effects to remove. An effect matching any of them is
/// removed.</param>
/// <param name="stacksToRemove">How many stacks to remove from each matching effect. Any negative value, the default,
/// removes the effects entirely regardless of their stack count.</param>
public class RemoveOtherEffectComponent(EffectQuery[] removeQueries, int stacksToRemove = -1) : IEffectComponent
{
	private ActiveEffectHandle[]? _ownHandle;

	internal EffectQuery[] RemoveQueries { get; } = removeQueries;

	internal int StacksToRemove { get; } = stacksToRemove;

	// An empty query matches every effect, stripping the target of everything. EffectData rejects it.
	internal bool HasEmptyQuery => Array.Exists(RemoveQueries, x => x.IsEmpty);

	/// <inheritdoc/>
	public IEffectComponent CreateInstance()
	{
		// Create a new instance for each effect application to isolate the handle it excludes from removal.
		return new RemoveOtherEffectComponent(RemoveQueries, StacksToRemove);
	}

	/// <inheritdoc/>
	public bool OnActiveEffectAdded(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		// OnEffectApplied runs immediately after this, in the same loop iteration, so the handle is already stashed by
		// the time the first removal pass needs to exclude it. Instant effects never get here, and are never in the
		// active list to begin with, so they have nothing to exclude.
		_ownHandle = [activeEffectEvaluatedData.ActiveEffectHandle];

		return true;
	}

	/// <inheritdoc/>
	public void OnEffectApplied(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData)
	{
		foreach (EffectQuery query in RemoveQueries)
		{
			// See the remarks on why an empty query removes nothing instead of everything.
			if (query.IsEmpty)
			{
				continue;
			}

			target.EffectsManager.RemoveEffects(query, StacksToRemove, _ownHandle);
		}
	}
}
