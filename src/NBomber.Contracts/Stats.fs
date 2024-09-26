namespace NBomber.Contracts.Stats

#nowarn "0044"

open System
open System.Data

open FSharp.Json
open MessagePack

open NBomber.Contracts.Internal.Serialization.JsonTransforms

type ReportFormat =
    | Txt = 0
    | Html = 1
    | Csv = 2
    | Md = 3

[<CLIMutable>]
[<MessagePackObject>]
type TestInfo = {
    [<Key 0>] SessionId: string
    [<Key 1>] TestSuite: string
    [<Key 2>] TestName: string
    [<Key 3>] ClusterId: string
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

[<CLIMutable>]
[<MessagePackObject>]
type NodeInfo = {
    [<Key 0>] MachineName: string
    [<Key 1>] NodeType: NodeType
    [<Key 2>] CurrentOperation: OperationType
    [<Key 3>] OS: string
    [<Key 4>] DotNetVersion: string
    [<Key 5>] Processor: string
    [<Key 6>] CoresCount: int
    [<Key 7>] NBomberVersion: string
}
with
    [<CompiledName("Empty")>]
    static member empty = {
        MachineName = ""; NodeType = NodeType.SingleNode; CurrentOperation = OperationType.None
        OS = ""; DotNetVersion = ""; Processor = ""; CoresCount = 0; NBomberVersion = ""
    }

[<CLIMutable>]
[<MessagePackObject>]
type StatusCodeStats = {
    [<Key 0>] StatusCode: string
    [<Key 1>] IsError: bool
    [<Key 2>] Message: string
    [<Key 3>] Count: int
    [<Key 4>] mutable Percent: int
}

[<CLIMutable>]
[<MessagePackObject>]
type RequestStats = {
    [<Key 0>] Count: int
    [<Key 1>] RPS: float
    [<Key 2>] mutable Percent: int
}

[<CLIMutable>]
[<MessagePackObject>]
type LatencyCount = {
    [<Key 0>] LessOrEq800: int
    [<Key 1>] More800Less1200: int
    [<Key 2>] MoreOrEq1200: int
}

[<CLIMutable>]
[<MessagePackObject>]
type LatencyStats = {
    [<Key 0>] MinMs: float
    [<Key 1>] MeanMs: float
    [<Key 2>] MaxMs: float
    [<Key 3>] Percent50: float
    [<Key 4>] Percent75: float
    [<Key 5>] Percent95: float
    [<Key 6>] Percent99: float
    [<Key 7>] StdDev: float
    [<Key 8>] LatencyCount: LatencyCount
}

[<CLIMutable>]
[<MessagePackObject>]
type DataTransferStats = {
    [<Key 0>] MinBytes: int64
    [<Key 1>] MeanBytes: int64
    [<Key 2>] MaxBytes: int64
    [<Key 3>] Percent50: int64
    [<Key 4>] Percent75: int64
    [<Key 5>] Percent95: int64
    [<Key 6>] Percent99: int64
    [<Key 7>] StdDev: float
    [<Key 8>] AllBytes: int64
}

[<CLIMutable>]
[<MessagePackObject>]
type MeasurementStats = {
    [<Key 0>] Request: RequestStats
    [<Key 1>] Latency: LatencyStats
    [<Key 2>] DataTransfer: DataTransferStats
    [<Key 3>] StatusCodes: StatusCodeStats[]
}

[<CLIMutable>]
[<MessagePackObject>]
type StepStats = {
    [<Key 0>] StepName: string
    [<Key 1>] Ok: MeasurementStats
    [<Key 2>] Fail: MeasurementStats 
}

[<CLIMutable>]
[<MessagePackObject>]
type LoadSimulationStats = {
    [<Key 0>] SimulationName: string
    [<Key 1>] Value: int
}

[<CLIMutable>]
[<MessagePackObject>]
type ScenarioStats = {    
    [<Key 0>] ScenarioName: string     
    [<Key 1>] Ok: MeasurementStats
    [<Key 2>] Fail: MeasurementStats
    [<Key 3>] StepStats: StepStats[]    
    [<Key 4>] LoadSimulationStats: LoadSimulationStats
    [<Key 5>] CurrentOperation: OperationType
    [<Key 6>] AllRequestCount: int
    [<Key 7>] AllOkCount: int
    [<Key 8>] AllFailCount: int
    [<Key 9>] AllBytes: int64    
    [<Key 10>] Duration: TimeSpan
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

type ReportFile = {
    FilePath: string
    ReportFormat: ReportFormat
    ReportContent: string
}

type ThresholdResult = {
    ScenarioName: string
    StepName: string
    CheckExpression: string
    ExceptionMsg: string
    ErrorCount: int
    IsFailed: bool
}

[<CLIMutable>]
[<MessagePackObject>]
type NodeStats = {
    [<Key 0>] ScenarioStats: ScenarioStats[]
    [<IgnoreMember>] Thresholds: ThresholdResult[]
    [<IgnoreMember>] [<JsonField(Transform=typeof<DateTableTransform>)>] PluginStats: DataSet[]
    [<Key 1>] NodeInfo: NodeInfo
    [<Key 2>] TestInfo: TestInfo
    [<IgnoreMember>] ReportFiles: ReportFile[]
    [<Key 3>] AllRequestCount: int
    [<Key 4>] AllOkCount: int
    [<Key 5>] AllFailCount: int
    [<Key 6>] AllBytes: int64
    [<Key 7>] Duration: TimeSpan
}
with
    [<Obsolete("Please use extension method 'Get(name)' instead. Example: data.ScenarioStats.Get(name)")>]
    member this.GetScenarioStats(scenarioName: string) = NodeStats.getScenarioStats scenarioName this
    
    [<Obsolete("Please use extension method 'Find(name)' instead. Example: data.ScenarioStats.Find(name)")>]
    member this.FindScenarioStats(scenarioName: string) = NodeStats.findScenarioStats scenarioName this

    [<CompiledName("Empty")>]
    static member empty = {        
        ScenarioStats = Array.empty
        Thresholds = Array.empty
        PluginStats = Array.empty
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
    
[<CLIMutable>]        
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