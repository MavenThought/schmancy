namespace Schmancy

open System
open Nancy
open Nancy.Hosting.Self
open Nancy.TinyIoc
open Nancy.Bootstrapper
open Nancy.Extensions
open Nancy.Responses
open FSharpx
open FSharpx.Choice

type RequestType =
    | Any
    | Get
    | Post
    | Put
    | Delete

[<AutoOpen>]
module Implementation =
    type ResponseFn = (Request -> Response -> unit)

    type ResponseType =
    | JsonResponse of string
    | TextResponse of string
    | CustomResponse of ResponseFn


    type RequestMatcher = {
        Type:RequestType
        Path:string
        StatusCode: HttpStatusCode
        Response: ResponseType option
        Body: string option
        Headers: Map<string, string seq> option
        Query: Map<string, string> option
    }

    type SiteMatcher = {
        BaseUri:string
        Requests: RequestMatcher list
    }


    module Map =
        let ofHeaders dictionary = 
            (dictionary :> seq<_>)
            |> Seq.map (|KeyValue|)
            |> Map.ofSeq

        let ofQuery (dd:obj) = 
            let dict = (dd :?> Nancy.DynamicDictionary)
            dict
            |> Seq.map (fun k -> (k, dict.[k].ToString()))
            |> Map.ofSeq

        let keys (aMap:Map<_, _>) = aMap |> Seq.map (fun kvp -> kvp.Key)

    type SchmancyModule (site: SiteMatcher) as x =
        inherit NancyModule()
            
        do 
            let ok = Choice1Of2 HttpStatusCode.OK
            let notFound = Choice2Of2 HttpStatusCode.NotFound

            let matchRuleFor (fn:'T->'U->bool) (optional:'T option) (actual:'U) =
                match optional with
                | Some expected -> if (actual |> fn expected) then ok else notFound
                | None -> ok

            let contains fn (aMap:Map<_,_>) (bMap:Map<_, _>) =
                let sameKeyAndValue k v = bMap |> Map.containsKey k && bMap.[k] |> fn aMap.[k]
                aMap |> Map.forall sameKeyAndValue
            
            let addRequest request =            
                let matchBody () = x.Request.Body.AsString() |> matchRuleFor (=) request.Body
            
                let matchHeaders _ = 
                    let headerEq aSeq bSeq =
                        aSeq |> Array.ofSeq = (bSeq |> Array.ofSeq)

                    x.Request.Headers |> Map.ofHeaders |> matchRuleFor (contains headerEq) request.Headers
            
                let matchQuery _ = x.Request.Query |> Map.ofQuery |> matchRuleFor (contains (=)) request.Query

                let callResponse code = 
                    let response = new Response(StatusCode=request.StatusCode)

                    let streamString (str:string) = new System.Action<IO.Stream>(fun (stream:IO.Stream) -> 
                            let writer = new System.IO.StreamWriter(stream)
                            writer.Write str
                            writer.Flush()
                    )

                    match request.Response with
                    | Some (JsonResponse json)  -> 
                        response.WithContentType("json") |> ignore
                        response.Contents <- streamString json
                    | Some (TextResponse text) -> 
                        response.WithContentType("text/html") |> ignore
                        response.Contents <- streamString text
                    | Some (CustomResponse fn) -> fn x.Request response
                    | None -> ()

                    response

                let response = new Func<obj, obj>(fun _ ->
                    () 
                    |> matchBody
                    >>= matchHeaders
                    >>= matchQuery
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

    let private defaultRequest = {
             Type=RequestType.Get
             Path=""
             StatusCode=HttpStatusCode.OK
             Response=None
             Body=None
             Headers=None
             Query= None
        }

    let stubRequest (uri:string) (rt: RequestType) (path:string) = 
        let request = {defaultRequest with Type=rt;Path=path}
        {BaseUri=uri;Requests = [request]}

    let updateRequest fn site =
        match site.Requests with
        | r::rest -> {site with Requests= (fn r)::rest}
        | _  -> site

    let andStub (rt:RequestType) (path:string) site = 
        let request = {defaultRequest with Type=rt;Path=path}
        {site with Requests = request::site.Requests}

    let withJsonResponse json = updateRequest (fun r -> {r with Response=Some (JsonResponse json)})

    let withTextResponse text = updateRequest (fun r -> {r with Response=Some (TextResponse text)})

    let withStatus status = updateRequest (fun r -> {r with StatusCode=status})

    let withBody body = updateRequest (fun r -> {r with Body=Some body})

    let withParameter name value =
        updateRequest (fun r -> 
            {r with Query=r.Query 
                          |> Option.getOrElse (Map[]) 
                          |> Map.remove name 
                          |> Map.add name value 
                          |> Some}
        )

    let withHeader header (value:obj) =
        updateRequest (fun r -> 
            let headers = r.Headers |> Option.getOrElse (Map[])
            let values = headers 
                         |> Map.tryFind header
                         |> Option.getOrElse Seq.empty
                         |> Seq.append [value.ToString()] 
        
            {r with Headers=headers |> Map.remove header |> Map.add header values |> Some}
        )

    type Host = | SchmancyHost of NancyHost

    let start site =
        let host = new NancyHost(
                        new Uri(site.BaseUri),
                        new CustomBootstrapper(site),
                        new HostConfiguration(UrlReservations=new UrlReservations(CreateAutomatically=true))
                   )
        
        
        host.Start()

        SchmancyHost host

    let stop = function
    | SchmancyHost nh -> 
        nh.Stop()
        nh.Dispose()

    let hostAndCall fn site = 
        
        let host = site |> start
         
        let result = fn()
            
        host |> stop

        result



type SchmancyBuilder (baseUri) =

    let mutable target:SiteMatcher = {BaseUri=baseUri;Requests=[]}

    member this.WhenRequesting path reqType = target <- target |> andStub reqType path; this

    member this.Return (code:System.Net.HttpStatusCode) = 
        let theCode = Enum.ToObject(typeof<Nancy.HttpStatusCode>, code) :?> Nancy.HttpStatusCode;
        target <- target |> withStatus theCode; this

    member this.HostWith (fn:System.Func<'T>) = target |> hostAndCall (fun () -> fn.Invoke())

    member this.RespondWithJson json = target <- target |> withJsonResponse json; this