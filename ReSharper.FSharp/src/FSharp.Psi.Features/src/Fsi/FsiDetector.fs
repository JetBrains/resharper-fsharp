module rec JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Fsi.FsiDetector

open System
open System.Globalization
open JetBrains.Application
open JetBrains.Application.platforms
open JetBrains.ProjectModel
open JetBrains.ProjectModel.NuGet.Packaging
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Fsi.Settings
open JetBrains.Util

let [<Literal>] defaultFsiName        = "fsi.exe"
let [<Literal>] defaultAnyCpuFsiName  = "fsiAnyCpu.exe"
let [<Literal>] monoFsiName           = "fsharpi"
let [<Literal>] monoAnyCpuMonoFsiName = "fsharpiAnyCpu"

let [<Literal>] fctPackageName = "FSharp.Compiler.Tools"


[<RequireQualifiedAccess>]
type FsiNaming =
    | Default
    | Mono

type FsiTool =
    { Title: string
      Path: FileSystemPath
      FsiNaming: FsiNaming
      ShadowCopyAllowed: bool }

    static member FromFctNupkg(nupkg: NuGetNupkg, source) =
        let version = nupkg.PackageIdentity.Version
        if version.Major < 10 then None else

        Some
            { Title = sprintf "%s %s (%s)" fctPackageName nupkg.Version source
              Path = nupkg.InstallationDirectory / "tools"
              FsiNaming = FsiNaming.Default
              ShadowCopyAllowed = version.Major >= 10 && version.Minor >= 1 }

    static member Create(title, dir) =
        { Title = title
          Path = dir
          FsiNaming = FsiNaming.Default
          ShadowCopyAllowed = true  }

    member x.GetFsiName(useAnyCpu) =
        match x.FsiNaming with
        | FsiNaming.Default  -> if useAnyCpu then defaultAnyCpuFsiName  else defaultFsiName
        | FsiNaming.Mono     -> if useAnyCpu then monoAnyCpuMonoFsiName else monoFsiName
    
    member x.GetFsiPath(useAnyCpu) =
        x.Path / x.GetFsiName(useAnyCpu)

    member x.IsCustom = x == customTool

let customTool: FsiTool =
    { Title = customToolText
      Path = FileSystemPath.Empty
      FsiNaming = FsiNaming.Default
      ShadowCopyAllowed = true }


[<ShellComponent>]
type FsiDetector(providers: IFsiDirectoryProvider seq) =
    let providers: IFsiDirectoryProvider[] =
        [| SolutionInstalledFsiProvider()
           VsFsiProvider()
           MonoFsiProvider()
           LegacyVsFsiProvider()
           NugetCacheFsiProvider()
           CustomFsiProvider() |]

    member x.GetFsiTools(solution) =
        let packagesByProvider =
            providers
            |> Array.map (fun provider -> provider.GetFsiTools(solution))
        packagesByProvider
        |> Seq.concat
        |> List.ofSeq
        |> List.distinctBy (fun fsiTool -> fsiTool.Path)
        |> Array.ofList

    member x.GetAutodetected(solution) =
        providers
        |> Seq.map (fun provider -> provider.GetFsiTools(solution))
        |> Seq.concat
        |> Seq.head

type IFsiDirectoryProvider =
    abstract GetFsiTools: ISolution -> FsiTool seq


type VsFsiProvider() =
    static let versions = [| "2019"; "2017" |]
    static let editions = [| "BuildTools"; "Community"; "Professional"; "Enterprise" |]
    static let fsharpRelativePath = RelativePath.Parse("Common7/IDE/CommonExtensions/Microsoft/FSharp")

    interface IFsiDirectoryProvider with
        member x.GetFsiTools(_) =
            if not PlatformUtil.IsRunningUnderWindows then [] :> _ else

            let vsPath = PlatformUtils.GetProgramFiles86() / "Microsoft Visual Studio"
            if not vsPath.ExistsDirectory then [] :> _ else

            seq {
                for version in versions do
                    for edition in editions do
                        let fsiDir = vsPath / version / edition / fsharpRelativePath
                        let fsiPath = fsiDir / defaultFsiName
    
                        if fsiPath.ExistsFile then
                            let title = sprintf "Visual Studio %s %s" version edition
                            yield FsiTool.Create(title, fsiDir) }


type LegacyVsFsiProvider() =
    interface IFsiDirectoryProvider with
        member x.GetFsiTools(_) =
            if not PlatformUtil.IsRunningUnderWindows then [] :> _ else

            let legacySdkPath = PlatformUtils.GetProgramFiles86() / "Microsoft SDKs" / "F#"
            if not legacySdkPath.ExistsDirectory then [] :> _ else

            legacySdkPath.GetChildDirectories()
            |> Seq.choose (fun path ->
                match Double.TryParse(path.Name, NumberStyles.Any, CultureInfo.InvariantCulture) with
                | false, _ -> None
                | _, version ->

                let fsiDir = path / "Framework" / "v4.0"
                let fsiPath = fsiDir / defaultFsiName
                if fsiPath.ExistsFile then Some (version, fsiDir) else None)
            |> Seq.sortBy fst
            |> Seq.map (fun (version, fsiDir) -> FsiTool.Create("F# " + string version, fsiDir))


type MonoFsiProvider() =
    interface IFsiDirectoryProvider with
        member x.GetFsiTools(_) =
            [] :> _


type SolutionInstalledFsiProvider() =
    let [<Literal>] source = "Installed package"

    interface IFsiDirectoryProvider with
        member x.GetFsiTools(solution) =
            if isNull solution then [] :> _ else

            let packageReferenceTracker = solution.GetComponent<NuGetPackageReferenceTracker>()
            let nugetStorage = solution.GetComponent<NuGetNupkgStorage>()

            packageReferenceTracker.GetAllInstalledPackages()
            |> Seq.filter (fun pkg -> pkg.PackageIdentity.Id = fctPackageName)
            |> Seq.sortByDescending (fun pkg -> pkg.PackageIdentity.Version)
            |> Seq.map (fun pkg -> nugetStorage.GetNupkg(pkg.PackageIdentity))
            |> Seq.filter isNotNull
            |> Seq.choose (fun nupkg -> FsiTool.FromFctNupkg(nupkg, source))


type NugetCacheFsiProvider() =
    let [<Literal>] source = "NuGet cache"

    interface IFsiDirectoryProvider with
        member x.GetFsiTools(solution) =
            if isNull solution then [] :> _ else

            let nugetStorage = solution.GetComponent<NuGetNupkgStorage>()

            nugetStorage.GetAllNupkgsByName(fctPackageName)
            |> Seq.sortByDescending (fun pkg -> pkg.PackageIdentity.Version)
            |> Seq.choose (fun nupkg -> FsiTool.FromFctNupkg(nupkg, source))


type CustomFsiProvider() =
    let tools = [| customTool |]
    interface IFsiDirectoryProvider with
        member x.GetFsiTools(_) =
            tools :> _
