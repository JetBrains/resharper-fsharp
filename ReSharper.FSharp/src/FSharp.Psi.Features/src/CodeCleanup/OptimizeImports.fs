module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCleanup.OptimizeImports

open JetBrains.ReSharper.Daemon.CSharp.CodeCleanup
open JetBrains.ReSharper.Feature.Services.CodeCleanup
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.Util

[<CodeCleanupModule>]
type FSharpOptimizeImports() =
    interface IWholeFileCleanupOnSaveModule with
        member this.Name = "Remove unused opens"
        member this.LanguageType = FSharpLanguage.Instance :> _
        member this.Descriptors = EmptyList.Instance :> _
        member this.IsAvailableOnSelection = true

        member this.IsAvailable(sourceFile: IPsiSourceFile) =
            sourceFile.PrimaryPsiLanguage :? FSharpLanguage

        member this.IsAvailable(profile: CodeCleanupProfile): bool =
            profile.GetSetting(OptimizeUsings.OptimizeUsingsDescriptor)

        member this.Process(sourceFile, _, _, _, _) =
            sourceFile.GetPsiServices().Transactions.Execute("Code cleanup", fun _ ->
                let unusedOpens = UnusedOpensStageProcess.getUnusedOpens sourceFile.FSharpFile EmptyAction.Instance
                Array.iter OpensUtil.removeOpen unusedOpens) |> ignore

        member this.SetDefaultSetting(_, _) = ()
