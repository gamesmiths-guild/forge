// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Effects;

/// <summary>
/// The context of the effect, representing who caused the effect using what.
/// </summary>
/// <param name="Owner">Who triggered the action that caused the effect.</param>
/// <param name="Source">What object or entity actually caused the effect.</param>
public readonly record struct EffectOwnership(IForgeEntity? Owner, IForgeEntity? Source);
