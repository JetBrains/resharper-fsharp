module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.EnumCaseLikeDeclarationUtil

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.CodeStyle
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell

let addBarIfNeeded (caseDeclaration: IEnumCaseLikeDeclaration) =
    if isNotNull caseDeclaration.Bar || isNull caseDeclaration.FirstChild then () else

    use cookie = WriteLockCookie.Create(caseDeclaration.IsPhysical())
    let bar = FSharpTokenType.BAR.CreateLeafElement()
    addNodeBefore caseDeclaration.FirstChild bar
    caseDeclaration.FormatNode()
