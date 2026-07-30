// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Providers;

/// <summary>
/// Declares one member of an activation-data type, serving both directions at once. On the sending side it renders as
/// a nested resolver on the provider's <c>Activation Data</c> section, and its resolved value reaches the provider
/// through <see cref="AbilityActivationDataInputs"/>. On the reading side it is offered as a bindable field of the data
/// the ability was activated with.
/// </summary>
/// <param name="Name">The member name. It is both the editor label and the key to read the authored value with
/// <see cref="AbilityActivationDataInputs.Get{T}(string)"/>, and it must match the public field or property on the
/// activation-data type, since the reading side resolves it there by name.</param>
/// <param name="ValueType">The value type as the graph sees it. The editor lists resolvers compatible with this type,
/// so it must be supported by <see cref="Variant128"/> (numbers, vectors, planes, quaternions, and so on) rather than
/// being an engine-specific equivalent.</param>
public sealed record AbilityActivationDataMember(string Name, Type ValueType);
