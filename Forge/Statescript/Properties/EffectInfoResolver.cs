// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves an aggregate <see langword="int"/> over the active applications of a given <see cref="EffectData"/> on a
/// resolved entity.
/// </summary>
/// <remarks>
/// <para>By default, this resolver targets the owner entity through <see cref="AbilityOwnerResolver"/>.</para>
/// <para><see cref="EffectInfoType.TotalStackCount"/> is the "current number of stacks" query: it sums the stacks of
/// every active application of the effect on the entity.</para>
/// <para>If the selected entity is not available or the effect is not active, the resolver returns <c>0</c>.</para>
/// </remarks>
/// <param name="effectData">The effect data to query for.</param>
/// <param name="infoType">Which aggregate to compute.</param>
/// <param name="entityResolver">The entity resolver that selects which entity to inspect.</param>
public class EffectInfoResolver(
	EffectData effectData,
	EffectInfoType infoType,
	IEntityResolver? entityResolver = null) : IPropertyResolver
{
	private static readonly IEntityResolver _defaultEntityResolver = new AbilityOwnerResolver();

	private readonly EffectData _effectData = effectData;
	private readonly EffectInfoType _infoType = infoType;
	private readonly IEntityResolver _entityResolver = entityResolver ?? _defaultEntityResolver;

	/// <inheritdoc/>
	public Type ValueType => typeof(int);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		IForgeEntity? entity = _entityResolver.Resolve(graphContext);

		if (entity is null)
		{
			return new Variant128(0);
		}

		int totalStacks = 0;
		int instanceCount = 0;
		int maxLevel = 0;

		foreach (EffectStackInstanceData instanceData in entity.EffectsManager.GetEffectInfo(_effectData))
		{
			totalStacks += instanceData.StackCount;
			instanceCount++;
			maxLevel = Math.Max(maxLevel, instanceData.EffectLevel);
		}

		return _infoType switch
		{
			EffectInfoType.TotalStackCount => new Variant128(totalStacks),
			EffectInfoType.InstanceCount => new Variant128(instanceCount),
			EffectInfoType.MaxLevel => new Variant128(maxLevel),
			_ => default,
		};
	}
}
