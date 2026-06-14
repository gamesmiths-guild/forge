// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;

namespace Gamesmiths.Forge.Tests.Helpers;

/// <summary>
/// A test cue custom-parameters provider that returns a fixed parameter bag, so cue-node tests can assert the
/// dictionary reaches the cue handler.
/// </summary>
internal sealed class TestCueCustomParametersProvider : CueCustomParametersProvider
{
	/// <summary>
	/// The value written for <see cref="PowerKey"/>.
	/// </summary>
	public const int PowerValue = 5;

	/// <summary>
	/// The single key the provider writes into the bag.
	/// </summary>
	public static readonly StringKey PowerKey = new("power");

	/// <inheritdoc/>
	public override Dictionary<StringKey, object> CreateCustomParameters(
		GraphContext graphContext,
		CueCustomParameterInputs inputs)
	{
		return new Dictionary<StringKey, object> { { PowerKey, PowerValue } };
	}
}
