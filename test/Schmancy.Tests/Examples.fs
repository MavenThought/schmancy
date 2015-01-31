namespace Schmancy.Tests

open FsUnit
open NUnit.Framework
open RestSharp

open Schmancy

[<AutoOpen>]
module Common =

    let url = "http://localhost:9988"
    let client = new RestClient(url)

module ``Stubbing for any method`` =

    let callWithMethod m =
        stubRequest url RequestType.Any "/"
        |> withStatus Nancy.HttpStatusCode.OK
        |> hostAndCall (fun _ -> 
            let response = client.Execute(new RestRequest("/", m))
            response.StatusCode
        )
        |> should equal System.Net.HttpStatusCode.OK

    [<Test>]
    let ``When calling with GET`` () = callWithMethod Method.GET 

    [<Test>]
    let ``When calling with POST`` () = callWithMethod Method.POST

module ``Returning JSON`` =

    open Newtonsoft.Json

    type Customer = {Name:string; Id:int}

    [<Test>]
    let ``When the call returns JSON`` () =
        let request = new RestRequest("/customers", Method.GET)
        request.RequestFormat <- DataFormat.Json

        let customer = {Name="Charles Magnus"; Id=3}

        stubRequest url RequestType.Get "/customers"
        |> withJsonResponse  (JsonConvert.SerializeObject customer)
        |> hostAndCall (fun _ -> 
            let response = client.Execute(request)
            response.StatusCode, JsonConvert.DeserializeObject<Customer>(response.Content)
          )
        |> should equal (System.Net.HttpStatusCode.OK, customer)


    [<Test>]
    let ``When calling multiple paths`` () =
        let getRequest (path:string) =
            let request = new RestRequest(path, Method.GET)
            request.RequestFormat <- DataFormat.Json
            client.Execute(request)

        stubRequest url RequestType.Get "/customers"
        |> withJsonResponse "{customers:[1, 2, 3, 4, 5, 6]}"
        |> andStub RequestType.Get "/customers/1"
        |> withJsonResponse "{name: 'Charles', id: 1}"
        |> hostAndCall (fun _ -> 
            let r1 = getRequest("/customers")
            let r2 = getRequest("/customers/1")

            r1.StatusCode, r2.StatusCode, r1.Content, r2.Content
          )
        |> should equal (System.Net.HttpStatusCode.OK, System.Net.HttpStatusCode.OK,
                         "{customers:[1, 2, 3, 4, 5, 6]}", 
                         "{name: 'Charles', id: 1}"
                         )


module ``Stubbing with body and headers`` =

    [<Test>]
    let ``When using the expected body`` () =
        let request = new RestRequest("/", Method.POST)
        request.RequestFormat <- DataFormat.Json

        stubRequest url RequestType.Post "/"
        |> withBody "\"abc\""
        |> withHeader "Content-Length" 3
        |> hostAndCall (fun _ -> 
            request
                .AddBody("abc")
                .AddHeader("Content-Length", "3")
                |> ignore

            client.Execute(request).StatusCode
            )
        |> should equal System.Net.HttpStatusCode.OK

    [<Test>]
    let ``When the body does not match`` () =
        let request = new RestRequest("/", Method.POST)
        request.RequestFormat <- DataFormat.Json

        stubRequest url RequestType.Post "/"
        |> withBody "\"abc\""
        |> withHeader "Content-Length" 3
        |> hostAndCall (fun _ -> 
            request
                .AddBody("ddd")
                |> ignore

            client.Execute(request).StatusCode
            )
        |> should equal System.Net.HttpStatusCode.NotFound


   