namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<TestPackages("FSharp.Core")>]
type RedundantQualifiersTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/redundantQualifiers"

    override x.HighlightingPredicate(highlighting, _, _) =
        highlighting :? RedundantQualifierWarning

    [<Test; Explicit>] member x.``AutoOpen - Assembly 01``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``AutoOpen - Assembly 02 - Implicit FSharp``() = x.DoNamedTest()

    [<Test>] member x.``AutoOpen - Global 01``() = x.DoNamedTest()

    [<Test>] member x.``AutoOpen - Nested 01``() = x.DoNamedTest()
    [<Test>] member x.``AutoOpen - Nested 02 - Qualified``() = x.DoNamedTest() // todo: overlapped

    [<Test>] member x.``AutoOpen 01``() = x.DoNamedTest()
    [<Test>] member x.``AutoOpen 02 - Qualified``() = x.DoNamedTest()

    [<Test>] member x.``Namespace 01``() = x.DoNamedTest()
    [<Test>] member x.``Namespace 02 - Qualified``() = x.DoNamedTest()
    [<Test>] member x.``Namespace 03 - Multiple import``() = x.DoNamedTest()
    [<Test>] member x.``Namespace 04 - Global``() = x.DoNamedTest()
    [<Test>] member x.``Namespace 05 - Prefix``() = x.DoNamedTest()

    [<Test>] member x.``Type extension 01``() = x.DoNamedTest()

    [<Test>] member x.``Compiled names - ModuleSuffix 01``() = x.DoNamedTest()

    [<TestReferences("../../../assemblies/ImplicitModule.dll")>]
    [<Test>] member x.``Compiled names - ModuleSuffix 02 - Implicit``() = x.DoNamedTest()

    [<Test>] member x.``Opens 01``() = x.DoNamedTest()

    [<Test>] member x.``Attributes 01 - Top level module``() = x.DoNamedTest()

    [<Test>] member x.``Delegate - Partially shadowed 01``() = x.DoNamedTest()
