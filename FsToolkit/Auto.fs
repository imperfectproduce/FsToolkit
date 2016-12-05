namespace FsToolkit

open System
open System.Collections.Generic

[<AutoOpen>]
module Auto = 
    ///Coalesce option value
    let inline (|?) x y = defaultArg x y
    ///Coalesce null value
    let inline (|??) (x:'a) y = if obj.ReferenceEquals(x, Unchecked.defaultof<'a>) then y else x

    type IDictionary<'k,'v> with
        /// Gets the value associated with the specified key, or None if key not found.
        member inline x.TryGet key =
          match x.TryGetValue key with
          | true, v -> Some v
          | _ -> None

    /// Converts the result of a TryParse() method to an Option.
    let inline tryParse (s : string) : ^o option =
        let mutable o = Unchecked.defaultof<(^o)>
        if (^o : (static member TryParse : string * ^o byref -> bool) (s, &o)) then
            Some o
        else 
            None

    module String =
        /// True if s is null or whitespace.
        let inline isEmpty s = String.IsNullOrWhiteSpace(s)

        /// Trims a string, returning None if string is null or whitespace.
        let trimToOption str =
            if isEmpty str then None
            else Some (str.Trim())  

    type Uri with
        /// Gets the part of the URI before the last '/' character.
        member x.FirstPart =
            let uri = x.AbsoluteUri
            match uri.LastIndexOf '/' with
            | i when i > 0 -> uri.Substring(0, i)
            | _ -> String.Empty

        /// Gets the remainder of the URI after the last '/' character.
        member x.LastPart =
            let uri = x.AbsoluteUri
            match uri.LastIndexOf '/' with
            | i when i > 0 && uri.Length > i -> uri.Substring(i + 1)
            | _ -> String.Empty