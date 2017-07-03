namespace FsToolkit

open System

module ResizeArray =
    ///Remove the item matching the predicate
    let remove predicate (xs: ResizeArray<'t>) =
        let i = xs.FindIndex(Predicate predicate)
        xs.RemoveAt i

    ///Try remove the item matching the predicate, returning it if exists
    let tryRemove predicate (xs: ResizeArray<'t>) =
        match xs.FindIndex(Predicate predicate) with
        | -1 -> None
        | i ->
            let item = xs.[i]
            xs.RemoveAt i
            Some(item)

    ///Replace the item matching the predicate with the given replacement item
    let replace predicate item (xs: ResizeArray<'t>) =
        let i = xs.FindIndex(Predicate predicate)
        xs.[i] <- item

    ///Replace or add (to the end) the item matching the predicate with the given replacement item
    let replaceOrAdd predicate item (xs: ResizeArray<'t>) =
        match xs.FindIndex(Predicate predicate) with
        | -1 -> xs.Add(item)
        | i  -> xs.[i] <- item

    ///Replace the item matching the given predicate with the result of `f`
    let update predicate f (xs:ResizeArray<_>) =
        let i = xs.FindIndex(Predicate predicate)
        let item = xs.[i]
        xs.[i] <- f item
