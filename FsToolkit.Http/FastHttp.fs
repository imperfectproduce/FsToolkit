namespace FsToolkit.Http

open System
open System.Net
open System.Net.Http
open System.Net.Http.Headers
open System.Threading

type FastRequest = {
    Method : string
    Url: string
    Headers : (string * string) list
    Body : string
    ///The request / response timeout (ignored if an CancellationToken is given)
    Timeout : TimeSpan
    ///The optional CancellationToken (Timeout is ignored if given)
    CancellationToken : CancellationToken option
} with 
    ///Default fast request: GET | http://example.com | JSON | no body | timeout = 12s
    static member Default = {
        Method = "GET" 
        Url = "http://example.org"
        Headers = 
            [ ("content-type", "application/json; charset=utf8") 
              ("accept", "application/json, text/html, text/plain, application/xml, application/xhtml+xml") 
              ("Accept-Encoding", "gzip, deflate") ]
        Body = null
        Timeout = TimeSpan.FromSeconds(12.)
        CancellationToken = None
    }

type FastResponse = {
    StatusCode : int
    Headers : (string * string) list
    Body : string
}

module FastHttp =
    //don't limit number of http connections
    do System.Net.ServicePointManager.DefaultConnectionLimit <- Int32.MaxValue
    //take advantage singleton built-in socket pooling

    let handler = new HttpClientHandler()
    do
        handler.AllowAutoRedirect <- false
        handler.UseCookies <- false
        handler.UseDefaultCredentials <- true
        handler.AutomaticDecompression <- handler.AutomaticDecompression ||| DecompressionMethods.GZip
        handler.AutomaticDecompression <- handler.AutomaticDecompression ||| DecompressionMethods.Deflate

    let client = new HttpClient(handler)

    let sendAsync fastRequest = async {
        use request = new HttpRequestMessage()
        request.RequestUri <- Uri(fastRequest.Url)
        request.Method <- HttpMethod(fastRequest.Method)
        for (k,v) in fastRequest.Headers do
            if ["content-type"] |> Seq.contains (k.ToLower()) |> not then
                request.Headers.TryAddWithoutValidation(k,v) |> ignore
        match fastRequest.Body with
        | null | "" -> ()
        | body -> 
            request.Content <- new StringContent(body)
            match fastRequest.Headers |> Seq.tryFind (fun (k,v) -> k.ToLower() = "content-type") with
            | Some(_,value) ->
                let value = value.Split(';').[0]
                request.Content.Headers.ContentType <- new MediaTypeHeaderValue(value)
            | None ->
                request.Content.Headers.ContentType <- new MediaTypeHeaderValue("application/json")

        let ct = 
            match fastRequest.CancellationToken with
            | Some ct -> ct
            | _ ->
                let cts = new CancellationTokenSource(fastRequest.Timeout.TotalMilliseconds |> int)
                cts.Token

        use! response = client.SendAsync(request, ct) |> Async.AwaitTask
        let! responseBody = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        let responseHeaders =
            response.Headers // does this include the content header?
            |> Seq.map (|KeyValue|)
            |> Seq.map (fun (k,v) -> k, String.Join(", ", v))
            |> Seq.toList

        return {
            StatusCode = response.StatusCode |> int
            Headers = responseHeaders
            Body = responseBody
        }
    }

    let send = sendAsync>>Async.RunSynchronously