﻿namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

type GenerateInterfaceMembersFixTest() =
    inherit FSharpQuickFixTestBase<GenerateInterfaceMembersFix>()

    override x.RelativeTestDataPath = "features/quickFixes/generateInterfaceMembers"

    [<Test>] member x.``Before other 01 - Partial``() = x.DoNamedTest()

    [<Test>] member x.``Empty impl 01``() = x.DoNamedTest()
    [<Test>] member x.``Empty impl 02 - Generate multiple``() = x.DoNamedTest()
    [<Test>] member x.``Empty impl 03``() = x.DoNamedTest()
    [<Test>] member x.``Empty impl 04 - With keyword``() = x.DoNamedTest()

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
    [<Test>] member x.``Method - Type parameters 03 - Implicit``() = x.DoNamedTest()
    [<Test>] member x.``Method - Substitution 01 - Param``() = x.DoNamedTest()
    [<Test>] member x.``Method - Substitution 02 - Return``() = x.DoNamedTest()
    [<Test>] member x.``Method - Substitution 03 - Multiple``() = x.DoNamedTest()
    [<Test>] member x.``Method - Substitution 04 - Inherited interface``() = x.DoNamedTest()
    [<Test>] member x.``Method - Substitution 05 - Inherited interface``() = x.DoNamedTest()

    [<Test>] member x.``Property - Indexer 01``() = x.DoNamedTest()
    [<Test>] member x.``Property - Indexer 02 - Named param``() = x.DoNamedTest()
    [<Test>] member x.``Property - Indexer 03``() = x.DoNamedTest()
    [<Test>] member x.``Property 01``() = x.DoNamedTest()
    [<Test>] member x.``Property 02 - Setter``() = x.DoNamedTest()
    [<Test>] member x.``Property 03 - Setter only``() = x.DoNamedTest()
    [<Test>] member x.``Property 04``() = x.DoNamedTest()
    [<Test>] member x.``Property 05``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Property 06``() = x.DoNamedTest() // todo: fix in 233
    [<Test>] member x.``Property 07``() = x.DoNamedTest()

    [<Test>] member x.``Overloads 01``() = x.DoNamedTest()
    [<Test>] member x.``Overloads 02``() = x.DoNamedTest()
    [<Test>] member x.``Overloads 03``() = x.DoNamedTest()

    [<Test>] member x.``Escaped names 01 - Member``() = x.DoNamedTest()
    [<Test>] member x.``Escaped names 02 - Param``() = x.DoNamedTest()
    [<Test>] member x.``Escaped names 03 - Method``() = x.DoNamedTest()
    [<Test>] member x.``Escaped names 04 - Operator``() = x.DoNamedTest()

    [<Test>] member x.``Nested interface 01``() = x.DoNamedTest()
    [<Test>] member x.``Nested interface 02 - Multiple``() = x.DoNamedTest()
    [<Test>] member x.``Nested interface 03 - Partially base implemented``() = x.DoNamedTest()


[<FSharpTest>]
type GenerateMissingMembersFixTest() =
    inherit FSharpQuickFixTestBase<GenerateMissingOverridesFix>()

    override x.RelativeTestDataPath = "features/quickFixes/generateMissingMembers"

    [<Test>] member x.``Context - Common namespace 01``() = x.DoNamedTest()
    [<Test>] member x.``Explicit impl 01``() = x.DoNamedTest()
    [<Test>] member x.``Explicit impl 02``() = x.DoNamedTest()
    [<Test>] member x.``Explicit impl 03``() = x.DoNamedTest()
    [<Test>] member x.``Default impl 01``() = x.DoNamedTest()
    [<Test>] member x.``Partial type 01``() = x.DoNamedTest()
    [<Test>] member x.``Property - Accessor - Setter 01``() = x.DoNamedTest()
    [<Test>] member x.``Property - Accessor - Setter 02``() = x.DoNamedTest()
    [<Test>] member x.``Property - Accessor - Setter 03``() = x.DoNamedTest()
    [<Test>] member x.``Property - Indexer 01``() = x.DoNamedTest()
    [<Test>] member x.``Property 01``() = x.DoNamedTest()
    [<Test>] member x.``Same name 01``() = x.DoNamedTest()
    [<Test>] member x.``Same name 02``() = x.DoNamedTest()
    [<Test>] member x.``Substitution 01``() = x.DoNamedTest()
    [<Test>] member x.``Substitution 02``() = x.DoNamedTest()
    [<Test>] member x.``Substitution 03``() = x.DoNamedTest()
    [<Test>] member x.``Super 01 - Different type parameter name``() = x.DoNamedTest()
