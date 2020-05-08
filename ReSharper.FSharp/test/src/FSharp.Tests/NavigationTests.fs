namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Navigation

open JetBrains.ReSharper.Feature.Services.Navigation.ContextNavigation
open JetBrains.ReSharper.Features.Navigation.Features.FindDeclarations
open JetBrains.ReSharper.Features.Navigation.Features.GoToDeclaration
open JetBrains.ReSharper.IntentionsTests.Navigation
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<AbstractClass; FSharpTest; TestPackages("FSharp.Core")>]
type FSharpContextSearchTestBase(extraPath) =
    inherit AllNavigationProvidersTestBase()

    override x.RelativeTestDataPath = "features/navigation/" + extraPath
    override x.ExtraPath = null


type FSharpGoToUsagesTest() =
    inherit FSharpContextSearchTestBase("usages")

    override x.CreateContextAction(solution, textControl) =
        base.CreateContextAction(solution, textControl)
        |> Seq.filter (fun p -> p :? IGotoUsagesProvider)

    [<Test>] member x.``Compiled active pattern case``() = x.DoNamedTest()
    [<Test>] member x.``Compiled union case``() = x.DoNamedTest()

    [<Test>] member x.``Record Ctor 01 - Source``() = x.DoNamedTest()

    [<TestReferences("../../../assemblies/FSharpRecord.dll")>]
    [<Test>] member x.``Record Ctor 02 - Compiled``() = x.DoNamedTest()

    [<Test>] member x.``Anon record 01 - Ctor``() = x.DoNamedTest()
    [<Test>] member x.``Anon record 02 - Type``() = x.DoNamedTest()
    [<Test>] member x.``Anon record 03 - Getter``() = x.DoNamedTest()


type FSharpGoToInheritorsTest() =
    inherit FSharpContextSearchTestBase("inheritors")

    override x.CreateContextAction(solution, textControl) =
        base.CreateContextAction(solution, textControl)
        |> Seq.filter (fun p -> p :? IGotoInheritorsProvider)

    [<Test>] member x.``Union case 01``() = x.DoNamedTest()
    [<Test>] member x.``Types 01``() = x.DoNamedTest()
    [<Test>] member x.``Types 02``() = x.DoNamedTest()

    [<Test>] member x.``Interface 01``() = x.DoNamedTest()
    [<Test>] member x.``Interface 02 - Member``() = x.DoNamedTest()
    [<Test>] member x.``Interface 03 - Internal type impl``() = x.DoNamedTest()
    [<Test>] member x.``Interface 04 - Overloads``() = x.DoNamedTest()

    [<Test>] member x.``Object expr - Interface 01``() = x.DoNamedTest()
    [<Test>] member x.``Object expr - Interface 02 - Dispose``() = x.DoNamedTest()
    [<Test>] member x.``Object expr - Override 01 - Default member``() = x.DoNamedTest()
    [<Test>] member x.``Object expr - Override 02 - Default member and interface``() = x.DoNamedTest()
    [<Test>] member x.``Object expr - Override 03 - Interface and default member``() = x.DoNamedTest()
    [<Test>] member x.``Object expr - Type 01``() = x.DoNamedTest()


type FSharpGoToBaseTest() =
    inherit FSharpContextSearchTestBase("base")

    override x.CreateContextAction(solution, textControl) =
        base.CreateContextAction(solution, textControl)
        |> Seq.filter (fun p -> p :? INavigateToBaseProvider)

    [<Test>] member x.``Union case 01``() = x.DoNamedTest()
    [<Test>] member x.``Exception 01``() = x.DoNamedTest()
    [<Test>] member x.``Enum 01``() = x.DoNamedTest()

    [<Test>] member x.``Object expr - Interface 01``() = x.DoNamedTest()
    [<Test>] member x.``Object expr - Interface 02 - Dispose``() = x.DoNamedTest()
    [<Test>] member x.``Object expr - Override 01 - ToString``() = x.DoNamedTest()
    [<Test>] member x.``Object expr - Override 02 - Default member``() = x.DoNamedTest()
    [<Test>] member x.``Object expr - Override 03 - Default member and interface``() = x.DoNamedTest()
    [<Test>] member x.``Object expr - Override 04 - Interface and default member``() = x.DoNamedTest()
    [<Test>] member x.``Object expr - Type 01``() = x.DoNamedTest()


type FSharpGoToDeclarationTest() =
    inherit FSharpContextSearchTestBase("declaration")

    override x.CreateContextAction(solution, textControl) =
        base.CreateContextAction(solution, textControl)
        |> Seq.filter (fun p -> p :? IGotoDeclarationProvider)

    [<Test>] member x.``Own member vs interface``() = x.DoNamedTest()

    [<TestReferences("Library1.dll", "Library2.dll")>]
    [<Test>] member x.``Same type from different assemblies``() = x.DoNamedTest()


type FSharpGoToTypeTest() =
    inherit FSharpContextSearchTestBase("type")

    override x.CreateContextAction(solution, textControl) =
        base.CreateContextAction(solution, textControl)
        |> Seq.filter (fun p -> p :? GotoTypeDeclarationProvider)

    [<Test>] member x.``Anon record field 01``() = x.DoNamedTest()

    [<Test; Explicit("Support external type parameters")>]
    member x.``Anon record field 02 - Substitution``() = x.DoNamedTest()
