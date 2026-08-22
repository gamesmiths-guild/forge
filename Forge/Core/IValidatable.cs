// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Implemented by types whose values can stop being usable without becoming <see langword="null"/>.
/// </summary>
/// <remarks>
/// <para>Handles are the usual case: an <see cref="Effects.ActiveEffectHandle"/> whose effect has been removed, or an
/// <see cref="Abilities.AbilityHandle"/> whose ability has been revoked, is still a perfectly good reference to an
/// object that no longer refers to anything. A null check says such a value is fine, and everything downstream then
/// acts on something that is not there.</para>
/// <para>Implementing this is what lets generic code - notably the Is Valid resolver - ask the question properly
/// instead of guessing from nullability. Types already exposing an <c>IsValid</c> property satisfy it by declaration
/// alone.</para>
/// </remarks>
public interface IValidatable
{
	/// <summary>
	/// Gets a value indicating whether this value still refers to something usable.
	/// </summary>
	bool IsValid { get; }
}
