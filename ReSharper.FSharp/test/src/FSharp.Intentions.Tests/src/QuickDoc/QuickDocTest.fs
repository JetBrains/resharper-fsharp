namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickDoc

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
    
    override this.DoTest(lifetime, _) =
        let textControl = this.OpenTextControl(lifetime)
        let solution = this.Solution
        let document = textControl.Document

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

                let startIdx = html.Text.IndexOf("  <head>", StringComparison.Ordinal)
                if startIdx >= 0 then
                    let endIdx = html.Text.IndexOf("</head>", StringComparison.Ordinal) + "</head>".Length
                    Assert.AreEqual(String.CompareOrdinal(html.Text, endIdx, "\n<body>", 0, "\n<body>".Length), 0)

                    writer.Write(html.Text.Remove(startIdx, endIdx - startIdx + 1))
                else writer.Write(html.Text)
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
    
    [<Test>] member x.``Class 02``() = x.DoNamedTest()
    
    [<Test>] member x.``Top Level Module 01``() = x.DoNamedTest()
    
    [<Test>] member x.``Nested Module 01``() = x.DoNamedTest()