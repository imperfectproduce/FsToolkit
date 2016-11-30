namespace FsToolkit

open System

[<AutoOpen>]
module Auto = 
    /// True if s is null or whitespace.
    let inline isEmpty s = String.IsNullOrWhiteSpace(s)
    ///Coalesce option value
    let inline (|?) x y = defaultArg x y
    ///Coalesce null value
    let inline (|??) (x:'a) y = if obj.ReferenceEquals(x, Unchecked.defaultof<'a>) then y else x

    module String =
        /// Trims a string, returning None if string is null or whitespace.
        let trimToOption str =
            if isEmpty str then None
            else Some (str.Trim())  