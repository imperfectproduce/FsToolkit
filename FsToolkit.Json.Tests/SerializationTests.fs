namespace FsToolkit.Json.Tests

open System
open Swensen.Unquote
open NUnit.Framework
open Newtonsoft.Json.Linq
open FsToolkit.Json.Serialization
open System.Collections.Generic

module SerializtionTests =
    module Client =
        type EnumDu =
            | A
            | B

        type JsonObj1<'a> = { Field1: 'a }
        type JsonObj2<'a> = { FieldA: 'a option }
        [<Test>]
        let ``round-trip client DU`` () =
            let value = { Field1=B }
            let json = serialize Client value
            test <@ json = "{\"field1\":\"B\"}" @>
            let value' = deserialize Client json
            test <@ value = value' @>

        [<Test>]
        let ``None cases client DU`` () =
            let expected = { FieldA=(None:EnumDu option) }
            let jsonCases = [
                "{\"field1\":\"\"}"
                "{\"field1\":null}"
            ]
            for json in jsonCases do
                let actual = deserialize Client json
                test <@ actual = expected @>

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

        [<Test>]
        let ``map insertion order`` () =
            let json = """{ b: 1, a: 2 }"""
            let value: FsToolkit.InsertionDictionary<string,int> = deserialize Client json
            let expected = [("b", 1); ("a", 2)]
            let actual = value |> Seq.map (|KeyValue|) |> Seq.toList
            test <@ expected = actual @>

        type SingleValueDu =
            | Case1 of int
            | Case2 of int * string
            | Case3 of a:int * b:string * d:string

        [<Test>]
        let ``round-trip client DU single value`` () =
            let values = [
                Case1(3)
                Case2(3,"4")
                Case3(3,"4","5")
            ]
            for value in values do
                let json = serialize Client value
                let value' = deserialize Client json
                test <@ value = value' @>
    module OneWayToClient =

        [<Test>]
        let ``deserializing is prohibited `` () =
            raises<FsToolkit.Json.Converters.JsonDeserializingNotSupportedException>
                <@
                    let json = """{"foo":"bar"}"""
                    deserialize OneWayToClient json
                @>
        type OneWayDuCaseNestedDu =
            | OneWayDuCaseNestedDuCaseEnumLike
            | OneWayDuCaseNestedDuCaseInt of int
        type OneWayDu =
            | OneWayDuCaseInt of int
            | OneWayDuCaseBool of bool
            | OneWayDuCaseTuple of int * string
            | OneWayDuCaseList of int list
            | OneWayDuCaseSeq of int seq
            | OneWayDuCaseMap of Map<string, int>
            | OneWayDuCaseEnumLike
            | OneWayDuCaseRecord of {| foo: string; bar: int |}
            | OneWayDuCaseOption of int option
            | OneWayDuCaseNestedDu of OneWayDuCaseNestedDu
        [<Test>]

        let ``serialize client DU values and strip out cases `` () =
            let values = [
                OneWayDuCaseInt(3)
                OneWayDuCaseBool true
                OneWayDuCaseTuple(3,"4")
                OneWayDuCaseList [3]
                OneWayDuCaseList [3; 4]
                OneWayDuCaseSeq ([3; 4] |> List.toSeq)
                OneWayDuCaseMap (Map [ ("foo", 3); ("bar", 4) ])
                OneWayDuCaseEnumLike
                OneWayDuCaseRecord({|foo = "fooValue"; bar = 3|})
                OneWayDuCaseOption (Some 3)
                OneWayDuCaseOption None
                OneWayDuCaseNestedDu (OneWayDuCaseNestedDuCaseInt 3)
            ]
            let expectedResults = [
                "3"
                "true"
                """[3,"4"]"""
                "[3]"
                "[3,4]"
                "[3,4]"
                """{"bar":4,"foo":3}"""
                "\"OneWayDuCaseEnumLike\""
                """{"bar":3,"foo":"fooValue"}"""
                "3"
                "null"
                "3"
            ]
            for (value, expected) in (List.zip values expectedResults) do
                let json = serialize OneWayToClient value
                test <@ expected = json @>

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
