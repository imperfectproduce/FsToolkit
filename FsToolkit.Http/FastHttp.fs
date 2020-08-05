namespace FsToolkit.Http

open System
open System.Net
open System.Net.Http
open System.Net.Http.Headers
open System.Threading
open System.Text
open System.Text.RegularExpressions

//TODO
// - content-type headers for response

///Models an HTTP request
type FastRequest = {
    Method : string
    ///The request URL (query string params overwritten if Query is non-empty)
    Url: string
    ///The (unescaped) query string params (if non-empty overwrites Url query string params)
    QueryParams : (string * string) list
    ///The request headers as (key * value) pairs (duplicates are allowed)
    Headers : (string * string) list
    ///The request body
    Body : string
    ///The request / response timeout (ignored if an CancellationToken is given)
    Timeout : TimeSpan
    ///The optional CancellationToken (Timeout is ignored if given)
    CancellationToken : CancellationToken option
    ///Indicates whether or not UTF-8 BOM should be include when encoding / decoding utf-8 charset content
    IncludeUtf8Bom : bool
    ///Indicates whether non-2xx response should fail with exception
    Ensure2xx : bool
    ///Indicates whether query param keys and values should be escaped
    EscapeQueryParams : bool
} with 
    ///Default fast request: GET | http://example.com | JSON | no body | timeout = 60s | Ensure2xx false | EscapeQueryParams true
    static member Default = {
        Method = "GET" 
        Url = "http://example.org"
        QueryParams = []
        Headers = 
            [ ("content-type", "application/json; charset=utf-8") 
              ("accept", "application/json, text/html, text/plain, application/xml, application/xhtml+xml") 
              ("Accept-Encoding", "gzip, deflate") ]
        Body = null
        Timeout = TimeSpan.FromSeconds(60.)
        CancellationToken = None
        IncludeUtf8Bom = false
        Ensure2xx = false
        EscapeQueryParams = true
    }

///Models an HTTP response
type FastResponse = {
    ///The request URL
    RequestUrl : string
    ///The response status code
    StatusCode : int
    ///The response headers as (key * value) pairs (duplicate keys are possible)
    Headers : (string * string) list
    ///The response decoded as a string
    Body : string
    ///A measure of the total time from begin sending request to end reading response
    Elapsed : TimeSpan
} with
    ///Determines whether the StatusCode is within the 200-range
    member this.Is2xx =
        this.StatusCode >= 200 && this.StatusCode <= 299
    ///Raises an HttpRequestException with details about the FastResponse
    member this.Fail() =
        let msg = sprintf "The remote server response was rejected by caller: %i. Response from %s: %s" this.StatusCode this.RequestUrl this.Body
        raise <| HttpRequestException(msg)
    ///Raises an HttpRequestException is the StatusCode is outside of the 200-range
    member this.Ensure2xx() =
        if this.Is2xx |> not then
            this.Fail()

[<AutoOpen>]
module ResponsePatterns =
    let private statusOption code response =
        if response.StatusCode = code
        then Some response
        else None

    ///Match succeeds if response status code is in the 200-range
    let (|Status2xx|_|) (response:FastResponse) =
        if response.Is2xx 
        then Some response
        else None

    ///400 Bad Request
    let (|Status400|_|) = statusOption 400
    ///401 Unauthorized
    let (|Status401|_|) = statusOption 401
    ///403 Forbidden
    let (|Status403|_|) = statusOption 403
    ///404 Not Found
    let (|Status404|_|) = statusOption 404
    ///422 Unprocessable Entity
    let (|Status422|_|) = statusOption 422

///Perform HTTP operations optimized for speed and ergonomics
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
        client.Timeout <- TimeSpan.FromDays(1.) // FastRequest.Timeout should be used instead
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

    ///Send a FastRequest asynchronously and receive a FastResponse
    let sendAsync fastRequest = async {
        use request = new HttpRequestMessage()
        request.RequestUri <- 
            let builder = UriBuilder(fastRequest.Url)
            if fastRequest.QueryParams <> [] then
                let qs = 
                    fastRequest.QueryParams
                    |> Seq.map (fun (k,v) -> 
                        if fastRequest.EscapeQueryParams 
                        then sprintf "%s=%s" (Uri.EscapeDataString k) (Uri.EscapeDataString v)
                        else sprintf "%s=%s" k v)
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
            let parseHeaders headers =
                headers
                |> Seq.map (function KeyValue(k,v:string seq) -> k, String.Join(", ", v))
                |> Seq.toList
            parseHeaders response.Headers @ parseHeaders response.Content.Headers

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

    ///Send a FastRequest synchronously and receive a FastResponse
    let send = sendAsync>>Async.RunSynchronously
