namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open NUnit.Framework

type UpdateCompiledNameInSignatureFixTest() =
    inherit FSharpQuickFixTestBase<UpdateCompiledNameInSignatureFix>()

    override x.RelativeTestDataPath = "features/quickFixes/updateCompiledNameInSignatureFix"
    
    [<Test>] member x.``Attribute in implementation - 01`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Attribute in implementation - 02`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``No attribute in implementation - 01`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``No attribute in implementation - 02`` () = x.DoNamedTestWithSignature()
