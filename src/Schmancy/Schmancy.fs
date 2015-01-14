namespace Schmancy

open System
open Nancy
open Nancy.Hosting.Self
open Nancy.TinyIoc
open Nancy.Bootstrapper
open Nancy.Extensions
open FSharpx

[<AutoOpen>]
module Implementation =
    type RequestType =
        | Any
        | Get
        | Post
        | Put
        | Delete

    type RequestBuilder = {
        Type:RequestType
        Path:string
        Response:Request -> HttpStatusCode
        BaseUri:string
        Body: string option
        Headers: Map<string, string seq>
    }

    type SchmancyModule (request: RequestBuilder) as x =
        inherit NancyModule()
            
        do 
            let ok = Choice1Of2 HttpStatusCode.OK
            let notFound = Choice2Of2 HttpStatusCode.NotFound

            let toMap dictionary = 
                (dictionary :> seq<_>)
                |> Seq.map (|KeyValue|)
                |> Map.ofSeq

            let matchBody () = 
                match request.Body with
                | Some body -> 
                    if x.Request.Body.AsString() = body then ok
                    else notFound
                | None -> ok
            
            let matchHeaders code = 
                if request.Headers = (x.Request.Headers |> toMap) then ok
                else notFound
            
            let callResponse code = request.Response x.Request

            let response = new Func<obj, obj>(fun _ ->
                    ()
                    |> matchBody
                    |> Choice.map matchHeaders
                    |> Choice.map callResponse
                    |> function
                       | Choice1Of2 x -> x
                       | Choice2Of2 x -> x
                    |> (fun code -> 
                            System.Diagnostics.Trace.WriteLine(sprintf "Responding with code %O" code)
                            new Response(StatusCode=code) |> box)
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
                    
    type CustomBootstrapper (request:RequestBuilder) =
        inherit DefaultNancyBootstrapper()

        override this.ConfigureApplicationContainer(container: TinyIoCContainer) =
            base.ConfigureApplicationContainer(container)

            let customCatalog = {new INancyModuleCatalog with
                                    member this.GetAllModules(context) = 
                                        [new SchmancyModule(request)] |> Seq.cast<INancyModule>

                                    member this.GetModule(moduleType, context) = 
                                        match moduleType with
                                        | t when t = typeof<SchmancyModule> -> new SchmancyModule(request) :> INancyModule
                                        | _ -> null
                                }

            container.Register<INancyModuleCatalog>(customCatalog) |> ignore

    let stubRequest (uri:string) (rt: RequestType) (path:string) = 
        {Type=rt
         Path=path
         Response=(fun _ -> HttpStatusCode.OK)
         BaseUri=uri
         Body=None
         Headers= Map[]
         }

    let withResponse (fn:Request -> HttpStatusCode) request = {request with Response=fn}

    let withBody body request = {request with Body=Some body}

    let withHeader header (value:obj) request =
        let values = 
            match request.Headers |> Map.tryFind header with
            | Some values -> values
            | None        -> Seq.empty
            |> Seq.append [value.ToString()] 
        
        let rest = request.Headers |> Map.remove header

        {request with Headers=rest |> Map.add header values}

    let hostAndCall fn request = 
        use host = new NancyHost(
                        new Uri(request.BaseUri),
                        new CustomBootstrapper(request),
                        new HostConfiguration(UrlReservations=new UrlReservations(CreateAutomatically=true))
                   )
        
        
        host.Start()
            
        let result = fn()
            
        host.Stop()

        result

