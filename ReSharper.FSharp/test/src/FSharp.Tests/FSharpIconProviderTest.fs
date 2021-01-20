module JetBrains.ReSharper.Plugins.FSharp.Tests.Features.IconProvider

open JetBrains.ReSharper.Feature.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.LanguageService
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest>]
type FSharpIconProviderTest() =
    inherit BaseTestWithTextControl()

    override x.RelativeTestDataPath = "features/icons"

    [<Test>] member x.``Union case property 01``() = x.DoNamedTest()
    [<Test>] member x.``Union case property 02 - private``() = x.DoNamedTest()
    [<Test>] member x.``Union case property 03 - internal``() = x.DoNamedTest()
    [<Test>] member x.``Union case class``() = x.DoNamedTest()

    override x.DoTest(lifetime, _) =
        let textControl = x.OpenTextControl(lifetime)
        let declaration = TextControlToPsi.GetElementFromCaretPosition<IDeclaration>(x.Solution, textControl)
        let declaredElement = declaration.DeclaredElement

        let declaredElement =
            match declaredElement with
            | :? IUnionCase as u when isNotNull u.NestedType -> u.NestedType :> IDeclaredElement
            | _ -> declaredElement

        let iconProvider = FSharpDeclaredElementIconProvider() :> IDeclaredElementIconProvider
        let icon, _ = iconProvider.GetImageId(declaredElement, FSharpLanguage.Instance)

        x.ExecuteWithGold(fun writer ->
            writer.WriteLine $"{declaredElement.ShortName} ({declaredElement.GetType().Name}): {icon}")
        |> ignore
