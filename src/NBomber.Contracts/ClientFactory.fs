namespace NBomber.Contracts

open System.Threading.Tasks

module internal Constants =
    
    [<Literal>]
    let DefaultClientCount = 1

type ClientFactory<'TClient>(name: string,
                             clientCount: int,
                             initClient: int * IBaseContext -> Task<'TClient>, // number * context
                             disposeClient: 'TClient * IBaseContext -> Task) =

    // we use lazy to prevent multiply initialization in one scenario
    // also, we do check on duplicates (that has the same name but different implementation) within one scenario
    let untypedFactory = lazy (
        ClientFactory<obj>(name, clientCount,
            initClient = (fun (number,token) -> task {
                let! client = initClient(number, token)
                return client :> obj
            }),
            disposeClient = (fun (client,context) -> disposeClient(client :?> 'TClient, context))
        )
    )

    member _.FactoryName = name
    member _.ClientCount = clientCount
    
    member internal _.GetUntyped() = untypedFactory.Value
    member internal _.Clone(newName: string) = ClientFactory<'TClient>(newName, clientCount, initClient, disposeClient)
    member internal _.Clone(newClientCount: int) = ClientFactory<'TClient>(name, newClientCount, initClient, disposeClient)
    member internal _.InitClient(number, context) = initClient(number, context)
    member internal _.DisposeClient(client, context) = disposeClient(client, context)
    
namespace NBomber.FSharp

open System
open System.Runtime.InteropServices
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
                          [<Optional;DefaultParameterValue(Constants.DefaultClientCount)>] ?clientCount: int) =

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
        
        let count = defaultArg clientCount Constants.DefaultClientCount
        ClientFactory(name, count, initClient, dispose)
        
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
         [<Optional;DefaultParameterValue(Constants.DefaultClientCount)>] clientCount: int) =

        let defaultDispose = (fun (client,context) ->
            match client :> obj with
            | :? IDisposable as d -> d.Dispose()
            | _ -> ()
            Task.CompletedTask
        )

        let dispose =
            if isNull(disposeClient :> obj) then defaultDispose
            else disposeClient.Invoke

        ClientFactory(name, clientCount, initClient.Invoke, dispose)