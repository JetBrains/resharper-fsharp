namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.QuickDoc

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

type TemporaryQuickDocTestBase() =
    inherit BaseTestWithTextControl()

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
                let endIdx = html.Text.IndexOf("</head>", StringComparison.Ordinal) + "</head>".Length
                Assert.AreEqual(String.CompareOrdinal(html.Text, endIdx, "\n<body>", 0, "\n<body>".Length), 0)

                writer.Write(html.Text.Remove(startIdx, endIdx - startIdx + 1))
            )
        )
