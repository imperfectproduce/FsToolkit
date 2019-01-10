﻿namespace FsToolkit.Json

open System
open Microsoft.FSharp.Reflection
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open System.ComponentModel
open System.Reflection

///Serialization helpers that utilize customer JsonConverter implementations
module Serialization =
    open FsToolkit.Json.Converters

    ///Ignore property if JsonTarget = Client
    type ClientIgnore() =
        inherit System.Attribute()

    ///Ignore property if JsonTarget = Storage
    type StorageIgnore() =
        inherit System.Attribute()

    let setJsonPropertyIgnore ignoreAttribute (p:Serialization.JsonProperty) (mi:MemberInfo) =
        if mi.GetCustomAttribute(ignoreAttribute) <> null then
            p.Ignored <- true
        else
            ()

    type ClientContractResolver() =
        inherit Serialization.CamelCasePropertyNamesContractResolver()
        let ignoreAttribute = typeof<ClientIgnore>
    with
        override this.CreateProperty(mi: MemberInfo, ms: MemberSerialization) =
            let p = base.CreateProperty(mi, ms)
            setJsonPropertyIgnore ignoreAttribute p mi
            p

    type StorageContractResolver() =
        inherit Serialization.DefaultContractResolver()
        let ignoreAttribute = typeof<StorageIgnore>
    with
        override this.CreateProperty(mi: MemberInfo, ms: MemberSerialization) =
            let p = base.CreateProperty(mi, ms)
            setJsonPropertyIgnore ignoreAttribute p mi
            p
  
    let clientSettings =
        let jsonSettings = JsonSerializerSettings()
        jsonSettings.Converters.Add(ClientDUJsonConverter()) 
        jsonSettings.Converters.Add(OptionConverter()) 
        jsonSettings.Converters.Add(TupleConverter()) 
        jsonSettings.ContractResolver <- ClientContractResolver()
        jsonSettings

    let storageSettings =
        let jsonSettings = JsonSerializerSettings()
        jsonSettings.Converters.Add(StorageDUJsonConverter()) 
        jsonSettings.Converters.Add(OptionConverter()) 
        jsonSettings.Converters.Add(TupleConverter()) 
        jsonSettings.ContractResolver <- StorageContractResolver()
        jsonSettings

    let transferSettings =
        let jsonSettings = JsonSerializerSettings()
        jsonSettings

    type JsonTarget =
        ///Optimized for javascript clients
        | Client
        ///Optimized for storage (more flexible)
        | Storage
        ///Optimized for marshalling data (fast, for machines only)
        | Transfer
    with 
        member this.Settings =
            match this with
            | Client -> clientSettings
            | Storage -> storageSettings
            | Transfer -> transferSettings
  
    let inline serialize (target:JsonTarget) data =
        JsonConvert.SerializeObject(data, target.Settings)
  
    let inline deserialize<'t> (target:JsonTarget) (json : string) =
        JsonConvert.DeserializeObject<'t>(json, target.Settings)
