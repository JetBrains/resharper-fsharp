namespace JetBrains.ReSharper.Plugins.FSharp.Util

open System.Collections.Generic
open FSharp.Compiler.Symbols
open JetBrains.Metadata.Reader.API
open JetBrains.Metadata.Reader.Impl
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.Util

[<Sealed; AbstractClass>]
type FcsAttributeUtil =
    [<Extension>]
    static member GetClrNameFullName(attr: FSharpAttribute) =
        attr.AttributeType.BasicQualifiedName
        |> Option.defaultValue SharedImplUtil.MISSING_DECLARATION_NAME

    [<Extension>]
    static member GetClrName(attr: FSharpAttribute) =
        ClrTypeName(attr.GetClrNameFullName())

    [<Extension>]
    static member HasAttributeInstance(attrs: IList<FSharpAttribute>, clrName: string) =
        attrs |> Seq.exists (fun a -> a.GetClrNameFullName() = clrName)

    [<Extension>]
    static member GetAttributes(attrs: IList<FSharpAttribute>, clrName: string) =
        let filteredAttributes = attrs |> Seq.filter (fun a -> a.GetClrNameFullName() = clrName)
        filteredAttributes.AsIList()

    [<Extension>]
    static member TryFindAttribute(attrs: IList<FSharpAttribute>, clrName: string) =
        attrs |> Seq.tryFind (fun a -> a.GetClrNameFullName() = clrName)

    [<Extension>]
    static member HasAttributeInstance(attrs: IList<FSharpAttribute>, clrTypeName: IClrTypeName) =
        attrs.HasAttributeInstance(clrTypeName.FullName)

    [<Extension>]
    static member GetAttributes(attrs: IList<FSharpAttribute>, clrName: IClrTypeName) =
        attrs.GetAttributes(clrName.FullName)
