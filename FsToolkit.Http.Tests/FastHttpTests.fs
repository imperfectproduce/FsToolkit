namespace FsToolkit.Http.Tests

open System
open Swensen.Unquote
open NUnit.Framework
open FsToolkit
open FsToolkit.Http

module FastHttpTests =

    let inline mkResponseAssertions (response:FastResponse) =
        test <@ response.Is2xx @>
        test <@ response.Body.StartsWith("<!doctype html>") @>
        test <@ response.Headers |> List.contains ("Content-Type", "text/html; charset=utf-8") @>

    [<Test>]
    let ``test sync http request`` () =
        let request = { FastRequest.Default with Url = "http://www.example.org" }
        let response = FastHttp.send request
        mkResponseAssertions response

    [<Test>]
    let ``test async http request`` () = Async.StartAsyncUnitAsTask <| async {
        let request = { FastRequest.Default with Url = "http://www.example.org" }
        let! response = FastHttp.sendAsync request
        mkResponseAssertions response
    }
