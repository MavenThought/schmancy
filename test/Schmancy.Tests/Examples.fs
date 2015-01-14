namespace Schmancy.Tests

open FsUnit
open NUnit.Framework
open RestSharp

open Schmancy

[<AutoOpen>]
module Common =

    let url = "http://www.example.com"
    let client = new RestClient(url)

module ``Stubbing for any method`` =

    let callWithMethod m =
        stubRequest url RequestType.Any "/"
        |> hostAndCall (fun _ -> client.Execute(new RestRequest("/", m)).StatusCode)
        |> should equal System.Net.HttpStatusCode.OK

    [<Test>]
    let ``When calling with GET`` () = callWithMethod Method.GET

    [<Test>]
    let ``When calling with POST`` () = callWithMethod Method.POST


module ``Stubbing with body and headers`` =

    [<Test>]
    let ``When using the expected body`` () =
        stubRequest url RequestType.Post "/"
        |> withBody "abc"
        |> withHeader "Content-Length" 3
        |> hostAndCall (fun _ -> 
            let request = new RestRequest("/", Method.POST)
            request
                .AddBody("abc")
                .AddHeader("Content-Length", "3")
                |> ignore

            client.Execute(request).StatusCode
            )
        |> should equal System.Net.HttpStatusCode.OK

    [<Test>]
    let ``When using not matching body`` () =
        stubRequest url RequestType.Post "/"
        |> hostAndCall (fun _ -> 
            let request = new RestRequest("/", Method.POST)
            request
                .AddBody("ddd")
                |> ignore

            client.Execute(request).StatusCode
            )
        |> should equal System.Net.HttpStatusCode.NotFound

