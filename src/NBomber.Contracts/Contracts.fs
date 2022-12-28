namespace NBomber.Contracts

open System
open System.Collections.Generic
open System.Data
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
        member this.StatusCode = this.StatusCode
        member this.IsError = this.IsError
        member this.SizeBytes = this.SizeBytes
        member this.LatencyMs = this.LatencyMs
        member this.Message = this.Message
        
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
    abstract TestInfo: TestInfo
    abstract ScenarioInfo: ScenarioInfo
    abstract NodeInfo: NodeInfo
    abstract Logger: ILogger
    abstract InvocationNumber: int
    abstract Data: Dictionary<string,obj>
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

/// LoadSimulation helps to define the load profile for the target system. 
type LoadSimulation =
    /// Injects a given number of scenario copies (threads) with a linear ramp over a given duration.
    /// Every single scenario copy will iterate while the specified duration.
    /// Use it for ramp up and rump down.
    | RampingConstant of copies:int * during:TimeSpan
    
    /// A fixed number of scenario copies (threads) executes as many iterations as possible for a specified amount of time.
    /// Every single scenario copy will iterate while the specified duration.
    /// Use it when you need to run a specific amount of scenario copies (threads) for a certain amount of time.
    | KeepConstant of copies:int * during:TimeSpan
    
    /// Injects a given number of scenario copies (threads) from the current rate to the target rate during a given duration.
    /// Every single scenario copy will run only once.
    | RampingInject of rate:int * interval:TimeSpan * during:TimeSpan
    
    /// Injects a given number of scenario copies (threads) during a given duration.
    /// Every single scenario copy will run only once.
    /// Use it when you want to maintain a constant rate of requests without being affected by the performance of the system under test.    
    | Inject of rate:int * interval:TimeSpan * during:TimeSpan
    
    /// Injects a random number of scenario copies (threads) during a given duration.
    /// Every single scenario copy will run only once.
    /// Use it when you want to maintain a random rate of requests without being affected by the performance of the system under test.
    | InjectRandom of minRate:int * maxRate:int * interval:TimeSpan * during:TimeSpan
    
    /// Pause for a given duration
    | Pause of during:TimeSpan

type ScenarioProps = {
    ScenarioName: string
    Init: (IScenarioInitContext -> Task) option
    Clean: (IScenarioInitContext -> Task) option
    Run: (IScenarioContext -> Task<IResponse>) option
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