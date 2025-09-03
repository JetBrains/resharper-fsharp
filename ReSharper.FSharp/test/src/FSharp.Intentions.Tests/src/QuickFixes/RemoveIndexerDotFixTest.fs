namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes
open NUnit.Framework

type RemoveIndexerDotFixTest() =
    inherit FSharpQuickFixTestBase<RemoveIndexerDotFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeIndexerDot"

    [<Test; DumpPsiTree>] member x.``Indexer 01``() = x.DoNamedTest()
    [<Test; DumpPsiTree>] member x.``Indexer 02 - Tuple``() = x.DoNamedTest()
    [<Test; DumpPsiTree>] member x.``Indexer 03 - Open range``() = x.DoNamedTest()
    [<Test; DumpPsiTree>] member x.``Indexer 04 - Set``() = x.DoNamedTest()
    [<Test; DumpPsiTree>] member x.``Indexer 05``() = x.DoNamedTest()
    [<Test; DumpPsiTree>] member x.``Indexer 06``() = x.DoNamedTest()
    [<Test; DumpPsiTree>] member x.``Indexer 07``() = x.DoNamedTest()

    [<Test; ExecuteScopedActionInFile>] member x.``Scoped 01``() = x.DoNamedTest()
