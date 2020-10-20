namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features
open NUnit.Framework

type GenerateInterfaceMembersFixTest() =
    inherit FSharpQuickFixTestBase<GenerateInterfaceMembersFix>()

    override x.RelativeTestDataPath = "features/quickFixes/generateInterfaceMembers"

    [<Test>] member x.``Before other 01 - Partial``() = x.DoNamedTest()

    [<Test>] member x.``Empty impl 01``() = x.DoNamedTest()
    [<Test>] member x.``Empty impl 02 - Generate multiple``() = x.DoNamedTest()
    [<Test>] member x.``Empty impl 03``() = x.DoNamedTest()

    [<Test>] member x.``Partial impl 01``() = x.DoNamedTest()
    [<Test>] member x.``Partial impl 02 - Generate multiple``() = x.DoNamedTest()
    [<Test>] member x.``Partial impl 03 - Multiline``() = x.DoNamedTest()

    [<Test>] member x.``Event - Cli 01``() = x.DoNamedTest()

    [<Test>] member x.``Method - Parameters - Curried 01``() = x.DoNamedTest()
    [<Test>] member x.``Method - Parameters - Curried 02``() = x.DoNamedTest()
    [<Test>] member x.``Method - Parameters - Empty 01``() = x.DoNamedTest()
    [<Test>] member x.``Method - Parameters - Multiple 01``() = x.DoNamedTest()
    [<Test>] member x.``Method - Parameters - Multiple 02 - Anon``() = x.DoNamedTest()
    [<Test>] member x.``Method - Parameters - Single 01``() = x.DoNamedTest()
    [<Test>] member x.``Method - Parameters - Single 02 - Anon``() = x.DoNamedTest()
    [<Test>] member x.``Method - Type parameters 01``() = x.DoNamedTest()
    [<Test>] member x.``Method - Type parameters 02 - Multiple``() = x.DoNamedTest()
    [<Test>] member x.``Method - Substitution 01 - Param``() = x.DoNamedTest()
    [<Test>] member x.``Method - Substitution 02 - Return``() = x.DoNamedTest()
    [<Test>] member x.``Method - Substitution 03 - Multiple``() = x.DoNamedTest()
    [<Test>] member x.``Method - Substitution 04 - Inherited interface``() = x.DoNamedTest()
    [<Test>] member x.``Method - Substitution 05 - Inherited interface``() = x.DoNamedTest()

    [<Test>] member x.``Overloads 01``() = x.DoNamedTest()
    [<Test>] member x.``Overloads 02``() = x.DoNamedTest()
    [<Test>] member x.``Overloads 03``() = x.DoNamedTest()

    [<Test>] member x.``Escaped names 01 - Member``() = x.DoNamedTest()
    [<Test>] member x.``Escaped names 02 - Param``() = x.DoNamedTest()
    [<Test>] member x.``Escaped names 03 - Method``() = x.DoNamedTest()
    [<Test>] member x.``Escaped names 04 - Operator``() = x.DoNamedTest()

    [<Test>] member x.``Nested interface 01``() = x.DoNamedTest()
    [<Test>] member x.``Nested interface 02 - Multiple``() = x.DoNamedTest()


type GenerateMissingMembersFixTest() =
    inherit FSharpQuickFixTestBase<GenerateMissingOverridesFix>()

    override x.RelativeTestDataPath = "features/quickFixes/generateMissingMembers"

    [<Test>] member x.``Property 01``() = x.DoNamedTest()