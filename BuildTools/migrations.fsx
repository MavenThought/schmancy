// include Fake lib
#r @"../packages/FAKE/tools/FakeLib.dll"
#r @"../packages/FAKE/tools/Fake.SQL.dll"

#r "System.Data.dll"
#r "FSharp.Data.TypeProviders.dll"
#r "System.Data.Linq.dll"
#r "System.Xml.Linq.dll"

#load "./userInput.fsx"
#load "./config.fsx"

open System
open System.IO
open System.Xml.XPath
open System.Data.SqlClient

open Fake
open Fake.SQL
open Microsoft.FSharp.Data.TypeProviders

open UserInput
open Config

module Database =

    let migrationsPrj = @"Src\Database.Migrations\Database.Migrations.csproj"
    let private migrationAssembly = sprintf @"Src\Database.Migrations\bin\%s\Database.Migrations.dll" (buildMode())
    let private migrateTool = @"packages\FluentMigrator.1.3.0.0\tools\Migrate.exe"

    let private askTheUserToChoose connections =
        let question = sprintf "\nPlease enter the db you want to migrate [1-%d]:" (connections |> Seq.length)
        let options = connections |> Seq.map(fun (name, conn) -> sprintf "%15s = %s" name conn)
        match chooseAnOption(question, options) with
            | Some(i, _) -> Some(connections |> Seq.nth i |> snd )
            | _ -> None

    let private ifValid = Option.iter

    let private initializeTheDb connString =
        let createDbScript = @"Db\CreateDatabase.sql"
        runScript (getServerInfo connString) createDbScript
        
    let private chooseDbAnd dbAction =
        // MSBuildRelease "" "Rebuild" [ migrationsPrj ] |> ignore
        
        readDbConnections()
            |> askTheUserToChoose
            |> ifValid dbAction
            |> resetColor

    Target "db:test:setup"   (fun _ -> chooseDbAnd initializeTheDb)

