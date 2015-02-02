namespace Schmancy.Tests

open FsUnit
open NUnit.Framework
open RestSharp

open Schmancy
open Newtonsoft.Json

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

