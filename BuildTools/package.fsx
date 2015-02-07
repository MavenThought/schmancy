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

    type PublishOptions =
        | DontPublish
        | NugetServer
        | Custom of string

    let localPackages = @"c:\packages"
    let LocalServer = Custom localPackages 

    let private package publishCfg =
        // Copy all the package files into a package folder
        let prjFolder = srcDir @@ prjName
        let prj = prjFolder @@ (prjName + ".fsproj")
        
        MSBuildRelease "" "Rebuild" [ prj ] |> ignore

        FileUtils.rm_rf packagingDir

        FileUtils.mkdir packagingDir

        CopyFiles (packagingDir @@ "lib" @@ "net40") [prjFolder @@ (sprintf "bin/release/%s.dll" prjName)]

        let dependency name = name, GetPackageVersion "./packages/" name

        let publishIt, publishUrl = 
            match publishCfg with
            | DontPublish -> false, null
            | NugetServer -> true , null
            | Custom url  -> true , url

        NuGet (fun p -> 
            {p with
                Authors = ["Amir Barylko";]
                Project = prjName
                Description = "Library to facility HTTP integration testing"                              
                OutputPath = "."
                Summary = "Schmancy provides an easy way to specify HTTP URIs with expected parameters and responses to test APIs or any other software that uses HTTP calls"
                WorkingDir = packagingDir
                Version = Version.Current
                Publish = publishIt
                PublishUrl = publishUrl
                Dependencies = [
                                dependency "Nancy"
                                dependency "Nancy.Hosting.Self"
                                dependency "FSharpx.Core"
                    ]

            }) 
            "template.nuspec"

    let publishTo publishCfg = publishCfg |> package

    Target "Package"       (fun _ -> package   DontPublish)

    Target "Package:Local" (fun _ -> publishTo LocalServer)

    Target "Package:Nuget" (fun _ -> publishTo NugetServer)