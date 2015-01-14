namespace System
open System.Reflection
open System.Runtime.InteropServices

[<assembly: AssemblyCompanyAttribute("MavenThought Inc.")>]
[<assembly: AssemblyProductAttribute("Schmancy HTTP Testing")>]
[<assembly: ComVisibleAttribute(false)>]
[<assembly: AssemblyVersionAttribute("0.1.0.0")>]
[<assembly: AssemblyFileVersionAttribute("0.1.0.0")>]
[<assembly: AssemblyMetadataAttribute("githash","caa682")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.1.0.0"
