namespace NBomber.Contracts

open System.Collections.Generic
open System.Threading.Tasks

type IClientFactory<'TClient> =
    abstract FactoryName: string    
    abstract ClientCount: int
    abstract InitializedClients: IReadOnlyList<'TClient>
    
type internal IUntypedClientFactory =
    abstract FactoryName: string    
    abstract ClientCount: int
    abstract SetName: name:string -> unit
    abstract SetClientCount: count:int -> unit
    abstract GetClient: number:int -> obj
    abstract InitClient: number:int * context:IBaseContext -> Task<unit>
    abstract DisposeClient: number:int * context:IBaseContext -> Task    

type internal ClientFactory<'TClient>(name: string,
                                      clientCount: int,
                                      initClient: int * IBaseContext -> Task<'TClient>, // number * context
                                      disposeClient: 'TClient * IBaseContext -> Task) =

    let mutable _factoryName = name
    let mutable _clientCount = clientCount
    let _initializedClients = ResizeArray<_>()

    interface IClientFactory<'TClient> with
        member _.FactoryName = _factoryName
        member _.ClientCount = _clientCount
        member _.InitializedClients = _initializedClients :> IReadOnlyList<_>
    
    interface IUntypedClientFactory with
        member _.FactoryName = _factoryName
        member _.ClientCount = _clientCount
        member _.SetName(name) = _factoryName <- name
        member _.SetClientCount(count) = _clientCount <- count
        member _.GetClient(number) = _initializedClients[number]
        
        member _.InitClient(number, context) = task {
            let! client = initClient(number, context)
            _initializedClients.Add client
        }
        
        member _.DisposeClient(number, context) = task {
            let client = _initializedClients[number]
            do! disposeClient(client, context)
        }
    
namespace NBomber.FSharp

open System
open System.Threading.Tasks
open NBomber.Contracts

/// ClientFactory helps create and initialize API clients to work with specific API or protocol (HTTP, WebSockets, gRPC, GraphQL).
[<RequireQualifiedAccess>]
type ClientFactory =
    
    /// Creates ClientFactory.
    /// ClientFactory helps create and initialize API clients to work with specific API or protocol (HTTP, WebSockets, gRPC, GraphQL).
    static member create (name: string,
                          initClient: int * IBaseContext -> Task<'TClient>,
                          ?disposeClient: 'TClient * IBaseContext -> Task<unit>,
                          ?clientCount: int) =

        let defaultDispose = (fun (client,context) ->
            match client :> obj with
            | :? IDisposable as d -> d.Dispose()
            | _ -> ()
            Task.CompletedTask
        )

        let dispose =
            disposeClient
            |> Option.map(fun dispose -> fun (c,ctx) -> dispose(c,ctx) :> Task)
            |> Option.defaultValue defaultDispose        
        
        let count = defaultArg clientCount 1
        ClientFactory(name, count, initClient, dispose) :> IClientFactory<_>
        
namespace NBomber.CSharp

open System
open System.Runtime.InteropServices
open System.Threading.Tasks
open NBomber.Contracts

/// ClientFactory helps you create and initialize API clients to work with specific API or protocol (HTTP, WebSockets, gRPC, GraphQL).
type ClientFactory =

    /// Creates ClientFactory.
    /// ClientFactory helps create and initialize API clients to work with specific API or protocol (HTTP, WebSockets, gRPC, GraphQL).
    static member Create
        (name: string,
         initClient: Func<int,IBaseContext,Task<'TClient>>,
         [<Optional;DefaultParameterValue(null)>] disposeClient: Func<'TClient,IBaseContext,Task>,
         [<Optional;DefaultParameterValue(1)>] clientCount: int) =

        let defaultDispose = (fun (client,context) ->
            match client :> obj with
            | :? IDisposable as d -> d.Dispose()
            | _ -> ()
            Task.CompletedTask
        )

        let dispose =
            if isNull(disposeClient :> obj) then defaultDispose
            else disposeClient.Invoke

        ClientFactory(name, clientCount, initClient.Invoke, dispose) :> IClientFactory<_>