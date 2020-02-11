namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests.Common
open NUnit.Framework

[<FSharpTest>]
type RemoveUnexpectedArgumentsTest() =
    inherit QuickFixTestBase<RemoveUnexpectedArgumentsFix>()
    
    override x.RelativeTestDataPath = "features/quickFixes/removeUnexpectedArguments"
    
    [<Test>] member x.``Simple - with no args``() = x.DoNamedTest()
    [<Test>] member x.``Function 1 - one arg``() = x.DoNamedTest()
    [<Test>] member x.``Function 2 - in expression``() = x.DoNamedTest()    
    [<Test>] member x.``Function 3 - many args``() = x.DoNamedTest()
    [<Test>] member x.``Function 4 - expression args``() = x.DoNamedTest()
    [<Test>] member x.``Function 5 - several errors in single line 1``() = x.DoNamedTest()
    [<Test>] member x.``Function 5 - several errors in single line 2``() = x.DoNamedTest()
    [<Test>] member x.``Function 5 - several errors in single line 3``() = x.DoNamedTest()
    [<Test>] member x.``Property 1 - simple``() = x.DoNamedTest()
    [<Test>] member x.``Expression 1 - simple``() = x.DoNamedTest()
    