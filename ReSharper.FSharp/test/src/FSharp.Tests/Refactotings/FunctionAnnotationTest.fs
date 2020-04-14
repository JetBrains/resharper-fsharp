namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Refactorings

open JetBrains.ReSharper.Plugins.FSharp.Tests.Common
open JetBrains.ReSharper.FeaturesTestFramework.Refactorings
open JetBrains.ReSharper.Feature.Services.Refactorings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings.FunctionAnnotation
open NUnit.Framework

[<FSharpTest>]
type FunctionAnnotationTest() =
    inherit DrivenTestBase()
    
    override x.RelativeTestDataPath = "features/refactorings/functionAnnotation"
    
    [<Test>] member x.``Let - No existing annotations``() = x.DoNamedTest()
    
    override x.CreateRefactoringWorkflow(textControl, context) =
        let workflow =
            RefactoringsManager.Instance.GetWorkflowProviders<AnnotateFunctionWorkflowProvider>()
            |> Seq.collect(fun wofkflowProvider ->
                (wofkflowProvider :> IRefactoringWorkflowProvider).CreateWorkflow context)
            |> Seq.head
        
        workflow

