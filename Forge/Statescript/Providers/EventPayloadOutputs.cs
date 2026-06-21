// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Providers;

/// <summary>
/// Writes the values an <see cref="IEventPayloadProvider"/> extracts from a received event payload to the graph
/// variables bound to the provider's declared <see cref="IEventPayloadProvider.Outputs"/>. Output names with no binding
/// are skipped.
/// </summary>
public sealed class EventPayloadOutputs
{
	private readonly GraphContext _graphContext;
	private readonly IReadOnlyDictionary<string, EventOutputBinding> _bindings;

	internal EventPayloadOutputs(GraphContext graphContext, IReadOnlyDictionary<string, EventOutputBinding> bindings)
	{
		_graphContext = graphContext;
		_bindings = bindings;
	}

	/// <summary>
	/// Writes an unmanaged value to the variable bound to the named output.
	/// </summary>
	/// <typeparam name="T">The value type. Must be supported by <see cref="Variant128"/>.</typeparam>
	/// <param name="name">The declared output name.</param>
	/// <param name="value">The value to write.</param>
	public void Set<T>(string name, T value)
		where T : unmanaged
	{
		Variables? variables = ResolveBinding(name, out StringKey variableName);

		if (variables is null)
		{
			return;
		}

		// Floating-point graph variables are double-backed, so widen single-precision values before storing them.
		if (typeof(T) == typeof(float))
		{
			variables.SetVar(variableName, (double)(float)(object)value);
		}
		else
		{
			variables.SetVar(variableName, value);
		}
	}

	/// <summary>
	/// Writes a reference value to the variable bound to the named output.
	/// </summary>
	/// <typeparam name="T">The reference value type.</typeparam>
	/// <param name="name">The declared output name.</param>
	/// <param name="value">The value to write.</param>
	public void SetObject<T>(string name, T value)
	{
		Variables? variables = ResolveBinding(name, out StringKey variableName);
		variables?.SetObject(variableName, value);
	}

	/// <summary>
	/// Gets a value indicating whether a binding exists for the given output name.
	/// </summary>
	/// <param name="name">The declared output name.</param>
	/// <returns><see langword="true"/> when a graph variable is bound for the name.</returns>
	public bool Has(string name)
	{
		return _bindings.ContainsKey(name);
	}

	private Variables? ResolveBinding(string name, out StringKey variableName)
	{
		variableName = default;

		if (!_bindings.TryGetValue(name, out EventOutputBinding binding))
		{
			return null;
		}

		variableName = binding.VariableName;
		return binding.Scope == VariableScope.Shared
			? _graphContext.SharedVariables
			: _graphContext.GraphVariables;
	}
}
