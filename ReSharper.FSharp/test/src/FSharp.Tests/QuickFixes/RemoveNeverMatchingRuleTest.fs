namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type RemoveNeverMatchingRuleTest() =
    inherit QuickFixTestBase<RemoveNeverMatchingRuleFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeNeverMatchingRule"

    [<Test>] member x.``Match expr - Single rule 01``() = x.DoNamedTest()
    [<Test>] member x.``Match expr - Single rule 02 - Comment``() = x.DoNamedTest()
    [<Test>] member x.``Match expr - Multiple rules 01``() = x.DoNamedTest()
    [<Test>] member x.``Match expr - Multiple rules 02 - Comment``() = x.DoNamedTest()
    [<Test>] member x.``Match expr - Multiple rules 03 - Single line``() = x.DoNamedTest()
    [<Test>] member x.``Match expr - Multiple rules 04 - First clause on a line``() = x.DoNamedTest()
    [<Test>] member x.``Match lambda expr - Single rule 01``() = x.DoNamedTest()
    [<Test>] member x.``Try with expr - Single rule 01``() = x.DoNamedTest()

    [<Test; ExecuteScopedQuickFixInFile>] member x.``Scoped 01 - Multiple match exprs``() = x.DoNamedTest()
    [<Test; ExecuteScopedQuickFixInFile>] member x.``Scoped 02 - Match expr and Try with expr``() = x.DoNamedTest()
