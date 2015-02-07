#r @"../packages/FAKE/tools/FakeLib.dll"

#load "./config.fsx"
#load "./userInput.fsx"
#load "./version.fsx"

open Fake
open Fake.FileSystem
open Config
open Version

module Package =

    let packagingDir = "packaging"

    let noPublish = None

    let publishTo str = Some str

    let private package publishUrl =
        // Copy all the package files into a package folder
        let prjFolder = srcDir @@ prjName
        let prj = prjFolder @@ (prjName + ".fsproj")
        
        MSBuildRelease "" "Rebuild" [ prj ] |> ignore

        FileUtils.rm_rf packagingDir

        FileUtils.mkdir packagingDir

        CopyFiles (packagingDir @@ "lib" @@ "net40") [prjFolder @@ (sprintf "bin/release/%s.dll" prjName)]

        let dependency name = name, GetPackageVersion "./packages/" name

        NuGet (fun p -> 
            {p with
                Authors = ["Amir Barylko";]
                Project = prjName
                Description = "Library to facility HTTP integration testing"                              
                OutputPath = "."
                Summary = "Schmancy provides an easy way to specify HTTP URIs with expected parameters and responses to test APIs or any other software that uses HTTP calls"
                WorkingDir = packagingDir
                Version = Version.Current
                Publish = publishUrl |> Option.isSome
                PublishUrl = if publishUrl |> Option.isSome then publishUrl |> Option.get else ""
                Dependencies = [
                                dependency "Nancy"
                                dependency "Nancy.Hosting.Self"
                                dependency "FSharpx.Core"
                    ]

            }) 
            "template.nuspec"

    Target "Package" (fun _ -> package noPublish)

    Target "Package:Local" (fun _ -> publishTo @"c:\packages" |> package)

    Target "Package:Nuget" (fun _ -> publishTo @"http://nuget.org" |> package)