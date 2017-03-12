#I __SOURCE_DIRECTORY__
#load "load-references-debug.fsx"
#load "../AssemblyInfo.fs"
      "../FastHttp.fs"

open System
open FsToolkit.Http
open FsToolkit.Http.FastHttp
let request = { 
    FastRequest.Default with 
        Url="https://example.org?hello=it%20baby"
        //Query=[("one","two"); ("one","three"); ("and that's", "it+baby")]
        Method="POST"
        Body="hello"
        Headers=["content-type", "application/json"] 
}
match send request with
| Status2xx response -> 
    printfn "OK"
    response
| Status400 response -> 
    printfn "400"
    response
| response -> response.Fail()
