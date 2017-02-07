namespace FsToolkit.Json.Tests

open System
open Swensen.Unquote
open NUnit.Framework
open Newtonsoft.Json.Linq
open FsToolkit.Json.Linq

module LinqTests =
    
    [<Test>]
    let ``get existing value type property`` () =
        let json = JObject.Parse """{ hello: 3 }"""
        test <@ json?hello = 3 @>

    [<Test>]
    let ``get existing value type property of option type`` () =
        let json = JObject.Parse """{ hello: 3 }"""
        test <@ json?hello = Some(3) @>

    [<Test>]
    let ``get missing value type property`` () =
        let json = JObject.Parse """{ }"""
        test <@ json?hello = 0 @>

    [<Test>]
    let ``get missing value type property of option type`` () =
        let json = JObject.Parse """{ }"""
        test <@ json?hello = (None:int option) @>
        
    [<Test>]
    let ``missing property chain`` () =
        let json = JObject.Parse """{ hello: {} }"""
        test <@ json?hello?world?goodbye = 0 @>

    [<Test>]
    let ``missing property chain of option`` () =
        let json = JObject.Parse """{ hello: {} }"""
        test <@ json?hello?world?goodbye = (None:int option) @>

    [<Test>]
    let ``get value in property`` () =
        let json = JObject.Parse """{ hello: { world: 3 } }"""
        test <@ json?hello?world = 3 @>

    [<Test>]
    let ``guid conversion`` () =
        let guid = Guid.NewGuid()
        let json = JObject.Parse <| """{ hello: '""" + guid.ToString() + """' }"""
        test <@ json?hello = guid @>

    [<Test>]
    let ``case tolerance`` () =
        let json = JObject.Parse """{ hello: { world: 3 } }"""
        test <@ json?Hello?WORLD = 3 @>

    type Test1 = { Prop1: string; Prop2: int }

    [<Test>]
    let ``convert record type`` () =
        let json = JObject.Parse """{ hello: { Prop1: "a", Prop2: 3 } }"""
        test <@ json?hello = { Prop1="a"; Prop2=3 } @>

    [<AllowNullLiteral>]
    type Test2() =
        member val Prop1 = "" with get, set
        member val Prop2 = 0 with get, set

    [<Test>]
    let ``convert regular type`` () =
        let json = JObject.Parse """{ hello: { Prop1: "a", Prop2: 3 } }"""
        let actual: Test2 = json?hello
        test <@ actual.Prop1 = "a" && actual.Prop2 = 3 @>

    [<Test>]
    let ``convert regular type when null`` () =
        let json = JObject.Parse """{ hello: null }"""
        let actual: Test2 = json?hello
        test <@ actual = null @>

    [<Test>]
    let ``list of values`` () =
        let json = JObject.Parse """{ hello: [1,2,3] }"""
        test <@ json?hello = [1;2;3] @>

    [<Test>]
    let ``filter list of values`` () =
        let json = JObject.Parse """{ hello: [1,2,3] }"""
        test <@ json?hello |> List.filter (fun x -> x > 1) = [2;3] @>

    [<Test>]
    let ``seq of values`` () =
        let json = JObject.Parse """{ hello: [1,2,3] }"""
        test <@ json?hello |> Seq.tryHead = Some 1 @>

    [<Test>]
    let ``list of objects`` () =
        let json = JObject.Parse """{ hello: [{ Prop1: "a", Prop2: 3 }] }"""
        test <@ json?hello = [{Prop1="a"; Prop2=3}] @>

    [<Test>]
    let ``seq of json objects`` () =
        let json = JObject.Parse """{ hello: [{ Prop1: "a", Prop2: 3 }, { Prop1: "b", Prop2: 4 }] }"""
        test <@ json?hello |> Seq.map (fun j -> j?Prop1) |> Seq.toList = ["a";"b"] @>

    [<Test>]
    let ``objects in list not copied on iterating`` () =
        let json = JObject.Parse """{ hello: [{ Prop1: "a", Prop2: 3 }, { Prop1: "b", Prop2: 4 }] }"""
        let xl = (json?hello : JArray) |> Seq.map id |> Seq.toList
        let yl = (json?hello : JArray) |> Seq.map id |> Seq.toList
        test <@ xl = yl @>

