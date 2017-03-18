namespace FsToolkit.Json.Tests

open System
open Swensen.Unquote
open NUnit.Framework
open Newtonsoft.Json.Linq
open FsToolkit.Json.Serialization

module SerializtionTests =

    type Du1 =
        | Case1
        | Case2

    type Du2 =
        | CaseA of Du1
        | CaseB of Du1 Option
        | CaseC of string
        | CaseD of string option
        | CaseE of int
        | CaseF of int option
        | CaseG

    [<Test>]
    let ``round-trip DU including option cases`` () =
        let values = [
            CaseA(Case2)
            CaseB(None)
            CaseB(Some Case2)
            CaseC("hello")
            CaseD(None)
            CaseD(Some "hello")
            CaseE(3)
            CaseF(None)
            CaseF(Some 3)
            CaseG
        ]
        for value in values do
            let json = serialize Storage value
            let value' = deserialize Storage json
            test <@ value = value' @>

    

