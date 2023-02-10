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
    | Init = 0
    | Clean = 1
    | WarmUp = 2
    | Bombing = 3

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
    /// Gets current test info. 
    abstract TestInfo: TestInfo
    /// Gets current node info.
    abstract GetNodeInfo: unit -> NodeInfo    
    /// NBomber's logger.
    abstract Logger: ILogger

/// ScenarioContext represents the execution context of the currently running Scenario.
/// It provides functionality to log particular events, get information about the test, thread id, scenario copy/instance number, etc.
/// Also, it provides the option to stop all or particular scenarios manually.
type IScenarioContext =
    abstract TestInfo: TestInfo
    abstract ScenarioInfo: ScenarioInfo
    abstract NodeInfo: NodeInfo
    abstract Logger: ILogger
    abstract InvocationNumber: int
    abstract Data: Dictionary<string,obj>
    abstract StopScenario: scenarioName:string * reason:string -> unit
    abstract StopCurrentTest: reason:string -> unit

/// Represents scenario partition.
/// In the cluster mode, the Coordinator automatically assigns ScenarioPartition to each Agent that runs the same Scenario.
type ScenarioPartition = {    
    /// Gets scenario partition number in the cluster.    
    Number: int
    
    /// Gets scenario partitions count in the cluster.
    Count: int
} with
    [<CompiledName("Empty")>]
    static member empty = { Number = 1; Count = 1 }

type IScenarioInitContext =
    /// Gets current test info
    abstract TestInfo: TestInfo
    
    /// Gets current Scenario info
    abstract ScenarioInfo: ScenarioInfo
    
    /// Gets current node info
    abstract NodeInfo: NodeInfo
    
    /// Gets client settings content from configuration file
    abstract CustomSettings: IConfiguration
    
    /// Gets scenario partition in the cluster.
    /// In the cluster mode, the Coordinator automatically assigns ScenarioPartition to each Agent that runs the same Scenario. 
    abstract ScenarioPartition: ScenarioPartition    
    
    /// NBomber's logger
    abstract Logger: ILogger

/// LoadSimulation allows configuring parallelism and workload profiles. 
type LoadSimulation =
    /// <summary>
    /// Adds or removes a given number of Scenario copies(instances) with a linear ramp over a given duration.    
    /// Each Scenario copy behaves like a long-running thread that runs continually(by specified duration) and will be destroyed when the current load simulation stops.
    /// Use it for a smooth ramp up and ramp down.
    /// Usually, this simulation type is used to test databases, message brokers, or any other system that works with a static client's pool of connections and reuses them.        
    /// </summary>
    /// <param name="copies">The number of concurrent Scenario copies that will be running in parallel.</param>
    /// <param name="during">The duration of load simulation.</param>
    | RampingConstant of copies:int * during:TimeSpan
    
    /// <summary>
    /// Keeps activated(constantly running) a fixed number of Scenario copies(instances) which executes as many iterations as possible for a specified duration.
    /// Each Scenario copy behaves like a long-running thread that runs continually(by specified duration) and will be destroyed when the current load simulation stops.
    /// Use it when you need to run and keep a constant amount of Scenario copies for a specific period.
    /// Usually, this simulation type is used to test databases, message brokers, or any other system that works with a static client's pool of connections and reuses them.    
    /// </summary>
    /// <param name="copies">The number of concurrent Scenario copies that will be running in parallel.</param>
    /// <param name="during">The duration of load simulation.</param>
    | KeepConstant of copies:int * during:TimeSpan
    
    /// <summary>
    /// Injects a given number of Scenario copies(instances) with a linear ramp over a given duration.
    /// Each Scenario copy behaves like a short-running thread that runs only once and then is destroyed.
    /// With this simulation, you control the Scenario injection rate and injection interval.
    /// Use it for a smooth ramp up and ramp down.
    /// Usually, this simulation type is used to test HTTP API.
    /// </summary>
    /// <param name="rate">The injection rate of Scenario copies. It configures how many concurrent copies will be injected at a time.</param>
    /// <param name="interval">The injection interval. It configures the interval between injections. </param>
    /// <param name="during">The duration of load simulation.</param>
    | RampingInject of rate:int * interval:TimeSpan * during:TimeSpan
    
    /// <summary>
    /// Injects a given number of Scenario copies(instances) during a given duration.
    /// Each Scenario copy behaves like a short-running thread that runs only once and then is destroyed.
    /// With this simulation, you control the Scenario injection rate and injection interval.
    /// Use it when you want to maintain a constant rate of requests without being affected by the performance of the system you load test.
    /// Usually, this simulation type is used to test HTTP API.
    /// </summary>
    /// <param name="rate">The injection rate of Scenario copies. It configures how many concurrent copies will be injected at a time.</param>
    /// <param name="interval">The injection interval. It configures the interval between injections. </param>
    /// <param name="during">The duration of load simulation.</param>   
    | Inject of rate:int * interval:TimeSpan * during:TimeSpan
    
    /// <summary>
    /// Injects a given random number of Scenario copies(instances) during a given duration.
    /// Each Scenario copy behaves like a short-running thread that runs only once and then is destroyed.
    /// With this simulation, you control the Scenario injection rate and injection interval.
    /// Use it when you want to maintain a random rate of requests without being affected by the performance of the system you load test.
    /// Usually, this simulation type is used to test HTTP API.
    /// </summary>
    /// <param name="minRate">The min injection rate of Scenario copies.</param>
    /// <param name="maxRate">The max injection rate of Scenario copies.</param>
    /// <param name="interval">The injection interval. It configures the interval between injections.</param>
    /// <param name="during">The duration of load simulation.</param>
    | InjectRandom of minRate:int * maxRate:int * interval:TimeSpan * during:TimeSpan
    
    /// <summary>
    /// Introduces Scenario pause simulation for a given duration.
    /// It's useful for cases when some Scenario start should be delayed or paused in the middle of execution.
    /// </summary>
    /// <param name="during">The duration of load simulation.</param>
    | Pause of during:TimeSpan

type ScenarioProps = {
    ScenarioName: string
    Init: (IScenarioInitContext -> Task) option
    Clean: (IScenarioInitContext -> Task) option
    Run: (IScenarioContext -> Task<IResponse>) option
    WarmUpDuration: TimeSpan option
    LoadSimulations: LoadSimulation list
    RestartIterationOnFail: bool
    MaxFailCount: int
}

/// ReportingSink provides functionality for saving real-time and final statistics.
type IReportingSink =
    inherit IDisposable
    abstract SinkName: string
    
    /// <summary>
    /// Inits ReportingSink.
    /// Usually, in this method, ReportingSink reads JSON configuration and establishes a connection to reporting data storage.
    /// </summary>
    /// <param name="context">Base NBomber execution context. It can be used to get a logger, test info, etc.</param>
    /// <param name="infraConfig">Represent JSON config for infrastructure.</param>
    abstract Init: context:IBaseContext * infraConfig:IConfiguration -> Task
    
    /// <summary>
    /// Starts execution and saves a metric representing the load test's START.
    /// This method will be invoked two times: for a warm-up(if it's enabled) and the bombing.    
    /// </summary>
    /// <example>
    /// <code>
    /// // to get info about the current operation:    
    /// IBaseContext.GetNodeInfo().CurrentOperation == OperationType.WarmUp    
    /// </code>
    /// </example>
    abstract Start: unit -> Task
    
    /// <summary>
    /// Saves real-time stats data.
    /// This method will be invoked periodically, by specified ReportingInterval.
    /// </summary>
    /// <param name="stats">Real-time stats data of the running scenarios.</param>
    abstract SaveRealtimeStats: stats:ScenarioStats[] -> Task
    
    /// <summary>
    /// Saves final stats data.
    /// This method will be invoked when the load test is finished.
    /// </summary>
    /// <param name="stats">Final stats data of the finished scenarios.</param>
    abstract SaveFinalStats: stats:NodeStats -> Task
    
    /// <summary>
    /// Stops execution and saves a metric representing the load test's STOP.    
    /// This method will be invoked two times: for a warm-up(if it's enabled) and the bombing.
    /// By default, this method shouldn't execute any logic related to cleaning ReportingSink's resources, opened connections, etc.
    /// To clean resources, ReportingSink implements the IDisposal interface. 
    /// </summary>
    /// <example>
    /// <code>
    /// // to get info about the current operation:    
    /// IBaseContext.GetNodeInfo().CurrentOperation == OperationType.WarmUp    
    /// </code>
    /// </example>
    abstract Stop: unit -> Task

/// WorkerPlugin provides functionality for building background workers.
/// The basic concept of a background worker - it's a worker that starts in parallel with a test and does some work, and then can return statistics that will be included into report.
/// A good example of a background worker is PingPlugin which checks the physical latency between NBomber's agent and target system and then prints results in a report. 
type IWorkerPlugin =
    inherit IDisposable
    abstract PluginName: string
    
    /// <summary>
    /// Inits WorkerPlugin.
    /// Usually, in this method, WorkerPlugin reads JSON configuration and prepare all necessary dependencies.
    /// </summary>
    /// <param name="context">Base NBomber execution context. It can be used to get a logger, test info, etc.</param>
    /// <param name="infraConfig">Represent JSON config for infrastructure.</param>
    abstract Init: context:IBaseContext * infraConfig:IConfiguration -> Task
    
    /// <summary>
    /// Starts execution.
    /// This method will be invoked two times: for a warm-up(if it's enabled) and the bombing.
    /// </summary>    
    /// <example>
    /// <code>
    /// // to get info about the current operation:    
    /// IBaseContext.GetNodeInfo().CurrentOperation == OperationType.WarmUp    
    /// </code>
    /// </example>
    abstract Start: unit -> Task
    
    abstract GetStats: stats:NodeStats -> Task<DataSet>
    abstract GetHints: unit -> string[]
    
    /// <summary>
    /// Stops execution.
    /// This method will be invoked two times: for a warm-up(if it's enabled) and the bombing.
    /// </summary>    
    /// <example>
    /// <code>
    /// // to get info about the current operation:    
    /// IBaseContext.GetNodeInfo().CurrentOperation == OperationType.WarmUp    
    /// </code>
    /// </example>
    abstract Stop: unit -> Task

type ApplicationType =
    | Process = 0
    | Console = 1