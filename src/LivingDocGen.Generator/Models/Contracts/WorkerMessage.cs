using System.Collections.Generic;

namespace LivingDocGen.Generator.Models.Contracts;

/// <summary>
/// Message contract for communication between the main thread and the search worker.
/// Defines all message types, protocol versioning, and diagnostic fields.
/// </summary>
public class WorkerMessage
{
    /// <summary>
    /// Protocol version for worker message compatibility.
    /// </summary>
    public const string ProtocolVersion = "1.1";

    /// <summary>
    /// Message type determining the payload interpretation.
    /// </summary>
    public WorkerMessageType Type { get; set; }

    /// <summary>
    /// Protocol version string for compatibility checks.
    /// </summary>
    public string Protocol { get; set; } = ProtocolVersion;

    /// <summary>
    /// Unique identifier for this request/response pair.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Monotonically increasing query sequence number.
    /// Used to discard stale/out-of-order results on the main thread.
    /// </summary>
    public long QuerySeq { get; set; }

    /// <summary>
    /// UTC timestamp in milliseconds when this message was created.
    /// </summary>
    public long Timestamp { get; set; }

    /// <summary>
    /// Message payload (varies by message type).
    /// </summary>
    public WorkerPayload Payload { get; set; } = new WorkerPayload();
}

/// <summary>
/// Supported message types for the worker protocol.
/// </summary>
public enum WorkerMessageType
{
    /// <summary>Initialize the search index in the worker.</summary>
    InitIndex,

    /// <summary>Worker is ready to accept queries.</summary>
    Ready,

    /// <summary>Execute a search/filter query.</summary>
    Query,

    /// <summary>Cancel an in-flight query.</summary>
    QueryCancel,

    /// <summary>Query result from worker to main thread.</summary>
    Result,

    /// <summary>Error report from worker.</summary>
    Error,

    /// <summary>Diagnostic metrics from worker.</summary>
    Metrics
}

/// <summary>
/// Payload for worker messages. Fields are populated based on the message type.
/// </summary>
public class WorkerPayload
{
    // --- Query fields (Type = Query) ---

    /// <summary>Free-text search query string.</summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>Status filter: "all", "passed", "failed", "skipped", "untested".</summary>
    public string Status { get; set; } = "all";

    /// <summary>Tag filter: only scenarios matching ALL listed tags are returned.</summary>
    public List<string> Tags { get; set; } = new List<string>();

    // --- Result fields (Type = Result) ---

    /// <summary>Matching scenario ordinals.</summary>
    public List<int> ScenarioOrdinals { get; set; } = new List<int>();

    /// <summary>Matching feature ordinals (derived from scenario results).</summary>
    public List<int> FeatureOrdinals { get; set; } = new List<int>();

    /// <summary>Total count of matching scenarios.</summary>
    public int Count { get; set; }

    // --- Error fields (Type = Error) ---

    /// <summary>Error description message.</summary>
    public string Message { get; set; } = string.Empty;

    // --- Metrics/Stats fields ---

    /// <summary>Query evaluation statistics.</summary>
    public WorkerQueryStats Stats { get; set; }
}

/// <summary>
/// Diagnostic statistics for a single query evaluation.
/// </summary>
public class WorkerQueryStats
{
    /// <summary>Total query evaluation time in milliseconds.</summary>
    public double EvalMs { get; set; }

    /// <summary>Number of candidate scenarios evaluated before final filtering.</summary>
    public int CandidateCount { get; set; }

    /// <summary>Number of index lookups performed.</summary>
    public int IndexLookups { get; set; }
}
