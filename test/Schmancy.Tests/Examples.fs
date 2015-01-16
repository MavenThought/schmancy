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

    type Customer = {Name:string;Age:int}

    [<Test>]
    let ``When the call returns JSON`` () =
        let request = new RestRequest("/customers", Method.GET)
        request.RequestFormat <- DataFormat.Json

        stubRequest url RequestType.Get "/customers"
        |> withJsonResponse "{customer:'Charles Magnus'}"
        |> hostAndCall (fun _ -> 
            let response = client.Execute(request)
            response.StatusCode, response.Content
          )
        |> should equal (System.Net.HttpStatusCode.OK, "{customer:'Charles Magnus'}")

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


   