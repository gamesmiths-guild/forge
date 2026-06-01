// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Statescript.Nodes;

internal static class EffectApplicationUtilities
{
	public static void ApplyEffects(
		GraphContext graphContext,
		StringKey effectInputName,
		StringKey entityInputName,
		StringKey levelInputName,
		StringKey ownershipInputName,
		ICollection<ActiveEffectHandle>? activeHandles = null)
	{
		if (!TryResolveEffects(graphContext, effectInputName, out IReadOnlyList<EffectData> effects)
			|| !TryResolveEntities(graphContext, entityInputName, out IReadOnlyList<IForgeEntity> entities))
		{
			return;
		}

		EffectOwnership ownership = ResolveOwnership(graphContext, ownershipInputName);
		int level = ResolveLevel(graphContext, levelInputName);

		for (int entityIndex = 0; entityIndex < entities.Count; entityIndex++)
		{
			IForgeEntity entity = entities[entityIndex];

			for (int effectIndex = 0; effectIndex < effects.Count; effectIndex++)
			{
				var effect = new Effect(effects[effectIndex], ownership, level);
				ActiveEffectHandle? handle = entity.EffectsManager.ApplyEffect(effect);

				if (handle is not null && activeHandles is not null)
				{
					activeHandles.Add(handle);
				}
			}
		}
	}

	public static void RemoveEffects(IList<ActiveEffectHandle> activeHandles)
	{
		for (int i = 0; i < activeHandles.Count; i++)
		{
			ActiveEffectHandle handle = activeHandles[i];

			if (!handle.IsValid || handle.ActiveEffect is null)
			{
				continue;
			}

			handle.ActiveEffect.EffectEvaluatedData.Target.EffectsManager.RemoveEffect(handle);
		}

		activeHandles.Clear();
	}

	public static bool RetainActiveEffects(IList<ActiveEffectHandle> activeHandles)
	{
		for (int i = activeHandles.Count - 1; i >= 0; i--)
		{
			if (!activeHandles[i].IsValid)
			{
				activeHandles.RemoveAt(i);
			}
		}

		return activeHandles.Count > 0;
	}

	private static bool TryResolveEffects(
		GraphContext graphContext,
		StringKey effectInputName,
		out IReadOnlyList<EffectData> effects)
	{
		if (graphContext.TryResolveObjectArray(effectInputName, typeof(EffectData), out object?[]? resolvedEffectArray))
		{
			var resolvedEffects = new List<EffectData>(resolvedEffectArray.Length);

			for (int i = 0; i < resolvedEffectArray.Length; i++)
			{
				if (resolvedEffectArray[i] is EffectData effectData)
				{
					resolvedEffects.Add(effectData);
				}
			}

			effects = resolvedEffects;
			return resolvedEffects.Count > 0;
		}

		if (graphContext.TryResolveObject(effectInputName, typeof(EffectData), out object? resolvedEffect)
			&& resolvedEffect is EffectData singleEffect)
		{
			effects = [singleEffect];
			return true;
		}

		effects = [];
		return false;
	}

	private static bool TryResolveEntities(
		GraphContext graphContext,
		StringKey entityInputName,
		out IReadOnlyList<IForgeEntity> entities)
	{
		if (graphContext.TryResolveObjectArray(
			entityInputName,
			typeof(IForgeEntity),
			out object?[]? resolvedEntityArray))
		{
			var resolvedEntities = new List<IForgeEntity>(resolvedEntityArray.Length);

			for (int i = 0; i < resolvedEntityArray.Length; i++)
			{
				if (resolvedEntityArray[i] is IForgeEntity entity)
				{
					resolvedEntities.Add(entity);
				}
			}

			entities = resolvedEntities;
			return resolvedEntities.Count > 0;
		}

		if (graphContext.TryResolveObject(entityInputName, typeof(IForgeEntity), out object? resolvedEntity)
			&& resolvedEntity is IForgeEntity singleEntity)
		{
			entities = [singleEntity];
			return true;
		}

		entities = [];
		return false;
	}

	private static EffectOwnership ResolveOwnership(GraphContext graphContext, StringKey ownershipInputName)
	{
		if (ownershipInputName != StringKey.Empty
			&& graphContext.TryResolveObject(ownershipInputName, typeof(EffectOwnership), out object? resolvedOwnership)
			&& resolvedOwnership is EffectOwnership ownership)
		{
			return ownership;
		}

		if (!graphContext.TryGetActivationContext(out AbilityBehaviorContext? abilityContext))
		{
			return new EffectOwnership(null, null);
		}

		return abilityContext.Ownership;
	}

	private static int ResolveLevel(GraphContext graphContext, StringKey levelInputName)
	{
		if (levelInputName != StringKey.Empty && graphContext.TryResolve(levelInputName, out int level))
		{
			return level;
		}

		return graphContext.TryGetActivationContext(out AbilityBehaviorContext? abilityContext)
			? abilityContext.Level
			: 1;
	}
}
