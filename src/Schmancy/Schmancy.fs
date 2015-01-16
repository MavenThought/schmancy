namespace Schmancy

open System
open Nancy
open Nancy.Hosting.Self
open Nancy.TinyIoc
open Nancy.Bootstrapper
open Nancy.Extensions
open Nancy.Responses
open FSharpx
open FSharpx.Validation

[<AutoOpen>]
module Implementation =
    type RequestType =
        | Any
        | Get
        | Post
        | Put
        | Delete

    type ResponseFn = (Request -> Response -> unit)

    type ResponseType =
    | JsonResponse of string
    | CustomResponse of ResponseFn

    type RequestMatcher = {
        Type:RequestType
        Path:string
        StatusCode: HttpStatusCode
        Response: ResponseType option
        Body: string option
        Headers: Map<string, string seq>
    }

    type SiteMatcher = {
        BaseUri:string
        Requests: RequestMatcher list
    }

    type SchmancyModule (site: SiteMatcher) as x =
        inherit NancyModule()
            
        do 
            let ok = Choice1Of2 HttpStatusCode.OK
            let notFound = Choice2Of2 HttpStatusCode.NotFound

            let toMap dictionary = 
                (dictionary :> seq<_>)
                |> Seq.map (|KeyValue|)
                |> Map.ofSeq

            let addRequest request =            
                let matchBody () = 
                    match request.Body with
                    | Some body -> 
                        if x.Request.Body.AsString() = body then ok
                        else notFound
                    | None -> ok
            
                let matchHeaders code = 
                    if request.Headers = (x.Request.Headers |> toMap) then ok
                    else notFound
            
                let callResponse code = 
                    let response = new Response(StatusCode=request.StatusCode)

                    match request.Response with
                    | Some (JsonResponse s)  -> 
                        response.ContentType <- "json"
                        response.Contents <- (fun stream -> 
                            let writer = new System.IO.StreamWriter(stream)
                            writer.Write s
                            writer.Flush()
                        )
                    | Some (CustomResponse fn) -> fn x.Request response
                    | None -> ()

                    response

                let response = new Func<obj, obj>(fun _ ->
                        ()
                        |> matchBody
                        |> Choice.map matchHeaders
                        |> Choice.map callResponse
                        |> function
                           | Choice1Of2 response -> response
                           | Choice2Of2 code -> new Response(StatusCode=code)
                        |> box
                    )
                
                match request.Type with
                | Any    -> 
                    x.Get.[request.Path]    <- response
                    x.Post.[request.Path]   <- response
                    x.Put.[request.Path]    <- response
                    x.Delete.[request.Path] <- response

                | Get    -> x.Get.[request.Path]    <- response
                | Post   -> x.Post.[request.Path]   <- response
                | Put    -> x.Put.[request.Path]    <- response
                | Delete -> x.Delete.[request.Path] <- response

            site.Requests |> Seq.iter addRequest
                    
    type CustomBootstrapper (site:SiteMatcher) =
        inherit DefaultNancyBootstrapper()

        override this.ConfigureApplicationContainer(container: TinyIoCContainer) =
            base.ConfigureApplicationContainer(container)

            let instance () = new SchmancyModule(site) :> INancyModule

            let customCatalog = {new INancyModuleCatalog with

                                    member this.GetAllModules(context) = 
                                        [new SchmancyModule(site)] |> Seq.cast<INancyModule>

                                    member this.GetModule(moduleType, context) = 
                                        match moduleType with
                                        | t when t = typeof<SchmancyModule> -> new SchmancyModule(site) :> INancyModule
                                        | _ -> null
                                }

            container.Register<INancyModuleCatalog>(customCatalog) |> ignore

    let stubRequest (uri:string) (rt: RequestType) (path:string) = 
        let request = {
             Type=rt
             Path=path
             StatusCode=HttpStatusCode.OK
             Response=None
             Body=None
             Headers= Map[]
        }

        {BaseUri=uri;Requests = [request]}

    let updateRequest fn site =
        match site.Requests with
        | r::rest -> {site with Requests= (fn r)::rest}
        | _  -> site

    let withJsonResponse response = updateRequest (fun r -> {r with Response=Some (JsonResponse response)})

    let withStatus status = updateRequest (fun r -> {r with StatusCode=status})

    let withBody body = updateRequest (fun r -> {r with Body=Some body})

    let withHeader header (value:obj) =
        updateRequest (fun r -> 
            let values = 
                match r.Headers |> Map.tryFind header with
                | Some values -> values
                | None        -> Seq.empty
                |> Seq.append [value.ToString()] 
        
            let rest = r.Headers |> Map.remove header
        
            {r with Headers=rest |> Map.add header values}
        )

    let start site =
        let host = new NancyHost(
                        new Uri(site.BaseUri),
                        new CustomBootstrapper(site),
                        new HostConfiguration(UrlReservations=new UrlReservations(CreateAutomatically=true))
                   )
        
        
        host.Start()

        host

    let stop (host:NancyHost) = host.Stop()

    let hostAndCall fn site = 
        
        use host = site |> start
         
        let result = fn()
            
        host |> stop

        result

