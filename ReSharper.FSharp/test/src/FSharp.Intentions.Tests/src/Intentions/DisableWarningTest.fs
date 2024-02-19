namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions

open JetBrains.ReSharper.Feature.Services.Bulbs
open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type DisableWarningTest() =
    inherit BulbActionTestBase<IBulbAction>()

    override x.RelativeTestDataPath = "features/intentions/disableWarning"

    [<Test>] member x.``Disable once 01``() = x.DoNamedTest()
    [<Test>] member x.``Disable once 02 - Compiler warning``() = x.DoNamedTest()
    [<Test>] member x.``Disable once 03 - Indent``() = x.DoNamedTest()

    [<Test>] member x.``Disable and restore 01``() = x.DoNamedTest()
    [<Test>] member x.``Disable and restore 02``() = x.DoNamedTest()
    [<Test>] member x.``Disable and restore 03``() = x.DoNamedTest()
    [<Test>] member x.``Disable and restore 04 - Indent``() = x.DoNamedTest()
    [<Test>] member x.``Disable and restore 05``() = x.DoNamedTest()

    [<Test>] member x.``Disable in file 01``() = x.DoNamedTest()
    [<Test>] member x.``Disable in file 02``() = x.DoNamedTest()
    [<Test>] member x.``Disable in file 03``() = x.DoNamedTest()

    [<Test>] member x.``Disable all 01``() = x.DoNamedTest()
    [<Test>] member x.``Disable all 02``() = x.DoNamedTest()
    [<Test>] member x.``Disable all 03``() = x.DoNamedTest()

    override this.ExecuteAllTexts(_, _, _) = ()
    override this.IsAvailable _ = true

    override this.CreateQuickFix(project, textControl, highlighting) =
        let quickFixToExecute = BulbActionTestBase.GetSetting(textControl, "QF_TO_EXECUTE")
        this.GetCustomWarningAction(project, textControl, (fun action -> action.Text.Equals(quickFixToExecute)), &highlighting)

    override this.ExecuteQuickFix(_, textControl, quickFix, _) =
        quickFix.Execute(this.Solution, textControl)
