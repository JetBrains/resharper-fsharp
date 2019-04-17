namespace rec JetBrains.ReSharper.Plugins.FSharp.Tests.Features.NavigationTests

open JetBrains.ReSharper.Feature.Services.Navigation.ContextNavigation
open JetBrains.ReSharper.IntentionsTests.Navigation
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<AbstractClass; FSharpTest; TestPackages("FSharp.Core")>]
type FSharpContextSearchTestBase(extraPath) =
    inherit AllNavigationProvidersTestBase()

    override x.RelativeTestDataPath = "features/navigation/" + extraPath
    override x.ExtraPath = null


type FSharpGoToUsagesTest() =
    inherit FSharpContextSearchTestBase("usages")

    override x.CreateContextAction(solution, textControl) =
        base.CreateContextAction(solution, textControl)
        |> Seq.filter (fun p -> p :? IGotoUsagesProvider)

    [<Test>] member x.``Compiled active pattern case``() = x.DoNamedTest()
    [<Test>] member x.``Compiled union case``() = x.DoNamedTest()
