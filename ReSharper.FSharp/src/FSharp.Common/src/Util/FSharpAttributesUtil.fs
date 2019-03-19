namespace JetBrains.ReSharper.Plugins.FSharp.Common.Util

open System.Collections.Generic
open JetBrains.Metadata.Reader.API
open JetBrains.Util
open Microsoft.FSharp.Compiler.SourceCodeServices

[<Extension; Sealed; AbstractClass>]
type FSharpAttributeUtil =
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
