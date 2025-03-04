namespace NBomber.Contracts

open System
open System.Collections.Generic
open System.Data
open System.Linq.Expressions
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Threading
open System.Threading.Tasks
open Serilog
open Microsoft.Extensions.Configuration
open NBomber.Contracts.Stats
open NBomber.Contracts.Metrics

/// Represents a generic NBomber response interface.
type IResponse =
    /// Gets StatusCode, which typically represents the HTTP status code (e.g., "200", "404") or any application-specific status indicator.
    abstract StatusCode: string
    /// Boolean flag indicating if the response contains an error. `true` if the response is an error; `false` if the response is successful.
    abstract IsError: bool
    /// Size of the response in bytes. Helpful in tracking response size for performance analysis.
    abstract SizeBytes: int64
    /// Specifies a custom latency to override the original response latency. This is useful in scenarios where the operation's latency needs to be measured in a custom manner.
    abstract CustomLatencyMs: float
    /// Message associated with the response, often used to provide a human-readable description of the response or error details if `IsError` is true.
    abstract Message: string     

/// Represents a generic NBomber response type.
type Response<'T> = {
    /// Gets StatusCode, which typically represents the HTTP status code (e.g., "200", "404") or any application-specific status indicator.
    StatusCode: string
    /// Boolean flag indicating if the response contains an error. `true` if the response is an error; `false` if the response is successful.
    IsError: bool
    /// Size of the response in bytes. Helpful in tracking response size for performance analysis.
    SizeBytes: int64
    /// Specifies a custom latency to override the original response latency. This is useful in scenarios where the operation's latency needs to be measured in a custom manner.  
    CustomLatencyMs: float
    /// Message associated with the response, often used to provide a human-readable description of the response or error details if `IsError` is true.
    Message: string
    /// Optional payload. It will contain `Some(value)` if there is a payload, or `None` if absent.
    Payload: 'T option
}
with
    interface IResponse with        
        member this.StatusCode = this.StatusCode
        member this.IsError = this.IsError
        member this.SizeBytes = this.SizeBytes
        member this.CustomLatencyMs = this.CustomLatencyMs
        member this.Message = this.Message
        
type ScenarioOperation =
    | Init = 0
    | Clean = 1
    | WarmUp = 2
    | Bombing = 3

/// Represents information about a scenario instance, including unique identifiers, operation details, etc.
type ScenarioInfo = {
    /// Gets the current scenario thread id.
    /// You can use it as correlation id.
    [<Obsolete("Please use InstanceId instead")>] ThreadId: string
    [<Obsolete("Please use InstanceNumber instead")>] ThreadNumber: int
    
    /// Unique identifier for the scenario instance. This can be used as a correlation ID to distinguish between different instances.
    InstanceId: string    
    /// A unique, sequential number for each scenario instance. Serves as a simple integer identifier for instances of the scenario.
    InstanceNumber: int    
    /// The name of the scenario.
    ScenarioName: string    
    /// The planned duration of the scenario.
    ScenarioDuration: TimeSpan
    /// Provides information about the type of the current operation within the scenario.
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
    /// Registers a counter metric.
    abstract RegisterMetric: counter:ICounter -> unit
    /// Registers a gauge metric.
    abstract RegisterMetric: gauge:IGauge -> unit

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
    /// Returns the current execution time of the Scenario timer.  
    abstract GetScenarioTimerTime: unit -> TimeSpan

/// Represents a partition of a scenario in a distributed or clustered environment.
/// In the cluster mode, the Coordinator automatically assigns ScenarioPartition to each Agent that runs the same Scenario.
type ScenarioPartition = {    
    /// Gets scenario partition number in the cluster.    
    /// Each partition has a unique number that identifies its place in the cluster. 
    Number: int
    
    /// The total number of partitions for the Scenario within the cluster.
    /// Defines the total count of partitions, which allows each Agent to determine
    /// its scope and workload in relation to other partitions.
    Count: int
}
with
    [<CompiledName("Empty")>]
    static member empty = { Number = 1; Count = 1 }

/// Defines the context for initializing a scenario.
/// Provides access to configuration, logging, and metadata required for setting up and running a scenario.
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
    /// Registers a counter metric.
    abstract RegisterMetric: counter:ICounter -> unit
    /// Registers a gauge metric.
    abstract RegisterMetric: gauge:IGauge -> unit

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
                        checkStep: Expression<Func<StepStats, bool>>,
                        checkScenario: Expression<Func<ScenarioStats, bool>>,
                        checkMetric: Expression<Func<MetricStats, bool>>,
                        abortWhenErrorCount: Nullable<int>,
                        startCheckAfter: Nullable<TimeSpan>) =
    
    /// Gets StepName for the current Scenario's threshold.
    member this.StepName = stepName
    /// Gets Step's check function. This function is executed periodically to monitor and check metrics.
    member this.CheckStep = checkStep
    /// Gets Scenario's check function. This function is executed periodically to monitor and check metrics.
    member this.CheckScenario = checkScenario
    /// Gets Scenario's metric check function. This function is executed periodically to monitor and check metrics.
    member this.CheckMetric = checkMetric
    /// Gets error threshold count. Once this limit is reached, NBomber will terminate the session earlier. The default value is null, meaning NBomber will not end the session early, even if the failed thresholds are met.
    member this.AbortWhenErrorCount = abortWhenErrorCount
    /// Gets the start time (delay) after which NBomber will begin executing the threshold check function.
    member this.StartCheckAfter = startCheckAfter    
    
    /// <summary>
    /// Creates a runtime threshold.
    /// Thresholds are the pass/fail criteria that you define for your test metrics.
    /// </summary>
    /// <param name="checkScenario">Specifies the threshold check function. This function is executed periodically to monitor and check metrics.</param>
    /// <param name="abortWhenErrorCount">Sets the error threshold count. Once this limit is reached, NBomber will terminate the session earlier. The default value is null, meaning NBomber will not end the session early, even if the failed thresholds are met.</param>
    /// <param name="startCheckAfter">Specifies the start time (delay) after which NBomber will begin executing the threshold check function.</param>
    [<CompiledName("Create")>]
    static member create (checkScenario: Expression<Func<ScenarioStats, bool>>,
                          [<Optional;DefaultParameterValue(Nullable<int>())>] abortWhenErrorCount: Nullable<int>,
                          [<Optional;DefaultParameterValue(Nullable<TimeSpan>())>] startCheckAfter: Nullable<TimeSpan>) =
        
        Threshold("", null, checkScenario, null, abortWhenErrorCount, startCheckAfter)

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
                          checkStep: Expression<Func<StepStats, bool>>,
                          [<Optional;DefaultParameterValue(Nullable<int>())>] abortWhenErrorCount: Nullable<int>,
                          [<Optional;DefaultParameterValue(Nullable<TimeSpan>())>] startCheckAfter: Nullable<TimeSpan>) =
        
        Threshold(stepName, checkStep, null, null, abortWhenErrorCount, startCheckAfter)
        
    /// <summary>
    /// Creates a runtime threshold.
    /// Thresholds are the pass/fail criteria that you define for your test metrics.
    /// </summary>    
    /// <param name="checkMetric">Specifies the threshold check function for the current Scenario's metrics. This function is executed periodically to monitor and check metrics.</param>
    /// <param name="abortWhenErrorCount">Sets the error threshold count. Once this limit is reached, NBomber will terminate the session earlier. The default value is null, meaning NBomber will not end the session early, even if the failed thresholds are met.</param>
    /// <param name="startCheckAfter">Specifies the start time (delay) after which NBomber will begin executing the threshold check function.</param>
    [<CompiledName("Create")>]         
    static member create (checkMetric: Expression<Func<MetricStats, bool>>,
                          [<Optional;DefaultParameterValue(Nullable<int>())>] abortWhenErrorCount: Nullable<int>,
                          [<Optional;DefaultParameterValue(Nullable<TimeSpan>())>] startCheckAfter: Nullable<TimeSpan>) =
        
        Threshold("", null, null, checkMetric, abortWhenErrorCount, startCheckAfter)         
        
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
    /// Gets Scenario name.
    ScenarioName: string
    /// Gets Scenario's sorting index. It can be used to order Scenario on UI. 
    SortIndex: int
}

/// Provides session details about the Scenarios that are scheduled to start.
type SessionStartInfo = {
    /// Gets list of Scenarios that are scheduled to start
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
        
    /// <summary>
    /// Gets plugin data.
    /// </summary>
    /// <param name="stats">Final stats data of the finished scenarios.</param>
    abstract GetData: stats:NodeStats -> Task<PluginData>
    
    /// <summary>
    /// Stops execution.    
    /// This method can also be used to clean up resources, such as database connections.     
    /// </summary>
    abstract Stop: unit -> Task

/// Provides methods for creating metric instances.
type Metric =
    
    /// <summary>
    /// Creates a new counter metric.
    /// </summary>
    /// <param name="metricName">The name of the counter metric.</param>
    /// <param name="unitOfMeasure">The unit of measure for the counter.</param>
    /// <returns>An instance of <see cref="ICounter"/>.</returns>
    [<CompiledName("CreateCounter")>]
    static member createCounter(metricName, unitOfMeasure) =
        Counter(metricName, unitOfMeasure) :> ICounter
        
    /// <summary>
    /// Creates a new gauge metric.
    /// </summary>
    /// <param name="metricName">The name of the gauge metric.</param>
    /// <param name="unitOfMeasure">The unit of measure for the gauge.</param>
    /// <returns>An instance of <see cref="IGauge"/>.</returns>            
    [<CompiledName("CreateGauge")>]        
    static member createGauge(metricName, unitOfMeasure) =
        Gauge(metricName, unitOfMeasure) :> IGauge

type ApplicationType =
    | Process = 0
    | Console = 1

/// Provides extension methods for searching and retrieving statistics data
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
    
    static let rec findCounterMetric (counters: CounterStats[]) (metricName: string) (index: int) =
        if index >= counters.Length then
            ValueNone
        elif counters[index].MetricName = metricName then
            ValueSome counters[index]        
        else
            findCounterMetric counters metricName (index + 1)
    
    static let rec findGaugeMetric (gauges: GaugeStats[]) (metricName: string) (index: int) =
        if index >= gauges.Length then
            ValueNone
        elif gauges[index].MetricName = metricName then
            ValueSome gauges[index]        
        else
            findGaugeMetric gauges metricName (index + 1)
    
    /// Retrieves the `StatusCodeStats` with the specified status code.
    /// Throws a `KeyNotFoundException` if the status code is not found.
    [<Extension>]
    static member Get(statusCodes: StatusCodeStats[], statusCode: string) =
        match findStatus statusCodes statusCode 0 with
        | ValueSome v -> v
        | ValueNone   -> raise (KeyNotFoundException $"Status code: '{statusCode}' is not found.") 
        
    /// Finds the `StatusCodeStats` with the specified status code.
    /// Returns the matched `StatusCodeStats` if found, otherwise `null`.        
    [<Extension>]
    static member Find(statusCodes: StatusCodeStats[], statusCode: string) =
        findStatus statusCodes statusCode 0
        |> ValueOption.defaultValue(Unchecked.defaultof<_>)        

    /// Checks if a `StatusCodeStats` with the specified status code exists.
    /// Returns `true` if found, otherwise `false`.
    [<Extension>]
    static member Exists(statusCodes: StatusCodeStats[], statusCode: string) =
        findStatus statusCodes statusCode 0
        |> ValueOption.isSome
            
    /// Retrieves the `ScenarioStats` with the specified scenario name.
    /// Throws a `KeyNotFoundException` if the scenario name is not found.            
    [<Extension>]
    static member Get(scenarioStats: ScenarioStats[], name: string) =
        match findScenario scenarioStats name 0 with        
        | ValueSome v -> v
        | ValueNone   -> raise (KeyNotFoundException $"Scenario: '{name}' is not found.")                
          
    /// Finds the `ScenarioStats` with the specified scenario name.
    /// Returns the matched `ScenarioStats` if found, otherwise `null`.            
    [<Extension>]
    static member Find(scenarioStats: ScenarioStats[], name: string) =
        findScenario scenarioStats name 0
        |> ValueOption.defaultValue(Unchecked.defaultof<_>)
        
    /// Checks if a `ScenarioStats` with the specified scenario name exists.
    /// Returns `true` if found, otherwise `false`.        
    [<Extension>]
    static member Exists(scenarioStats: ScenarioStats[], name: string) =
        findScenario scenarioStats name 0
        |> ValueOption.isSome
        
    /// Retrieves the `StepStats` with the specified step name.
    /// Throws a `KeyNotFoundException` if the step name is not found.        
    [<Extension>]
    static member Get(stepStats: StepStats[], name: string) =        
        match findStep stepStats name 0 with        
        | ValueSome v -> v
        | ValueNone   -> raise (KeyNotFoundException $"Step: '{name}' is not found.")         

    /// Finds the `StepStats` with the specified step name.
    /// Returns the matched `StepStats` if found, otherwise `null`.            
    [<Extension>]
    static member Find(stepStats: StepStats[], name: string) =
        findStep stepStats name 0
        |> ValueOption.defaultValue(Unchecked.defaultof<_>)        
        
    /// Checks if a `StepStats` with the specified step name exists.
    /// Returns `true` if found, otherwise `false`.
    [<Extension>]
    static member Exists(stepStats: StepStats[], name: string) =
        findStep stepStats name 0
        |> ValueOption.isSome
        
    /// Finds the `CounterStats` with the specified metric name.
    /// Returns the matched `CounterStats` if found, otherwise `null`.        
    [<Extension>]
    static member Find(counters: CounterStats[], metricName: string) =
        findCounterMetric counters metricName 0
        |> ValueOption.defaultValue(Unchecked.defaultof<_>)
        
    /// Retrieves the `CounterStats` with the specified metric name.
    /// Throws a `KeyNotFoundException` if the metric name is not found.        
    [<Extension>]
    static member Get(counters: CounterStats[], metricName: string) =        
        match findCounterMetric counters metricName 0 with        
        | ValueSome v -> v
        | ValueNone   -> raise (KeyNotFoundException $"Counter metric: '{metricName}' is not found.")
        
    /// Checks if a `CounterStats` with the specified metric name exists.
    /// Returns `true` if found, otherwise `false`.        
    [<Extension>]
    static member Exists(counters: CounterStats[], metricName: string) =
        findCounterMetric counters metricName 0
        |> ValueOption.isSome
        
    /// Finds the `GaugeStats` with the specified metric name.
    /// Returns the matched `GaugeStats` if found, otherwise `null`.          
    [<Extension>]
    static member Find(gauges: GaugeStats[], metricName: string) =
        findGaugeMetric gauges metricName 0
        |> ValueOption.defaultValue(Unchecked.defaultof<_>)
        
    /// Retrieves the `GaugeStats` with the specified metric name.
    /// Throws a `KeyNotFoundException` if the metric name is not found.        
    [<Extension>]
    static member Get(gauges: GaugeStats[], metricName: string) =        
        match findGaugeMetric gauges metricName 0 with        
        | ValueSome v -> v
        | ValueNone   -> raise (KeyNotFoundException $"Gauge metric: '{metricName}' is not found.")
        
    /// Checks if a `GaugeStats` with the specified metric name exists.
    /// Returns `true` if found, otherwise `false`.         
    [<Extension>]
    static member Exists(gauges: GaugeStats[], metricName: string) =
        findGaugeMetric gauges metricName 0
        |> ValueOption.isSome                                            