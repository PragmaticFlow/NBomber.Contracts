namespace NBomber.Contracts.Metrics

open System
open MessagePack

/// MetricType represents various metrics, each providing its usefulness depending on the tracked measurement.
type MetricType =
    /// A Counter metric is a type of metric for representing a custom cumulative values. Counters are typically used to measure events such as requests, errors, or processed jobs.
    | Counter = 0
    /// A Gauge metric is a type of metric holding only the latest value added. It's used in monitoring to represent a value that can go up and down over time. It is commonly used to track measurements that fluctuate, such as: CPU usage, memory usage, disk space, temperature, active connections, queue length.
    | Gauge = 1

type internal MetricHistoryValue = {    
    [<Key 0>] Value: float
    [<Key 1>] Timestamp: TimeSpan
}

[<CLIMutable; MessagePackObject>]
type internal MetricStats = {
    [<Key 0>] Name: string
    [<Key 1>] MeasureUnit: string
    [<Key 2>] MetricType: MetricType
    [<Key 3>] History: MetricHistoryValue[]    
}

type GaugeMetric = {
    MetricName: string
    Value: float
}

type CounterMetric = {
    MetricName: string
    Value: int64
}

type MetricStats2 = {
    Counters: CounterMetric[]
    Gauges: GaugeMetric[]
}

type IMetric =
    abstract MetricName: string
    abstract UnitOfMeasure: string

type ICounter =
    inherit IMetric
    abstract Add: value:int64 -> unit
    
type IGauge =
    inherit IMetric
    abstract Set: value:float -> unit