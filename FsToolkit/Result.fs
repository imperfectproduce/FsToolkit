namespace FsToolkit

//Matches definition from https://github.com/Microsoft/visualfsharp/pull/964
///Type for Railway Oriented Programming: http://fsharpforfunandprofit.com/posts/recipe-part2/
[<StructuralEquality; StructuralComparison>]
[<CompiledName("FSharpResult`2")>]
type Result<'T,'TError> = 
    | Ok of 'T 
    | Error of 'TError

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
        |> (fun m -> (Map.tryFind true m, Map.tryFind false m)

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
