namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Refactorings

open System.Linq
open JetBrains.ProjectModel
open JetBrains.ReSharper.FeaturesTestFramework.Refactorings
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest>]
type FSharpRenameTest() =
    inherit RenameTestBase()

    override x.RelativeTestDataPath = "features/refactorings/rename"

    override x.ProvideOccurrencesData(occurrences, _, _, _) =
        // When multiple overloads are available, we want to rename initial element.
        // Current occurrences are:
        // "Rename initial element"
        // "Rename with overloads"
        occurrences.FirstOrDefault()

    member x.DoNamedTestFsCs() =
        let testName = x.TestMethodName
        let csExt = CSharpProjectFileType.CS_EXTENSION
        let fsExt = FSharpProjectFileType.FsExtension
        x.DoTestSolution(testName + csExt, testName + fsExt)

    member x.DoNamedTestFsiFsProgram() =
        let testName = x.TestMethodName
        let fsExt = FSharpProjectFileType.FsExtension
        let fsiExt = FSharpSignatureProjectFileType.FsiExtension
        x.DoTestSolution(testName + fsiExt, testName + fsExt, $"{testName} - Program" + fsExt)

    [<Test>] member x.``Escaped name 01 - Type``() = x.DoNamedTest()
    [<Test>] member x.``Escaped name 02 - Binding``() = x.DoNamedTest()

    [<Test>] member x.``Inline - Declaration``() = x.DoNamedTest()
    [<Test>] member x.``Inline - Use``() = x.DoNamedTest()
    [<Test>] member x.``Inline - Member self id``() = x.DoNamedTest()
    [<Test>] member x.``Inline - Ctor self id``() = x.DoNamedTest()
    [<Test>] member x.``Inline - Secondary ctor self id``() = x.DoNamedTest()
    [<Test>] member x.``Inline - Ctor do``() = x.DoNamedTest()
    [<Test>] member x.``Inline - Pattern 01``() = x.DoNamedTest()

    [<Test>] member x.``Inline - Lambda param 01``() = x.DoNamedTest()
    [<Test>] member x.``Inline - Lambda param 02 - Typed``() = x.DoNamedTest()

    [<Test>] member x.``Inline - synPat or 1``() = x.DoNamedTest()
    [<Test>] member x.``Inline - synPat or 2``() = x.DoNamedTest()
    [<Test>] member x.``Inline - synPat or 3``() = x.DoNamedTest()
    [<Test>] member x.``Inline - synPat or 4``() = x.DoNamedTest()
    [<Test>] member x.``Inline - synPat or 5``() = x.DoNamedTest()
    [<Test>] member x.``Inline - synPat or 6``() = x.DoNamedTest()

    [<Test>] member x.``Inline - Not allowed - Active pattern 01 - Match``() = x.DoNamedTest()
    [<Test>] member x.``Inline - Not allowed - Active pattern 02 - For``() = x.DoNamedTest()
    [<Test>] member x.``Inline - Not allowed - Active pattern 03 - Param``() = x.DoNamedTest()
    [<Test>] member x.``Inline - Not allowed - Union case 01``() = x.DoNamedTest()

    [<Test>] member x.``Module binding - Simple pattern, declaration``() = x.DoNamedTest()
    [<Test>] member x.``Module binding - Simple pattern, reference``() = x.DoNamedTest()
    [<Test>] member x.``Module binding - Function``() = x.DoNamedTest()
    [<Test>] member x.``Module binding - Tuple``() = x.DoNamedTest()
    [<Test>] member x.``Module binding - Named pat 01 - id``() = x.DoNamedTest()
    [<Test>] member x.``Module binding - Named pat 02 - pat``() = x.DoNamedTest()
    [<Test>] member x.``Module binding - Named pat 03 - nested pat``() = x.DoNamedTest()

    [<Test>] member x.``Module binding - synPat or``() = x.DoNamedTest()

    [<Test>] member x.``Module binding - nested synPat or 1``() = x.DoNamedTest()
    [<Test>] member x.``Module binding - nested synPat or 2``() = x.DoNamedTest()
    [<Test>] member x.``Module binding - nested synPat or 3``() = x.DoNamedTest()

    [<Test>] member x.``Module binding - Record pat 01``() = x.DoNamedTest()
    [<Test>] member x.``Module binding - Record pat 02 - nested pat``() = x.DoNamedTest()
    [<Test>] member x.``Module binding - Record pat 03 - nested named pat``() = x.DoNamedTest()
    [<Test>] member x.``Module binding - Record pat 04 - nested named pat 2``() = x.DoNamedTest()

    [<Test>] member x.``Literal pattern 01``() = x.DoNamedTest()
    [<Test>] member x.``Literal pattern 02``() = x.DoNamedTest()
    [<Test>] member x.``Literal pattern 03``() = x.DoNamedTest()
    [<Test>] member x.``Literal pattern 04 - Qualified``() = x.DoNamedTest()

    [<Test>] member x.``Params - Declaration``() = x.DoNamedTest()
    [<Test>] member x.``Params - Use``() = x.DoNamedTest()
    [<Test>] member x.``Params - Or``() = x.DoNamedTest()
    [<Test>] member x.``Params - Type private function``() = x.DoNamedTest()
    [<Test>] member x.``Params - Attributes``() = x.DoNamedTest()
    [<Test>] member x.``Params - Optional param 01``() = x.DoNamedTest()

    [<Test>] member x.``Ctor params 01``() = x.DoNamedTest()
    [<Test>] member x.``Ctor params 02``() = x.DoNamedTest()
    [<Test>] member x.``Ctor params 03``() = x.DoNamedTest()

    [<Test>] member x.``Type private binding - function``() = x.DoNamedTest()
    [<Test>] member x.``Type private binding - value``() = x.DoNamedTest()

    [<Test>] member x.``Types - Record 01``() = x.DoNamedTest()
    [<Test>] member x.``Types - Record 02 - Struct``() = x.DoNamedTest()

    [<Test>] member x.``Types - Enum 01``() = x.DoNamedTest()
    [<Test>] member x.``Types - Struct 01``() = x.DoNamedTest()
    [<Test>] member x.``Types - Interface 01``() = x.DoNamedTest()
    [<Test>] member x.``Types - Exception 01``() = x.DoNamedTest()
    [<Test>] member x.``Types - Abbreviations 01``() = x.DoNamedTest()
    [<Test>] member x.``Types - Measure 01``() = x.DoNamedTest()
    [<Test>] member x.``Types - Measure 02``() = x.DoNamedTest()

    [<Test>] member x.``Attribute - Arg 01 - Record field``() = x.DoNamedTest()
    [<Test>] member x.``Attribute - Attribute type 01``() = x.DoNamedTest()

    [<Test>] member x.``Union Case - Single - Different name 01``() = x.DoNamedTest()
    [<Test>] member x.``Union Case - Single - Different name 02``() = x.DoNamedTest()
    [<Test>] member x.``Union Case - Single - Same name 01``() = x.DoNamedTest()
    [<Test>] member x.``Union Case - Single - Same name 02``() = x.DoNamedTest()
    [<Test>] member x.``Union Case 01 - Should start with upper case``() = x.DoNamedTest()

    [<Test>] member x.``Types - Ctor 01``() = x.DoNamedTest()
    [<Test>] member x.``Types - Ctor 02 - Secondary``() = x.DoNamedTest()

    [<Test>] member x.``Types - Generic 01``() = x.DoNamedTest()
    [<Test>] member x.``Types - Generic 02``() = x.DoNamedTest()

    [<Test>] member x.``Types - Inheritance 01``() = x.DoNamedTest()
    [<Test>] member x.``Types - Inheritance 02 - object expressions``() = x.DoNamedTest()

    [<Test>] member x.``Types - Inheritance - Type params``() = x.DoNamedTest()
    [<Test>] member x.``Types - Inheritance - Type params - Qualified``() = x.DoNamedTest()
    [<Test>] member x.``Types - Inheritance - Type params - Qualifier``() = x.DoNamedTest()
    [<Test>] member x.``Types - Inheritance - Type params - Nested qualified``() = x.DoNamedTest()

    [<Test>] member x.``Types - New expr 01``() = x.DoNamedTest()

    [<Explicit>]
    [<TestPackages(SqlProviderPackage, Inherits = true)>]
    [<TestSetting(typeof<JetBrains.ReSharper.Plugins.FSharp.Settings.FSharpExperimentalFeatures>, "OutOfProcessTypeProviders", "false")>]
    [<Test>] member x.``Types - Arg - Expr 01``() = x.DoNamedTest()

    [<Test>] member x.``Interface 01 - Impl``() = x.DoNamedTest()
    [<Test>] member x.``Interface 02 - Internal type impl``() = x.DoNamedTest()

    [<Test>] member x.``Type parameters - Type 01``() = x.DoNamedTest()
    [<Test>] member x.``Type parameters - Type reference 01``() = x.DoNamedTest()
    [<Test>] member x.``Type parameters - Constraints 01``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Type parameters - Constraints 02 - Function``() = x.DoNamedTest()
    [<Test>] member x.``Type parameters - Constraints 03``() = x.DoNamedTest()

    [<Test>] member x.``Active patterns - Local - Partial 01 - Decl``() = x.DoNamedTest()
    [<Test>] member x.``Active patterns - Local - Partial 02 - Pattern``() = x.DoNamedTest()
    [<Test>] member x.``Active patterns - Local - Single 01 - Decl``() = x.DoNamedTest()
    [<Test>] member x.``Active patterns - Local - Single 02 - Pattern``() = x.DoNamedTest()
    [<Test>] member x.``Active patterns - Local - Single 03 - Pattern 2``() = x.DoNamedTest()
    [<Test>] member x.``Active patterns - Local - Total 01 - Decl``() = x.DoNamedTest()
    [<Test>] member x.``Active patterns - Local - Total 02 - Use in decl``() = x.DoNamedTest()
    [<Test>] member x.``Active patterns - Local - Total 03 - Use``() = x.DoNamedTest()

    [<Test>] member x.``Active patterns - Module - No param - Single 01 - Decl``() = x.DoNamedTest()
    [<Test>] member x.``Active patterns - Module - No param - Single 02``() = x.DoNamedTest()
    [<Test>] member x.``Active patterns - Module - No param - Single 03 - Expr``() = x.DoNamedTest()
    [<Test>] member x.``Active patterns - Module - No param - Total 01 - Decl``() = x.DoNamedTest()
    [<Test>] member x.``Active patterns - Module - No param - Total 02 - Pattern``() = x.DoNamedTest()
    [<Test>] member x.``Active patterns - Module - No param - Total 03 - Expr``() = x.DoNamedTest()

    [<Test>] member x.``Active patterns - Module - Partial 01 - Decl``() = x.DoNamedTest()
    [<Test>] member x.``Active patterns - Module - Partial 02 - Pattern``() = x.DoNamedTest()
    [<Test>] member x.``Active patterns - Module - Single 01 - Decl``() = x.DoNamedTest()
    [<Test>] member x.``Active patterns - Module - Single 02 - Pattern``() = x.DoNamedTest()
    [<Test>] member x.``Active patterns - Module - Single 03 - Pattern 2``() = x.DoNamedTest()
    [<Test>] member x.``Active patterns - Module - Total 01 - Decl``() = x.DoNamedTest()
    [<Test>] member x.``Active patterns - Module - Total 02 - Use in decl``() = x.DoNamedTest()
    [<Test>] member x.``Active patterns - Module - Total 03 - Use``() = x.DoNamedTest()

    [<Test>] member x.``Active patterns - Type private 01 - Partial``() = x.DoNamedTest()
    [<Test>] member x.``Active patterns - Type private 02 - Single``() = x.DoNamedTest()
    [<Test>] member x.``Active patterns - Type private 03 - Total``() = x.DoNamedTest()

    [<Test>] member x.``Active patterns - Qualified 01 - Decl``() = x.DoNamedTest()
    [<Test>] member x.``Active patterns - Qualified 02 - Expr``() = x.DoNamedTest()
    [<Test>] member x.``Active patterns - Qualified 03 - Pattern``() = x.DoNamedTest()

    [<Test>] member x.``Active patterns - Unavailable``() = x.DoNamedTest()
    [<Test>] member x.``Active patterns - Should start with upper case``() = x.DoNamedTest()

    [<Test>] member x.``Extensions - Optional - Instance 01``() = x.DoNamedTest()
    [<Test>] member x.``Extensions - Optional - Instance 02``() = x.DoNamedTest()
    [<Test>] member x.``Extensions - Optional - Instance 03 - Overloads 01``() = x.DoNamedTest()
    [<Test>] member x.``Extensions - Optional - Instance 04 - Overloads 02``() = x.DoNamedTest()

    [<Test>] member x.``Extensions - Optional - Static 01 - Same params 01``() = x.DoNamedTest()
    [<Test>] member x.``Extensions - Optional - Static 02 - Same params 02``() = x.DoNamedTest()
    [<Test>] member x.``Extensions - Optional - Static 03 - Different params 01``() = x.DoNamedTest()
    [<Test>] member x.``Extensions - Optional - Static 04 - Different params 02``() = x.DoNamedTest()

    [<Test>] member x.``Extensions - Optional - Type 01``() = x.DoNamedTest()
    [<Test>] member x.``Extensions - Optional - Type 02 - Qualified``() = x.DoNamedTest()
    [<Test>] member x.``Extensions - Optional - Type 03 - Qualified 2``() = x.DoNamedTest()

    [<Test>] member x.``Implicit module - From same name 01``() = x.DoNamedTestFsCs()
    [<Test>] member x.``Implicit module - From same name 02``() = x.DoNamedTestFsCs()
    [<Test>] member x.``Implicit module 02 - To same name``() = x.DoNamedTestFsCs()

    [<Test>] member x.``Generated members - Record fields 01 - Ctor param``() = x.DoNamedTestFsCs()

    [<Test>] member x.``Related symbols - Single case union 01 - Union``() = x.DoNamedTestFsCs()
    [<Test>] member x.``Related symbols - Single case union 02 - Case``() = x.DoNamedTestFsCs()

    [<Test>] member x.``Object expr 01``() = x.DoNamedTest()

    [<Test>] member x.``Type member - Abstract 01 - Name``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Abstract 02 - Type``() = x.DoNamedTest()

    [<Test>] member x.``Type member - Virtual - Method 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Virtual - Method 02``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Virtual - Method 03 - Overload``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Virtual - Method 04 - Overload``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Virtual - Auto property 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Virtual - Auto property 02``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Virtual - Property 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Virtual - Property 02``() = x.DoNamedTest()

    [<Test>] member x.``Type member - Auto prop 01 - Name``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Auto prop 02 - Type``() = x.DoNamedTest()

    [<Test>] member x.``Type member - Field 01 - Name``() = x.DoNamedTest()
    [<Test>] member x.``Type member - Field 02 - Type``() = x.DoNamedTest()

    [<Test>] member x.``Union Case Field - Nested type - Named arg 01``() = x.DoNamedTest() // todo: struct

    [<Test>] member x.``Union Case Field - Creation param 01``() = x.DoNamedTest()
    [<Test>] member x.``Union Case Field - Creation param 02 - Tuple``() = x.DoNamedTest()

    [<Test>] member x.``Union Case Field - Pattern param 01``() = x.DoNamedTest()
    [<Test>] member x.``Union Case Field - Pattern param 02``() = x.DoNamedTest()
    [<Test>] member x.``Union Case Field - Pattern param 03 - Pat``() = x.DoNamedTest()

    [<Test>] member x.``Anon record 01``() = x.DoNamedTest()
    [<Test>] member x.``Anon record 02 - Invalid name``() = x.DoNamedTest()

    [<Test>] member x.``Error - If 01``() = x.DoNamedTest()

    [<Test>] member x.``Inherit expr 01 - Arg``() = x.DoNamedTest()
    [<Test>] member x.``Inherit expr 02 - Type name``() = x.DoNamedTest()
    [<Test>] member x.``Inherit expr 03 - Field``() = x.DoNamedTest()

    [<Test>] member x.``Wild - Let - Top 01``() = x.DoNamedTest()
    [<Test>] member x.``Wild - Match 01``() = x.DoNamedTest()

    [<Test>] member x.``Property accessors 01``() = x.DoNamedTestFsCs()
    [<Test>] member x.``Property accessors 02 - Direct call``() = x.DoNamedTest()

    [<Test>] member x.``Sig - Record 01``() = x.DoNamedTestFsiFsProgram()
    [<Test>] member x.``Sig - Record 02 - Struct``() = x.DoNamedTestFsiFsProgram()
    [<Test>] member x.``Sig - Union 01``() = x.DoNamedTestFsiFsProgram()
    [<Test>] member x.``Sig - Union 02``() = x.DoNamedTestFsiFsProgram()
    [<Test>] member x.``Sig - Union 03 - Struct``() = x.DoNamedTestFsiFsProgram()

    [<Test>] member x.``Not allowed - Dot lambda - Shorthand``() = x.DoNamedTest()

    [<Test>] member x.``Dot lambda 01 - Property``() = x.DoNamedTest()

    [<Test>] member x.``Hash type usage 01``() = x.DoNamedTest()
