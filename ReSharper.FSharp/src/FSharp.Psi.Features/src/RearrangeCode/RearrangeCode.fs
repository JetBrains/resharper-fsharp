module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.RearrangeCode.RearrangeCode

open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.RearrangeCode
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

// todo: move xmldoc

type RearrangeableCaseFieldDeclaration(field: ICaseFieldDeclaration) =
    inherit RearrangeableElementSwap<ICaseFieldDeclaration>(field, "field declaration", Direction.LeftRight)

    override this.GetSiblings() =
        UnionCaseFieldDeclarationListNavigator.GetByField(field).NotNull().Fields :> _

[<RearrangeableElementType>]
type RearrangeableCaseFieldDeclarationProvider() =
    inherit RearrangeableSingleElementBase<ICaseFieldDeclaration>.TypeBase()

    override this.CreateElement(field: ICaseFieldDeclaration): IRearrangeable =
        RearrangeableCaseFieldDeclaration(field) :> _
