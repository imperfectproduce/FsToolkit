namespace FsToolkit.Http.Tests

open System
open Swensen.Unquote
open NUnit.Framework
open FsToolkit
open FsToolkit.Http

module FastHttpTests =

    [<Test>]
    let ``test sync http request`` () =
        let request = { FastRequest.Default with Url = "http://www.example.org" }
        let response = FastHttp.send request
        test <@ response.Is2xx @>

    [<Test>]
    let ``test async http request`` () = Async.StartAsyncUnitAsTask <| async {
        let request = { FastRequest.Default with Url = "http://www.example.org" }
        let! response = FastHttp.sendAsync request
        test <@ response.Is2xx @>
    }
