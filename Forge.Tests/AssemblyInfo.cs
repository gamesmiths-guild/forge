// Copyright © Gamesmiths Guild.

// Validation.Enabled is a global switch, so the test classes that turn it on to assert a ValidationException cannot
// run alongside anything else: they would either validate unrelated effect data that is deliberately invalid, or have
// their own flag cleared by another class finishing first. The whole suite runs in well under a second, so
// serializing the collections costs nothing and removes that race entirely.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
