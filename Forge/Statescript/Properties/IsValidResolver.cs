// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a <see cref="bool"/> indicating whether a nested object-backed resolver produces a usable value. Use this
/// to validate object variables (entities, effects, handles) before acting on them, e.g. "is the stored target still
/// set?".
/// </summary>
/// <remarks>
/// <para>A value is usable when it is not <see langword="null"/> and, for types that implement
/// <see cref="IValidatable"/>, when it also reports itself valid. That second half matters: a handle whose effect was
/// removed or whose ability was revoked is not null, so a plain null check would call it valid and everything
/// downstream would act on something that is no longer there.</para>
/// <para>Missing variables resolve to <see langword="null"/> and are therefore reported as invalid. For an "is null"
/// check, wrap this resolver in a <see cref="NotResolver"/>.</para>
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
		return new Variant128(IsValid(_source.Resolve(graphContext)));
	}

	/// <summary>
	/// Reports whether a resolved value is valid: present, and valid when it can say. Override to add a check this
	/// library cannot make on its own, such as whether an engine object has already been destroyed.
	/// </summary>
	/// <param name="value">The value to check.</param>
	/// <returns><see langword="true"/> when the value is valid.</returns>
	protected virtual bool IsValid(object? value)
	{
		return value switch
		{
			null => false,
			IValidatable validatable => validatable.IsValid,
			_ => true,
		};
	}
}
