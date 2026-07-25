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
/// <para><see cref="EffectStackDataType.TotalStackCount"/> is the "current number of stacks" query: it sums the stacks
/// of every active application of the effect on the entity.</para>
/// <para>If the selected entity is not available or the effect is not active, the resolver returns <c>0</c>.</para>
/// </remarks>
/// <param name="effectData">The effect data to query for.</param>
/// <param name="dataType">Which aggregate to compute.</param>
/// <param name="entityResolver">The entity resolver that selects which entity to inspect.</param>
public class EffectStackDataResolver(
	EffectData effectData,
	EffectStackDataType dataType,
	IEntityResolver? entityResolver = null) : IPropertyResolver
{
	private static readonly IEntityResolver _defaultEntityResolver = new AbilityOwnerResolver();

	private readonly EffectData _effectData = effectData;
	private readonly EffectStackDataType _dataType = dataType;
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

		foreach (EffectStackInstanceData instanceData in entity.EffectsManager.GetEffectStackData(_effectData))
		{
			totalStacks += instanceData.StackCount;
			instanceCount++;
			maxLevel = Math.Max(maxLevel, instanceData.EffectLevel);
		}

		return _dataType switch
		{
			EffectStackDataType.TotalStackCount => new Variant128(totalStacks),
			EffectStackDataType.InstanceCount => new Variant128(instanceCount),
			EffectStackDataType.MaxLevel => new Variant128(maxLevel),
			_ => default,
		};
	}
}
