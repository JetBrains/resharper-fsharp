namespace JetBrains.ReSharper.Plugins.FSharp.ProjectModel

open System.Collections.Generic
open JetBrains.ReSharper.Feature.Services
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.Util
open JetBrains.Util.dataStructures

type FSharpLanguageLevel =
    | FSharp45 = 45

    /// Anon records
    | FSharp46 = 46

    /// Implicit yield, wild pat self id, constructor/static method parameters deindent
    | FSharp47 = 47

    /// String interpolation, nameof, open types
    | FSharp50 = 50

    /// Indexing without dot
    | FSharp60 = 60

    /// Lowercase union cases
    | FSharp70 = 70

    /// Nested record field copy and update/shorthand lambda
    | FSharp80 = 80

    /// Includes fixes for shorthand lambda
    | FSharp8Patched = 81

    /// Nullness
    | FSharp90 = 90

    | Latest = 90

    | Preview = 2147483646 // Int32.MaxValue - 1


type FSharpLanguageVersion =
    | Default = 0
    | FSharp46 = 46
    | FSharp47 = 47
    | FSharp50 = 50
    | FSharp60 = 60
    | FSharp70 = 70
    | FSharp80 = 80
    | FSharp90 = 90
    | LatestMajor = 2147483644 // Int32.MaxValue - 3
    | Latest = 2147483645 // Int32.MaxValue - 2
    | Preview = 2147483646 // Int32.MaxValue - 1


[<Extension; RequireQualifiedAccess>]
module FSharpLanguageLevel =
    let toLanguageVersion (level: FSharpLanguageLevel) =
        match level with
        | FSharpLanguageLevel.FSharp46 -> FSharpLanguageVersion.FSharp46
        | FSharpLanguageLevel.FSharp47 -> FSharpLanguageVersion.FSharp47
        | FSharpLanguageLevel.FSharp50 -> FSharpLanguageVersion.FSharp50
        | FSharpLanguageLevel.FSharp60 -> FSharpLanguageVersion.FSharp60
        | FSharpLanguageLevel.FSharp70 -> FSharpLanguageVersion.FSharp70
        | FSharpLanguageLevel.FSharp80
        | FSharpLanguageLevel.FSharp8Patched -> FSharpLanguageVersion.FSharp80
        | FSharpLanguageLevel.FSharp90 -> FSharpLanguageVersion.FSharp90
        | FSharpLanguageLevel.Preview -> FSharpLanguageVersion.Preview
        | _ -> failwithf $"Unexpected language level: {level}"

    let ofLanguageVersion (version: FSharpLanguageVersion) =
        match version with
        | FSharpLanguageVersion.FSharp46 -> FSharpLanguageLevel.FSharp46
        | FSharpLanguageVersion.FSharp47 -> FSharpLanguageLevel.FSharp47
        | FSharpLanguageVersion.FSharp50 -> FSharpLanguageLevel.FSharp50
        | FSharpLanguageVersion.FSharp60 -> FSharpLanguageLevel.FSharp60
        | FSharpLanguageVersion.FSharp70 -> FSharpLanguageLevel.FSharp70
        | FSharpLanguageVersion.FSharp80 -> FSharpLanguageLevel.FSharp80
        | FSharpLanguageVersion.FSharp90 -> FSharpLanguageLevel.FSharp90
        | FSharpLanguageVersion.Preview -> FSharpLanguageLevel.Preview
        | _ -> FSharpLanguageLevel.Latest

    let key = Key<Boxed<FSharpLanguageLevel>>("LanguageLevel")

    let ofPsiModuleNoCache (psiModule: IPsiModule) =
        let levelProvider =
            psiModule.GetPsiServices()
                .GetComponent<SolutionFeaturePartsContainer>()
                .GetFeatureParts<ILanguageLevelProvider<FSharpLanguageLevel, FSharpLanguageVersion>>(fun p ->
                    p.IsApplicable(psiModule))
                .SingleItem()

        levelProvider.GetLanguageLevel(psiModule)

    [<Extension; CompiledName("GetFSharpLanguageLevel")>]
    let ofTreeNode (treeNode: ITreeNode) =
        PsiFileCachedDataUtil.GetPsiModuleData<FSharpLanguageLevel>(treeNode, key, ofPsiModuleNoCache)

    [<Extension; CompiledName("IsFSharp47Supported")>]
    let isFSharp47Supported (treeNode: ITreeNode) =
        ofTreeNode treeNode >= FSharpLanguageLevel.FSharp47

    [<Extension; CompiledName("IsFSharp50Supported")>]
    let isFSharp50Supported (treeNode: ITreeNode) =
        ofTreeNode treeNode >= FSharpLanguageLevel.FSharp50

    [<Extension; CompiledName("IsFSharp60Supported")>]
    let isFSharp60Supported (treeNode: ITreeNode) =
        ofTreeNode treeNode >= FSharpLanguageLevel.FSharp60

    [<Extension; CompiledName("IsFSharp70Supported")>]
    let isFSharp70Supported (treeNode: ITreeNode) =
        ofTreeNode treeNode >= FSharpLanguageLevel.FSharp70

    [<Extension; CompiledName("IsFSharp80Supported")>]
    let isFSharp80Supported (treeNode: ITreeNode) =
        ofTreeNode treeNode >= FSharpLanguageLevel.FSharp80

    [<Extension; CompiledName("IsFSharp8PatchedSupported")>]
    let isFSharp8PatchedSupported (treeNode: ITreeNode) =
        ofTreeNode treeNode >= FSharpLanguageLevel.FSharp8Patched

    [<Extension; CompiledName("IsFSharp90Supported")>]
    let isFSharp90Supported (treeNode: ITreeNode) =
        ofTreeNode treeNode >= FSharpLanguageLevel.FSharp90

[<RequireQualifiedAccess>]
module FSharpLanguageVersion =
    let tryParseCompilationOption (emptyVersion: FSharpLanguageVersion) (langVersion: string): FSharpLanguageVersion option =
        if langVersion.IsNullOrEmpty() then Some(emptyVersion) else

        match langVersion with
        | IgnoreCase "Default" -> Some(FSharpLanguageVersion.Default)
        | IgnoreCase "LatestMajor" -> Some(FSharpLanguageVersion.LatestMajor)
        | IgnoreCase "Latest" -> Some(FSharpLanguageVersion.Latest)
        | IgnoreCase "Preview" -> Some(FSharpLanguageVersion.Preview)

        | "4.6" -> Some(FSharpLanguageVersion.FSharp46)
        | "4.7" -> Some(FSharpLanguageVersion.FSharp47)
        | "5" | "5.0" -> Some(FSharpLanguageVersion.FSharp50)
        | "6" | "6.0" -> Some(FSharpLanguageVersion.FSharp60)
        | "7" | "7.0" -> Some(FSharpLanguageVersion.FSharp70)
        | "8" | "8.0" -> Some(FSharpLanguageVersion.FSharp80)
        | "9" | "9.0" -> Some(FSharpLanguageVersion.FSharp90)

        | _ -> None

    let parseCompilationOption langVersion: FSharpLanguageVersion =
        tryParseCompilationOption FSharpLanguageVersion.Default langVersion
        |> Option.defaultValue FSharpLanguageVersion.Latest

    let toString (version: FSharpLanguageVersion) =
        match version with
        | FSharpLanguageVersion.Default -> "Default"
        | FSharpLanguageVersion.FSharp46 -> "F# 4.6"
        | FSharpLanguageVersion.FSharp47 -> "F# 4.7"
        | FSharpLanguageVersion.FSharp50 -> "F# 5.0"
        | FSharpLanguageVersion.FSharp60 -> "F# 6.0"
        | FSharpLanguageVersion.FSharp70 -> "F# 7.0"
        | FSharpLanguageVersion.FSharp80 -> "F# 8.0"
        | FSharpLanguageVersion.FSharp90 -> "F# 9.0"
        | FSharpLanguageVersion.LatestMajor -> "Latest major"
        | FSharpLanguageVersion.Latest -> "Latest"
        | FSharpLanguageVersion.Preview -> "Preview"
        | _ -> failwithf "Unexpected language version: %A" version

    let toCompilerOptionValue (version: FSharpLanguageVersion) =
        match version with
        | FSharpLanguageVersion.Default -> "default"
        | FSharpLanguageVersion.FSharp46 -> "4.6"
        | FSharpLanguageVersion.FSharp47 -> "4.7"
        | FSharpLanguageVersion.FSharp50 -> "5.0"
        | FSharpLanguageVersion.FSharp60 -> "6.0"
        | FSharpLanguageVersion.FSharp70 -> "7.0"
        | FSharpLanguageVersion.FSharp80 -> "8.0"
        | FSharpLanguageVersion.FSharp90 -> "9.0"
        | FSharpLanguageVersion.LatestMajor -> "latestmajor"
        | FSharpLanguageVersion.Latest -> "latest"
        | FSharpLanguageVersion.Preview -> "preview"
        | _ -> failwithf "Unexpected language version: %A" version

    let toCompilerArg =
        toCompilerOptionValue >> sprintf "--langversion:%s"

type FSharpLanguageLevelComparer() =
    static member val Instance = FSharpLanguageLevelComparer()

    interface IComparer<FSharpLanguageLevel> with
        member this.Compare(x, y) = x.CompareTo(y)
