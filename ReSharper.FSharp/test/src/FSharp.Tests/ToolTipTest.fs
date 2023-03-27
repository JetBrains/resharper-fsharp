namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.QuickDoc

open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type ToolTipTest() =
    inherit TemporaryQuickDocTestBase()
    override x.RelativeTestDataPath = "features/quickdoc"
    
    [<Test>] member x.``ActivePattern 01``() = x.DoNamedTest()
    
    [<Test>] member x.``ActivePattern 02``() = x.DoNamedTest()