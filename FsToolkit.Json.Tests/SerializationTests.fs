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
        let ``None cases client DU`` () =
            let expected = { FieldA=None }
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

        //type InsertionDictionary<'T, 'K when 'T : equality>() = 
        //    let dictionary = new ResizeArray<KeyValuePair<'T, 'K>>()

        //    interface IDictionary<'T,'K> with
        //        member this.Add(key, value) = dictionary.Add(KeyValuePair<'T,'K>(key, value))
        //        member this.Add(kvp) = dictionary.Add(kvp)
        //        member this.ContainsKey(key) = dictionary |> Seq.exists(fun kvp -> kvp.Key = key)
        //        //member this.Contains(kvp) = dictionary.TryGetValue(kvp.Key.ToUpper(), ref kvp.Value)
        //        //member this.Item with get key = dictionary.Item(key) and set key value = dictionary.Item(key) <- value
        //        //member this.Count with get() = dictionary.Count
        //        //member this.IsReadOnly with get() = false
        //        //member this.Keys = dictionary.Keys :> ICollection<String>
        //        //member this.Remove key = dictionary.Remove(key)
        //        //member this.Remove(kvp : KeyValuePair<String,String>) = dictionary.Remove(kvp.Key.ToUpper())
        //        //member this.TryGetValue(key, value) = dictionary.TryGetValue(key, ref value)
        //        //member this.Values = dictionary.Values :> ICollection<String>
        //        //member this.Clear() = dictionary.Clear()
        //        //member this.CopyTo(array, arrayIndex) = (dictionary :> IDictionary<string, string>).CopyTo(array, arrayIndex)
        //        //member this.GetEnumerator() = dictionary.GetEnumerator() :> System.Collections.IEnumerator
        //        //member this.GetEnumerator() = dictionary.GetEnumerator() :> IEnumerator<KeyValuePair<String,String>>

        [<Test>]
        let ``map insertion order`` () =
            let json = """{ b: 1, a: 2 }"""
            let value: System.Collections.Specialized.OrderedDictionary = deserialize Client json
            let expected = [("b", 1L); ("a", 2L)]
            let actual = 
                let keys = seq { for k in value.Keys -> k :?> string }
                let values = seq { for v in value.Values -> v :?> int64 }
                Seq.zip keys values |> Seq.toList
            test <@ expected = actual @>

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