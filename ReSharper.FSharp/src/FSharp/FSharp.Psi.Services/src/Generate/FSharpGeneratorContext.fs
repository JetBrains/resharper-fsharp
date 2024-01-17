namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Generate

open System
open FSharp.Compiler.Symbols
open JetBrains.Annotations
open JetBrains.ReSharper.Feature.Services.Generate
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree

type IFSharpGeneratorElement =
    abstract Mfv: FSharpMemberOrFunctionOrValue
    abstract DisplayContext: FSharpDisplayContext
    abstract Substitution: (FSharpGenericParameter * FSharpType) list
    abstract AddTypes: bool
    abstract IsOverride: bool


[<AllowNullLiteral>]
type FSharpGeneratorContext(kind, [<NotNull>] treeNode: ITreeNode,
        [<CanBeNull>] typeDecl: IFSharpTypeElementDeclaration) =
    inherit GeneratorContextBase(kind)

    let mutable selectedRange = TreeTextRange.InvalidRange

    member x.TypeDeclaration = typeDecl

    override x.Language = FSharpLanguage.Instance :> _

    override x.Root = typeDecl :> _
    override val Anchor = null with get, set

    override x.PsiModule = treeNode.GetPsiModule()
    override this.Solution = treeNode.GetSolution()

    override x.GetSelectionTreeRange() = selectedRange

    override x.CreatePointer() =
        FSharpGeneratorWorkflowPointer(x) :> _

    member x.SetSelectedRange(range) =
        selectedRange <- range

    static member Create(kind, [<NotNull>] treeNode: ITreeNode, [<CanBeNull>] typeDecl: IFSharpTypeElementDeclaration,
            anchor) =
        if isNotNull treeNode && treeNode.IsFSharpSigFile() then null else

        FSharpGeneratorContext(kind, treeNode, typeDecl, Anchor = anchor)


and FSharpGeneratorWorkflowPointer(context: FSharpGeneratorContext) =
    interface IGeneratorContextPointer with
        // todo: use actual pointers
        member x.TryRestoreContext() = context :> _

[<Flags>]
type PropertyOverrideState =
    | None = 0
    | Getter = 1
    | Setter = 2

type FSharpGeneratorElement(element: IOverridableMember, mfvInstance: FcsMfvInstance, addTypes) =
    inherit GeneratorDeclaredElement(element)

    member x.AddTypes = addTypes
    member x.Mfv = mfvInstance.Mfv
    member x.MfvInstance = mfvInstance
    member x.Member = element

    interface IFSharpGeneratorElement with
        member x.Mfv = x.Mfv
        member x.DisplayContext = mfvInstance.DisplayContext
        member x.Substitution = mfvInstance.Substitution
        member x.AddTypes = x.AddTypes
        member x.IsOverride = true

    override x.ToString() = element.ToString()
