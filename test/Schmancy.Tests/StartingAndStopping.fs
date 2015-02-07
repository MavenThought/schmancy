namespace Schmancy.Tests

open FsUnit
open NUnit.Framework
open RestSharp

open Schmancy
open Newtonsoft.Json

module ``Starting and stopping`` =

    let request = new RestRequest("/Version", Method.GET)

    let schmancy = stubRequest url RequestType.Get "/Version"

    [<Test>]
    let ``When doing the first test`` () =

        let api = schmancy |> withTextResponse "Version 1.0" |> start 

        let response = client.Execute(request)
        
        response.StatusCode |> should equal System.Net.HttpStatusCode.OK
        response.Content    |> should equal "Version 1.0"

        api |> stop


    [<Test>]
    let ``When doing the second test`` () =

        let api = schmancy |> withTextResponse "Version 2.0" |> start 

        let response = client.Execute(request)
        
        response.StatusCode |> should equal System.Net.HttpStatusCode.OK
        response.Content    |> should equal "Version 2.0"

        api |> stop

   