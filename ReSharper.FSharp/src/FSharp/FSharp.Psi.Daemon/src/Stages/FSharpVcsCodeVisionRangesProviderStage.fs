namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Stages

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Application.Parts
open JetBrains.RdBackend.Common.Features.CodeInsights.Stages.Vcs
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.RdBackend.Common.Env

[<DaemonStage(Instantiation.DemandAnyThreadSafe)>]
[<ZoneMarker(typeof<IReSharperHostNetFullFeatureZone>)>]
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
                 declaration :? IUnionCaseLikeDeclaration)

        | :? ITypeMemberDeclaration ->
            declaration :? ITopBinding ||
            declaration :? IMemberDeclaration ||
            declaration :? IAbstractMemberDeclaration ||
            declaration :? IAutoPropertyDeclaration ||
            declaration :? ISecondaryConstructorDeclaration

        | _ -> false
