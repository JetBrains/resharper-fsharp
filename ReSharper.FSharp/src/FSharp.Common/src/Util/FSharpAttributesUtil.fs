namespace JetBrains.ReSharper.Plugins.FSharp.Common.Util

open System
open System.Collections.Generic
open System.Runtime.CompilerServices
open JetBrains.Metadata.Reader.API
open JetBrains.Util.Extension
open JetBrains.Util
open Microsoft.FSharp.Compiler.SourceCodeServices

[<Extension; Sealed; AbstractClass>]
type FSharpAttributeUtil =
    [<Extension>]
    static member GetClrName (attr: FSharpAttribute) = attr.AttributeType.QualifiedBaseName
    
    [<Extension>]
    static member HasAttributeInstance (attrs: IList<FSharpAttribute>, clrName: string) =
        attrs |> Seq.exists (fun a -> FSharpAttributeUtil.GetClrName(a).Equals(clrName))

    [<Extension>]
    static member GetAttributes (attrs: IList<FSharpAttribute>, clrName) =
        (attrs |> Seq.filter (fun a -> FSharpAttributeUtil.GetClrName(a) = clrName)).AsIList()

    [<Extension>]
    static member FirstOrDefault (attrs: IList<FSharpAttribute>, clrName: string) =
        attrs |> Seq.tryFind (fun a -> FSharpAttributeUtil.GetClrName(a).Equals(clrName))

    [<Extension>]
    static member HasAttributeInstance (attrs: IList<FSharpAttribute>, clrTypeName: IClrTypeName) =
        FSharpAttributeUtil.HasAttributeInstance(attrs, clrTypeName.FullName)

    [<Extension>]
    static member GetAttributes (attrs: IList<FSharpAttribute>, clrName: IClrTypeName) =
        FSharpAttributeUtil.GetAttributes(attrs, clrName.FullName)
