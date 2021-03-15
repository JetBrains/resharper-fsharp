namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type RemoveCtorTest() =
    inherit FSharpQuickFixTestBase<RemoveConstructorFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeCtor"

    [<Test>] member x.``Enum 01 - Space``() = x.DoNamedTest()
    [<Test>] member x.``Record 01``() = x.DoNamedTest()
    [<Test>] member x.``Union 01``() = x.DoNamedTest()
    [<Test>] member x.``Union 02 - Self id``() = x.DoNamedTest()
