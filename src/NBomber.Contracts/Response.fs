namespace NBomber.FSharp

open System
open NBomber.Contracts
open System.Runtime.CompilerServices

module internal ResponseInternal =
    
    [<Literal>]
    let UnhandledExceptionCode = "-101"
    [<Literal>]
    let TimeoutStatusCode = "-100"
    [<Literal>]
    let OperationTimeoutMessage = "operation timeout"

    let okEmpty : Response<obj> =
        { StatusCode = ""; IsError = false; SizeBytes = 0; Message = ""; Payload = None }

    let failEmpty<'T> : Response<'T> =
        { StatusCode = ""; IsError = true; SizeBytes = 0; Message = ""; Payload = None }

    let failUnhandled<'T> (ex: Exception) : Response<'T> =
        { StatusCode = UnhandledExceptionCode; IsError = true; SizeBytes = 0; Message = ex.Message; Payload = None }

    let failTimeout<'T> : Response<'T> =
        { StatusCode = TimeoutStatusCode; IsError = true; SizeBytes = 0; Message = OperationTimeoutMessage; Payload = None }

type Response =

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    static member ok () = ResponseInternal.okEmpty

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    static member fail () = ResponseInternal.failEmpty<obj>

    static member inline ok<'T>(
        ?payload: 'T,
        ?statusCode: string,
        ?sizeBytes: int64,
        ?message: string) =

        { StatusCode = statusCode |> Option.defaultValue ""
          IsError = false
          SizeBytes = sizeBytes |> Option.defaultValue 0
          Message = message |> Option.defaultValue ""
          Payload = payload }

    static member inline fail<'T>(
        ?statusCode: string,
        ?message: string,
        ?payload: 'T,
        ?sizeBytes: int64) =

        { StatusCode = statusCode |> Option.defaultValue ""
          IsError = true
          SizeBytes = sizeBytes |> Option.defaultValue 0
          Message = message |> Option.defaultValue ""
          Payload = payload }
        
namespace NBomber.CSharp

open System
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open NBomber.Contracts
open NBomber.FSharp

type Response =

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    static member Ok() = ResponseInternal.okEmpty

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    static member Fail() = ResponseInternal.failEmpty<obj>

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    static member Ok(
        [<Optional;DefaultParameterValue("")>] statusCode: string,
        [<Optional;DefaultParameterValue(0L)>] sizeBytes: int64,
        [<Optional;DefaultParameterValue("")>] message: string) : Response<obj> =

        { StatusCode = statusCode
          IsError = false
          SizeBytes = sizeBytes
          Message = if isNull message then String.Empty else message
          Payload = None }

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    static member Ok<'T>(
        [<Optional;DefaultParameterValue("")>] statusCode: string,
        [<Optional;DefaultParameterValue(0L)>] sizeBytes: int64,
        [<Optional;DefaultParameterValue("")>] message: string) : Response<'T> =

        { StatusCode = statusCode
          IsError = false
          SizeBytes = sizeBytes
          Message = if isNull message then String.Empty else message
          Payload = None }

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    static member Ok<'T>(
        payload: 'T,
        [<Optional;DefaultParameterValue("")>] statusCode: string,
        [<Optional;DefaultParameterValue(0L)>] sizeBytes: int64,
        [<Optional;DefaultParameterValue("")>] message: string) : Response<'T> =

        { StatusCode = statusCode
          IsError = false
          SizeBytes = sizeBytes
          Message = if isNull message then String.Empty else message
          Payload = Some payload }

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    static member Fail(
        [<Optional;DefaultParameterValue("")>] statusCode: string,
        [<Optional;DefaultParameterValue("")>] message: string,
        [<Optional;DefaultParameterValue(0L)>] sizeBytes: int64) : Response<obj> =

        { StatusCode = statusCode
          IsError = true
          SizeBytes = sizeBytes
          Message = if isNull message then String.Empty else message
          Payload = None }

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    static member Fail<'T>(
        [<Optional;DefaultParameterValue("")>] statusCode: string,
        [<Optional;DefaultParameterValue("")>] message: string,
        [<Optional;DefaultParameterValue(0L)>] sizeBytes: int64) : Response<'T> =

        { StatusCode = statusCode
          IsError = true
          SizeBytes = sizeBytes
          Message = if isNull message then String.Empty else message
          Payload = None }

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    static member Fail<'T>(
        payload: 'T,
        [<Optional;DefaultParameterValue("")>] statusCode: string,
        [<Optional;DefaultParameterValue("")>] message: string,
        [<Optional;DefaultParameterValue(0L)>] sizeBytes: int64) : Response<'T> =

        { StatusCode = statusCode
          IsError = true
          SizeBytes = sizeBytes
          Message = if isNull message then String.Empty else message
          Payload = Some payload }        