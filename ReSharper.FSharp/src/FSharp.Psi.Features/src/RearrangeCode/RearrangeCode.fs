module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.RearrangeCode.RearrangeCode

open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.RearrangeCode
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

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
