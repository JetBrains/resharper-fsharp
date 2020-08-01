namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type GenerateMissingRecordFieldsTest() =
    inherit QuickFixTestBase<GenerateMissingRecordFieldsFix>()

    override x.RelativeTestDataPath = "features/quickFixes/generateMissingRecordFields"

    [<Test>] member x.``Empty 01``() = x.DoNamedTest()
    [<Test>] member x.``Empty 02 - Space``() = x.DoNamedTest()
    [<Test>] member x.``Empty 03 - New line``() = x.DoNamedTest()

    [<Test>] member x.``Single line 01``() = x.DoNamedTest()
    [<Test>] member x.``Single line 02 - Semi``() = x.DoNamedTest()
    [<Test>] member x.``Single line 03 - Spaces``() = x.DoNamedTest()
    [<Test>] member x.``Single line 04 - Add two fields``() = x.DoNamedTest()
    [<Test>] member x.``Single line 05 - Name with spaces``() = x.DoNamedTest()
    [<Test>] member x.``Single line 06 - Convert to multiline``() = x.DoNamedTest()

    [<Test>] member x.``Multiline 01``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 02``() = x.DoNamedTest()
    
    [<Test>] member x.``Empty function``() = x.DoNamedTest()
    [<Test>] member x.``Function statement``() = x.DoNamedTest()
    [<Test>] member x.``If statement in function``() = x.DoNamedTest()
    [<Test>] member x.``Elif statement``() = x.DoNamedTest()
    [<Test>] member x.``Match statement in function``() = x.DoNamedTest()
    
[<FSharpTest>]
type GenerateMissingRecordFieldsAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/generateMissingRecordFields"

    // The quickfix should apply if the empty record is the final statement of a function binding, as that's what the
    // annotated return type pertains to.
    [<Test>] member x.``Empty function statement no-op``() = x.DoNamedTest()
