namespace NBomber.Contracts

open System
open System.Data
open System.Threading.Tasks
open MessagePack
open Serilog
open Microsoft.Extensions.Configuration
open NBomber.Contracts.Stats

type IResponse =
    abstract StatusCode: string
    abstract IsError: bool
    abstract SizeBytes: int
    abstract LatencyMs: float
    abstract Message: string     

[<CLIMutable>]
[<MessagePackObject>]
type Response<'T> = {
    [<Key 0>] StatusCode: string
    [<Key 1>] IsError: bool
    [<Key 2>] SizeBytes: int
    [<Key 3>] LatencyMs: float
    [<IgnoreMember>] Message: string
    [<IgnoreMember>] Payload: 'T option
} with
    interface IResponse with
        member x.StatusCode = x.StatusCode
        member x.IsError = x.IsError
        member x.LatencyMs = x.LatencyMs
        member x.Message = x.Message
        member x.SizeBytes = x.SizeBytes

type ScenarioOperation =
    | WarmUp = 0
    | Bombing = 1

type ScenarioInfo = {
    /// Gets the current scenario thread id.
    /// You can use it as correlation id.
    ThreadId: string
    ThreadNumber: int
    ScenarioName: string
    ScenarioDuration: TimeSpan
    /// Returns info about current operation type.
    /// It can be: WarmUp or Bombing.
    ScenarioOperation: ScenarioOperation
}

type IBaseContext =
    /// Gets current test info
    abstract TestInfo: TestInfo
    /// Gets current node info
    abstract GetNodeInfo: unit -> NodeInfo    
    /// NBomber's logger
    abstract Logger: ILogger

type IScenarioContext =    
    abstract ScenarioInfo: ScenarioInfo
    abstract Logger: ILogger    
    abstract InvocationNumber: int    
    abstract StopScenario: scenarioName:string * reason:string -> unit
    abstract StopCurrentTest: reason:string -> unit

type IScenarioInitContext =
    /// Gets current test info
    abstract TestInfo: TestInfo
    /// Gets current node info
    abstract NodeInfo: NodeInfo
    /// Gets client settings content from configuration file
    abstract CustomSettings: IConfiguration    
    /// NBomber's logger
    abstract Logger: ILogger

type LoadSimulation =
    /// Injects a given number of scenario copies (threads) with a linear ramp over a given duration.
    /// Every single scenario copy will iterate while the specified duration.
    /// Use it for ramp up and rump down.
    | RampConstant of copies:int * during:TimeSpan
    /// A fixed number of scenario copies (threads) executes as many iterations as possible for a specified amount of time.
    /// Every single scenario copy will iterate while the specified duration.
    /// Use it when you need to run a specific amount of scenario copies (threads) for a certain amount of time.
    | KeepConstant of copies:int * during:TimeSpan
    /// Injects a given number of scenario copies (threads) per 1 sec from the current rate to target rate during a given duration.
    /// Every single scenario copy will run only once.
    | RampPerSec   of rate:int * during:TimeSpan
    /// Injects a given number of scenario copies (threads) per 1 sec during a given duration.
    /// Every single scenario copy will run only once.
    /// Use it when you want to maintain a constant rate of requests without being affected by the performance of the system under test.
    | InjectPerSec of rate:int * during:TimeSpan
    /// Injects a random number of scenario copies (threads) per 1 sec during a given duration.
    /// Every single scenario copy will run only once.
    /// Use it when you want to maintain a random rate of requests without being affected by the performance of the system under test.
    | InjectPerSecRandom of minRate:int * maxRate:int * during:TimeSpan

type ScenarioProps = {
    ScenarioName: string
    Init: (IScenarioInitContext -> Task) option
    Clean: (IScenarioInitContext -> Task) option
    Run: (IScenarioContext -> Task<Response<obj>>) option
    WarmUpDuration: TimeSpan option
    LoadSimulations: LoadSimulation list
    ResetIterationOnFail: bool
    MaxFailCount: int
}

type IReportingSink =
    inherit IDisposable
    abstract SinkName: string
    abstract Init: context:IBaseContext * infraConfig:IConfiguration -> Task
    abstract Start: unit -> Task
    abstract SaveRealtimeStats: stats:ScenarioStats[] -> Task
    abstract SaveFinalStats: stats:NodeStats -> Task
    abstract Stop: unit -> Task

type IWorkerPlugin =
    inherit IDisposable
    abstract PluginName: string
    abstract Init: context:IBaseContext * infraConfig:IConfiguration -> Task
    abstract Start: unit -> Task
    abstract GetStats: stats:NodeStats -> Task<DataSet>
    abstract GetHints: unit -> string[]
    abstract Stop: unit -> Task

type ApplicationType =
    | Process = 0
    | Console = 1