// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the SetByCaller magnitude (<see langword="float"/>) currently stored on an <see cref="Effect"/> for a
/// given identifier tag.
/// </summary>
/// <remarks>
/// Missing effects or tags that were never set resolve to <c>0</c>.
/// </remarks>
/// <param name="effectResolver">The resolver that produces the effect to inspect.</param>
/// <param name="identifierTag">The SetByCaller identifier tag to read.</param>
public class SetByCallerMagnitudeResolver(
	IObjectResolver<Effect> effectResolver,
	Tag identifierTag) : IPropertyResolver
{
	private readonly IObjectResolver<Effect> _effectResolver = effectResolver
		?? throw new ArgumentNullException(nameof(effectResolver));

	private readonly Tag _identifierTag = identifierTag;

	/// <inheritdoc/>
	public Type ValueType => typeof(float);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Effect? effect = _effectResolver.Resolve(graphContext);

		if (effect is null || !effect.DataTag.TryGetValue(_identifierTag, out float magnitude))
		{
			return new Variant128(0f);
		}

		return new Variant128(magnitude);
	}
}
