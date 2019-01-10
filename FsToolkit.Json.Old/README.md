#FsToolkit.Json

Various helpers that make working with JSON easier using F#. Built on top of `Newtonsoft.Json`

NuGet package feed: https://ci.appveyor.com/nuget/fstoolkit-json

##FsToolkit.Json.Linq

Features a `(?)` implementation (dynamic operator) that makes manipulating parsed `JObject`s super easy
  - Handles `null` and missing properties the same
  - null-safe nested property navigation
  - `null` return values safe to use with value types (uses `Unchecked.defaultof<'t>`)
  - Smartly wraps return values in `Some` / `None` based on return type
  - Collection return type intuitive conversion
  - Case-insensitive property selection (but preferring case-sensitive match) 

##FsToolkit.Json.Converters

A set of `JsonConverter` implementations for better client and / or storage serialization of exotic F# types.

  - `OptionConverter` erases Some / None wrappers around objects and values. e.g. `Some "3" -> "3"` and `None -> null`
  - `TupleConverter` serializes tuples as arrays, e.g. `(2, "3") -> [2, "3"]`
  - `StorageDUConverter` serializes DUs as flat objects with explicit fields names when given, and normalized auto-field names otherwise
  - `ClientDUConverter` serializes DUs as flat objects with explicit field names when given. "enum-like" DUs (all cases lack fields) are serialized as case-name strings. Single-field DUs without explicit field name are serialized with `value` field name.
  
##FsToolkit.Json.Serialization

An _opinionated_ set of helpers for serialization and deserializing exotic F# types.

Distinguishes between three types of serialization targets: Client, Storage, and Transfer. Provides `ClientIgnore` and `StorageIgnore` attributes for controlling fields used in object serialization.
