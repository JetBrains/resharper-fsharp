namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open FSharp.Compiler.Symbols
open JetBrains.Application.Components
open JetBrains.Diagnostics
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest; TestPackages(FSharpCorePackage)>]
type NameResolutionTest() =
    inherit BaseTestWithTextControl()

    let [<Literal>] Name = "NAME"
    let [<Literal>] FullName = "FULL_NAME"

    let getFullName (symbol: FSharpSymbol) =
        match symbol with
        | :? FSharpMemberOrFunctionOrValue as mfv when mfv.IsModuleValueOrMember -> Some (mfv.GetXmlDocId())
        | :? FSharpEntity as entity -> Some entity.XmlDocSig
        | _ -> None

    override x.RelativeTestDataPath = "common/checker/nameResolution"

    [<Test>] member x.``Id 01 - Global``() = x.DoNamedTest()
    [<Test>] member x.``Id 02 - Module member``() = x.DoNamedTest()
    [<Test>] member x.``Id 03 - Local``() = x.DoNamedTest()
    [<Test>] member x.``Id 04 - Recursive``() = x.DoNamedTest()

    [<Test; Explicit("Bug in FCS, #7694")>] member x.``Id 05 - Non-recursive``() = x.DoNamedTest()
    [<Test; Explicit("Bug in FCS, #7694")>] member x.``Id 06``() = x.DoNamedTest()

    [<Test>] member x.``Function 01 - Name of type``() = x.DoNamedTest()

    [<Test>] member x.``Module 01``() = x.DoNamedTest()
    
    override x.DoTest(lifetime, _) =
        let textControl = x.OpenTextControl(lifetime)
        let checkerService = x.ShellInstance.GetComponent<FcsCheckerService>()
        let sourceFile = textControl.Document.GetPsiSourceFile(x.Solution)

        let name = BaseTestWithTextControl.GetSetting(textControl, Name).NotNull()
        let fullName = BaseTestWithTextControl.GetSetting(textControl, FullName) |> Option.ofObj
        let coords = textControl.Caret.Position.Value.ToDocLineColumn()

        let symbol = checkerService.ResolveNameAtLocation(sourceFile, name, coords, x.TestName).Value.Symbol
        let symbolFullName = getFullName symbol
        Assert.AreEqual(fullName, symbolFullName)
