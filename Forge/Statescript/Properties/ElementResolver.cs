// Copyright © Gamesmiths Guild.

using System.Diagnostics.CodeAnalysis;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the object-backed array element currently being iterated by an enclosing array resolver (e.g.
/// <see cref="ObjectWhereResolver{T}"/>, <see cref="ObjectOrderByResolver{T}"/>). Use this inside nested "lambda"
/// resolvers as the stand-in for the lambda parameter.
/// </summary>
/// <remarks>
/// If no array element is currently being iterated, or the current element is not compatible with
/// <typeparamref name="T"/>, <see langword="null"/> is returned.
/// </remarks>
/// <typeparam name="T">The element type this resolver produces.</typeparam>
public class ElementResolver<T> : ObjectResolver<T>
{
	/// <inheritdoc/>
	[return: MaybeNull]
	public override T Resolve(GraphContext graphContext)
	{
		if (graphContext.TryGetCurrentElement(out ElementFrame frame) && frame.ObjectValue is T typedValue)
		{
			return typedValue;
		}

		return default;
	}
}
