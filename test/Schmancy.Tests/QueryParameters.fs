namespace Schmancy.Tests

open FsUnit
open NUnit.Framework
open RestSharp

open Schmancy
open Newtonsoft.Json

module ``Query parameters`` =

    let request = new RestRequest("/customers", Method.GET)

    let stub =         
        stubRequest url RequestType.Get "/customers"
        |> withParameter "ids" "1,2,3,4"
        |> withJsonResponse (JsonConvert.SerializeObject customer)

    [<Test>]
    let ``When the call uses the parameters`` () =
        request.AddQueryParameter("ids", "1,2,3,4") |> ignore

        stub
        |> hostAndCall (fun _ -> 
            let response = client.Execute(request)
            response.StatusCode, JsonConvert.DeserializeObject<Customer>(response.Content)
          )
        |> should equal (System.Net.HttpStatusCode.OK, customer)

    [<Test>]
    let ``When the call does not use the parameters`` () =
        stub
        |> hostAndCall (fun _ -> client.Execute(request).StatusCode)
        |> should equal System.Net.HttpStatusCode.NotFound

