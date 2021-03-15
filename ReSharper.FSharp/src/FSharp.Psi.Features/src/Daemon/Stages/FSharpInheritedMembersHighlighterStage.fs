namespace rec JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages

open System
open System.Collections.Generic
open JetBrains.ReSharper.Daemon.Specific.InheritedGutterMark
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2
open JetBrains.ReSharper.Psi.Search
open JetBrains.ReSharper.Psi.Tree

[<Language(typeof<FSharpLanguage>)>]
type FSharpInheritedMembersHighlighterStageProcessFactory() =
    inherit InheritedMembersHighlighterProcessFactory()

    override x.CreateProcess(daemonProcess, file) =
        match file.As<IFSharpFile>() with
        | null -> null
        | fsFile -> InheritedMembersStageProcess(fsFile, daemonProcess) :> _


type InheritedMembersStageProcess(fsFile, daemonProcess) =
    inherit FSharpDaemonStageProcessBase(fsFile, daemonProcess)

    let psiModule = fsFile.GetPsiModule()
    let symbolScope = getSymbolScope psiModule
    let searchDomain = SearchDomainFactory.Instance.CreateSearchDomain(daemonProcess.Solution, false)

    let processDeclaration (result: IList<_>) (typeMemberDeclaration: ITypeMemberDeclaration) =
        match typeMemberDeclaration.As<IFSharpTypeDeclaration>() with
        | null -> ()
        | typeDecl ->

        // Don't add inherited icon for unions with cases compiled as nested inherited types.
        if typeDecl.TypeRepresentation :? IUnionRepresentation then () else

        // This is a workaround until we can resolve types without waiting for FCS to type check projects graph
        // up to the possible inheritor point.
        // Using this approach may add unwanted gutter icons in some cases.
        if isNull typeDecl.DeclaredElement then () else

        let typeElement = typeDecl.DeclaredElement
        if not (typeElement :? IClass || typeElement :? IInterface) then () else

        let typeElement = typeElement.As<TypeElement>()
        if isNotNull typeElement && typeElement.IsSealed then () else

        let inheritors = symbolScope.GetPossibleInheritors(typeDecl.CompiledName)
        if not (Seq.exists searchDomain.HasIntersectionWith inheritors) then () else

        let range = typeDecl.GetNameDocumentRange()
        result.Add(HighlightingInfo(range, TypeIsInheritedMarkOnGutter(typeDecl, range)))

    override x.Execute(committer) =
        let result = List()
        let processor = RecursiveElementProcessor<ITypeMemberDeclaration>(Action<_>(processDeclaration result))

        processor.InteriorShouldBeProcessedHandler <-
            fun node -> node :? IModuleLikeDeclaration || node :? ITypeDeclarationGroup

        x.FSharpFile.ProcessDescendants(processor)
        committer.Invoke(DaemonStageResult(result))
