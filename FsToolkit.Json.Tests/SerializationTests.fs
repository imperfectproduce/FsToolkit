namespace FsToolkit.Json.Tests

open System
open Swensen.Unquote
open NUnit.Framework
open Newtonsoft.Json.Linq
open FsToolkit.Json.Serialization

module SerializtionTests =
    module Client =
        type EnumDu =
            | A
            | B
        
        type JsonObj1 = { Field1: EnumDu }
        type JsonObj2 = { FieldA: EnumDu option }
        [<Test>]
        let ``round-trip client DU`` () =
            let value = { Field1=B }
            let json = serialize Client value
            test <@ json = "{\"field1\":\"B\"}" @>
            let value' = deserialize Client json
            test <@ value = value' @>

        [<Test>]
        let ``round-trip client DU including option cases`` () =
            let values = [
                { FieldA=None }
                { FieldA=Some(A) }
            ]
            for value in values do
                let json = serialize Client value
                let value' = deserialize Client json
                test <@ value = value' @>

    module Storage =
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
        let ``round-trip storage DU including option cases`` () =
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

    

