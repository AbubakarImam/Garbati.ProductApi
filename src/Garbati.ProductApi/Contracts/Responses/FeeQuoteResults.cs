namespace Garbati.ProductApi.Contracts.Responses;

// DESIGN.md Section 0/3: the closed vocabulary for FeeQuoteResponse.FeeResult.
public static class FeeQuoteResults
{
    // The one result every real caller gets today - no FeeRule row is ever seeded in this task.
    public const string NoFeeConfigured = "NoFeeConfigured";

    // Untested-for-real branch (task file: "clearly marked as such") - reachable only if a future task
    // seeds a real FeeRule row, which this task deliberately never does.
    public const string FeeApplied = "FeeApplied";
}
