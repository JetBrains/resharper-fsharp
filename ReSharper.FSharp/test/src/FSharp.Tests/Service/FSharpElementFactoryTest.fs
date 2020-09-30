namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open System.Linq
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open JetBrains.ReSharper.Resources.Shell
open NUnit.Framework

[<FSharpTest>]
type FSharpElementFactoryTest() =
    inherit BaseTestWithSingleProject()

    let mutable testAction = Unchecked.defaultof<_>
    let languageService = FSharpLanguage.Instance.FSharpLanguageService

    member x.DoTest(action: IFSharpElementFactory -> unit) =
        testAction <- action
        x.DoTestSolution Array.empty<string>

    override x.DoTest(_, project: IProject) =
        let psiModule = project.GetPsiModules().Single()
        let elementFactory = languageService.CreateElementFactory(psiModule)
        testAction elementFactory

    [<Test>]
    member x.``Open statement 01``() =
        x.DoTest(fun elementFactory ->
            let ns = "System.Linq"
            let openStatement = elementFactory.CreateOpenStatement(ns)
            Assert.AreEqual("open " + ns, openStatement.GetText()))

    [<Test>]
    member x.``Wild pat 01``() =
        x.DoTest(fun elementFactory ->
            use readCookie = ReadLockCookie.Create()
            let wildPat = elementFactory.CreateWildPat()
            Assert.AreEqual(ElementType.WILD_PAT, wildPat.NodeType)
            Assert.AreEqual(1, wildPat.Children().Count())
            Assert.AreEqual(FSharpTokenType.UNDERSCORE, wildPat.FirstChild.GetTokenType())
            Assert.AreEqual("_", wildPat.GetText()))

    [<Test>]
    member x.``CreateReturnTypeInfo from type strings``() =
        x.DoTest(fun elementFactory ->
            use readCookie = ReadLockCookie.Create()
            let returnTypeInfo =
                "string"
                |> elementFactory.CreateTypeUsage
                |> elementFactory.CreateReturnTypeInfo
            let returnInfoTypeName =
                returnTypeInfo.ReturnType.As<INamedTypeUsage>().ReferenceName.Names |> Seq.exactlyOne
            Assert.AreEqual("string", returnInfoTypeName))
