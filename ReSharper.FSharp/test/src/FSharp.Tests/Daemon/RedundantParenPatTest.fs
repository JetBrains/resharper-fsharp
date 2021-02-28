namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<TestPackages(FSharpCorePackage)>]
type RedundantParenPatTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/redundantParens/pat"

    override x.HighlightingPredicate(highlighting, _, _) =
        highlighting :? RedundantParenPatWarning

    [<Test>] member x.``And 01``() = x.DoNamedTest()

    [<Test>] member x.``As - And 01``() = x.DoNamedTest()
    [<Test>] member x.``As - Attribute 01``() = x.DoNamedTest()
    [<Test>] member x.``As - Let 01``() = x.DoNamedTest()
    [<Test>] member x.``As - List cons 01``() = x.DoNamedTest()
    [<Test>] member x.``As - Or 01``() = x.DoNamedTest()
    [<Test>] member x.``As - Param owner 01``() = x.DoNamedTest()
    [<Test>] member x.``As - Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``As - Tuple 02 - List cons``() = x.DoNamedTest()
    [<Test>] member x.``As - Tuple 03 - Typed``() = x.DoNamedTest()
    [<Test>] member x.``As - Typed 01``() = x.DoNamedTest()

    [<Test>] member x.``Const 01``() = x.DoNamedTest()
    [<Test>] member x.``List cons 01``() = x.DoNamedTest()

    [<Test>] member x.``Or 01``() = x.DoNamedTest()
    [<Test>] member x.``Or 02 - Nested``() = x.DoNamedTest()

    [<Test>] member x.``Parameter owner 01``() = x.DoNamedTest()
    [<Test>] member x.``Ref 01``() = x.DoNamedTest()

    [<Test>] member x.``Tuple 01 - Param``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 02 - Nested``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 03 - List cons``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 04 - Match lambda``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 05 - Member like param``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 06 - Struct``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 07 - Inner pattern``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 08 - Inner list cons``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 09 - As``() = x.DoNamedTest()

    [<Test>] member x.``Typed 01 - As``() = x.DoNamedTest()
    [<Test>] member x.``Typed 02 - Attr``() = x.DoNamedTest()
    [<Test>] member x.``Typed 03 - Param owner``() = x.DoNamedTest()
    [<Test>] member x.``Typed 04 - Binding``() = x.DoNamedTest()
    [<Test>] member x.``Typed 05 - Members param decl``() = x.DoNamedTest()
    [<Test>] member x.``Typed 06 - Tuple``() = x.DoNamedTest()
    [<Test>] member x.``Typed 07 - Match clause``() = x.DoNamedTest()

    [<Test>] member x.``Wild - Function param 01``() = x.DoNamedTest()
    [<Test>] member x.``Wild - Pattern param 01``() = x.DoNamedTest()
    [<Test>] member x.``Wild - Pattern param 02 - Nested``() = x.DoNamedTest()
    [<Test>] member x.``Wild - Value 01``() = x.DoNamedTest()
    [<Test>] member x.``Wild - Parameter 01``() = x.DoNamedTest()
    [<Test>] member x.``Wild - Parameter 02``() = x.DoNamedTest()

    [<Test>] member x.``Binding 01``() = x.DoNamedTest()
    [<Test>] member x.``Lambda 01``() = x.DoNamedTest()
    [<Test>] member x.``Deindent 01``() = x.DoNamedTest()
    [<Test>] member x.``Type 01``() = x.DoNamedTest()
