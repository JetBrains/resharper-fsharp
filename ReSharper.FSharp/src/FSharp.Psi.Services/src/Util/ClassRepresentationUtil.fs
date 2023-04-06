module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.ClassRepresentationUtil

open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree

let isEmptyClassRepr (typeRepr: ITypeRepresentation) =
    match typeRepr with
    | :? IClassRepresentation as classRepr ->
        isNotNull classRepr.FirstChild
        && isNotNull classRepr.LastChild
        && classRepr.FirstChild.GetTokenType() = FSharpTokenType.CLASS
        && classRepr.LastChild.GetTokenType() = FSharpTokenType.END
        && classRepr.LastChild.GetPreviousNonWhitespaceToken().GetTokenType() = FSharpTokenType.CLASS
    | _ -> false
