// include Fake lib
#r @"../packages/FAKE/tools/FakeLib.dll"
#load "./config.fsx"

open System
open Fake
open Fake.AssemblyInfoFile
open Fake.Git
open Microsoft.FSharp.Reflection

open Config

module Version =

    let unionToString (x:'a) = 
        match FSharpValue.GetUnionFields(x, typeof<'a>) with
        | case, _ -> case.Name

    type Part =
        | Major    
        | Minor
        | Revision
        | Build

    type VersionNumber(version:string) =
        let major, minor, revision, build = 
            match version.Split '.' |> Array.map Int32.Parse  with
            | [|a;b;c;d|] -> a, b, c, d
            | _ -> 0, 0, 0, 0

        override this.ToString () = sprintf "%d.%d.%d.%d" major minor revision build
        
        member this.Save () = WriteFile (srcDir @@ "VERSION") [this.ToString()]

        member this.Bump part =
            let ma, mi, re, b = match part with
                                | Major    -> major + 1, 0, 0, 0
                                | Minor    -> major, minor + 1, 0, 0
                                | Revision -> major, minor, revision + 1, 0
                                | Build    -> major, minor, revision, build + 1

            let newVersion = VersionNumber (sprintf "%d.%d.%d.%d" ma mi re b)
            newVersion.Save()
            newVersion


    let Current = ReadLine (srcDir @@ "VERSION")

    let attributes =
        let commitHash = Information.getCurrentSHA1 "."
        [
            Attribute.Company "MavenThought Inc."
            Attribute.Product "Schmancy HTTP Testing"
            Attribute.ComVisible false
            Attribute.Version Current
            Attribute.FileVersion Current
            Attribute.Metadata("githash", commitHash.Substring(0, 6))
        ]

    let printVersion (versionNumber:VersionNumber) =
        printfn "Current Version: %O" versionNumber
    
    Target "Version" (fun _ ->
        printVersion (VersionNumber (Current))
    )

    Target "Version:Set" (fun _ ->
        CreateFSharpAssemblyInfo (srcDir @@ "SolutionAssemblyVersionInfo.fs") attributes
    )

    [Major; Minor; Revision] |> Seq.iter (fun p ->
        let bumpTarget = sprintf "Version:Bump:%s" (unionToString p)
        Target bumpTarget (fun _ ->
            let oldVersionNumber = VersionNumber(Current)
            printfn "Old Version: %O" oldVersionNumber
            oldVersionNumber.Bump p |> printVersion
        )
    )

    