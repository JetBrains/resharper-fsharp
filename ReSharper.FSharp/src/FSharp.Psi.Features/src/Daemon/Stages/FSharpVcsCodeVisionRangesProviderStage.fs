namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Host.Features.CodeInsights.Stages.Vcs
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree

[<DaemonStage>]
type FSharpVcsCodeVisionRangesProviderStage() =
    inherit CodeInsightsVcsRangesStageBase<FSharpLanguage>()

    override x.CreateProcess(file, daemonProcess) =
        FSharpVcsCodeVisionRangesProviderProcess(file, daemonProcess) :> _

and FSharpVcsCodeVisionRangesProviderProcess(file, daemonProcess) =
    inherit CodeInsightsVcsRangesDaemonProcess(file, daemonProcess)

    override x.IsApplicable(declaration) =
        match declaration with
        | :? ITypeDeclaration ->
            not (declaration :? IAnonModuleDeclaration ||
                 declaration :? IUnionCaseDeclaration)

        | :? ITypeMemberDeclaration ->
            declaration :? ITopBinding ||
            declaration :? IMemberDeclaration ||
            declaration :? IAbstractMemberDeclaration ||
            declaration :? IAutoPropertyDeclaration ||
            declaration :? IMemberConstructorDeclaration ||
            declaration :? IValFieldDeclaration

        | _ -> false
