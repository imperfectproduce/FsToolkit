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
// - elapsed property
// - content-type headers for response

type FastRequest = {
    Method : string
    ///The Url (query string params overwritten if Query is non-empty)
    Url: string
    ///The (unescaped) query string params (if non-empty overwrites Url query string params)
    Query : (string * string) list
    Headers : (string * string) list
    Body : string
    ///The request / response timeout (ignored if an CancellationToken is given)
    Timeout : TimeSpan
    ///The optional CancellationToken (Timeout is ignored if given)
    CancellationToken : CancellationToken option
    ///Indicates whether or not UTF-8 BOM should be include when encoding / decoding utf-8 charset content
    IncludeUtf8Bom : bool
    ///Indicates whether non-2xx response should fail with exception
    Ensure2xx : bool
} with 
    ///Default fast request: GET | http://example.com | JSON | no body | timeout = 60s | Ensure2xx false
    static member Default = {
        Method = "GET" 
        Url = "http://example.org"
        Query = []
        Headers = 
            [ ("content-type", "application/json; charset=utf-8") 
              ("accept", "application/json, text/html, text/plain, application/xml, application/xhtml+xml") 
              ("Accept-Encoding", "gzip, deflate") ]
        Body = null
        Timeout = TimeSpan.FromSeconds(60.)
        CancellationToken = None
        IncludeUtf8Bom = false
        Ensure2xx = false
    }

type FastResponse = {
    RequestUrl : string
    StatusCode : int
    Headers : (string * string) list
    Body : string
    ///A measure of the total time from begin sending request to end reading response
    Elapsed : TimeSpan
} with 
    member this.Is2xx =
        this.StatusCode >= 200 && this.StatusCode <= 299
    member this.Fail() =
        let msg = sprintf "The remote server response was rejected by caller: %i. Response from %s: %s" this.StatusCode this.RequestUrl this.Body
        raise <| HttpRequestException(msg)
    member this.Ensure2xx() =
        if this.Is2xx |> not then
            this.Fail()

[<AutoOpen>]
module ResponsePatterns =
    let (|Status2xx|_|) (response:FastResponse) =
        if response.Is2xx 
        then Some response
        else None

    let (|Status400|_|) response =
        if response.StatusCode = 400
        then Some response
        else None

module FastHttp =

    ///Apply global (ServicePointManager) optimizations: call this once at application startup.
    let optimize () =
        //don't limit number of http connections
        System.Net.ServicePointManager.DefaultConnectionLimit <- Int32.MaxValue
        //speeds up PUT and POST requests
        System.Net.ServicePointManager.Expect100Continue <- false

    ///Handler used by the HttpClient singleton
    let handler = 
        let handler = new HttpClientHandler()
        handler.AllowAutoRedirect <- false
        handler.UseCookies <- false
        handler.UseDefaultCredentials <- true
        handler.AutomaticDecompression <- DecompressionMethods.GZip ||| DecompressionMethods.Deflate
        handler

    //HttpClient used for all requests (taking advantage singleton built-in socket pooling)
    let client = 
        let client = new HttpClient(handler)
        //speeds up PUT and POST requests
        client.DefaultRequestHeaders.ExpectContinue <- Nullable(false)
        client

    let private utf8Encoding includeBom = UTF8Encoding(includeBom) :> Encoding
    open System.Text.RegularExpressions
    let private charsetRegex = 
        Regex(@"(?<mime>[^;]*)(;\s*charset=(?<charset>.*))?", RegexOptions.Compiled ||| RegexOptions.CultureInvariant)

    let private createRequestContent (fastRequest:FastRequest) =
        let ct = fastRequest.Headers |> Seq.tryFind (fun (k,v) -> k.ToLower() = "content-type")
        match ct with
        | Some(_,ct) ->
            let parts = charsetRegex.Match(ct)
            //printfn "%A" parts
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
        request.RequestUri <- 
            let builder = UriBuilder(fastRequest.Url)
            if fastRequest.Query <> [] then
                let qs = 
                    fastRequest.Query 
                    |> Seq.map (fun (k,v) -> 
                        sprintf "%s=%s" (Uri.EscapeDataString k) (Uri.EscapeDataString v))
                builder.Query <- String.Join("&", qs)
            builder.Uri

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

        let sw = Diagnostics.Stopwatch() 
        sw.Start()
        use! response = client.SendAsync(request, ct) |> Async.AwaitTask
        let! responseBody = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        sw.Stop()
        let responseHeaders =
            response.Headers // does this include the content header?
            |> Seq.map (function KeyValue(k,v) -> k, String.Join(", ", v))
            |> Seq.toList

        let fastResponse = {
            RequestUrl = request.RequestUri.AbsoluteUri
            Elapsed = sw.Elapsed
            StatusCode = response.StatusCode |> int
            Headers = responseHeaders
            Body = responseBody }

        if fastRequest.Ensure2xx then
            fastResponse.Ensure2xx()

        return fastResponse
    }

    let send = sendAsync>>Async.RunSynchronously