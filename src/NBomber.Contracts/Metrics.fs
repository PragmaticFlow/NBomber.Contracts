namespace NBomber.Contracts.Stats

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

type MetricType =
    | Histogram = 0
    | Gauge = 1

[<CLIMutable>]
[<MessagePackObject>]
type MetricStats = {
    [<Key 0>] Name: string
    [<Key 1>] MeasureUnit: string
    [<Key 2>] MetricType: MetricType
    [<Key 3>] Current: float
    [<Key 4>] Max: float
    [<Key 5>] Duration: TimeSpan
    [<Key 6>] Percentiles: MetricPercentiles option
}