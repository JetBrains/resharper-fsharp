namespace JetBrains.ReSharper.Plugins.FSharp.ProjectModel

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

    | Latest = 50

    | Preview = 2147483646 // Int32.MaxValue - 1


type FSharpLanguageVersion =
    | Default = 0
    | FSharp46 = 46
    | FSharp47 = 47
    | FSharp50 = 50
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
        | FSharpLanguageLevel.Preview -> FSharpLanguageVersion.Preview
        | _ -> failwithf $"Unexpected language level: {level}"

    let ofLanguageVersion (version: FSharpLanguageVersion) =
        match version with
        | FSharpLanguageVersion.FSharp46 -> FSharpLanguageLevel.FSharp46
        | FSharpLanguageVersion.FSharp47 -> FSharpLanguageLevel.FSharp47
        | FSharpLanguageVersion.FSharp50 -> FSharpLanguageLevel.FSharp50
        | FSharpLanguageVersion.Preview -> FSharpLanguageLevel.Preview
        | _ -> FSharpLanguageLevel.Latest

    let key = Key<Boxed<FSharpLanguageLevel>>("LanguageLevel")

    let private ofPsiModuleNoCache (psiModule: IPsiModule) =
        let levelProvider = 
            psiModule.GetPsiServices()
                .GetComponent<SolutionFeaturePartsContainer>()
                .GetFeatureParts<ILanguageLevelProvider<FSharpLanguageLevel, FSharpLanguageVersion>>(fun p ->
                    p.IsApplicable(psiModule))
                .SingleItem()

        levelProvider.GetLanguageLevel(psiModule)

    [<Extension; CompiledName("GetLanguageLevel")>]
    let ofTreeNode (treeNode: ITreeNode) =
        PsiFileCachedDataUtil.GetPsiModuleData<FSharpLanguageLevel>(treeNode, key, ofPsiModuleNoCache)

    [<Extension; CompiledName("IsFSharp47Supported")>]
    let isFSharp47Supported (treeNode: ITreeNode) =
        ofTreeNode treeNode >= FSharpLanguageLevel.FSharp47

    [<Extension; CompiledName("IsFSharp50Supported")>]
    let isFSharp50Supported (treeNode: ITreeNode) =
        ofTreeNode treeNode >= FSharpLanguageLevel.FSharp50


[<RequireQualifiedAccess>]
module FSharpLanguageVersion =
    let tryParseCompilationOption emptyVersion unknownVersion (langVersion: string) =
        if langVersion.IsNullOrEmpty() then emptyVersion else

        match langVersion with
        | IgnoreCase "Default" -> FSharpLanguageVersion.Default
        | IgnoreCase "LatestMajor" -> FSharpLanguageVersion.LatestMajor 
        | IgnoreCase "Latest" -> FSharpLanguageVersion.Latest 
        | IgnoreCase "Preview" -> FSharpLanguageVersion.Preview

        | "4.6" -> FSharpLanguageVersion.FSharp46
        | "4.7" -> FSharpLanguageVersion.FSharp47
        | "5" | "5.0" -> FSharpLanguageVersion.FSharp50

        | _ -> unknownVersion

    let parseCompilationOption langVersion =
        tryParseCompilationOption FSharpLanguageVersion.Default FSharpLanguageVersion.Latest langVersion

    let toString (version: FSharpLanguageVersion) =
        match version with
        | FSharpLanguageVersion.Default -> "Default"
        | FSharpLanguageVersion.FSharp46 -> "F# 4.6"
        | FSharpLanguageVersion.FSharp47 -> "F# 4.7"
        | FSharpLanguageVersion.FSharp50 -> "F# 5.0"
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
        | FSharpLanguageVersion.LatestMajor -> "latestmajor"
        | FSharpLanguageVersion.Latest -> "latest"
        | FSharpLanguageVersion.Preview -> "preview"
        | _ -> failwithf "Unexpected language version: %A" version

    let toCompilerArg =
        toCompilerOptionValue >> sprintf "--langversion:%s"
