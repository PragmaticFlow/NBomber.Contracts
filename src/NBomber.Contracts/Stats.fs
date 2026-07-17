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

type NodeType =
    | SingleNode
    | Coordinator
    | Agent

type OperationType =
    | None = 0
    | Init = 1
    | WarmUp = 2
    | Bombing = 3
    | Stop = 4     
    | Complete = 5
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
    /// The total number of requests executed.
    [<Key 0>] Count: int
    
    /// The number of requests per second (RPS).
    /// Represents the throughput rate of requests during the test.
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

/// Represents a statistical summary of response latencies for a scenario or step.
/// Used to analyze performance characteristics such as average response time, distribution percentiles.
[<CLIMutable; MessagePackObject>]
type LatencyStats = {
    /// The minimum response time observed during the measurement period, in milliseconds.
    [<Key 0>] MinMs: float
    
    /// The mean (average) response time, in milliseconds.
    [<Key 1>] MeanMs: float
    
    /// The maximum response time observed during the measurement period, in milliseconds.
    [<Key 2>] MaxMs: float
    
    /// The 50th percentile (median) response time, in milliseconds.
    /// Half of the responses were faster than or equal to this value.
    [<Key 3>] Percent50: float
    
    /// The 75th percentile response time, in milliseconds.
    /// 75% of responses were faster than or equal to this value.
    [<Key 4>] Percent75: float
    
    /// The 95th percentile response time, in milliseconds.
    /// 95% of responses were faster than or equal to this value.
    [<Key 5>] Percent95: float
    
    /// The 99th percentile response time, in milliseconds.
    /// 99% of responses were faster than or equal to this value.
    [<Key 6>] Percent99: float
    
    /// The standard deviation of response times, in milliseconds.
    /// Indicates the variability or dispersion of latency values.
    [<Key 7>] StdDev: float
    
    /// Represents the total count of responses grouped by latency ranges.
    /// Useful for understanding how many requests fell into each latency bucket.
    [<Key 8>] LatencyCount: LatencyCount
}

/// Represents statistical information about data transferred (in bytes) during scenario or step execution.
/// Useful for analyzing the size distribution of response/request payloads over time.
[<CLIMutable; MessagePackObject>]
type DataTransferStats = {
    /// The minimum number of bytes transferred in a single operation.
    [<Key 0>] MinBytes: int64
    
    /// The average (mean) number of bytes transferred per operation.
    [<Key 1>] MeanBytes: int64
    
    /// The maximum number of bytes transferred in a single operation.
    [<Key 2>] MaxBytes: int64
    
    /// The 50th percentile (median) of transferred bytes.
    /// Half of the transfers were smaller than or equal to this size.
    [<Key 3>] Percent50: int64
    
    /// The 75th percentile of transferred bytes.
    /// 75% of transfers were smaller than or equal to this size.
    [<Key 4>] Percent75: int64
    
    /// The 95th percentile of transferred bytes.
    /// 95% of transfers were smaller than or equal to this size.
    [<Key 5>] Percent95: int64
    
    /// The 99th percentile of transferred bytes.
    /// 99% of transfers were smaller than or equal to this size.
    [<Key 6>] Percent99: int64
    
    /// The standard deviation of data transferred, showing how much variation exists from the average value.
    [<Key 7>] StdDev: float
    
    /// The total number of bytes transferred during the entire measurement period.
    [<Key 8>] AllBytes: int64

    /// The average data transfer throughput during the measurement period, in bytes per second.
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

    /// Total RPS across all steps for successful (OK) requests only.
    [<Key 12>] TotalOkStepsRPS: float

    /// Total RPS across all steps for failed requests only.
    [<Key 13>] TotalFailStepsRPS: float
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