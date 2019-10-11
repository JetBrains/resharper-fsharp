namespace JetBrains.ReSharper.Plugins.FSharp.ProjectModel

type FSharpLanguageLevel =
    /// Anon records
    | FSharp46 = 46

    /// Implicit yield
    | FSharp47 = 47

    /// Open static classes
    | Preview = 2147483646 // Int32.MaxValue - 1

type FSharpLanguageVersion =
    | Default = 0
    | FSharp46 = 46
    | FSharp47 = 47
    | LatestMajor = 2147483644 // Int32.MaxValue - 3
    | Latest = 2147483645 // Int32.MaxValue - 2
    | Preview = 2147483646 // Int32.MaxValue - 1

[<RequireQualifiedAccess>]
module FSharpLanguageVersion =
    let toLanguageLevel (version: FSharpLanguageVersion) =
        match version with
        | FSharpLanguageVersion.Default -> FSharpLanguageLevel.FSharp47
        | FSharpLanguageVersion.FSharp46 -> FSharpLanguageLevel.FSharp46
        | FSharpLanguageVersion.FSharp47 -> FSharpLanguageLevel.FSharp47
        | FSharpLanguageVersion.LatestMajor -> FSharpLanguageLevel.FSharp46
        | FSharpLanguageVersion.Latest -> FSharpLanguageLevel.FSharp47
        | FSharpLanguageVersion.Preview -> FSharpLanguageLevel.Preview
        | _ -> failwithf "Unexpected language level: %A" version

    let toString (version: FSharpLanguageVersion) =
        match version with
        | FSharpLanguageVersion.Default -> "Default"
        | FSharpLanguageVersion.FSharp46 -> "F# 4.6"
        | FSharpLanguageVersion.FSharp47 -> "F# 4.7"
        | FSharpLanguageVersion.LatestMajor -> "Latest major"
        | FSharpLanguageVersion.Latest -> "Latest"
        | FSharpLanguageVersion.Preview -> "Preview"
        | _ -> failwithf "Unexpected language version: %A" version

    let toCompilerOption (version: FSharpLanguageVersion) =
        match version with
        | FSharpLanguageVersion.Default -> "default"
        | FSharpLanguageVersion.FSharp46 -> "4.6"
        | FSharpLanguageVersion.FSharp47 -> "4.7"
        | FSharpLanguageVersion.LatestMajor -> "latestmajor"
        | FSharpLanguageVersion.Latest -> "latest"
        | FSharpLanguageVersion.Preview -> "preview"
        | _ -> failwithf "Unexpected language version: %A" version
