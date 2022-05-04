namespace FsToolkit

///A basic Result builder
type ResultBuilder () =
    member this.Bind(x, f) = Result.bind f x
    member this.ReturnFrom(x:Result<_,_>) = x
    member this.Return x = Ok x
    member this.Zero () = Ok ()

[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ResultBuilder =
    ///A builder for Result<'T,'TError> types
    let result = ResultBuilder ()

///Extensions to the Result module in FSharp.Core
module Result =
    let (>>=) r f = Result.bind f r

    let switch f x =
        f x |> Ok

    let partition rs =
        let isOk r =
            match r with
            | Ok _ -> true
            | _ -> false
        rs
        |> Seq.groupBy isOk
        |> Map.ofSeq
        |> (fun m ->
            let oks = Map.tryFind true m
                      |> Option.map (Seq.map (fun r -> match r with | Ok x -> x | x -> failwithf "Unexpected result: %A" x))
                      |> (fun rs -> match rs with | None -> Seq.empty | Some x -> x)
            let errs = Map.tryFind false m
                       |> Option.map (Seq.map (fun r -> match r with | Error x -> x | x -> failwithf "Unexpected result: %A" x))
                       |> (fun rs -> match rs with | None -> Seq.empty | Some x -> x)
            (oks, errs))

    let oks rs =
        rs
        |> partition
        |> fst

    let errors rs =
        rs
        |> partition
        |> snd

module AsyncResult =
    let bind f r = async {
        let! r = r
        match r with
        | Ok x -> return! f(x)
        | Error x -> return Error x
    }
    let (>>=) r f = bind f r

    let map f r = async {
        let! r = r
        match r with
        | Ok x ->
            let! x' = f x
            return Ok(x')
        | Error x -> return Error x
    }
    
    let mapError f r = async {
        let! r = r
        match r with
        | Ok x -> return Ok x
        | Error x ->
            let! x' = f x
            return Error(x')
    }

    let switch f x = async {
        let! x = f x
        return x |> Ok
    }
