namespace FsToolkit

///Type for Railway Oriented Programming: http://fsharpforfunandprofit.com/posts/recipe-part2/
type Result<'TSuccess,'TFailure> =
    | Success of 'TSuccess
    | Failure of 'TFailure

module Result =
    let bind f r =
        match r with
        | Success x -> f(x)
        | Failure x -> Failure x
    let (>>=) f r = bind f r

    let switch f x = 
        f x |> Success

    let map f r =
        match r with
        | Success x -> Success(f x)
        | Failure x -> Failure x

module AsyncResult =
    let bind f r = async {
        let! r = r
        match r with
        | Success x -> return! f(x)
        | Failure x -> return Failure x
    }
    let (>>=) a b = bind b a

    let map f r = async {
        let! r = r
        match r with
        | Success x -> 
            let! x' = f x
            return Success(x')
        | Failure x -> return Failure x
    }

    let switch f x = async {
        let! x = f x
        return x |> Success
    }
