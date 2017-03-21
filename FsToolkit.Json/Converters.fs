namespace FsToolkit.Json

open System
open Microsoft.FSharp.Reflection
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open System.ComponentModel
open System.Reflection

///Custom converters for "exotic" F# types
module Converters = 
    /// True if a given generic Type definition matches a given Type.
    let isGeneric td (t : Type) = t.IsGenericType && t.GetGenericTypeDefinition() = td
    let isList = isGeneric typedefof<FSharp.Collections.List<_>>
    let isOption = isGeneric typedefof<FSharp.Core.Option<_>>

    /// Some 3 -> "3"; None -> null
    type OptionConverter() =
        inherit JsonConverter()

        override __.CanConvert(objType) = 
            isOption objType

        override __.ReadJson(reader, objType, _, serializer) = 
            //adapted from https://github.com/kolektiv/FifteenBelow.Json/blob/master/src/FifteenBelow.Json/Converters.fs#L150
            let cases = FSharpType.GetUnionCases (objType)
            let args = objType.GetGenericArguments ()
 
            let optionOf =
                match args.[0].IsValueType with
                | true -> (typedefof<Nullable<_>>).MakeGenericType ([| args.[0] |]) //??
                | _ -> args.[0]
 
            let jObj = JToken.ReadFrom reader
            let result = jObj.ToObject(optionOf, serializer)
            match result with
            | null -> FSharpValue.MakeUnion (cases.[0], [||])
            | value -> FSharpValue.MakeUnion (cases.[1], [| value |])

        override __.WriteJson(writer, value, serializer) = 
            let unionType = value.GetType()
            let caseInfo, values = FSharpValue.GetUnionFields(value, unionType)
            if caseInfo.Name = "Some" then
                let someValue = values.[0]
                serializer.Serialize(writer, someValue)
            else
                writer.WriteNull()

    /// (2, "3") -> [2, "3"]
    type TupleConverter() =
        inherit JsonConverter()

        override __.CanConvert(objType) = 
            FSharpType.IsTuple objType

        override __.ReadJson(reader, objType, existingValue, serializer) = 
            //adapted from https://github.com/kolektiv/FifteenBelow.Json/blob/master/src/FifteenBelow.Json/Converters.fs#L150
            let types = FSharpType.GetTupleElements objType

            let jtokens = 
                [|
                    reader.Read() |> ignore
                    while reader.TokenType <> JsonToken.EndArray do
                        yield JToken.ReadFrom reader
                        reader.Read() |> ignore
                |]

            let values = 
                //TODO: make more flexible for variable length source and targets
                Seq.zip jtokens types
                |> Seq.map(fun (jtoken, ty) -> 
                    //printfn "jtoken = %A, ty = %A" (jtoken.ToString()) ty
                    jtoken.ToObject(ty, serializer))
                |> Seq.toArray

            FSharpValue.MakeTuple (values, objType)

        override __.WriteJson(writer, value, serializer) = 
            let values = FSharpValue.GetTupleFields value
            serializer.Serialize(writer, values)

    let canConvertDu objType =
        FSharpType.IsUnion objType && not (isList objType) && not (isOption objType)

    type UnionCaseValue = {
        UnionType: Type
        CaseInfo: UnionCaseInfo
        Name: string
        Fields: (string * obj) list
    } with
        static member FromValue(value:obj) =
            let unionType = value.GetType()
            let caseInfo, values = FSharpValue.GetUnionFields(value, unionType)
            let fieldNames = caseInfo.GetFields() |> Seq.map (fun fi -> fi.Name)
            let fields = Seq.zip fieldNames values
            { UnionType = unionType
              CaseInfo = caseInfo
              Name = caseInfo.Name 
              Fields = fields |> Seq.toList }

    /// A more lenient, extensible and less performant version of JSON.NET DiscriminatedUnionConverter.
    type StorageDUJsonConverter() = 
        inherit JsonConverter()

        let readDuProperties (reader: JsonReader) = 
            let mutable caseName = ""
            let mutable properties = JObject()
            let jToken = JToken.ReadFrom reader 
            match jToken with
            | :? JValue as jValue when jValue.Value = null -> 
                null, null
            | _ ->
                let jObj = jToken :?> JObject
                caseName <- jObj.Value<string>("Case")
                jObj.Remove("Case") |> ignore
                //de-unify auto-field names
                if jObj.Properties() |> Seq.length = 1 && jObj.Property("Item1") <> null then
                    jObj.Add("Item", jObj.["Item1"]) |> ignore
                    jObj.Remove("Item1") |> ignore
                properties <- jObj
                caseName, properties
            
        let writeDuBasic (writer:JsonWriter) (value:UnionCaseValue) (serializer:JsonSerializer) =
            writer.WriteStartObject()
            writer.WritePropertyName "Case"
            writer.WriteValue value.Name
            match value.Fields with
            | [(name, value)] when name = "Item" ->
                writer.WritePropertyName "Item1" //unify single value DU with multi-value DUs
                serializer.Serialize(writer, value)
            | _ ->
                for name, value in value.Fields do
                    writer.WritePropertyName name
                    serializer.Serialize(writer, value)
            writer.WriteEndObject()
    
        override __.WriteJson(writer, value, serializer) = 
            let value = UnionCaseValue.FromValue(value)
            writeDuBasic writer value serializer
        
        override __.ReadJson(reader, objType, _, serializer) = 
            let caseName, properties = readDuProperties reader
            match caseName with
            | null -> null
            | _ ->
                let caseInfos = FSharpType.GetUnionCases objType
                let caseInfo = caseInfos |> Array.tryFind (fun a -> a.Name = caseName)
                match caseInfo with
                | None -> failwithf "Couldn't find case '%s' on %A" caseName objType
                | Some caseInfo -> 
                    let fields = caseInfo.GetFields()
                    let fieldValues = System.Collections.Generic.Dictionary<string, obj>()
                    for property in properties.Properties() do
                        let field = fields |> Array.tryFind (fun f -> f.Name = property.Name)
                        match field with
                        | Some f -> fieldValues.Add(property.Name, property.Value.ToObject(f.PropertyType, serializer))
                        | None -> fieldValues.Add(property.Name, null)
                    let values = 
                        fields 
                        |> Array.map (fun f -> 
                            match fieldValues.TryGetValue f.Name with
                            | true, v -> v
                            | _ -> null) //TODO: need more general default value (like None, 0, 0.0, etc. X.Empty)
                    FSharpValue.MakeUnion(caseInfo, values)
        
        override __.CanConvert(objType) = 
            canConvertDu objType

    type ClientDUJsonConverter() = 
        inherit JsonConverter()
        let duIsEnumLike (cases:UnionCaseInfo []) = 
            cases 
            |> Seq.forall (fun case ->
                case.GetFields() |> Array.isEmpty)

        override __.WriteJson(writer, value, serializer) = 
            let value = UnionCaseValue.FromValue(value)
            let cases = FSharpType.GetUnionCases(value.UnionType)
            let isEnumLike = duIsEnumLike cases
            if isEnumLike then
                writer.WriteValue(value.Name)
            else
                writer.WriteStartObject()
                writer.WritePropertyName "case"
                writer.WriteValue value.Name
                match value.Fields with
                | [(name, value)] when name = "Item" ->
                    writer.WritePropertyName "value"
                    serializer.Serialize(writer, value)
                | _ ->
                    for name, value in value.Fields do
                        if name = "case" then //avoid conflict with reserved "case" property name
                            writer.WritePropertyName "_case"
                        else
                            writer.WritePropertyName name
                        serializer.Serialize(writer, value)
                writer.WriteEndObject()

        override __.ReadJson(reader, objType, _, serializer) = 
            let cases = FSharpType.GetUnionCases(objType)
            let isEnumLike = duIsEnumLike cases
            if isEnumLike then
                let caseNameToken = JToken.ReadFrom reader
                match caseNameToken.Value<string>() with
                | null -> null //None
                | caseName ->
                    let case = cases |> Seq.find (fun c -> c.Name = caseName)
                    FSharpValue.MakeUnion(case,[||])
            else
                ///We actually do need a reader here since we are using the client serializer as API in / out model serializer
                raise <| System.InvalidOperationException("Client converter can't read")

        override __.CanConvert(objType) = 
            canConvertDu objType
  
  (* Example runtime schema-migrating converter
  type CustomEventJsonConverter() = 
    inherit DUJsonConverter()
    override __.ParseCase caseName properties serializer objType = 
      match caseName with
      | "Foo" -> // Foo has been replaced by Bar
        let prop = properties.Property("someProperty").Value.ToObject(serializer)
        upcast Bar prop
      | _ -> __.ParseCaseGeneric caseName objType properties serializer
    override __.CanConvert(objType) = // only accept desired types
  *)
