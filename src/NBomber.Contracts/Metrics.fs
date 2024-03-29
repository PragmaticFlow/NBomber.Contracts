﻿namespace NBomber.Contracts.Stats

open System
open MessagePack

[<CLIMutable>]
[<MessagePackObject>]
type MetricPercentiles = {
    [<Key 0>] Mean: float
    [<Key 1>] Max: float
    [<Key 2>] Percent50: float
    [<Key 3>] Percent75: float
    [<Key 4>] Percent95: float
    [<Key 5>] Percent99: float    
}

/// MetricType represents various metrics, each providing its usefulness depending on the tracked measurement.
type MetricType =    
    /// A Gauge represents a measure of a value where the value arbitrarily increases or decreases, for example, CPU usage, RAM usage.
    | Gauge = 1

type MetricHistoryValue = {    
    [<Key 0>] Value: float
    [<Key 1>] Timestamp: TimeSpan
}

[<CLIMutable>]
[<MessagePackObject>]
type MetricStats = {
    [<Key 0>] Name: string
    [<Key 1>] MeasureUnit: string
    [<Key 2>] MetricType: MetricType
    [<Key 3>] History: MetricHistoryValue list
    [<Key 4>] Percentiles: MetricPercentiles
}