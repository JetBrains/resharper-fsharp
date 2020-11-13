namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<TestPackages(FSharpCorePackage)>]
type InferredTypeCodeVisionProviderTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/inferredTypeCodeVision"

    override x.HighlightingPredicate(highlighting, _, _) =
        highlighting :? FSharpInferredTypeHighlighting

    [<Test>] member x.``Module functions and values``() = x.DoNamedTest()
    [<Test>] member x.``Type fields and members``() = x.DoNamedTest()
    [<Test>] member x.``Unopened namespace``() = x.DoNamedTest()

    [<Test>] member x.``Object expression 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple param``() = x.DoNamedTest()
    
    [<Test>] member x.``Top binding head pattern with parens``() = x.DoNamedTest()
