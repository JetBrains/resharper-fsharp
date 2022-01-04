namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type AddInstanceMemberSelfIdTest() =
    inherit FSharpQuickFixTestBase<AddInstanceMemberSelfIdFix>()

    override x.RelativeTestDataPath = "features/quickFixes/addInstanceMemberSelfId"

    [<Test>] member x.``Access modifier 01``() = x.DoNamedTest()
    [<Test>] member x.``Expression alignment 01``() = x.DoNamedTest()

    [<FSharpLanguageLevel(FSharpLanguageLevel.FSharp46)>]
    [<Test>] member x.``Version - 46 01``() = x.DoNamedTest()

    [<Test>] member x.``Version - Default 01``() = x.DoNamedTest()
