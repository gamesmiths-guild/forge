// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Nodes;

/// <summary>
/// Shared input-resolution helpers used by the ability nodes.
/// </summary>
internal static class AbilityNodeUtilities
{
	/// <summary>
	/// Resolves an entity input, falling back to the ability context's owner when the input is unbound or does not
	/// resolve.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="entityInputName">The bound name of the entity input property.</param>
	/// <returns>The resolved entity, or <see langword="null"/> when neither the input nor an ability context is
	/// available.</returns>
	public static IForgeEntity? ResolveEntityOrOwner(GraphContext graphContext, StringKey entityInputName)
	{
		return ResolveOptionalEntity(graphContext, entityInputName)
			?? (graphContext.TryGetActivationContext(out AbilityBehaviorContext? abilityContext)
				? abilityContext.Owner
				: null);
	}

	/// <summary>
	/// Resolves an optional entity input.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="entityInputName">The bound name of the entity input property.</param>
	/// <returns>The resolved entity, or <see langword="null"/> when the input is unbound or does not resolve.</returns>
	public static IForgeEntity? ResolveOptionalEntity(GraphContext graphContext, StringKey entityInputName)
	{
		if (entityInputName != StringKey.Empty
			&& graphContext.TryResolveObject(entityInputName, typeof(IForgeEntity), out object? resolved)
			&& resolved is IForgeEntity entity)
		{
			return entity;
		}

		return null;
	}

	/// <summary>
	/// Resolves a level input, falling back to the ability context's level, or <c>1</c> when neither is available.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="levelInputName">The bound name of the level input property.</param>
	/// <returns>The resolved level.</returns>
	public static int ResolveLevelOrContext(GraphContext graphContext, StringKey levelInputName)
	{
		if (levelInputName != StringKey.Empty
			&& graphContext.TryResolve(levelInputName, out int level))
		{
			return level;
		}

		return graphContext.TryGetActivationContext(out AbilityBehaviorContext? abilityContext)
			? abilityContext.Level
			: 1;
	}

	/// <summary>
	/// Resolves an <see cref="AbilityData"/> object input.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="abilityDataInputName">The bound name of the ability data input property.</param>
	/// <param name="abilityData">The resolved ability data.</param>
	/// <returns><see langword="true"/> when the input resolved to an <see cref="AbilityData"/>.</returns>
	public static bool TryResolveAbilityData(
		GraphContext graphContext,
		StringKey abilityDataInputName,
		out AbilityData abilityData)
	{
		if (graphContext.TryResolveObject(abilityDataInputName, typeof(AbilityData), out object? resolved)
			&& resolved is AbilityData resolvedData)
		{
			abilityData = resolvedData;
			return true;
		}

		abilityData = default;
		return false;
	}

	/// <summary>
	/// Resolves an optional activation-data input to the <see cref="AbilityActivator"/> that carries the graph's custom
	/// activation data.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="activationDataInputName">The bound name of the activation data input property.</param>
	/// <returns>The resolved activator, or <see langword="null"/> when the input is unbound or does not resolve, in
	/// which case the ability is activated without custom data.</returns>
	public static AbilityActivator? ResolveActivator(GraphContext graphContext, StringKey activationDataInputName)
	{
		if (activationDataInputName != StringKey.Empty
			&& graphContext.TryResolveObject(activationDataInputName, typeof(AbilityActivator), out object? resolved)
			&& resolved is AbilityActivator activator)
		{
			return activator;
		}

		return null;
	}

	/// <summary>
	/// Builds a <see cref="TagContainer"/> from a tag input that accepts a single <see cref="Tag"/> or an array of
	/// tags.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="tagInputName">The bound name of the tag input property.</param>
	/// <param name="entity">The entity whose tags manager is used to build the container.</param>
	/// <returns>The built container, or <see langword="null"/> when no tags resolved.</returns>
	public static TagContainer? BuildTagContainer(
		GraphContext graphContext,
		StringKey tagInputName,
		IForgeEntity entity)
	{
		if (!EffectApplicationUtilities.TryResolveTags(graphContext, tagInputName, out IReadOnlyList<Tag> tags))
		{
			return null;
		}

		return new TagContainer(entity.Tags.AllTags.TagsManager, [.. tags]);
	}

	/// <summary>
	/// Resolves an ability handle input that accepts either a single <see cref="AbilityHandle"/> or an array of
	/// handles.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="handleInputName">The bound name of the handle input property.</param>
	/// <param name="handles">The resolved handles.</param>
	/// <returns><see langword="true"/> when at least one handle resolved.</returns>
	public static bool TryResolveHandles(
		GraphContext graphContext,
		StringKey handleInputName,
		out IReadOnlyList<AbilityHandle> handles)
	{
		if (graphContext.TryResolveObjectArray(
			handleInputName,
			typeof(AbilityHandle),
			out object?[]? resolvedHandleArray))
		{
			var resolvedHandles = new List<AbilityHandle>(resolvedHandleArray.Length);

			for (int i = 0; i < resolvedHandleArray.Length; i++)
			{
				if (resolvedHandleArray[i] is AbilityHandle handle)
				{
					resolvedHandles.Add(handle);
				}
			}

			handles = resolvedHandles;
			return resolvedHandles.Count > 0;
		}

		if (graphContext.TryResolveObject(handleInputName, typeof(AbilityHandle), out object? resolvedHandle)
			&& resolvedHandle is AbilityHandle singleHandle)
		{
			handles = [singleHandle];
			return true;
		}

		handles = [];
		return false;
	}

	/// <summary>
	/// Writes an <see cref="AbilityHandle"/> to a node's bound object output variable.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="output">The node's handle output variable.</param>
	/// <param name="handle">The handle to write.</param>
	public static void WriteHandleOutput(GraphContext graphContext, OutputVariable output, AbilityHandle? handle)
	{
		if (output.BoundName == StringKey.Empty)
		{
			return;
		}

		Variables? variables = output.Scope == VariableScope.Shared
			? graphContext.SharedVariables
			: graphContext.GraphVariables;

		variables?.SetObject(output.BoundName, handle);
	}
}
