namespace Garbati.ProductApi.Contracts.Responses;

// DESIGN.md Section 3, endpoint 5 / Section 0. FeeResult is this service's own naming (task file: "your
// own naming, document it in the PR") for the one honest answer every real caller gets today -
// FeeQuoteResults.cs (same folder) names the exact values. FeeAmount/FeeType are populated only when
// FeeResult is FeeApplied - the branch that cannot currently happen for real (no FeeRule row is ever
// seeded, see docs/DESIGN.md Section 0), kept here only so the response shape doesn't have to change
// the day a future task starts seeding real fee rules.
public record FeeQuoteResponse(string FeeResult, decimal? FeeAmount, string? FeeType);
