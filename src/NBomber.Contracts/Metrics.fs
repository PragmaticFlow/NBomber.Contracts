namespace NBomber.Contracts.Metrics

type GaugeStats = {
    ScenarioName: string
    MetricName: string
    UnitOfMeasure: string
    Value: float
}

type CounterStats = {
    ScenarioName: string
    MetricName: string
    UnitOfMeasure: string
    Value: int64
}

type MetricStats = {
    Counters: CounterStats[]
    Gauges: GaugeStats[]
}
with
    [<CompiledName("Empty")>]
    static member empty = {
        Counters = Array.empty
        Gauges = Array.empty
    }
  
/// Represents a counter metric that tracks a cumulative value.  
type ICounter =    
    /// Gets the name of the metric.    
    abstract MetricName: string
    /// Gets the unit of measure associated with the metric.
    abstract UnitOfMeasure: string
    /// Adds the value to the counter metric.
    abstract Add: value:int64 -> unit
    
/// Represents a gauge metric that tracks a fluctuating value.    
type IGauge =
    /// Gets the name of the metric.
    abstract MetricName: string
    /// Gets the unit of measure associated with the metric.
    abstract UnitOfMeasure: string
    /// Sets the gauge to the specified value.
    abstract Set: value:float -> unit    
    
type internal Counter(metricName: string, unitOfMeasure: string) =
    
    let mutable _publishFn = None // metricName * value
    
    let add value =
        match _publishFn with
        | Some publish -> publish metricName value
        | None         -> ()
    
    interface ICounter with
        member this.MetricName = metricName
        member this.UnitOfMeasure = unitOfMeasure    
        member this.Add(value) = add value
    
    member this.Init(publishFn: string -> int64 -> unit) =
        _publishFn <- Some publishFn
        
type internal Gauge(metricName: string, unitOfMeasure: string) =
    
    let mutable _publishFn = None // metricName * value
    
    let add value =
        match _publishFn with
        | Some publish -> publish metricName value
        | None         -> ()
    
    interface IGauge with
        member this.MetricName = metricName
        member this.UnitOfMeasure = unitOfMeasure    
        member this.Set(value) = add value
    
    member this.Init(publishFn: string -> float -> unit) =
        _publishFn <- Some publishFn        
    