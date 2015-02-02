namespace Schmancy.Tests

open FsUnit
open NUnit.Framework
open RestSharp

open Schmancy
open Newtonsoft.Json

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


   