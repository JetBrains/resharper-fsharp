namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ProjectModel
open JetBrains.ReSharper.FeaturesTestFramework.Daemon
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest>]
type CSharpResolveTest() =
    inherit CSharpResolveTestBase(FSharpProjectFileType.FsExtension)

    [<Test>] member x.``Records 01 - Generated members``() = x.DoNamedTest()
    [<Test>] member x.``Records 02 - CliMutable``() = x.DoNamedTest()
    [<Test>] member x.``Records 03 - Override generated members``() = x.DoNamedTest()
    [<Test>] member x.``Records 04 - Sealed``() = x.DoNamedTest()
    [<Test>] member x.``Records 05 - Struct``() = x.DoNamedTest()
    [<Test>] member x.``Records 06 - Struct CliMutable``() = x.DoNamedTest()
    [<Test>] member x.``Records 07 - Field compiled name ignored``() = x.DoNamedTest()
    [<Test>] member x.``Records 08 - Interfaces``() = x.DoNamedTest()
    [<Test>] member x.``Records 09 - Private representation``() = x.DoNamedTest()

    [<Test>] member x.``Exceptions 01 - Empty``() = x.DoNamedTest()
    [<Test>] member x.``Exceptions 02 - Single field``() = x.DoNamedTest()
    [<Test>] member x.``Exceptions 03 - Multiple fields``() = x.DoNamedTest()
    [<Test>] member x.``Exceptions 04 - Protected ctor``() = x.DoNamedTest()
    [<Test>] member x.``Exceptions 05 - Augmentation``() = x.DoNamedTest()

    [<Test>] member x.``Interface 01``() = x.DoNamedTest()
    [<Test>] member x.``Interface 02 - Static members``() = x.DoNamedTest()

    [<Test>] member x.``Unions 01 - Simple generated members``() = x.DoNamedTest()
    [<Test>] member x.``Unions 02 - Singletons``() = x.DoNamedTest()
    [<Test>] member x.``Unions 03 - Nested types``() = x.DoNamedTest()
    [<Test>] member x.``Unions 04 - Single case with fields``() = x.DoNamedTest()
    [<Test>] member x.``Unions 05 - Struct single case with fields``() = x.DoNamedTest()
    [<Test>] member x.``Unions 06 - Struct nested types``() = x.DoNamedTest()
    [<Test>] member x.``Unions 07 - Private representation 01, singletons``() = x.DoNamedTest()
    [<Test>] member x.``Unions 08 - Private representation 02, nested types``() = x.DoNamedTest()
    [<Test>] member x.``Unions 09 - Private representation 03, struct``() = x.DoNamedTest()
    [<Test>] member x.``Unions 10 - Case compiled name ignored``() = x.DoNamedTest()
    [<Test>] member x.``Unions 11 - Empty single case``() = x.DoNamedTest()
    [<Test>] member x.``Unions 12 - Empty single case - Struct``() = x.DoNamedTest()
    [<Test>] member x.``Unions 13 - Empty single case - Members``() = x.DoNamedTest()
    [<Test>] member x.``Unions 14 - ReferenceEquality``() = x.DoNamedTest()

    [<Test>] member x.``Simple types 01 - Members``() = x.DoNamedTest()

    [<Test>] member x.``Class 01 - Abstract``() = x.DoNamedTest()
    [<Test>] member x.``Class 02 - Sealed``() = x.DoNamedTest()
    [<Test>] member x.``Class 03 - No attributes``() = x.DoNamedTest()
    [<Test>] member x.``Class 04 - Compiled Name - No Parens``() = x.DoNamedTest()
    [<Test>] member x.``Class 05 - Compiled Name - Double Parens``() = x.DoNamedTest()

    [<Test>] member x.``Class - Ctors 01 - Secondary``() = x.DoNamedTest()
    [<Test>] member x.``Class - Ctors 02 - Modifiers``() = x.DoNamedTest()

    [<Test>] member x.``Delegates 01 - Action``() = x.DoNamedTest()
    [<Test>] member x.``Delegates 02 - Parameters``() = x.DoNamedTest()
    [<Test>] member x.``Delegates 03 - Return type``() = x.DoNamedTest()
    [<Test>] member x.``Delegates 04 - Generic``() = x.DoNamedTest()
    [<Test>] member x.``Delegates 05 - Modifiers - Internal``() = x.DoNamedTest()
    [<Test>] member x.``Delegates 06 - Modifiers - Private``() = x.DoNamedTest()
    [<Test>] member x.``Delegates 07 - CompiledName``() = x.DoNamedTest()

    [<Test>] member x.``Val fields 01``() = x.DoNamedTest()
    [<Test>] member x.``Val fields 02 - compiled name ignored``() = x.DoNamedTest()
    [<Test>] member x.``Val fields 03 - struct``() = x.DoNamedTest()
    [<Test>] member x.``Val fields 04 - Private``() = x.DoNamedTest()

    [<Test>] member x.``Auto properties 01``() = x.DoNamedTest()
    [<Test>] member x.``Auto properties 02, compiled name``() = x.DoNamedTest()

    [<Test>] member x.``Members 01 - Virtual``() = x.DoNamedTest()

    [<Test>] member x.``Methods 01``() = x.DoNamedTest()
    [<Test>] member x.``Methods 02, compiled name``() = x.DoNamedTest()
    [<Test>] member x.``Methods 03, optional param``() = x.DoNamedTest()
    [<Test>] member x.``Methods 04, extension methods``() = x.DoNamedTest()
    [<Test>] member x.``Methods 05, void return``() = x.DoNamedTest()
    [<Test>] member x.``Methods 06, extension methods 02 - Parts``() = x.DoNamedTest()

    [<Test>] member x.``Methods - Parameters - outref``() = x.DoNamedTest()

    [<Test>] member x.``Properties 01``() = x.DoNamedTest()
    [<Test>] member x.``Properties 02 - Function type``() = x.DoNamedTest()
    [<Test>] member x.``Properties 03 - Auto with setter``() = x.DoNamedTest()
    [<Test>] member x.``Properties 04 - With setter``() = x.DoNamedTest()
    [<Test>] member x.``Properties 05 - With setter``() = x.DoNamedTest()
    [<Test>] member x.``Properties 06 - Indexed accessors 01``() = x.DoNamedTest()
    [<Test>] member x.``Properties 07 - Indexed private accessor``() = x.DoNamedTest()
    [<Test>] member x.``Properties 08 - Indexed static accessor``() = x.DoNamedTest()
    [<Test>] member x.``Properties 09 - Indexed generic accessor``() = x.DoNamedTest()
    [<Test>] member x.``Properties 10 - Simple accessors``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Properties 11 - Indexers``() = x.DoNamedTest()
    [<Test>] member x.``Properties 12 - Indexers - Access modifiers 01``() = x.DoNamedTest()
    [<Test>] member x.``Properties 13 - Indexers - Access modifiers 02``() = x.DoNamedTest()
    [<Test>] member x.``Properties 14 - Indexers - Access modifiers 03``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Properties 15 - Indexer - Static``() = x.DoNamedTest()
    [<Test>] member x.``Properties 16 - Indexers - Partial accessors``() = x.DoNamedTest()
    [<Test>] member x.``Properties 17 - Indexers - Compiled name``() = x.DoNamedTest()
    [<Test>] member x.``Properties 18 - Indexed accessors - Compiled name``() = x.DoNamedTest()
    [<Test>] member x.``Properties 19 - Simple property with getter``() = x.DoNamedTest()
    [<Test>] member x.``Properties 20 - Simple property with accessors``() = x.DoNamedTest()
    [<Test>] member x.``Properties 21 - static val``() = x.DoNamedTest()

    [<FSharpLanguageLevel(FSharpLanguageLevel.FSharp70)>]
    [<Test>] member x.``Module bindings - Extension 01 - F# 7``() = x.DoNamedTest()
    [<FSharpLanguageLevel(FSharpLanguageLevel.FSharp80)>]
    [<Test>] member x.``Module bindings - Extension 02 - F# 8``() = x.DoNamedTest()

    [<Test>] member x.``Module bindings 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Module bindings 02 - Records``() = x.DoNamedTest()
    [<Test>] member x.``Module bindings 03 - extension methods 01``() = x.DoNamedTest()
    [<Test>] member x.``Module bindings 04 - extension methods 02``() = x.DoNamedTest()
    [<Test>] member x.``Module bindings 05 - Generic function``() = x.DoNamedTest()
    [<Test>] member x.``Module bindings 06 - Type function``() = x.DoNamedTest()
    [<Test>] member x.``Module bindings 07 - extension methods 03 - Two params``() = x.DoNamedTest()
    [<Test>] member x.``Module bindings 08 - Mutable``() = x.DoNamedTest()
    [<Test>] member x.``Module bindings 09 - Access modifier``() = x.DoNamedTest()

    [<Test>] member x.``Module bindings - Compiled name 01``() = x.DoNamedTest()
    [<Test>] member x.``Module bindings - Compiled name 02 - Nested pat``() = x.DoNamedTest()
    [<Test>] member x.``Module bindings - Compiled name 03 - Overloads``() = x.DoNamedTest()

    [<Test>] member x.``Nested module 01``() = x.DoNamedTest()
    [<Test>] member x.``Nested module 02 - Compiled name``() = x.DoNamedTest()

    [<Test>] member x.``Operators - Math 01 - Addition``() = x.DoNamedTest()
    [<Test>] member x.``Operators - Math 02 - Subtraction``() = x.DoNamedTest()
    [<Test>] member x.``Operators - Math 03 - Multiplication``() = x.DoNamedTest()
    [<Test>] member x.``Operators - Math 04 - Division``() = x.DoNamedTest()

    [<Test>] member x.``Operators 01 - Module``() = x.DoNamedTest()
    [<Test>] member x.``Operators 02 - Type``() = x.DoNamedTest()
    [<Test>] member x.``Operators 03 - Greater, Less``() = x.DoNamedTest()
    [<Test>] member x.``Operators 04 - Implicit, Explicit``() = x.DoNamedTest()
    [<Test>] member x.``Operators 05 - Equals``() = x.DoNamedTest()

    [<Test>] member x.``Enum 01``() = x.DoNamedTest()

    [<Test>] member x.``Events 01``() = x.DoNamedTest()
    [<Test>] member x.``Events 02 - Abstract``() = x.DoNamedTest()

    [<Test>] member x.``Struct 01``() = x.DoNamedTest()

    [<Test>] member x.``Type Extension 01``() = x.DoNamedTest()
    [<Test>] member x.``Type Extension 02 - Struct``() = x.DoNamedTest()
    [<Test>] member x.``Type Extension 03 - Struct record``() = x.DoNamedTest()
    [<Test>] member x.``Type Extension 04 - Struct union``() = x.DoNamedTest()
    [<Test>] member x.``Type Extension 05 - Interface``() = x.DoNamedTest()
    [<Test>] member x.``Type Extension 06 - Enum``() = x.DoNamedTest()
    [<Test>] member x.``Type Extension 07 - Different parameters count``() = x.DoNamedTest()
    [<Test>] member x.``Type Extension 08 - Multiple extensions``() = x.DoNamedTest()
    [<Test>] member x.``Type Extension 09 - In namespace``() = x.DoNamedTest()
    [<Test>] member x.``Type Extension 10 - Compiled name``() = x.DoNamedTest()
    [<Test>] member x.``Type Extension 11 - Struct compiled name``() = x.DoNamedTest()
    [<Test>] member x.``Type Extension 12 - Optional extension``() = x.DoNamedTest()
    [<Test>] member x.``Type Extension 13 - C# extension 01``() = x.DoNamedTest()
    [<Test>] member x.``Type Extension 14 - C# extension 02``() = x.DoNamedTest()
    [<Test>] member x.``Type Extension 15 - Compiled name``() = x.DoNamedTest()
    [<Test>] member x.``Type Extension 16 - Compiled name - Static``() = x.DoNamedTest()

    [<Test>] member x.``Type repr - Class 01 - Impl``() = x.DoNamedTest()
    [<Test>] member x.``Type repr - Class 02 - Member``() = x.DoNamedTest()

    [<Test>] member x.``Type parameters - Method - Constraints 01``() = x.DoNamedTest()
    [<Test>] member x.``Type parameters - Method - Constraints 02 - Outer``() = x.DoNamedTest()
    [<Test>] member x.``Type parameters - Type - Constraints 01 - ValueType``() = x.DoNamedTest()
    [<Test>] member x.``Type parameters - Type - Constraints 02 - Reference``() = x.DoNamedTest()
    [<Test>] member x.``Type parameters - Type - Constraints 03 - Ctor``() = x.DoNamedTest()
    [<Test>] member x.``Type parameters - Type - Constraints 04 - Type``() = x.DoNamedTest()
    [<Test>] member x.``Type parameters - Type - Constraints 05 - Unmanaged``() = x.DoNamedTest()
    [<Test>] member x.``Type parameters 01``() = x.DoNamedTest()

    [<Test>] member x.``Implementations - Explicit impl 01``() = x.DoNamedTest()

    [<Test>] member x.``Abbreviations - Module 01``() = x.DoNamedTest()

    [<Test>] member x.``Abbreviations - Type 01``() = x.DoNamedTest()
    [<Test>] member x.``Abbreviations - Type 02 - Qualified``() = x.DoNamedTest()

    [<Test>] member x.``AttributeUsage 01 - AllowMultiple``() = x.DoNamedTest()
    [<Test>] member x.``AttributeUsage 02 - AttributeTargets``() = x.DoNamedTest()

    [<Test>] member x.``Parameters 01``() = x.DoNamedTest()


[<FSharpTest>]
type CSharpResolveFromSignatures() =
    inherit CSharpResolveTestBase(FSharpSignatureProjectFileType.FsiExtension)

    [<Test>] member x.``Type Extension 01 - Struct - Signature``() = x.DoNamedTest()
    [<Test>] member x.``Type Extension 02 - Class - Signature``() = x.DoNamedTest()


[<FSharpTest>]
[<TestFileExtension(".cs")>]
type CSharpTooltipTest() =
    inherit IdentifierTooltipTestBase()

    override x.RelativeTestDataPath = "cache/csharpResolve/tooltips"

    member x.DoNamedTestWithFs() =
        let testName = x.TestMethodName
        x.DoTestSolution(testName + CSharpProjectFileType.CS_EXTENSION, testName + FSharpProjectFileType.FsExtension)

    member x.DoNamedTestWithFsi() =
        let testName = x.TestMethodName
        x.DoTestSolution(testName + CSharpProjectFileType.CS_EXTENSION, testName + FSharpSignatureProjectFileType.FsiExtension)

    member x.DoNamedTestWithFsiAndFs() =
        let testName = x.TestMethodName
        x.DoTestSolution(testName + CSharpProjectFileType.CS_EXTENSION,
                         testName + FSharpSignatureProjectFileType.FsiExtension,
                         testName + FSharpProjectFileType.FsExtension)

    [<Test>] member x.``XmlDoc 01 - Top binding 01``() = x.DoNamedTestWithFs()
    [<Test>] member x.``XmlDoc 01 - Top binding 02 - Pattern``() = x.DoNamedTestWithFs()
    [<Test>] member x.``XmlDoc 02 - Type``() = x.DoNamedTestWithFs()
    [<Test>] member x.``XmlDoc 03 - Record field``() = x.DoNamedTestWithFs()
    [<Test>] member x.``XmlDoc 04 - Union case``() = x.DoNamedTestWithFs()
    [<Test>] member x.``XmlDoc 05 - Primary constructor``() = x.DoNamedTestWithFs()
    [<Test>] member x.``XmlDoc 06 - Type member``() = x.DoNamedTestWithFs()
    [<Test>] member x.``XmlDoc 07 - Abstract type member``() = x.DoNamedTestWithFs()
    [<Test>] member x.``XmlDoc 08 - Type member signature``() = x.DoNamedTestWithFsi()
    [<Test>] member x.``XmlDoc 09 - Val``() = x.DoNamedTestWithFsi()
    [<Test>] member x.``XmlDoc 10 - Module``() = x.DoNamedTestWithFs()
    [<Test>] member x.``XmlDoc 11 - Struct val``() = x.DoNamedTestWithFs()
    [<Test>] member x.``XmlDoc 12 - Type parts 01 - Class``() = x.DoNamedTestWithFsiAndFs()
    [<Test>] member x.``XmlDoc 12 - Type parts 02 - Struct``() = x.DoNamedTestWithFsiAndFs()
    [<Test>] member x.``XmlDoc 12 - Type parts 03 - Delegate``() = x.DoNamedTestWithFsiAndFs()
    [<Test>] member x.``XmlDoc 12 - Type parts 04 - Interface``() = x.DoNamedTestWithFsiAndFs()
    [<Test>] member x.``XmlDoc 12 - Type parts 05 - Enum``() = x.DoNamedTestWithFsiAndFs()
    [<Test>] member x.``XmlDoc 12 - Type parts 06 - Module``() = x.DoNamedTestWithFsiAndFs()
    [<Test>] member x.``XmlDoc 12 - Type parts 07 - Union``() = x.DoNamedTestWithFsiAndFs()
    [<Test>] member x.``XmlDoc 13 - Type members 01 - Top binding``() = x.DoNamedTestWithFsiAndFs()
    [<Test>] member x.``XmlDoc 13 - Type members 02 - Abstract member``() = x.DoNamedTestWithFsiAndFs()
