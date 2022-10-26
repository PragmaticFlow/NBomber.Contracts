namespace NBomber.Contracts

open System
open System.Data
open System.Runtime.InteropServices
open System.Threading
open System.Threading.Tasks

open Serilog
open Microsoft.Extensions.Configuration

open NBomber.Contracts.Stats

type IResponse =
    abstract StatusCode: string
    abstract IsError: bool
    abstract SizeBytes: int
    abstract LatencyMs: float
    abstract Message: string     

type Response<'T> = {
    StatusCode: string
    IsError: bool
    SizeBytes: int
    LatencyMs: float
    Message: string
    Payload: 'T option
} with
    interface IResponse with
        member x.StatusCode = x.StatusCode
        member x.IsError = x.IsError
        member x.LatencyMs = x.LatencyMs
        member x.Message = x.Message
        member x.SizeBytes = x.SizeBytes
        
type Response =        
        
    [<CompiledName("Ok")>]
    static member ok([<Optional;DefaultParameterValue("")>] statusCode: string,
                     [<Optional;DefaultParameterValue(0)>] sizeBytes: int,
                     [<Optional;DefaultParameterValue(0.0)>] latencyMs: float,
                     [<Optional;DefaultParameterValue("")>] message: string): Response<obj> =
        
        { StatusCode = statusCode
          IsError = false
          SizeBytes = sizeBytes
          Message = if isNull message then String.Empty else message
          LatencyMs = latencyMs
          Payload = None }
    
    [<CompiledName("Ok")>]
    static member ok<'T>(payload: 'T,
                         [<Optional;DefaultParameterValue("")>] statusCode: string,
                         [<Optional;DefaultParameterValue(0)>] sizeBytes: int,
                         [<Optional;DefaultParameterValue(0.0)>] latencyMs: float,
                         [<Optional;DefaultParameterValue("")>] message: string): Response<'T> =

        { StatusCode = statusCode
          IsError = false
          SizeBytes = sizeBytes
          Message = if isNull message then String.Empty else message
          LatencyMs = latencyMs
          Payload = Some payload }
        
    [<CompiledName("Fail")>]
    static member fail([<Optional;DefaultParameterValue("")>] error: string,
                       [<Optional;DefaultParameterValue("")>] statusCode: string,
                       [<Optional;DefaultParameterValue(0)>] sizeBytes: int,
                       [<Optional;DefaultParameterValue(0.0)>] latencyMs: float): Response<obj> =

        { StatusCode = statusCode
          IsError = true
          SizeBytes = sizeBytes
          Message = if isNull error then String.Empty else error
          LatencyMs = latencyMs
          Payload = None }
        
    [<CompiledName("Fail")>]
    static member fail(error: Exception,
                       [<Optional;DefaultParameterValue("")>] statusCode: string,
                       [<Optional;DefaultParameterValue(0)>] sizeBytes: int,
                       [<Optional;DefaultParameterValue(0.0)>] latencyMs: float): Response<obj> =        
    
        { StatusCode = statusCode
          IsError = true
          SizeBytes = sizeBytes
          Message = if isNull error then String.Empty else error.Message
          LatencyMs = latencyMs
          Payload = None }

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
    /// Cancellation token is a standard mechanics for canceling long-running operations.
    /// Cancellation token should be used to help NBomber stop scenarios when the test is finished.
    abstract CancellationToken: CancellationToken
    /// NBomber's logger
    abstract Logger: ILogger

type IScenarioContext =    
    abstract ScenarioInfo: ScenarioInfo
    abstract Logger: ILogger
    abstract CancellationToken: CancellationToken
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
    /// Cancellation token is a standard mechanics for canceling long-running operations.
    /// Cancellation token should be used to help NBomber stop scenarios when the test is finished.
    abstract CancellationToken: CancellationToken
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
        
module internal ResponseInternal =
    
    let emptyFail<'T> : Response<'T> =
        { StatusCode = ""
          IsError = true
          SizeBytes = 0
          Message = String.Empty
          LatencyMs = 0
          Payload = None }
        
    
    let fail (error: Exception) : Response<'T> =
        { StatusCode = ""
          IsError = true
          SizeBytes = 0
          Message = if isNull error then String.Empty else error.Message
          LatencyMs = 0
          Payload = None }