namespace NBomber.Contracts

open System
open System.Collections.Generic
open System.Data
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Threading
open System.Threading.Tasks
open Serilog
open Microsoft.Extensions.Configuration
open NBomber.Contracts.Stats

type IResponse =
    abstract StatusCode: string
    abstract IsError: bool
    abstract SizeBytes: int64    
    abstract Message: string     

type Response<'T> = {
    StatusCode: string
    IsError: bool
    SizeBytes: int64    
    Message: string
    Payload: 'T option
}
with
    interface IResponse with        
        member this.StatusCode = this.StatusCode
        member this.IsError = this.IsError
        member this.SizeBytes = this.SizeBytes        
        member this.Message = this.Message
        
type ScenarioOperation =
    | Init = 0
    | Clean = 1
    | WarmUp = 2
    | Bombing = 3

type ScenarioInfo = {
    /// Gets the current scenario thread id.
    /// You can use it as correlation id.
    [<Obsolete("Please use InstanceId instead")>] ThreadId: string
    [<Obsolete("Please use InstanceNumber instead")>] ThreadNumber: int
    
    InstanceId: string
    InstanceNumber: int
    ScenarioName: string
    ScenarioDuration: TimeSpan
    /// Returns info about current operation type.
    /// It can be: WarmUp or Bombing.
    ScenarioOperation: ScenarioOperation
}

/// MetricsProvider provides functionality for publishing custom metrics that will be aggregated.
/// It can be used for cases related to grabbing performance counters or other time series data.
/// It supports standard metric types: Histogram, Gauge.
type IMetricsProvider =
    
    /// <summary>
    /// Registers metric.
    /// The metric should be registered first before any usage. 
    /// </summary>
    /// <param name="metricName">Unique metric name.</param>
    /// <param name="measureUnit">Measure unit.</param>
    /// <param name="scalingFraction">Scaling fraction. Under the hood, the metric values are stored as int64.
    /// In order to be able to express a metric value as double, the multiplication by scaling factor is used.</param>
    /// <param name="metricType">Metric type.</param>
    /// <example>
    /// <code>
    ///  metricsProvider.RegisterMetric("thread-count", "MB", 1, MetricType.Gauge)    
    /// </code>
    /// </example>
    abstract RegisterMetric: metricName:string * measureUnit:string * scalingFraction:float * metricType:MetricType -> unit
    
    /// <summary>
    /// Publishes metric.
    /// </summary>
    /// <param name="metricName">Unique metric name. The metric name should be registered before publishing.</param>
    /// <param name="value">Metric value.</param>
    abstract PublishMetric: metricName:string * value:float -> unit

type IBaseContext =
    /// Gets current test info. 
    abstract TestInfo: TestInfo
    /// Gets current node info.
    abstract GetNodeInfo: unit -> NodeInfo    
    /// NBomber's logger.
    abstract Logger: ILogger
    /// Instance of metric provider. It should be used to record metrics.
    abstract MetricsProvider: IMetricsProvider

/// ScenarioContext represents the execution context of the currently running Scenario.
/// It provides functionality to log particular events, get information about the test, thread id, scenario copy/instance number, etc.
/// Also, it provides the option to stop all or particular scenarios manually.
type IScenarioContext =
    abstract TestInfo: TestInfo
    abstract ScenarioInfo: ScenarioInfo
    
    /// Returns information about the current node, node role, etc.
    /// For example, you can use it to get node roles: Coordinator, Agent, or SingleNode.
    abstract NodeInfo: NodeInfo
    abstract Logger: ILogger
    
    /// Represent the current Scenario instance invocation number. It starts from 1.
    abstract InvocationNumber: int64
    
    /// A dictionary that stores Scenario instance data and cleans it after Scenario iteration.
    /// It can be used to share some data between steps.
    abstract Data: Dictionary<string,obj>
    
    /// A dictionary that stores Scenario instance data and keep it for the whole scenario duration.
    /// It can be used to model Virtual User data that bound to Scenario instance. 
    abstract ScenarioInstanceData: Dictionary<string,obj>
    
    /// Indicates that scenario execution is finished or canceled.
    /// You can listen to changes via ScenarioCancellationToken.IsCancellationRequested.
    abstract ScenarioCancellationToken: CancellationToken

    /// Represent the basic .NET Random that should be used to introduce dynamic behavior. 
    abstract Random: Random
    
    /// Stops the specified scenario. In the cluster mode, NBomber will stop the specified scenario on all nodes.
    abstract StopScenario: scenarioName:string * reason:string -> unit
    
    /// Stops all scenarios. In the cluster mode, NBomber will stop all scenarios on all nodes.
    abstract StopCurrentTest: reason:string -> unit

/// Represents scenario partition.
/// In the cluster mode, the Coordinator automatically assigns ScenarioPartition to each Agent that runs the same Scenario.
type ScenarioPartition = {    
    /// Gets scenario partition number in the cluster.    
    Number: int
    
    /// Gets scenario partitions count in the cluster.
    Count: int
}
with
    [<CompiledName("Empty")>]
    static member empty = { Number = 1; Count = 1 }

type IScenarioInitContext =
    /// Gets current test info
    abstract TestInfo: TestInfo
    
    /// Gets current Scenario info
    abstract ScenarioInfo: ScenarioInfo
    
    /// Gets current node info
    abstract NodeInfo: NodeInfo
    
    /// Gets Scenario's custom settings from the configuration file
    abstract CustomSettings: IConfiguration
    
    /// Gets Global custom settings from the configuration file
    abstract GlobalCustomSettings: IConfiguration
    
    /// Gets scenario partition in the cluster.
    /// In the cluster mode, the Coordinator automatically assigns ScenarioPartition to each Agent that runs the same Scenario. 
    abstract ScenarioPartition: ScenarioPartition    
    
    /// NBomber's logger
    abstract Logger: ILogger

/// LoadSimulation allows configuring parallelism and workload profiles.
/// Link for info: https://nbomber.com/docs/nbomber/load-simulation 
type LoadSimulation =
    
    /// <summary>
    /// Increases or decreases the number of Scenario copies (virtual users) in a linear ramp over a specified duration.
    /// Each Scenario copy (virtual user) behaves like a long-running thread that runs continuously (by specified duration) and will be destroyed when the current load simulation stops.
    /// This simulation type is suitable if you require virtual users to gradually increase or decrease during specific time intervals.
    /// Typically, this simulation type is employed to test closed systems where you have control over the concurrent number (not rate) of users or client connections.
    /// Additionally, it is commonly used to test databases, message brokers, or any other system that uses a static client pool of persistent connections and reuses them.
    /// Link for info: https://nbomber.com/docs/nbomber/load-simulation 
    /// </summary>
    /// <param name="copies">The number of concurrent Scenario copies that will be running in parallel.</param>
    /// <param name="during">The duration of load simulation.</param>
    | RampingConstant of copies:int * during:TimeSpan
    
    /// <summary>
    /// Maintains a constant number of activated (constantly running) Scenario copies (virtual users) that execute as many iterations as possible within a specified duration.
    /// Each Scenario copy (virtual user) behaves like a long-running thread that runs continually (by specified duration) and will be destroyed when the current load simulation stops.
    /// Use this simulation type when you need to run and sustain a consistent number of scenario copies (virtual users) for a specific period.
    /// Typically, this simulation type is applied to test closed systems where you have control over the concurrent number (not rate) of users or client connections.
    /// It is also often used to test databases, message brokers, or any other system that uses a static client pool of persistent connections and reuses them.
    /// Link for info: https://nbomber.com/docs/nbomber/load-simulation 
    /// </summary>
    /// <param name="copies">The number of concurrent Scenario copies that will be running in parallel.</param>
    /// <param name="during">The duration of load simulation.</param>
    | KeepConstant of copies:int * during:TimeSpan
    
    /// <summary>
    /// Maintains a constant number of activated (constantly running) Scenario copies (virtual users), which continue executing until a specified iteration count is reached.
    /// Each Scenario copy (virtual user) behaves like a long-running thread that runs continually (by specified duration) and will be destroyed when the current load simulation stops.
    /// This load simulation type is appropriate when you aim for a specific number of virtual users to complete a fixed total number of iterations.
    /// Typically, this simulation type is applied to test closed systems where you have control over the concurrent number (not rate) of users or client connections.
    /// It can be applied to databases, message brokers, or any other system that uses a static client pool of persistent connections and reuses them.
    /// An example use case is quick performance tests in the development build cycle.
    /// As developers make changes, they might run the test against the local code to test for performance regressions.
    /// Link for info: https://nbomber.com/docs/nbomber/load-simulation 
    /// </summary>
    /// <param name="copies">The number of concurrent Scenario copies that will be running in parallel.</param>
    /// <param name="iterations">Total number of Scenario iterations to execute across all Scenario copies.</param>    
    | IterationsForConstant of copies:int * iterations:int
    
    /// <summary>
    /// Injects a given number of Scenario copies (virtual users) by rate until a specified iteration count.
    /// With this simulation, you control the Scenario injection rate and iteration count.
    /// Each Scenario copy (virtual user) behaves like a short-running thread that runs only once and then is destroyed.
    /// Use it when you want to maintain a constant rate of requests and run a fixed number of iterations without being affected by the performance of the system you load test.
    /// This simulation type is commonly employed for testing websites and HTTP APIs.
    /// An example use case is quick performance tests in the development build cycle.
    /// As developers make changes, they might run the test against the local code to test for performance regressions.
    /// Link for info: https://nbomber.com/docs/nbomber/load-simulation
    /// </summary>
    /// <param name="rate">The injection rate of Scenario copies. It configures how many concurrent copies will be injected at a time.</param>
    /// <param name="interval">The injection interval. It configures the interval between injections. </param>
    /// <param name="iterations">Total number of Scenario iterations to execute across all Scenario copies.</param>   
    | IterationsForInject of rate:int * interval:TimeSpan * iterations:int
    
    /// <summary>
    /// Injects a given number of Scenario copies (virtual users) by rate with a linear ramp over a given duration.
    /// With this simulation, you control the Scenario injection rate and injection interval.
    /// Each Scenario copy (virtual user) behaves like a short-running thread that runs only once and then is destroyed.
    /// Choose this approach when you aim to sustain a smooth ramp-up and ramp-down of request rates.
    /// Usually, this simulation type is used to test Open systems where you control the arrival rate of users.
    /// Additionally, this simulation type is commonly employed for testing websites and HTTP APIs.
    /// Link for info: https://nbomber.com/docs/nbomber/load-simulation
    /// </summary>
    /// <param name="rate">The injection rate of Scenario copies. It configures how many concurrent copies will be injected at a time.</param>
    /// <param name="interval">The injection interval. It configures the interval between injections. </param>
    /// <param name="during">The duration of load simulation.</param>
    | RampingInject of rate:int * interval:TimeSpan * during:TimeSpan
    
    /// <summary>
    /// Injects a given number of Scenario copies (virtual users) by rate during a given duration.
    /// With this simulation, you control the Scenario injection rate and injection interval.
    /// Each Scenario copy (virtual user) behaves like a short-running thread that runs only once and then is destroyed.
    /// Use it when you want to maintain a constant rate of requests without being affected by the performance of the system you load test.
    /// Usually, this simulation type is used to test Open systems where you control the arrival rate of users.
    /// Additionally. it is used to test Websites, HTTP API.
    /// Link for info: https://nbomber.com/docs/nbomber/load-simulation
    /// </summary>
    /// <param name="rate">The injection rate of Scenario copies. It configures how many concurrent copies will be injected at a time.</param>
    /// <param name="interval">The injection interval. It configures the interval between injections. </param>
    /// <param name="during">The duration of load simulation.</param>   
    | Inject of rate:int * interval:TimeSpan * during:TimeSpan
    
    /// <summary>
    /// Injects a given random number of Scenario copies (virtual users) by rate during a given duration.
    /// With this simulation, you control the Scenario injection rate and injection interval.
    /// Each Scenario copy(virtual user) behaves like a short-running thread that runs only once and then is destroyed.
    /// Use it when you want to maintain a random rate of requests without being affected by the performance of the system you load test.
    /// Usually, this simulation type is used to test Open systems where you control the arrival rate of users.
    /// Additionally. it is used to test Websites, HTTP API.
    /// Link for info: https://nbomber.com/docs/nbomber/load-simulation
    /// </summary>
    /// <param name="minRate">The min injection rate of Scenario copies.</param>
    /// <param name="maxRate">The max injection rate of Scenario copies.</param>
    /// <param name="interval">The injection interval. It configures the interval between injections.</param>
    /// <param name="during">The duration of load simulation.</param>
    | InjectRandom of minRate:int * maxRate:int * interval:TimeSpan * during:TimeSpan
    
    /// <summary>
    /// Introduces Scenario pause simulation for a given duration.
    /// It's useful for cases when some Scenario start should be delayed or paused in the middle of execution.
    /// Link for info: https://nbomber.com/docs/nbomber/load-simulation
    /// </summary>
    /// <param name="during">The duration of load simulation.</param>
    | Pause of during:TimeSpan

/// <summary>
/// Thresholds are the pass/fail criteria that you define for your test metrics.
/// The runtime thresholds will be executed periodically to check real-time and final metrics for Scenario and Step.
/// </summary>
type Threshold private (stepName: string,
                        checkStep: Func<StepStats, bool>,
                        checkScenario: Func<ScenarioStats, bool>,
                        abortWhenErrorCount: Nullable<int>,
                        startCheckAfter: Nullable<TimeSpan>) =
    
    member this.StepName = stepName
    member this.CheckStep = checkStep
    member this.CheckScenario = checkScenario    
    member this.AbortWhenErrorCount = abortWhenErrorCount
    member this.StartCheckAfter = startCheckAfter    
    
    /// <summary>
    /// Creates a runtime threshold.
    /// Thresholds are the pass/fail criteria that you define for your test metrics.
    /// </summary>
    /// <param name="checkScenario">Specifies the threshold check function. This function is executed periodically to monitor and check metrics.</param>
    /// <param name="abortWhenErrorCount">Sets the error threshold count. Once this limit is reached, NBomber will terminate the session earlier. The default value is null, meaning NBomber will not end the session early, even if the failed thresholds are met.</param>
    /// <param name="startCheckAfter">Specifies the start time (delay) after which NBomber will begin executing the threshold check function.</param>
    [<CompiledName("Create")>]
    static member create (checkScenario: Func<ScenarioStats, bool>,
                          [<Optional;DefaultParameterValue(Nullable<int>())>] abortWhenErrorCount: Nullable<int>,
                          [<Optional;DefaultParameterValue(Nullable<TimeSpan>())>] startCheckAfter: Nullable<TimeSpan>) =
        
        Threshold("", Unchecked.defaultof<_>, checkScenario, abortWhenErrorCount, startCheckAfter)
         
    /// <summary>
    /// Creates a runtime threshold.
    /// Thresholds are the pass/fail criteria that you define for your test metrics.
    /// </summary>
    /// <param name="stepName">Specifies StepName for the current Scenario's threshold.</param>
    /// <param name="checkStep">Specifies the threshold check function. This function is executed periodically to monitor and check metrics.</param>
    /// <param name="abortWhenErrorCount">Sets the error threshold count. Once this limit is reached, NBomber will terminate the session earlier. The default value is null, meaning NBomber will not end the session early, even if the failed thresholds are met.</param>
    /// <param name="startCheckAfter">Specifies the start time (delay) after which NBomber will begin executing the threshold check function.</param>
    [<CompiledName("Create")>]         
    static member create (stepName: string,
                          checkStep: Func<StepStats, bool>,
                          [<Optional;DefaultParameterValue(Nullable<int>())>] abortWhenErrorCount: Nullable<int>,
                          [<Optional;DefaultParameterValue(Nullable<TimeSpan>())>] startCheckAfter: Nullable<TimeSpan>) =
        
        Threshold(stepName, checkStep, Unchecked.defaultof<_>, abortWhenErrorCount, startCheckAfter)    
        
type ScenarioProps = {
    ScenarioName: string
    Init: (IScenarioInitContext -> Task) option
    Clean: (IScenarioInitContext -> Task) option    
    Run: (IScenarioContext -> Task<IResponse>) option
    WarmUpDuration: TimeSpan option
    LoadSimulations: LoadSimulation list
    Thresholds: Threshold list
    RestartIterationOnFail: bool
    MaxFailCount: int
    Weight: int option
}

/// Provides details about the Scenario that is scheduled to start.
type ScenarioStartInfo = {
    ScenarioName: string
    SortIndex: int
}

/// Provides session details about the Scenarios that are scheduled to start.
type SessionStartInfo = {
    Scenarios: ScenarioStartInfo[]   
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
    /// Starts execution, signifying the START event of the load test session.    
    /// </summary>
    /// <param name="sessionInfo">Provides session details about the Scenarios that are scheduled to start</param>    
    abstract Start: sessionInfo:SessionStartInfo -> Task
    
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
    /// Ends execution and records a metric representing the STOP event of the load test.    
    /// This method can also be used to clean up resources, such as database connections. 
    /// </summary>    
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
    /// Starts execution, signifying the START event of the load test session.
    /// </summary>
    /// <param name="sessionInfo">Provides session details about the Scenarios that are scheduled to start</param>
    abstract Start: sessionInfo:SessionStartInfo -> Task
    
    abstract GetStats: stats:NodeStats -> Task<DataSet>
    abstract GetHints: unit -> string[]
    
    /// <summary>
    /// Ends execution.    
    /// This method can also be used to clean up resources, such as database connections.     
    /// </summary>
    abstract Stop: unit -> Task

type ApplicationType =
    | Process = 0
    | Console = 1

type StatsExtensions() =    
    
    static let rec findStatus (stats: StatusCodeStats[]) (code: string) (index: int) =
        if index >= stats.Length then
            ValueNone
        elif stats[index].StatusCode = code then
            ValueSome stats[index]        
        else
            findStatus stats code (index + 1)
            
    static let rec findScenario (stats: ScenarioStats[]) (name: string) (index: int) =
        if index >= stats.Length then
            ValueNone
        elif stats[index].ScenarioName = name then
            ValueSome stats[index]        
        else
            findScenario stats name (index + 1)
            
    static let rec findStep (stats: StepStats[]) (name: string) (index: int) =
        if index >= stats.Length then
            ValueNone
        elif stats[index].StepName = name then
            ValueSome stats[index]        
        else
            findStep stats name (index + 1)            
    
    [<Extension>]
    static member Get(statusCodes: StatusCodeStats[], statusCode: string) =
        match findStatus statusCodes statusCode 0 with
        | ValueSome v -> v
        | ValueNone   -> raise (KeyNotFoundException $"Status code: '{statusCode}' is not found.") 
        
    [<Extension>]
    static member Find(statusCodes: StatusCodeStats[], statusCode: string) =
        findStatus statusCodes statusCode 0
        |> ValueOption.defaultValue(Unchecked.defaultof<_>)        

    [<Extension>]
    static member Exists(statusCodes: StatusCodeStats[], statusCode: string) =
        findStatus statusCodes statusCode 0
        |> ValueOption.isSome
            
    [<Extension>]
    static member Get(scenarioStats: ScenarioStats[], name: string) =
        match findScenario scenarioStats name 0 with        
        | ValueSome v -> v
        | ValueNone   -> raise (KeyNotFoundException $"Scenario: '{name}' is not found.")                
            
    [<Extension>]
    static member Find(scenarioStats: ScenarioStats[], name: string) =
        findScenario scenarioStats name 0
        |> ValueOption.defaultValue(Unchecked.defaultof<_>)
        
    [<Extension>]
    static member Exists(scenarioStats: ScenarioStats[], name: string) =
        findScenario scenarioStats name 0
        |> ValueOption.isSome
        
    [<Extension>]
    static member Get(stepStats: StepStats[], name: string) =        
        match findStep stepStats name 0 with        
        | ValueSome v -> v
        | ValueNone   -> raise (KeyNotFoundException $"Step: '{name}' is not found.")         
            
    [<Extension>]
    static member Find(stepStats: StepStats[], name: string) =
        findStep stepStats name 0
        |> ValueOption.defaultValue(Unchecked.defaultof<_>)        
        
    [<Extension>]
    static member Exists(stepStats: StepStats[], name: string) =
        findStep stepStats name 0
        |> ValueOption.isSome            