// include Fake lib
#r @"../packages/FAKE/tools/FakeLib.dll"

open System.IO
open System.Xml.XPath
open System.Xml
open Fake

// Properties
[<AutoOpen>]
module Config =
    let testDir     = "./"
    let srcDir      = "./"
    let mainSln     = "CadApi.sln"
    let mainTestPrj = @"CadApi.Tests"
    let mainPrj     = @"CadApi"
    let mainConfig  = mainPrj @@ "app.config"
    let testConfig  = mainTestPrj @@ "app.config"

    let environments = ["Debug"; "Release"; "CI"]
    let buildMode () = getBuildParamOrDefault "buildMode" "Release"
    let version      = "1.0.0.0"
    let targetWithEnv target env = sprintf "%s:%s" target env

    let setBuildMode = setEnvironVar "buildMode"

    let debugMode   () = setBuildMode "Debug"
    let releaseMode () = setBuildMode "Release"
    let ciMode () = setBuildMode "CI"

    let setParams defaults =
        { defaults with
            Targets = ["Build"]
            Properties =
                [
                    "Optimize", "True"
                    "Platform", "Any CPU"
                    "Configuration", buildMode()
                ]
        }


    let private connectionPath = @"//connectionStrings/add[@name='CadApi']"
    let private connAttr = "connectionString"

    let readDbConnections () =
        let findConnection (file:FileInfo) =
            XPathDocument(file.FullName).CreateNavigator()
            |> (fun xpath -> xpath.SelectSingleNode connectionPath)
            |> (fun node  -> if node = null then null else node.GetAttribute(connAttr, ""))

        let configName category (file:FileInfo) = 
            let name = (file.Name.Split '.').[1]
            if name = "config" then category 
            else sprintf "%s-%s" category name

        Map[
            "Test", mainTestPrj
            "Main", mainPrj
        ]
        |> Seq.map (fun kvp  -> 
            DirectoryInfo(kvp.Value).GetFiles "app*.config"
            |> Seq.map (fun fi -> configName kvp.Key fi, findConnection fi)
           )
        |> Seq.concat
        |> Seq.filter (fun (name, conn) -> conn |> System.String.IsNullOrEmpty |> not)

    let configureDbConn (file:string) target =
        let _, targetConn = readDbConnections() |> Seq.find (fun (name, _) -> name = target)
        let doc = XmlDocument()
        
        doc.Load file

        let node = doc.SelectSingleNode connectionPath

        printf "- Opening file %s\n" file
        printf "- Replacing connection %s\n" (node.Attributes.[connAttr].Value)
        printf "- With      connection %s\n" targetConn

        node.Attributes.[connAttr].Value <- targetConn

        doc.Save file

        



