namespace FsToolkit.Http

open System
open System.Net
open System.Net.Http
open System.Net.Http.Headers

type FastRequest = {
    Method : string
    Url: string
    Headers : (string * string) list
    Body : string
} with 
    static member Default = {
        Method = "GET" 
        Url = "http://example.org"
        Headers = 
            [ ("content-type", "application/json; charset=utf8") 
              ("accept", "application/json, text/html, text/plain, application/xml, application/xhtml+xml") 
              ("Accept-Encoding", "gzip, deflate") ]
        Body = null
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

        use! response = client.SendAsync(request) |> Async.AwaitTask
        let! responseBody = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        let responseHeaders =
            response.Headers // does this include the content header?
            |> Seq.map (|KeyValue|)
            |> Seq.map (fun (k,v) ->
                //or maybe join the values with '; '?
                match v |> Seq.tryHead with
                | Some(v) -> k, v            
                | None -> k, "")
            |> Seq.toList

        return {
            StatusCode = response.StatusCode |> int
            Headers = responseHeaders
            Body = responseBody
        }
    }

    let send = sendAsync>>Async.RunSynchronously