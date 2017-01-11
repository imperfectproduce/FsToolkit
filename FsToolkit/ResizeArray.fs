namespace FsToolkit

open System
open System.Collections.Generic

module ResizeArray =
    let remove predicate (xs: ResizeArray<'t>) =
        let i = xs.FindIndex(Predicate predicate)
        xs.RemoveAt i

    let replace predicate item (xs: ResizeArray<'t>) =
        let i = xs.FindIndex(Predicate predicate)
        xs.[i] <- item

    let replaceOrAdd predicate item (xs: ResizeArray<'t>) =
        match xs.FindIndex(Predicate predicate) with
        | -1 -> xs.Add(item)
        | i  -> xs.[i] <- item
