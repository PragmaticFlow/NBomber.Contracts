namespace NBomber.Contracts.Stats

#nowarn "0044"
open System
open System.Collections.Generic
open MessagePack
open NBomber.Contracts.Metrics

type ReportFormat =
    | Txt = 0
    | Html = 1
    | Csv = 2
    | Md = 3

/// Represents metadata about the currently executing test session.
/// This information is useful for logging, reporting, and tracking test runs across environments or clusters.
[<CLIMutable; MessagePackObject>]
type TestInfo = {
    /// A unique identifier for the current test session.
    /// It is automatically generated at the start of the test run.
    /// Also, it can be set by client via API or CLI arguments.
    [<Key 0>] SessionId: string
    
    /// The name of the test suite to which this test belongs.
    /// Useful for grouping related tests together in reports or dashboards.
    [<Key 1>] TestSuite: string
    
    /// The name of the individual test case.
    /// Typically used to identify the purpose or scope of the test.
    [<Key 2>] TestName: string
    
    /// A unique identifier for the cluster running the test.
    /// Acts as a namespace or folder or topic name that allows NBomber agents to discover each other 
    /// and form a virtual cluster during distributed test execution.
    /// It can be set via JSON Config, API or CLI arguments.  
    [<Key 3>] ClusterId: string
    
    /// The UTC timestamp indicating when the test session was created.
    [<Key 4>] Created: DateTime
}
with
    [<CompiledName("Empty")>]
    static member empty = { SessionId = ""; TestSuite = ""; TestName = ""; ClusterId = ""; Created = DateTime.MinValue }

/// Represents the role that an NBomber node plays during the test execution.
/// In a single-node test run, the node always has the SingleNode role.
/// In a distributed (cluster) test run, nodes are split into Coordinator and Agent roles.
type NodeType =
    /// The node runs the whole test on its own, without forming a cluster.
    /// This is the default role for a regular, non-distributed test run.
    | SingleNode

    /// The node that orchestrates a distributed test run.
    /// It starts the test session, coordinates all agents within the cluster,
    /// and gathers their statistics to build the final reports.
    | Coordinator

    /// The node that joins a cluster and executes the load assigned to it by the coordinator.
    /// It sends its statistics to the coordinator during and after the test run.
    | Agent

/// Represents the operation that an NBomber node is currently executing.
/// It reflects the lifecycle of a test session and is useful for diagnostics,
/// real-time monitoring, and reporting.
type OperationType =
    /// The node is idle: no operation is being executed at the moment.
    | None = 0

    /// The node is initializing the test session: loading configuration, initializing scenarios and plugins.
    | Init = 1

    /// The node is running the warm-up phase to let the system under test and the scenarios reach a steady state.
    /// Statistics collected during the warm-up are not included in the final reports.
    | WarmUp = 2

    /// The node is running the actual load test and collecting statistics.
    | Bombing = 3

    /// The node is stopping the test session: stopping scenarios and disposing resources.
    /// It can also indicate that the node was stopped earlier than planned,
    /// usually by an explicit user request (for example, via API or by cancelling the test run).
    | Stop = 4

    /// The node has finished the test session and all reports are built.
    | Complete = 5

    /// The node has stopped because of an unhandled error during the test session.
    | Error = 6

/// Represents metadata about the NBomber node that is executing the test.
/// Contains system, environment, and execution details relevant for diagnostics and reporting.
[<CLIMutable; MessagePackObject>]
type NodeInfo = {
    /// The name of the machine on which the current NBomber node is running.
    [<Key 0>] MachineName: string
    
    /// The role of the NBomber node (e.g., Coordinator, Agent, or SingleNode).
    [<Key 1>] NodeType: NodeType
    
    /// The current operation being performed by the node (e.g., Bombing, Complete).
    [<Key 2>] CurrentOperation: OperationType
    
    /// The operating system of the machine (e.g., Windows, Linux).
    [<Key 3>] OS: string
    
    /// The version of the .NET runtime installed on the node.
    [<Key 4>] DotNetVersion: string
    
    /// Information about the machine's processor (e.g., model name).
    [<Key 5>] Processor: string
    
    /// The number of CPU cores available on the machine.
    [<Key 6>] CoresCount: int
    
    /// The version of NBomber being used by this node.
    [<Key 7>] NBomberVersion: string
}
with
    [<CompiledName("Empty")>]
    static member empty = {
        MachineName = ""; NodeType = NodeType.SingleNode; CurrentOperation = OperationType.None
        OS = ""; DotNetVersion = ""; Processor = ""; CoresCount = 0; NBomberVersion = ""
    }

/// Represents statistics for a specific status code encountered during scenario execution.
/// Helps in understanding response code distribution.
[<CLIMutable; MessagePackObject>]
type StatusCodeStats = {
    /// The HTTP status code as a string (e.g., "200", "404", "500").
    [<Key 0>] StatusCode: string
    
    /// Indicates whether this status code is considered an error.
    [<Key 1>] IsError: bool
    
    /// A descriptive message or reason phrase associated with the status code.
    [<Key 2>] Message: string
    
    /// The total number of responses that returned this status code.
    [<Key 3>] Count: int
    
    /// The percentage of this status code relative to the total number of status codes.    
    [<Key 4>] mutable Percent: int
}

/// Represents statistics about the number and rate of requests during scenario execution.
/// Useful for analyzing throughput and request distribution across steps or scenarios.
[<CLIMutable; MessagePackObject>]
type RequestStats = {
    /// <summary>
    /// The number of requests executed.
    /// For <c>StepStats</c>, this is the count of this step only.
    /// For <c>ScenarioStats</c>, this is the total count across all steps of the scenario.
    /// </summary>
    [<Key 0>] Count: int

    /// <summary>
    /// The number of requests per second (RPS).
    /// Represents the throughput rate of requests during the test.
    /// For <c>StepStats</c>, this is the RPS of this step only.
    /// For <c>ScenarioStats</c>, this is the total RPS across all steps of the scenario.
    /// </summary>
    [<Key 1>] RPS: float
    
    /// The percentage of this request count relative to the total number of requests.    
    [<Key 2>] mutable Percent: int
}

/// Represents the count of requests grouped by latency ranges in milliseconds.
/// Useful for understanding the distribution of request latencies across predefined buckets.
[<CLIMutable; MessagePackObject>]
type LatencyCount = {
    /// The number of requests with latency less than or equal to 800 milliseconds.
    [<Key 0>] LessOrEq800: int
    
    /// The number of requests with latency greater than 800 milliseconds but less than 1200 milliseconds.
    [<Key 1>] More800Less1200: int
    
    /// The number of requests with latency greater than or equal to 1200 milliseconds.
    [<Key 2>] MoreOrEq1200: int
}

/// <summary>
/// Represents a statistical summary of response latencies for a scenario or step.
/// Used to analyze performance characteristics such as average response time, distribution percentiles.
/// For <c>StepStats</c>, all values are calculated from the latencies of this step only.
/// For <c>ScenarioStats</c>, all values are calculated from the latencies of all steps of the scenario combined.
/// </summary>
[<CLIMutable; MessagePackObject>]
type LatencyStats = {
    /// <summary>
    /// The minimum response time observed during the measurement period, in milliseconds.
    /// For <c>StepStats</c>, this is the minimum of this step only.
    /// For <c>ScenarioStats</c>, this is the minimum across all steps of the scenario.
    /// </summary>
    [<Key 0>] MinMs: float

    /// <summary>
    /// The mean (average) response time, in milliseconds.
    /// For <c>StepStats</c>, this is the mean of this step only.
    /// For <c>ScenarioStats</c>, this is the mean across all steps of the scenario.
    /// </summary>
    [<Key 1>] MeanMs: float

    /// <summary>
    /// The maximum response time observed during the measurement period, in milliseconds.
    /// For <c>StepStats</c>, this is the maximum of this step only.
    /// For <c>ScenarioStats</c>, this is the maximum across all steps of the scenario.
    /// </summary>
    [<Key 2>] MaxMs: float

    /// <summary>
    /// The 50th percentile (median) response time, in milliseconds.
    /// Half of the responses were faster than or equal to this value.
    /// For <c>StepStats</c>, this is calculated from this step only.
    /// For <c>ScenarioStats</c>, this is calculated across all steps of the scenario.
    /// </summary>
    [<Key 3>] Percent50: float

    /// <summary>
    /// The 75th percentile response time, in milliseconds.
    /// 75% of responses were faster than or equal to this value.
    /// For <c>StepStats</c>, this is calculated from this step only.
    /// For <c>ScenarioStats</c>, this is calculated across all steps of the scenario.
    /// </summary>
    [<Key 4>] Percent75: float

    /// <summary>
    /// The 95th percentile response time, in milliseconds.
    /// 95% of responses were faster than or equal to this value.
    /// For <c>StepStats</c>, this is calculated from this step only.
    /// For <c>ScenarioStats</c>, this is calculated across all steps of the scenario.
    /// </summary>
    [<Key 5>] Percent95: float

    /// <summary>
    /// The 99th percentile response time, in milliseconds.
    /// 99% of responses were faster than or equal to this value.
    /// For <c>StepStats</c>, this is calculated from this step only.
    /// For <c>ScenarioStats</c>, this is calculated across all steps of the scenario.
    /// </summary>
    [<Key 6>] Percent99: float

    /// <summary>
    /// The standard deviation of response times, in milliseconds.
    /// Indicates the variability or dispersion of latency values.
    /// For <c>StepStats</c>, this is calculated from this step only.
    /// For <c>ScenarioStats</c>, this is calculated across all steps of the scenario.
    /// </summary>
    [<Key 7>] StdDev: float

    /// <summary>
    /// Represents the total count of responses grouped by latency ranges.
    /// Useful for understanding how many requests fell into each latency bucket.
    /// For <c>StepStats</c>, these are the counts of this step only.
    /// For <c>ScenarioStats</c>, these are the total counts across all steps of the scenario.
    /// </summary>
    [<Key 8>] LatencyCount: LatencyCount
}

/// <summary>
/// Represents statistical information about data transferred (in bytes) during scenario or step execution.
/// Useful for analyzing the size distribution of response/request payloads over time.
/// For <c>StepStats</c>, all values are calculated from the transfers of this step only.
/// For <c>ScenarioStats</c>, all values are calculated from the transfers of all steps of the scenario combined.
/// </summary>
[<CLIMutable; MessagePackObject>]
type DataTransferStats = {
    /// <summary>
    /// The minimum number of bytes transferred in a single operation.
    /// For <c>StepStats</c>, this is the minimum of this step only.
    /// For <c>ScenarioStats</c>, this is the minimum across all steps of the scenario.
    /// </summary>
    [<Key 0>] MinBytes: int64

    /// <summary>
    /// The average (mean) number of bytes transferred per operation.
    /// For <c>StepStats</c>, this is the mean of this step only.
    /// For <c>ScenarioStats</c>, this is the mean across all steps of the scenario.
    /// </summary>
    [<Key 1>] MeanBytes: int64

    /// <summary>
    /// The maximum number of bytes transferred in a single operation.
    /// For <c>StepStats</c>, this is the maximum of this step only.
    /// For <c>ScenarioStats</c>, this is the maximum across all steps of the scenario.
    /// </summary>
    [<Key 2>] MaxBytes: int64

    /// <summary>
    /// The 50th percentile (median) of transferred bytes.
    /// Half of the transfers were smaller than or equal to this size.
    /// For <c>StepStats</c>, this is calculated from this step only.
    /// For <c>ScenarioStats</c>, this is calculated across all steps of the scenario.
    /// </summary>
    [<Key 3>] Percent50: int64

    /// <summary>
    /// The 75th percentile of transferred bytes.
    /// 75% of transfers were smaller than or equal to this size.
    /// For <c>StepStats</c>, this is calculated from this step only.
    /// For <c>ScenarioStats</c>, this is calculated across all steps of the scenario.
    /// </summary>
    [<Key 4>] Percent75: int64

    /// <summary>
    /// The 95th percentile of transferred bytes.
    /// 95% of transfers were smaller than or equal to this size.
    /// For <c>StepStats</c>, this is calculated from this step only.
    /// For <c>ScenarioStats</c>, this is calculated across all steps of the scenario.
    /// </summary>
    [<Key 5>] Percent95: int64

    /// <summary>
    /// The 99th percentile of transferred bytes.
    /// 99% of transfers were smaller than or equal to this size.
    /// For <c>StepStats</c>, this is calculated from this step only.
    /// For <c>ScenarioStats</c>, this is calculated across all steps of the scenario.
    /// </summary>
    [<Key 6>] Percent99: int64

    /// <summary>
    /// The standard deviation of data transferred, showing how much variation exists from the average value.
    /// For <c>StepStats</c>, this is calculated from this step only.
    /// For <c>ScenarioStats</c>, this is calculated across all steps of the scenario.
    /// </summary>
    [<Key 7>] StdDev: float

    /// <summary>
    /// The total number of bytes transferred during the entire measurement period.
    /// For <c>StepStats</c>, this is the total of this step only.
    /// For <c>ScenarioStats</c>, this is the total across all steps of the scenario.
    /// </summary>
    [<Key 8>] AllBytes: int64

    /// <summary>
    /// The average data transfer throughput during the measurement period, in bytes per second.
    /// For <c>StepStats</c>, this is the throughput of this step only.
    /// For <c>ScenarioStats</c>, this is the total throughput across all steps of the scenario.
    /// </summary>
    [<Key 9>] BytesPerSecond: float
}

[<CLIMutable; MessagePackObject>]
type MeasurementStats = {
    [<Key 0>] Request: RequestStats
    [<Key 1>] Latency: LatencyStats
    [<Key 2>] DataTransfer: DataTransferStats
    [<Key 3>] StatusCodes: StatusCodeStats[]
}

/// Represents statistics for a step within a scenario.
/// Contains measurements for successful and failed executions of the step.
[<CLIMutable; MessagePackObject>]
type StepStats = {
    /// The name of the step.
    [<Key 0>] StepName: string
    
    /// Measurement statistics for successful (OK) executions of this step.
    [<Key 1>] Ok: MeasurementStats
    
    /// Measurement statistics for failed executions of this step.
    [<Key 2>] Fail: MeasurementStats
    
    /// Index used for sorting steps in reports.
    [<Key 3>] SortIndex: int
}

[<CLIMutable; MessagePackObject>]
type LoadSimulationStats = {
    [<Key 0>] SimulationName: string
    [<Key 1>] Value: int
}

/// Represents aggregated statistics for a single scenario during a test session.
/// Includes measurements of successful and failed requests, step-level stats, load simulation info, and overall request counts.
[<CLIMutable; MessagePackObject>]
type ScenarioStats = {
    /// The name of the scenario.
    [<Key 0>] ScenarioName: string
    
    /// Measurement statistics for successful (OK) requests within the scenario.
    [<Key 1>] Ok: MeasurementStats
    
    /// Measurement statistics for failed requests within the scenario.
    [<Key 2>] Fail: MeasurementStats
    
    /// An array of statistics for individual steps executed in the scenario.
    [<Key 3>] StepStats: StepStats[]
    
    /// Statistics related to the load simulation applied to the scenario.
    [<Key 4>] LoadSimulationStats: LoadSimulationStats
    
    /// The current operation type for this scenario (e.g., Bombing, Complete).
    [<Key 5>] CurrentOperation: OperationType
    
    /// Total number of requests made during the scenario execution.    
    [<Obsolete("This property is obsolete. Please use ScenarioStats and StepStats to retrieve this data instead.")>]
    [<Key 6>] AllRequestCount: int
    
    /// Total number of successful (OK) requests.    
    [<Obsolete("This property is obsolete. Please use ScenarioStats and StepStats to retrieve this data instead.")>]
    [<Key 7>] AllOkCount: int
    
    /// Total number of failed requests.
    [<Obsolete("This property is obsolete. Please use ScenarioStats and StepStats to retrieve this data instead.")>]
    [<Key 8>] AllFailCount: int
    
    /// Total bytes transferred during the scenario execution.
    [<Obsolete("This property is obsolete. Please use ScenarioStats and StepStats to retrieve this data instead.")>]
    [<Key 9>] AllBytes: int64
    
    /// Duration of the scenario execution.
    [<Key 10>] Duration: TimeSpan

    /// Index used for sorting scenarios in reports.
    [<Key 11>] SortIndex: int
}
with
    [<Obsolete("Please use extension method 'Get(name)' instead. Example: data.StepStats.Get(name)")>]
    member this.GetStepStats(stepName: string) = ScenarioStats.getStepStats stepName this
    
    [<Obsolete("Please use extension method 'Find(name)' instead. Example: data.StepStats.Find(name)")>]
    member this.FindStepStats(stepName: string) = ScenarioStats.findStepStats stepName this

    [<Obsolete("Please use extension method 'Get(name)' instead. Example: data.StepStats.Get(name)")>]
    [<CompiledName("GetStepStats")>]
    static member getStepStats (stepName: string) (scenarioStats: ScenarioStats) =
        scenarioStats.StepStats
        |> Array.find(fun x -> x.StepName = stepName)
        
    [<Obsolete("Please use extension method 'Find(name)' instead. Example: data.StepStats.Find(name)")>]        
    [<CompiledName("FindStepStats")>]
    static member findStepStats (stepName: string) (scenarioStats: ScenarioStats) =
        scenarioStats.StepStats
        |> Array.tryFind(fun x -> x.StepName = stepName)
        |> Option.defaultValue(Unchecked.defaultof<_>)        

[<CLIMutable; MessagePackObject>]
type ReportFile = {
    [<Key 0>] FilePath: string
    [<Key 1>] ReportFormat: ReportFormat
    [<Key 2>] ReportContent: string
}

[<CLIMutable; MessagePackObject>]
type ThresholdResult = {
    [<Key 0>] ScenarioName: string
    [<Key 1>] StepName: string
    [<Key 2>] CheckExpression: string
    [<Key 3>] ExceptionMsg: string
    [<Key 4>] ErrorCount: int
    [<Key 5>] IsFailed: bool
}

[<CLIMutable; MessagePackObject>]
type PluginDataTable = {
    [<Key 0>] TableName: string
    [<Key 1>] Rows: ResizeArray<Dictionary<string,obj>>
}
with
    [<CompiledName("Create")>]
    static member create tableName = { TableName = tableName; Rows = ResizeArray() }

[<CLIMutable; MessagePackObject>]
type PluginData = {
    [<Key 0>] PluginName: string
    [<Key 1>] Tables: ResizeArray<PluginDataTable>
    [<Key 2>] Hints: ResizeArray<string>
}
with
    [<CompiledName("Create")>]
    static member create pluginName = { PluginName = pluginName; Tables = ResizeArray(); Hints = ResizeArray(); }

/// Represents aggregated statistics collected from a single node during a test session.
/// Includes scenario results, metrics, thresholds, node details, and additional reporting data.
[<CLIMutable; MessagePackObject>]
type NodeStats = {
    /// An array of statistics for each scenario executed on the node.
    [<Key 0>] ScenarioStats: ScenarioStats[]
    
    /// Aggregated custom and built-in metrics collected during the test.
    [<Key 1>] Metrics: MetricStats
    
    /// Results of threshold checks applied to scenario or test metrics.
    [<Key 2>] Thresholds: ThresholdResult[]
    
    /// Information about the node where the test was executed (machine name, OS, CPU, etc.).
    [<Key 3>] NodeInfo: NodeInfo
    
    /// Metadata about the test session (test name, suite, session id, creation time).
    [<Key 4>] TestInfo: TestInfo
    
    /// Total number of requests executed by the node.
    [<Obsolete("This property is obsolete. Please use ScenarioStats and StepStats to retrieve this data instead.")>]
    [<Key 5>] AllRequestCount: int
    
    /// Total number of successful (OK) requests.
    [<Obsolete("This property is obsolete. Please use ScenarioStats and StepStats to retrieve this data instead.")>]
    [<Key 6>] AllOkCount: int
    
    /// Total number of failed requests.
    [<Obsolete("This property is obsolete. Please use ScenarioStats and StepStats to retrieve this data instead.")>]
    [<Key 7>] AllFailCount: int
    
    /// Total number of bytes transferred during the test.    
    [<Obsolete("This property is obsolete. Please use ScenarioStats and StepStats to retrieve this data instead.")>]
    [<Key 8>] AllBytes: int64
    
    /// Duration of the test execution on this node.
    [<Key 9>] Duration: TimeSpan
    
    /// Data collected from plugins used during the test.
    [<Key 10>] PluginsData: PluginData[]

    /// References to generated report files related to this node’s test execution.
    [<Key 11>] ReportFiles: ReportFile[]
}
with
    [<Obsolete("Please use extension method 'Get(name)' instead. Example: data.ScenarioStats.Get(name)")>]
    member this.GetScenarioStats(scenarioName: string) = NodeStats.getScenarioStats scenarioName this
    
    [<Obsolete("Please use extension method 'Find(name)' instead. Example: data.ScenarioStats.Find(name)")>]
    member this.FindScenarioStats(scenarioName: string) = NodeStats.findScenarioStats scenarioName this

    [<CompiledName("Empty")>]
    static member empty = {        
        ScenarioStats = Array.empty
        Metrics = MetricStats.empty
        Thresholds = Array.empty
        PluginsData = Array.empty
        NodeInfo = NodeInfo.empty; TestInfo = TestInfo.empty; ReportFiles = Array.empty
        AllRequestCount = 0; AllOkCount = 0; AllFailCount = 0; AllBytes = 0
        Duration = TimeSpan.Zero
    }

    [<Obsolete("Please use extension method 'Get(name)' instead. Example: data.ScenarioStats.Get(name)")>]
    [<CompiledName("GetScenarioStats")>]
    static member getScenarioStats (scenarioName: string) (nodeStats: NodeStats) =
        nodeStats.ScenarioStats
        |> Array.find(fun x -> x.ScenarioName = scenarioName)        
        
    [<Obsolete("Please use extension method 'Find(name)' instead. Example: data.ScenarioStats.Find(name)")>]        
    [<CompiledName("FindScenarioStats")>]
    static member findScenarioStats (scenarioName: string) (nodeStats: NodeStats) =
        nodeStats.ScenarioStats
        |> Array.tryFind(fun x -> x.ScenarioName = scenarioName)
        |> Option.defaultValue(Unchecked.defaultof<_>)        
    
type ReportData = {
    ScenarioStats: ScenarioStats[]
}
with
    [<Obsolete("Please use extension method 'Get(name)' instead. Example: data.ScenarioStats.Get(name)")>]
    member this.GetScenarioStats(scenarioName: string) =
        this.ScenarioStats
        |> Array.find(fun x -> x.ScenarioName = scenarioName)        
        
    [<Obsolete("Please use extension method 'Find(name)' instead. Example: data.ScenarioStats.Find(name)")>]        
    member this.FindScenarioStats(scenarioName: string) =
        this.ScenarioStats
        |> Array.tryFind(fun x -> x.ScenarioName = scenarioName)
        |> Option.defaultValue(Unchecked.defaultof<_>)        
        
    [<CompiledName("Create")>]        
    static member create (scenarioStats) = {
        ScenarioStats = scenarioStats
    }        