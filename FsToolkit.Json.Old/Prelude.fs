namespace FsToolkit.Json

open System
open Microsoft.FSharp.Reflection
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open System.ComponentModel
open System.Reflection

[<System.Obsolete>] //marking as obsolete is a workaround F# not honoring EditorBrowsable(EditorBrowsableState.Never) to hide intellisense discoverability, thanks to Tomas Petricek's answer on SO: http://stackoverflow.com/questions/6527141/is-it-possible-to-mark-a-module-function-as-hidden-from-intellisense-discovery/6527933#6527933
module Prelude =
    let memoize (f: 'a -> 'b) =
        let cache = System.Collections.Concurrent.ConcurrentDictionary<'a, 'b>()
        fun x -> cache.GetOrAdd(x, f)

    /// True if a given generic Type definition matches a given Type.
    let private isGeneric td (t : Type) = t.IsGenericType && t.GetGenericTypeDefinition() = td

    let private _isList : Type -> bool = isGeneric typedefof<FSharp.Collections.List<_>>
    let isList : Type -> bool = memoize _isList

    let private _isOption : Type -> bool = isGeneric typedefof<FSharp.Core.Option<_>>
    let isOption : Type -> bool = memoize _isOption

    let getUnionCases : Type -> UnionCaseInfo[] = memoize FSharpType.GetUnionCases

    let isTuple : Type -> bool = memoize FSharpType.IsTuple 

    let getTupleElements : Type -> Type[] = memoize FSharpType.GetTupleElements

    let isUnion : Type -> bool = memoize FSharpType.IsUnion 

    let getGenericArguments : Type -> Type[] = memoize (fun (ty:Type) -> ty.GetGenericArguments ()) 
