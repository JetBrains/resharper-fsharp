namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

// todo: add test with signature files

[<FSharpTest; TestPackages(FSharpCorePackage)>]
type ToMutableFixTest() =
    inherit FSharpQuickFixTestBase<ToMutableFix>()

    override x.RelativeTestDataPath = "features/quickFixes/toMutable"

    [<Test>] member x.``Record field 01``() = x.DoNamedTest()
    [<Test>] member x.``Record field 02 - Attributes``() = x.DoNamedTest()

    [<Test>] member x.``Val - Local 01``() = x.DoNamedTest()
    [<Test>] member x.``Val - Top level 01``() = x.DoNamedTest()

    [<Test; Explicit>] member x.``Val - Parameter pattern 01 - Union case param``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Val - Parameter pattern 02 - Function param``() = x.DoNamedTest()
    [<Test>] member x.``Val - Parameter pattern 03 - Typed``() = x.DoNamedTest()
    
    [<Test>] member x.``TopAsPat``() = x.DoNamedTest()
    [<Test>] member x.``LocalAsPat``() = x.DoNamedTest()

    [<Test; NotAvailable>] member x.``LocalAsPat - Pattern matching, not available``() = x.DoNamedTest()
