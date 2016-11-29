#FsToolkit.Json

Various helpers that make working with JSON easier using F#. Built on top of Newtonsoft.Json

NuGet package feed: https://ci.appveyor.com/nuget/fstoolkit-json

##FsToolkit.Json.Linq

Features a `(?)` implementation (dynamic operator) that makes manipulating pared `JObject`s super easy
  - Handles `null` and missing properties the same
  - null-safe nested property navigation
  - `null` return values safe to use with value types (uses `Unchecked.defaultof<'t>`)
  - Smartly wraps return values in `Some` / `None` based on return type
  - Collection return type intuitive conversion
  - Case-insensitive property selection (but preferring case-sensitive match) 
