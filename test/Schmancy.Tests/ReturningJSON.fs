namespace Schmancy.Tests

open FsUnit
open NUnit.Framework
open RestSharp

open Schmancy
open Newtonsoft.Json

module ``Returning JSON`` =

    [<Test>]
    let ``When the call returns JSON`` () =
        let request = new RestRequest("/customers", Method.GET)
        request.RequestFormat <- DataFormat.Json

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