// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Statescript.Nodes;

internal static class EffectApplicationUtilities
{
	public static void ApplyEffects(
		GraphContext graphContext,
		StringKey effectInputName,
		StringKey entityInputName,
		ICollection<ActiveEffectHandle>? activeHandles = null)
	{
		if (!TryResolveEffects(graphContext, effectInputName, out IReadOnlyList<Effect> effects)
			|| !TryResolveEntities(graphContext, entityInputName, out IReadOnlyList<IForgeEntity> entities))
		{
			return;
		}

		for (int entityIndex = 0; entityIndex < entities.Count; entityIndex++)
		{
			IForgeEntity entity = entities[entityIndex];

			for (int effectIndex = 0; effectIndex < effects.Count; effectIndex++)
			{
				ActiveEffectHandle? handle = entity.EffectsManager.ApplyEffect(effects[effectIndex]);

				if (handle is not null && activeHandles is not null)
				{
					activeHandles.Add(handle);
				}
			}
		}
	}

	/// <summary>
	/// Writes the active effect handles produced by an application to the node's bound output variable, choosing a
	/// scalar or array write based on the bound variable's shape. Instant applications produce no handle, so the array
	/// form is compact (only real handles) and the scalar form is <see langword="null"/> when no handle was produced.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="output">The node's handle output variable.</param>
	/// <param name="handles">The handles collected during application.</param>
	public static void WriteHandleOutput(
		GraphContext graphContext,
		OutputVariable output,
		IReadOnlyList<ActiveEffectHandle> handles)
	{
		if (output.BoundName == StringKey.Empty)
		{
			return;
		}

		Variables? variables = output.Scope == VariableScope.Shared
			? graphContext.SharedVariables
			: graphContext.GraphVariables;

		if (variables is null)
		{
			return;
		}

		if (variables.TryGetObjectArrayElementType(output.BoundName, out _))
		{
			object?[] values = new object?[handles.Count];
			for (int i = 0; i < handles.Count; i++)
			{
				values[i] = handles[i];
			}

			variables.DefineObjectArrayVariable(output.BoundName, typeof(ActiveEffectHandle), values);
			return;
		}

		if (variables.TryGetObjectVariableType(output.BoundName, out _))
		{
			variables.SetObject(output.BoundName, handles.Count > 0 ? handles[0] : null);
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
		out IReadOnlyList<Effect> effects)
	{
		if (graphContext.TryResolveObjectArray(effectInputName, typeof(Effect), out object?[]? resolvedEffectArray))
		{
			var resolvedEffects = new List<Effect>(resolvedEffectArray.Length);

			for (int i = 0; i < resolvedEffectArray.Length; i++)
			{
				if (resolvedEffectArray[i] is Effect effect)
				{
					resolvedEffects.Add(effect);
				}
			}

			effects = resolvedEffects;
			return resolvedEffects.Count > 0;
		}

		if (graphContext.TryResolveObject(effectInputName, typeof(Effect), out object? resolvedEffect)
			&& resolvedEffect is Effect singleEffect)
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
}
