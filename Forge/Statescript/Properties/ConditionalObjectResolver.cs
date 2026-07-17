// Copyright © Gamesmiths Guild.

using System.Diagnostics.CodeAnalysis;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves one of two object-backed values based on a boolean condition — the ternary select for the object lane
/// (for example, picking between two entities).
/// </summary>
/// <remarks>
/// Only the selected branch resolver is evaluated.
/// </remarks>
/// <typeparam name="T">The value type to read.</typeparam>
/// <param name="condition">The resolver for the boolean condition.</param>
/// <param name="whenTrue">The resolver evaluated when the condition is <see langword="true"/>.</param>
/// <param name="whenFalse">The resolver evaluated when the condition is <see langword="false"/>.</param>
public class ConditionalObjectResolver<T>(
	IPropertyResolver condition,
	IObjectResolver<T> whenTrue,
	IObjectResolver<T> whenFalse) : ObjectResolver<T>
{
	private readonly IPropertyResolver _condition = condition;

	private readonly IObjectResolver<T> _whenTrue = whenTrue;

	private readonly IObjectResolver<T> _whenFalse = whenFalse;

	/// <inheritdoc/>
	[return: MaybeNull]
	public override T Resolve(GraphContext graphContext)
	{
		return _condition.Resolve(graphContext).AsBool()
			? _whenTrue.Resolve(graphContext)
			: _whenFalse.Resolve(graphContext);
	}
}
