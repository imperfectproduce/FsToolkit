namespace FsToolkit.Http

open System
open System.Net
open System.Net.Http
open System.Net.Http.Headers
open System.Threading
open System.Text
open System.Text.RegularExpressions

//TODO
// - query params

type FastRequest = {
    Method : string
    Url: string
    Headers : (string * string) list
    Body : string
    ///The request / response timeout (ignored if an CancellationToken is given)
    Timeout : TimeSpan
    ///The optional CancellationToken (Timeout is ignored if given)
    CancellationToken : CancellationToken option
    ///Indicates whether or not UTF-8 BOM should be include when encoding / decoding utf-8 charset content
    IncludeUtf8Bom : bool
} with 
    ///Default fast request: GET | http://example.com | JSON | no body | timeout = 12s
    static member Default = {
        Method = "GET" 
        Url = "http://example.org"
        Headers = 
            [ ("content-type", "application/json; charset=utf-8") 
              ("accept", "application/json, text/html, text/plain, application/xml, application/xhtml+xml") 
              ("Accept-Encoding", "gzip, deflate") ]
        Body = null
        Timeout = TimeSpan.FromSeconds(12.)
        CancellationToken = None
        IncludeUtf8Bom = false
    }

type FastResponse = {
    StatusCode : int
    Headers : (string * string) list
    Body : string
}

module FastHttp =
    //don't limit number of http connections
    do System.Net.ServicePointManager.DefaultConnectionLimit <- Int32.MaxValue
    do System.Net.ServicePointManager.Expect100Continue <- false
    //take advantage singleton built-in socket pooling

    let private handler = new HttpClientHandler()
    do
        handler.AllowAutoRedirect <- false
        handler.UseCookies <- false
        handler.UseDefaultCredentials <- true
        handler.AutomaticDecompression <- DecompressionMethods.GZip ||| DecompressionMethods.Deflate

    let private client = new HttpClient(handler)
    do client.DefaultRequestHeaders.ExpectContinue <- Nullable(false)

    let private utf8Encoding includeBom = UTF8Encoding(includeBom) :> Encoding

    let createRequestContent (fastRequest:FastRequest) =
        let ct = fastRequest.Headers |> Seq.tryFind (fun (k,v) -> k.ToLower() = "content-type")
        match ct with
        | Some(_,ct) ->
            let parts = System.Text.RegularExpressions.Regex.Match(ct, @"(?<mime>[^;]*)(;\s*charset=(?<charset>.*))?")
            printfn "%A" parts
            match parts.Groups.[2], parts.Groups.[3] with
            | mime, charset when mime.Success && charset.Success ->
                let encoding =
                    match charset.Value with
                    | "utf-8" ->  utf8Encoding fastRequest.IncludeUtf8Bom
                    | charset -> Encoding.GetEncoding(charset)
                new StringContent(fastRequest.Body, encoding, mime.Value)
            | mime, charset when mime.Success && not charset.Success ->
                new StringContent(fastRequest.Body, utf8Encoding fastRequest.IncludeUtf8Bom, mime.Value)
            | _ -> failwith "Couldn't parse content-type header"
        | None ->
            new StringContent(fastRequest.Body, utf8Encoding fastRequest.IncludeUtf8Bom, "text/plain")

    let sendAsync fastRequest = async {
        use request = new HttpRequestMessage()
        request.RequestUri <- Uri(fastRequest.Url)
        request.Method <- HttpMethod(fastRequest.Method)
        for (k,v) in fastRequest.Headers do
            if ["content-type"] |> Seq.contains (k.ToLower()) |> not then
                request.Headers.TryAddWithoutValidation(k,v) |> ignore

        if fastRequest.Body |> String.IsNullOrEmpty |> not && ["GET";"HEAD"] |> Seq.contains fastRequest.Method |> not
        then request.Content <- createRequestContent fastRequest
        else ()

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
            |> Seq.map (function KeyValue(k,v) -> k, String.Join(", ", v))
            |> Seq.toList

        return {
            StatusCode = response.StatusCode |> int
            Headers = responseHeaders
            Body = responseBody
        }
    }

    let send = sendAsync>>Async.RunSynchronously