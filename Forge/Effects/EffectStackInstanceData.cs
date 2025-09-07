// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Effects;

/// <summary>
/// Data with information about an applied effect stack.
/// </summary>
/// <param name="Owner">The owner of the effect stack.</param>
/// <param name="EffectLevel">The level of the effect stack.</param>
/// <param name="StackCount">The number of stacks in the effect stack.</param>
public readonly record struct EffectStackInstanceData(IForgeEntity? Owner, int EffectLevel, int StackCount);
