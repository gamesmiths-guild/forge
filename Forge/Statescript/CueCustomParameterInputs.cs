// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// Provides the resolved values for the inputs an <see cref="ICueCustomParametersProvider"/> declared through
/// <see cref="ICueCustomParametersProvider.Inputs"/>. Values are resolved lazily against the current
/// <see cref="GraphContext"/> when read, so an input that the provider never reads is never evaluated.
/// </summary>
public sealed class CueCustomParameterInputs
{
	private readonly GraphContext _graphContext;
	private readonly IReadOnlyDictionary<string, IPropertyResolver> _resolvers;

	internal CueCustomParameterInputs(
		GraphContext graphContext,
		IReadOnlyDictionary<string, IPropertyResolver> resolvers)
	{
		_graphContext = graphContext;
		_resolvers = resolvers;
	}

	/// <summary>
	/// Reads a declared input as the raw <see cref="Variant128"/> authored in the editor.
	/// </summary>
	/// <param name="name">The declared input name.</param>
	/// <returns>The resolved value, or <see langword="default"/> when no resolver is bound for the name.</returns>
	public Variant128 GetVariant(string name)
	{
		return _resolvers.TryGetValue(name, out IPropertyResolver? resolver)
			? resolver.Resolve(_graphContext)
			: default;
	}

	/// <summary>
	/// Reads a declared input as a specific value type.
	/// </summary>
	/// <typeparam name="T">The value type to interpret the resolved value as. Must be supported by
	/// <see cref="Variant128"/>.</typeparam>
	/// <param name="name">The declared input name.</param>
	/// <returns>The resolved value, or <see langword="default"/> when no resolver is bound for the name.</returns>
	public T Get<T>(string name)
		where T : unmanaged
	{
		return GetVariant(name).Get<T>();
	}

	/// <summary>
	/// Gets a value indicating whether a resolver is bound for the given input name.
	/// </summary>
	/// <param name="name">The declared input name.</param>
	/// <returns><see langword="true"/> when a resolver is bound for the name.</returns>
	public bool Has(string name)
	{
		return _resolvers.ContainsKey(name);
	}
}
