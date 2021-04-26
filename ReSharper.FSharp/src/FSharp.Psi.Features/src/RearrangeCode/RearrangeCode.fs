module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.RearrangeCode.RearrangeCode

open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.RearrangeCode
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell

// todo: move xmldoc

type RearrangeableCaseFieldDeclaration(field: ICaseFieldDeclaration) =
    inherit RearrangeableElementSwap<ICaseFieldDeclaration>(field, "case field declaration", Direction.LeftRight)

    override this.GetSiblings() =
        UnionCaseFieldDeclarationListNavigator.GetByField(field).NotNull().Fields :> _

[<RearrangeableElementType>]
type RearrangeableCaseFieldDeclarationProvider() =
    inherit RearrangeableSingleElementBase<ICaseFieldDeclaration>.TypeBase()

    override this.CreateElement(field: ICaseFieldDeclaration): IRearrangeable =
        RearrangeableCaseFieldDeclaration(field) :> _


type RearrangeableRecordFieldDeclaration(field: IRecordFieldDeclaration) =
    inherit RearrangeableElementSwap<IRecordFieldDeclaration>(field, "record field declaration", Direction.All)

    override this.GetSiblings() =
        RecordFieldDeclarationListNavigator.GetByFieldDeclaration(field).NotNull().FieldDeclarations :> _

[<RearrangeableElementType>]
type RearrangeableRecordFieldDeclarationProvider() =
    inherit RearrangeableSingleElementBase<IRecordFieldDeclaration>.TypeBase()

    override this.CreateElement(field: IRecordFieldDeclaration): IRearrangeable =
        RearrangeableRecordFieldDeclaration(field) :> _


type RearrangeableEnumCaseLikeDeclaration(caseDeclaration: IEnumCaseLikeDeclaration) =
    inherit RearrangeableElementSwap<IEnumCaseLikeDeclaration>(caseDeclaration, "union case declaration", Direction.All)

    let addBarIfNeeded (caseDeclaration: IEnumCaseLikeDeclaration) =
        if isNull caseDeclaration.Bar && isNotNull caseDeclaration.FirstChild then
            use cookie = WriteLockCookie.Create(caseDeclaration.IsPhysical())
            addNodesBefore caseDeclaration.FirstChild [
                FSharpTokenType.BAR.CreateLeafElement()
                Whitespace()
            ] |> ignore

    override this.GetSiblings() =
        match caseDeclaration with
        | :? IUnionCaseDeclaration as caseDeclaration ->
            UnionRepresentationNavigator.GetByUnionCase(caseDeclaration).NotNull().UnionCases |> Seq.cast

        | :? IEnumCaseDeclaration as caseD ->
            EnumRepresentationNavigator.GetByEnumCase(caseD).NotNull().EnumCases |> Seq.cast

        | _ -> failwithf $"Unexpected declaration: {caseDeclaration}"

    override this.Swap(child, target) =
        addBarIfNeeded child
        addBarIfNeeded target

        base.Swap(child, target)

[<RearrangeableElementType>]
type RearrangeableEnumCaseLikeDeclarationProvider() =
    inherit RearrangeableSingleElementBase<IEnumCaseLikeDeclaration>.TypeBase()

    override this.CreateElement(caseDeclaration: IEnumCaseLikeDeclaration): IRearrangeable =
        RearrangeableEnumCaseLikeDeclaration(caseDeclaration) :> _
