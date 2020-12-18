namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features
open NUnit.Framework

type GenerateObjInterfaceMembersFixTest() =
    inherit FSharpQuickFixTestBase<GenerateObjExprInterfaceMembersFix>()

    override x.RelativeTestDataPath = "features/quickFixes/generateObjExprMembers"
    
    [<Test>] member x.``Simple interface 01``() = x.DoNamedTest()
    [<Test>] member x.``Simple interface 02 - missing with``() = x.DoNamedTest()
    [<Test>] member x.``Partially complete implementation``() = x.DoNamedTest()