namespace Schmancy.Tests

open FsUnit
open NUnit.Framework
open RestSharp

open Schmancy
open Newtonsoft.Json

[<AutoOpen>]
module Helpers =

    let url = "http://localhost:9988"
    let client = new RestClient(url)

    type Customer = {Name:string; Id:int}

    let customer = {Name="Charles Magnus"; Id=3}

