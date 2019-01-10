namespace FsToolkit

module Result =
    let bind f r =
        match r with
        | Ok x -> f(x)
        | Error x -> Error x
    let (>>=) r f = bind f r

    let switch f x = 
        f x |> Ok

    let map f r =
        match r with
        | Ok x -> Ok(f x)
        | Error x -> Error x

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

    let switch f x = async {
        let! x = f x
        return x |> Ok
    }
