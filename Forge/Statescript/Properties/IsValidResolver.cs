// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a <see cref="bool"/> indicating whether a nested object-backed resolver produces a valid (non-null) value.
/// Use this to validate object variables (entities, effects, handles) before acting on them, e.g. "is the stored
/// target still set?".
/// </summary>
/// <remarks>
/// A value is considered valid when it is not <see langword="null"/>. Missing variables resolve to
/// <see langword="null"/> and are therefore reported as invalid. For an "is null" check, wrap this resolver in a
/// <see cref="NotResolver"/>.
/// </remarks>
/// <param name="source">The object-backed resolver whose result is checked.</param>
public class IsValidResolver(IObjectResolver source) : IPropertyResolver
{
	private readonly IObjectResolver _source = source ?? throw new ArgumentNullException(nameof(source));

	/// <inheritdoc/>
	public Type ValueType => typeof(bool);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		return new Variant128(_source.Resolve(graphContext) is not null);
	}
}
