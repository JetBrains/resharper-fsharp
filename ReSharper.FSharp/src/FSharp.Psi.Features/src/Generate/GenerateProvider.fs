namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features

open JetBrains.Application.Progress
open JetBrains.ReSharper.Feature.Services.Generate
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.DataContext
open JetBrains.ReSharper.Psi.Tree

[<AllowNullLiteral>]
type FSharpGeneratorContext(kind, typeDecl: IFSharpTypeDeclaration) =
    inherit GeneratorContextBase(kind)

    member x.TypeDeclaration = typeDecl

    override x.Language = FSharpLanguage.Instance :> _

    override x.Root = typeDecl :> _
    override val Anchor = null with get, set // todo

    override x.PsiModule = typeDecl.GetPsiModule()
    override x.Project = typeDecl.GetProject()

    override x.GetSelectionTreeRange() = TreeTextRange.InvalidRange // todo

    override x.CreatePointer() =
        FSharpGeneratorWorkflowPointer(x) :> _

    static member Create(kind, treeNode: ITreeNode) =
        let typeDeclaration = treeNode.As<IFSharpTypeDeclaration>()
        if isNull typeDeclaration || isNull typeDeclaration.DeclaredElement then null else

        FSharpGeneratorContext(kind, typeDeclaration)


and FSharpGeneratorWorkflowPointer(context: FSharpGeneratorContext) =
    interface IGeneratorContextPointer with
        // todo: use actual pointers
        member x.TryRestoreContext() = context :> _


[<Language(typeof<FSharpLanguage>)>]
type FSharpGeneratorContextFactory() =
    interface IGeneratorContextFactory with
        member x.TryCreate(kind: string, psiDocumentRangeView: IPsiDocumentRangeView): IGeneratorContext =
            let psiView = psiDocumentRangeView.View<FSharpLanguage>()
            // todo: get union through union case
            let typeDeclaration = psiView.GetSelectedTreeNode<IFSharpTypeDeclaration>()
            FSharpGeneratorContext.Create(kind, typeDeclaration) :> _

        member x.TryCreate(_, _, _) = null
        member x.TryCreate(_: string, _: IDeclaredElement): IGeneratorContext = null


[<GeneratorElementProvider(GeneratorStandardKinds.Overrides, typeof<FSharpLanguage>)>]
type FSharpOverridableMembersProvider() =
    inherit GeneratorProviderBase<FSharpGeneratorContext>()

    let canHaveOverrides (typeElement: ITypeElement) =
        // todo: filter out union cases
        match typeElement with
        | :? FSharpClass as fsClass -> not (fsClass.IsAbstract && fsClass.IsSealed)
        | :? IStruct -> true
        | _ -> false // todo: interfaces with default impl

    override x.Populate(context: FSharpGeneratorContext) =
        let typeElement = context.TypeDeclaration.DeclaredElement
        if not (canHaveOverrides typeElement) then () else

        let memberInstances = GenerateUtil.GetOverridableMembersOrder(typeElement, false)

        memberInstances
        |> Seq.map (fun i -> i.Member)
        |> Seq.filter (fun m ->
            // todo: events, anything else?
            // todo: separate getters/setters (including existing ones)
            (m :? IMethod || m :? IProperty) && m.GetContainingType() <> typeElement && m.CanBeOverridden())
        |> Seq.map GeneratorDeclaredElement
        |> Seq.iter context.ProvidedElements.Add


[<GeneratorBuilder(GeneratorStandardKinds.Overrides, typeof<FSharpLanguage>)>]
type FSharpOverridingMembersBuilder() =
    inherit GeneratorBuilderBase<FSharpGeneratorContext>()

    override x.Process(_: FSharpGeneratorContext, _: IProgressIndicator) =
        // todo: generate
        ()
