namespace JetBrains.ReSharper.Plugins.FSharp.Util

open System.Collections.Generic
open FSharp.Compiler.SourceCodeServices
open JetBrains.Metadata.Reader.API
open JetBrains.Util

[<Extension; Sealed; AbstractClass>]
type FcsAttributeUtil =
    [<Extension>]
    static member GetClrName(attr: FSharpAttribute) = attr.AttributeType.QualifiedBaseName
    
    [<Extension>]
    static member HasAttributeInstance(attrs: IList<FSharpAttribute>, clrName: string) =
        attrs |> Seq.exists (fun a -> a.GetClrName() = clrName)

    [<Extension>]
    static member GetAttributes(attrs: IList<FSharpAttribute>, clrName: string) =
        let filteredAttributes = attrs |> Seq.filter (fun a -> a.GetClrName() = clrName)
        filteredAttributes.AsIList()

    [<Extension>]
    static member TryFindAttribute(attrs: IList<FSharpAttribute>, clrName: string) =
        attrs |> Seq.tryFind (fun a -> a.GetClrName() = clrName)

    [<Extension>]
    static member HasAttributeInstance(attrs: IList<FSharpAttribute>, clrTypeName: IClrTypeName) =
        attrs.HasAttributeInstance(clrTypeName.FullName)

    [<Extension>]
    static member GetAttributes(attrs: IList<FSharpAttribute>, clrName: IClrTypeName) =
        attrs.GetAttributes(clrName.FullName)
