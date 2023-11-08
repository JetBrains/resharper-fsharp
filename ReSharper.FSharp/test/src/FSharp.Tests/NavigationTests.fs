namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Navigation

open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Navigation.ContextNavigation
open JetBrains.ReSharper.Features.Navigation.Features.FindDeclarations
open JetBrains.ReSharper.Features.Navigation.Features.GoToDeclaration
open JetBrains.ReSharper.IntentionsTests.Navigation
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<AbstractClass; FSharpTest>]
type FSharpContextSearchTestBase(extraPath) =
    inherit AllNavigationProvidersTestBase()

    member x.DoNamedTestWithExtensions(ext1, ext2) =
        let testName = x.TestMethodName
        x.DoTestSolution(testName + ext1, testName + ext2)
    
    member x.DoNamedTestWithCSharp() =
        x.DoNamedTestWithExtensions(CSharpProjectFileType.CS_EXTENSION, FSharpProjectFileType.FsExtension)

    member x.DoNamedTestWithSignature() =
        x.DoNamedTestWithExtensions(FSharpSignatureProjectFileType.FsiExtension, FSharpProjectFileType.FsExtension)

    override x.RelativeTestDataPath = "features/navigation/" + extraPath
    override x.ExtraPath = null


type FSharpGoToUsagesTest() =
    inherit FSharpContextSearchTestBase("usages")

    override x.CreateContextAction(solution, textControl) =
        base.CreateContextAction(solution, textControl)
        |> Seq.filter (fun p -> p :? IShowUsagesProvider)

    [<Test>] member x.``Compiled active pattern case``() = x.DoNamedTest()
    [<Test>] member x.``Compiled union case``() = x.DoNamedTest()

    [<Test>] member x.``Custom op 01``() = x.DoNamedTest()

    [<Test>] member x.``Record Ctor 01 - Source``() = x.DoNamedTest()

    [<TestReferences("../../../assemblies/FSharpRecord.dll")>]
    [<Test>] member x.``Record Ctor 02 - Compiled``() = x.DoNamedTest()

    [<Test>] member x.``Anon record 01 - Ctor``() = x.DoNamedTest()
    [<Test>] member x.``Anon record 02 - Type``() = x.DoNamedTest()
    [<Test>] member x.``Anon record 03 - Getter``() = x.DoNamedTest()

    [<Test>] member x.``Module suffix 01 - Compiled``() = x.DoNamedTest()

    [<Test>] member x.``Wild pat 01``() = x.DoNamedTest()

    [<Test>] member x.``Operator 01 - Pipe``() = x.DoNamedTest()
    [<Test; Explicit("Not implemented")>] member x.``Operator 02 - =``() = x.DoNamedTest()

    [<Test>] member x.``Union case 01 - Fields``() = x.DoNamedTestWithCSharp()
    [<Test>] member x.``Union case 02 - Singleton``() = x.DoNamedTestWithCSharp()

    [<Test>] member x.``Union case - Field 01``() = x.DoNamedTestWithCSharp()
    [<Test>] member x.``Union case - Field 02 - Single case``() = x.DoNamedTestWithCSharp()
    [<Test>] member x.``Union case - Field 03 - Struct``() = x.DoNamedTestWithCSharp()

    [<Test>] member x.``Property 01 - Explicit accessors``() = x.DoNamedTestWithCSharp()

    [<Test>] member x.``Type private function - Generic 01``() = x.DoNamedTest()

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
    [<Test>] member x.``Interface 05 - Inherit``() = x.DoNamedTest()
    [<Test>] member x.``Interface 06 - Implement multiple``() = x.DoNamedTest()
    [<Test>] member x.``Interface 07 - Implement multiple``() = x.DoNamedTest()
    [<Test>] member x.``Interface 08 - Union``() = x.DoNamedTest()
    [<Test>] member x.``Interface 09 - Record``() = x.DoNamedTest()

    [<Test>] member x.``Member - Override - Auto property 01``() = x.DoNamedTest()
    [<Test>] member x.``Member - Override - Auto property 02 - Default``() = x.DoNamedTest()
    [<Test>] member x.``Member - Override - Auto property 03 - Member``() = x.DoNamedTest()
    [<Test>] member x.``Member - Override - Property 01``() = x.DoNamedTest()
    [<Test>] member x.``Member - Override - Property 02 - Default``() = x.DoNamedTest()
    [<Test>] member x.``Member - Override - Property 03 - Member``() = x.DoNamedTest()
    [<Test>] member x.``Member - Virtual - Auto property 01``() = x.DoNamedTest()
    [<Test>] member x.``Member - Virtual - Auto property 02``() = x.DoNamedTest()
    [<Test>] member x.``Member - Virtual - Property 01``() = x.DoNamedTest()
    [<Test>] member x.``Member - Virtual - Property 02``() = x.DoNamedTest()

    [<Test>] member x.``Object expr - Interface 01``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Object expr - Interface 02 - Dispose``() = x.DoNamedTest()
    [<Test>] member x.``Object expr - Override 01 - Default member``() = x.DoNamedTest()
    [<Test>] member x.``Object expr - Override 02 - Default member and interface``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Object expr - Override 03 - Interface and default member``() = x.DoNamedTest()
    [<Test>] member x.``Object expr - Type 01``() = x.DoNamedTest()


type FSharpGoToBaseTest() =
    inherit FSharpContextSearchTestBase("base")

    override x.CreateContextAction(solution, textControl) =
        base.CreateContextAction(solution, textControl)
        |> Seq.filter (fun p -> p :? INavigateToBaseProvider)

    [<Test>] member x.``Union case 01``() = x.DoNamedTest()
    [<Test>] member x.``Exception 01``() = x.DoNamedTest()
    [<Test>] member x.``Enum 01``() = x.DoNamedTest()

    [<Test>] member x.``Interface 01``() = x.DoNamedTest()
    [<Test>] member x.``Interface 02 - Inherit``() = x.DoNamedTest()
    [<Test>] member x.``Interface 03 - Implement multiple``() = x.DoNamedTest()
    [<Test>] member x.``Member - Virtual - Auto property 01``() = x.DoNamedTest()
    [<Test>] member x.``Member - Virtual - Auto property 02``() = x.DoNamedTest()
    [<Test>] member x.``Member - Virtual - Method 01``() = x.DoNamedTest()
    [<Test>] member x.``Member - Virtual - Method 02``() = x.DoNamedTest()
    [<Test>] member x.``Member - Virtual - Property 01``() = x.DoNamedTest()
    [<Test>] member x.``Member - Virtual - Property 02``() = x.DoNamedTest()

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

    [<Test>] member x.``Active pattern 01 - Expr``() = x.DoNamedTest()
    [<Test>] member x.``Active pattern 02 - Pattern``() = x.DoNamedTest()
    [<Test>] member x.``Active pattern 03 - Signature``() = x.DoNamedTestWithSignature()

    [<Test>] member x.``Own member vs interface``() = x.DoNamedTest()
    [<Test>] member x.``Ctor 01 - Modifier``() = x.DoNamedTest()

    [<Test>] member x.``Member - Virtual - Auto property 01``() = x.DoNamedTest()
    [<Test>] member x.``Member - Virtual - Method 01``() = x.DoNamedTest()
    [<Test>] member x.``Member - Virtual - Method 02``() = x.DoNamedTest()
    [<Test>] member x.``Member - Virtual - Property 01``() = x.DoNamedTest()

    [<Test>] member x.``Signature 01``() = x.DoTestSolution("Signature 01.fsi", "Signature 01.fs")
    [<Test>] member x.``Signature 02 - Same range``() = x.DoTestSolution("Signature 02 - Same range.fsi", "Signature 02 - Same range.fs")

    [<TestReferences("Library1.dll", "Library2.dll")>]
    [<Test; Explicit>] member x.``Same type from different assemblies``() = x.DoNamedTest()

    [<Test>] member x.``Union - Case - Empty 01 - Expr``() = x.DoNamedTest()
    [<Test>] member x.``Union - Case - Empty 02 - Pattern``() = x.DoNamedTest()

    [<Test>] member x.``Union - Case - Fields 01 - Expr``() = x.DoNamedTest()
    [<Test>] member x.``Union - Case - Fields 02 - Pattern``() = x.DoNamedTest()

    [<Test>] member x.``Union - Case - Single - Fields 01 - Expr``() = x.DoNamedTest()
    [<Test>] member x.``Union - Case - Single - Fields 01 - Pattern``() = x.DoNamedTest()

    [<Test>] member x.``Union - Field 01``() = x.DoNamedTest()
    [<Test>] member x.``Union - Field 02 - Single case``() = x.DoNamedTest()
    [<Test>] member x.``Union - Field 03 - Struct``() = x.DoNamedTest()

    [<Test>] member x.``Dot lambda 01``() = x.DoNamedTest()


type FSharpGoToTypeTest() =
    inherit FSharpContextSearchTestBase("type")

    override x.CreateContextAction(solution, textControl) =
        base.CreateContextAction(solution, textControl)
        |> Seq.filter (fun p -> p :? GotoTypeDeclarationProvider)

    [<Test>] member x.``Anon record field 01``() = x.DoNamedTest()

    [<Test; Explicit("Support external type parameters")>]
    member x.``Anon record field 02 - Substitution``() = x.DoNamedTest()

    [<Test>] member x.``Wild pat 01``() = x.DoNamedTest()

    [<Test>] member x.``Dot lambda - Shorthand``() = x.DoNamedTest()
