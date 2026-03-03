namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickDoc

open JetBrains.ReSharper.Feature.Services.QuickDoc.Render
open JetBrains.ReSharper.Plugins.FSharp.Tests
open System
open JetBrains.Application.Components
open JetBrains.Application.DataContext
open JetBrains.Application.UI.Actions.ActionManager
open JetBrains.DocumentManagers
open JetBrains.DocumentModel
open JetBrains.DocumentModel.DataContext
open JetBrains.ProjectModel
open JetBrains.ProjectModel.DataContext
open JetBrains.ReSharper.Feature.Services.QuickDoc
open JetBrains.ReSharper.TestFramework
open JetBrains.TextControl
open JetBrains.TextControl.DataContext
open NUnit.Framework

[<FSharpTest>]
type QuickDocTest() =
    inherit BaseTestWithTextControl()

    override x.RelativeTestDataPath = "features/quickdoc"

    override this.DoTest(lifetime, project: IProject) =
        let textControl = this.OpenTextControl(lifetime)
        let solution = project.GetSolution()
        let document = textControl.Document
        let solutionPath = this.FinalSolutionItemsBasePath

        let createDataContext () =
            let actionManager = this.ShellInstance.GetComponent<IActionManager>()

            let dataRules =
                DataRules
                    .AddRule("Test", ProjectModelDataConstants.SOLUTION, fun _ -> solution)
                    .AddRule("Test", TextControlDataConstants.TEXT_CONTROL, fun _ -> textControl)
                    .AddRule("Test", DocumentModelDataConstants.DOCUMENT, fun _ -> document)
                    .AddRule("Test", DocumentModelDataConstants.EDITOR_CONTEXT, fun _ -> DocumentEditorContext(textControl.Caret.DocumentOffset()))

            actionManager.DataContexts.CreateWithDataRules(textControl.Lifetime, dataRules)

        let projectFile = this.Solution.GetComponent<DocumentManager>().TryGetProjectFile(document)
        Assert.IsNotNull(projectFile, "projectFile == null")

        let context = createDataContext ()
        let quickDocService = this.Solution.GetComponent<IQuickDocService>()
        Assert.IsTrue(quickDocService.CanShowQuickDoc(context), "No QuickDoc available")

        quickDocService.ResolveGoto(context, fun presenter language ->
            this.ExecuteWithGold(projectFile, fun writer ->
                let html = presenter.GetHtml(language).Text
                Assert.NotNull(html)
                
                let mutable text = html.Text
                let mutable startIdx = text.IndexOf(XmlDocHtmlUtil.START_HEAD_MARKER, StringComparison.Ordinal)
                let mutable endIdx = text.IndexOf(XmlDocHtmlUtil.END_HEAD_MARKER, StringComparison.Ordinal) + XmlDocHtmlUtil.END_HEAD_MARKER.Length;
                
                while startIdx <> -1 do
                    Assert.AreEqual(String.CompareOrdinal(text, endIdx, "\n<body>", 0, "\n<body>".Length), 0)
                    
                    text <- text.Remove(startIdx, endIdx - startIdx + 1)
                    startIdx <- text.IndexOf(XmlDocHtmlUtil.START_HEAD_MARKER, StringComparison.Ordinal)
                    endIdx <- text.IndexOf(XmlDocHtmlUtil.END_HEAD_MARKER, StringComparison.Ordinal) + XmlDocHtmlUtil.END_HEAD_MARKER.Length
                
                text <- text.Replace(solutionPath.FullPath, "<Test Solution Path>")
                writer.Write(text);
            )
        )

    [<Test>] member x.``ActivePattern 01``() = x.DoNamedTest()
    [<Test>] member x.``ActivePattern 02``() = x.DoNamedTest()
    [<Test>] member x.``ActivePattern 03``() = x.DoNamedTest()

    [<Test>] member x.``Partial ActivePattern 01``() = x.DoNamedTest()
    [<Test>] member x.``Partial ActivePattern 02``() = x.DoNamedTest()
    [<Test>] member x.``Partial ActivePattern 03``() = x.DoNamedTest()

    [<Test>] member x.``Let Binding 01``() = x.DoNamedTest()
    [<Test>] member x.``Let Binding 02``() = x.DoNamedTest()
    [<Test>] member x.``Let Binding 03``() = x.DoNamedTest()
    [<Test>] member x.``Let Binding 04``() = x.DoNamedTest()
    [<Test>] member x.``Let Binding 05``() = x.DoNamedTest()

    [<Test>] member x.``DiscriminatedUnion 01``() = x.DoNamedTest()
    [<Test>] member x.``DiscriminatedUnion 02``() = x.DoNamedTest()
    [<Test>] member x.``DiscriminatedUnion 03``() = x.DoNamedTest()

    [<Test>] member x.``Record 01``() = x.DoNamedTest()
    [<Test>] member x.``Record 02``() = x.DoNamedTest()

    [<Test>] member x.``Class 01``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Class 02``() = x.DoNamedTest()

    [<Test>] member x.``Top Level Module 01``() = x.DoNamedTest()
    [<Test>] member x.``Nested Module 01``() = x.DoNamedTest()

    [<Test>] member x.``Source directory 01``() = x.DoNamedTest() 
    [<Test>] member x.``Source file 01``() = x.DoNamedTest() 
    [<Test>] member x.``Line 01``() = x.DoNamedTest() 
