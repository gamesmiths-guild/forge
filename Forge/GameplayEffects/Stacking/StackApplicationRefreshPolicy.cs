// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.GameplayEffects.Stacking;

/// <summary>
/// Defines what happens to the duration of the effect when a new stack is applied.
/// </summary>
public enum StackApplicationRefreshPolicy : byte
{
	/// <summary>
	/// Refreshes the stack uppon successful application.
	/// </summary>
	RefreshOnSuccessfulApplication = 0,

	/// <summary>
	/// Never refreshes the stack.
	/// </summary>
	NeverRefresh = 1,
}
