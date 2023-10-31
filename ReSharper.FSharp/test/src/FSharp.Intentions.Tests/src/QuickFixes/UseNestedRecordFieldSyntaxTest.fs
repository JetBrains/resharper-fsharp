namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type UseNestedRecordFieldSyntaxTest() =
    inherit FSharpQuickFixTestBase<UseNestedRecordFieldSyntaxFix>()

    override x.RelativeTestDataPath = "features/quickFixes/useNestedRecordFieldSyntaxFix"

    [<Test; ExecuteScopedActionInFile>] member x.``File scoped`` () = x.DoNamedTest()
    [<Test; ExecuteScopedActionInFile>] member x.``File scoped - Overlap`` () = x.DoNamedTest()
    [<Test; ExecuteScopedActionInFile>] member x.``Resolve qualifiers 01`` () = x.DoNamedTest()
    [<Test; ExecuteScopedActionInFile>] member x.``Resolve qualifiers 02 - Namespaces`` () = x.DoNamedTest()
    [<Test>] member x.``Mangle field name`` () = x.DoNamedTest()


[<FSharpTest>]
type UseNestedRecordFieldSyntaxAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/useNestedRecordFieldSyntaxFix"
    override x.HighlightingPredicate(highlighting, _, _) =
        highlighting :? NestedRecordUpdateCanBeSimplifiedWarning

    [<Test>] member x.``Availability - text``() = x.DoNamedTest()
