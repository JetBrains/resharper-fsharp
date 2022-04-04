namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

type ReplaceXmlDocWithLineCommentFixTest() =
    inherit FSharpQuickFixTestBase<ReplaceXmlDocWithLineCommentFix>()

    override x.RelativeTestDataPath = "features/quickFixes/replaceXmlDocWithLineComment"

    [<Test>] member x.``XmlDoc 01``() = x.DoNamedTest()
    [<Test; ExecuteScopedActionInFile>] member x.``XmlDoc 02 - Scoped``() = x.DoNamedTest()


type RemoveXmlDocFixTest() =
    inherit FSharpQuickFixTestBase<RemoveXmlDocFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeXmlDoc"

    [<Test>] member x.``XmlDoc 01``() = x.DoNamedTest()
    [<Test>] member x.``XmlDoc 02``() = x.DoNamedTest()
    [<Test>] member x.``XmlDoc 03 - Remove line``() = x.DoNamedTest()

[<FSharpTest>]
type ReplaceXmlDocWithLineCommentAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/replaceXmlDocWithLineComment"

    [<Test>] member x.``Availability - text``() = x.DoNamedTest()


[<FSharpTest>]
type RemoveXmlDocAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/removeXmlDoc"

    [<Test>] member x.``Availability - text``() = x.DoNamedTest()
