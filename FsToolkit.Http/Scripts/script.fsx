#I __SOURCE_DIRECTORY__
#load "load-references-debug.fsx"
#load "../AssemblyInfo.fs"
      "../FastHttp.fs"

open System
open FsToolkit.Http
open FsToolkit.Http.FastHttp
let request = { FastRequest.Default with Method="POST"; Body="hello"; Headers=["content-type", "application/json; charset=utf-8"] }
send request