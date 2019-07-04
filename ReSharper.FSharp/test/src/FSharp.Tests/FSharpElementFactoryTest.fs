namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open System.Linq
open JetBrains.ProjectModel
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.LanguageService
open JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService
open JetBrains.ReSharper.Plugins.FSharp.Tests.Common
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest>]
type FSharpElementFactoryTest() =
    inherit BaseTestWithSingleProject()

    let mutable testAction = Unchecked.defaultof<_>
    let languageService = FSharpLanguage.Instance.LanguageService() :?> FSharpLanguageService

    member x.DoTest(action: IFSharpElementFactory -> unit) =
        testAction <- action
        x.DoTestSolution()

    override x.DoTest(_, project: IProject) =
        let psiModule = project.GetPsiModules().Single()
        let elementFactory = FSharpElementFactory(languageService, psiModule)
        testAction elementFactory

    [<Test>]
    member x.Test() =
        x.DoTest(fun elementFactory ->
            let ns = "System.Linq"
            let openStatement = elementFactory.CreateOpenStatement("System.Linq")
            Assert.AreEqual("open " + ns, openStatement.GetText()))
