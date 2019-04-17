namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Todo

open JetBrains.ReSharper.FeaturesTestFramework.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type FSharpTodoTest() =
    inherit ClrTodoHighlightingTestBase()

    override x.RelativeTestDataPath = "features/todo"
    
    override x.CompilerIdsLanguage = FSharpLanguage.Instance :> _
    
    [<Test>] member x.``Line comment``() = x.DoNamedTest()
    [<Test>] member x.``Block comment``() = x.DoNamedTest() // todo: second line range isn't reported in tests

