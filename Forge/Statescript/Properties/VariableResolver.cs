// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a property value by looking up another named variable or property from the graph's runtime
/// <see cref="Variables"/>. This enables nested property references: a resolver that reads its value from another
/// resolver by name at runtime.
/// </summary>
/// <remarks>
/// <para>Unlike <see cref="VariantResolver"/> which holds a value directly, this resolver delegates to whatever
/// resolver is registered under the given name at runtime. The referenced property can be a mutable variable, an
/// entity attribute, or any other <see cref="IPropertyResolver"/>.</para>
/// <para>If the referenced property does not exist, the resolver returns a default <see cref="Variant128"/>
/// (zero).</para>
/// </remarks>
/// <param name="referencedPropertyName">The name of the graph variable or property to resolve at runtime.</param>
/// <param name="valueType">The type of the value this resolver produces.</param>
public class VariableResolver(StringKey referencedPropertyName, Type valueType) : IPropertyResolver
{
	private readonly StringKey _referencedPropertyName = referencedPropertyName;

	/// <inheritdoc/>
	public Type ValueType { get; } = valueType;

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		if (!graphContext.GraphVariables.TryGetVariant(_referencedPropertyName, graphContext, out Variant128 value))
		{
			return default;
		}

		return value;
	}
}
