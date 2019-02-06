namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Refactorings

open JetBrains.ReSharper.FeaturesTestFramework.Refactorings
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type FSharpRenameTest() =
    inherit RenameTestBase()

    override x.RelativeTestDataPath = "features/refactorings/rename"

    override x.GetProjectProperties(targetFrameworkIds, flavours) =
        FSharpProjectPropertiesFactory.CreateProjectProperties(targetFrameworkIds)

    [<Test>] member x.``Inline - Declaration``() = x.DoNamedTest()
    [<Test>] member x.``Inline - Use``() = x.DoNamedTest()
    [<Test>] member x.``Inline - Member self id``() = x.DoNamedTest()
    [<Test>] member x.``Inline - Ctor self id``() = x.DoNamedTest()

    [<Test>] member x.``Inline - synPat or 1``() = x.DoNamedTest()
    [<Test>] member x.``Inline - synPat or 2``() = x.DoNamedTest()
    [<Test>] member x.``Inline - synPat or 3``() = x.DoNamedTest()
    [<Test>] member x.``Inline - synPat or 4``() = x.DoNamedTest()

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

    [<Test>] member x.``Params - Declaration``() = x.DoNamedTest()
    [<Test>] member x.``Params - Use``() = x.DoNamedTest()
    [<Test>] member x.``Params - Or``() = x.DoNamedTest()

    [<Test>] member x.``Types - Record 01``() = x.DoNamedTest()
    [<Test>] member x.``Types - Record 02 - Struct``() = x.DoNamedTest()

    [<Test>] member x.``Types - Enum 01``() = x.DoNamedTest()
    [<Test>] member x.``Types - Struct 01``() = x.DoNamedTest()
    [<Test>] member x.``Types - Interface 01``() = x.DoNamedTest()

    [<Test>] member x.``Types - Exception 01``() = x.DoNamedTest() // todo: make upper case name


    [<Test>] member x.``Active patterns - Local - Partial 01 - Decl``() = x.DoNamedTest()
    [<Test>] member x.``Active patterns - Local - Partial 02 - Pattern``() = x.DoNamedTest()
    [<Test>] member x.``Active patterns - Local - Single 01 - Decl``() = x.DoNamedTest()
    [<Test>] member x.``Active patterns - Local - Single 02 - Pattern``() = x.DoNamedTest()
    [<Test>] member x.``Active patterns - Local - Single 03 - Pattern 2``() = x.DoNamedTest()
    [<Test>] member x.``Active patterns - Local - Total 01 - Decl``() = x.DoNamedTest()
    [<Test>] member x.``Active patterns - Local - Total 02 - Use in decl``() = x.DoNamedTest()
    [<Test>] member x.``Active patterns - Local - Total 03 - Use``() = x.DoNamedTest()

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
