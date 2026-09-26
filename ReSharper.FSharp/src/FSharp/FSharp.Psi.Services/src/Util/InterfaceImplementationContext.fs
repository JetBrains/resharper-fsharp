namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree

[<Interface>]
type IInterfaceImplementationContext =
    abstract member FcsEntity: FSharpEntity
    abstract member TypeMembers: TreeNodeCollection<IOverridableMemberDeclaration>
    abstract member TypeName: ITypeReferenceName

type InterfaceImplementationContext(fcsEntity: FSharpEntity, typeMembers: TreeNodeCollection<IOverridableMemberDeclaration>, typeName: ITypeReferenceName) =
    member _.FcsEntity = fcsEntity
    member _.TypeMembers = typeMembers
    member _.TypeName = typeName

    interface IInterfaceImplementationContext with
        member _.FcsEntity = fcsEntity
        member _.TypeMembers = typeMembers
        member _.TypeName = typeName

    static member Create(impl: IInterfaceImplementation) =
        InterfaceImplementationContext(impl.FcsEntity, impl.TypeMembers, impl.TypeName)

    static member Create(objExpr: IObjExpr) =
        let reference = objExpr.TypeName.Reference
        let fcsSymbol = reference.GetFcsSymbol()
        let fscEntity = fcsSymbol.As<FSharpEntity>()

        InterfaceImplementationContext(fscEntity, objExpr.MemberDeclarations, objExpr.TypeName)
