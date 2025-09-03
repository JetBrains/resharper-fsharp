namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type ReplaceLambdaWithDotLambdaTest() =
    inherit FSharpQuickFixTestBase<ReplaceLambdaWithDotLambdaFix>()

    override x.RelativeTestDataPath = "features/quickFixes/useDotLambdaSyntaxFix"

    [<FSharpLanguageLevel(FSharpLanguageLevel.FSharp80)>]
    [<Test; ExecuteScopedActionInFile>] member x.``File scoped`` () = x.DoNamedTest()
    [<FSharpLanguageLevel(FSharpLanguageLevel.FSharp80)>]
    [<Test; ExecuteScopedActionInFile>] member x.``File scoped - Overlap 01`` () = x.DoNamedTest()
    [<FSharpLanguageLevel(FSharpLanguageLevel.FSharp80)>]
    [<Test; ExecuteScopedActionInFile>] member x.``File scoped - Overlap 02`` () = x.DoNamedTest()

    [<FSharpLanguageLevel(FSharpLanguageLevel.FSharp90)>]
    [<Test; ExecuteScopedActionInFile>] member x.``File scoped - F# 9`` () = x.DoNamedTest()
    [<FSharpLanguageLevel(FSharpLanguageLevel.FSharp90)>]
    [<Test; ExecuteScopedActionInFile>] member x.``File scoped - Overlap 01 - F# 9`` () = x.DoNamedTest()
    [<FSharpLanguageLevel(FSharpLanguageLevel.FSharp90)>]
    [<Test; ExecuteScopedActionInFile>] member x.``File scoped - Overlap 02 - F# 9`` () = x.DoNamedTest()

    [<FSharpLanguageLevel(FSharpLanguageLevel.FSharp90)>]
    [<Test; ExecuteScopedActionInFile>] member x.``File scoped - Remove parens`` () = x.DoNamedTest()

    [<FSharpLanguageLevel(FSharpLanguageLevel.FSharp90)>]
    [<Test>] member x.``Test1`` () = x.DoNamedTest()


[<FSharpTest>]
type ReplaceLambdaWithDotLambdaAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/useDotLambdaSyntaxFix"
    override x.HighlightingPredicate(highlighting, _, _) =
        highlighting :? DotLambdaCanBeUsedWarning

    [<Test>] member x.``Availability - text``() = x.DoNamedTest()
