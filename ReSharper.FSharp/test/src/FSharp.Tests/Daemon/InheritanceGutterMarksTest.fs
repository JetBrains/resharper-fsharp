namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open JetBrains.ReSharper.Feature.Services.Daemon
open NUnit.Framework

type InheritanceGutterMarks() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/inheritanceGutterMarks"

    override x.HighlightingPredicate(highlighting, _, _) = highlighting :? IInheritanceMarkOnGutter
    override x.InheritanceGutterMarks = true

    [<Test>] member x.``Inherited gutter mark``() = x.DoNamedTest()

    [<Test>] member x.``Module 01``() = x.DoNamedTest()
    [<Test>] member x.``Struct 01``() = x.DoNamedTest()
