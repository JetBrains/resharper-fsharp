namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree

type InterfaceImplementationContext = {
    FcsEntity: FSharpEntity
    TypeMembers: TreeNodeCollection<IOverridableMemberDeclaration>
    TypeName: ITypeReferenceName
} with
    static member Create(impl: IInterfaceImplementation) =
        { FcsEntity = impl.FcsEntity
          TypeMembers = impl.TypeMembers
          TypeName = impl.TypeName }

    static member Create(objExpr: IObjExpr) =
        let reference = objExpr.TypeName.Reference
        let fcsSymbol = reference.GetFcsSymbol()
        let fscEntity = fcsSymbol.As<FSharpEntity>()

        { FcsEntity = fscEntity
          TypeMembers = objExpr.MemberDeclarations
          TypeName = objExpr.TypeName }
