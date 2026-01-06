// Copyright © Gamesmiths Guild.

using System.Diagnostics.CodeAnalysis;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Effects;

/// <summary>
/// Base class for custom effect application context data.
/// Subclass this to pass arbitrary data through the effect pipeline to CustomCalculators and CustomExecutions.
/// </summary>
/// <remarks>
/// This allows custom data to flow through the effect system during application and execution.
/// </remarks>
public abstract class EffectApplicationContext
{
	/// <summary>
	/// Attempts to get the context data as a specific type.
	/// </summary>
	/// <typeparam name="TData">The expected data type.</typeparam>
	/// <param name="data">The extracted data, if successful.</param>
	/// <returns><see langword="true"/> if the context contains data of the expected type.</returns>
	public bool TryGetData<TData>([NotNullWhen(true)] out TData? data)
	{
#pragma warning disable S3060 // "is" should not be used with "this"
		if (this is EffectApplicationContext<TData> typedContext)
		{
			data = typedContext.Data;
			Validation.Assert(
				data is not null,
				"EffectApplicationContext data should never be null for a successfully typed context.");
			return true;
		}
#pragma warning restore S3060 // "is" should not be used with "this"

		data = default;
		return false;
	}
}
