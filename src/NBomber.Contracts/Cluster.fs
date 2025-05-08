namespace NBomber.Contracts.Cluster

open System
open MessagePack
open NBomber.Contracts.Stats

[<CLIMutable; MessagePackObject>]
type MessageHeaders = {
    [<Key 0>] CorrelationId: string
    [<Key 1>] ClientId: string
    [<Key 2>] SessionId: string
    [<Key 3>] ResponseTopic: string option
}

[<CLIMutable; MessagePackObject>]
type Message<'T> = {
    [<Key 0>] Headers: MessageHeaders
    [<Key 1>] Payload: 'T
}

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

