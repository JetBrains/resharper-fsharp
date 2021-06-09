namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features
open NUnit.Framework

type ReplaceWithTypeRefExprTest() =
    inherit FSharpQuickFixTestBase<ReplaceWithTypeRefExprFix>()

    override x.RelativeTestDataPath = "features/quickFixes/replaceWithTypeRefExprFix"

    [<Test>] member x.``Field 01``() = x.DoNamedTest()

    [<Test>] member x.``Method 01``() = x.DoNamedTest()
    [<Test>] member x.``Method 02 - Method group``() = x.DoNamedTest()
    [<Test>] member x.``Method 03 - Curried``() = x.DoNamedTest()

    [<Test>] member x.``Property - Extension 01``() = x.DoNamedTest()
    [<Test>] member x.``Property - Extension 02 - Qualified``() = x.DoNamedTest()
    [<Test>] member x.``Property - Generic 01``() = x.DoNamedTest()
    [<Test>] member x.``Property - Generic 02``() = x.DoNamedTest()
    [<Test>] member x.``Property 01``() = x.DoNamedTest()
    [<Test>] member x.``Property 02``() = x.DoNamedTest()
