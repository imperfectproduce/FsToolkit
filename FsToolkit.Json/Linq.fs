namespace FsToolkit.Json

open System
open Newtonsoft.Json.Linq
open Microsoft.FSharp.Reflection
open System.Reflection

module Linq =

    ///True if missing or null valued property
    let isNullToken (j: JObject) = 
        isNull j || j.Type = JTokenType.Null

    ///None if 't is a type of Option<_>, null if 't is a reference type, default if 't is a value type
    let inline nullTokenObject<'t> =
        if typedefof<'t> = typedefof<FSharp.Core.Option<_>> then
            let cases = FSharpType.GetUnionCases (typeof<'t>)
            FSharpValue.MakeUnion (cases.[0], [||]) :?> 't
        else
            Unchecked.defaultof<'t>

    ///Case tolerant get property value (exact case is attempted first)
    let getPropertyValue (j:JObject) (n:string) =
        let jn' = j.[n]
        if jn' = null then
            j.Properties()
            |> Seq.tryFind (fun p -> String.Equals(p.Name, n, StringComparison.OrdinalIgnoreCase))
            |> (function None -> null | Some(p) -> p.Value)
        else
            jn'

    ///Null and case tolerant dyanmic JObject selection with support for
    ///option type wrappers around results.
    let inline (?) (j : JObject) (n : string) : 't =
        if isNullToken j then
            nullTokenObject<'t>
        else
            let jn = getPropertyValue j n
            if isNull jn || jn.Type = JTokenType.Null then
                nullTokenObject<'t>
            else
                if typedefof<'t> = typedefof<FSharp.Core.Option<_>> then
                    let cases = FSharpType.GetUnionCases (typeof<'t>)
                    let args = typeof<'t>.GetGenericArguments ()
                    let optionOf =
                        match args.[0].IsValueType with
                        | true -> (typedefof<Nullable<_>>).MakeGenericType ([| args.[0] |])
                        | _ -> args.[0]
                    let value = jn.ToObject(optionOf)
                    FSharpValue.MakeUnion (cases.[1], [| value |]) :?> 't
                else
                    if jn.GetType() = typeof<'t> then
                        jn :> obj :?> 't
                    else
                        jn.ToObject<'t>()

    ///Helper for creating Maps of string * obj for serialization to json
    ///e.g. `dict [P("key1" 2); P("key2", "value")]`
    let J x y = x.ToString(), y :> obj

