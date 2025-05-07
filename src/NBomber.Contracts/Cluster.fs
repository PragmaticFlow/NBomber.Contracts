namespace NBomber.Contracts.Cluster

open System
open MessagePack
open NBomber.Contracts.Stats

type ClusterCommand =
    | StopScenario of scenarioName:string * reason:string
    | StopTest     of reason:string
    | GetClusterInfo

[<CLIMutable; MessagePackObject>]
type ClusterInfo = {
    [<Key 0>] StartTime: DateTime
    [<Key 1>] Duration: TimeSpan
    [<Key 2>] CurrentDuration: TimeSpan
    [<Key 3>] CurrentOperation: OperationType
}

