module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings.FunctionAnnotation

open JetBrains.Application.DataContext
open JetBrains.ReSharper.Feature.Services.Refactorings
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.DataContext
open JetBrains.ReSharper.Psi.Pointers
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ProjectModel.DataContext
open JetBrains.ReSharper.Feature.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open FSharp.Compiler.SourceCodeServices

[<RefactoringWorkflowProvider>]
type AnnotateFunctionWorkflowProvider() =
    interface IRefactoringWorkflowProvider with
        member this.CreateWorkflow context =
            let solution = context.GetData(ProjectModelDataConstants.SOLUTION)
            AnnotateFunctionWorkflow(solution, "Annotate function") :> IRefactoringWorkflow |> Seq.singleton

and AnnotateFunctionWorkflow(solution, actionId) =
    inherit DrivenRefactoringWorkflow(solution, actionId)
    
    let mutable (MethodPointer : IDeclaredElementPointer<IMethod>) = null
    
    member this.GetMethodPointer = MethodPointer
    
    override this.CreateRefactoring (driver : IRefactoringDriver) =
        AnnotateFunctionRefactoring(this, solution, driver) :> IRefactoringExecuter
    override this.get_HelpKeyword () = "Add type annotations to function"
    /// Don't prompt with any UI before refactoring
    override this.get_FirstPendingRefactoringPage () = null
    /// Should "enable undo" be shown in the UI
    override this.get_MightModifyManyDocuments () = true
    member private this.GetMethod (context : IDataContext) =
        context.GetData(PsiDataConstants.DECLARED_ELEMENTS)
            |> Option.ofObj
            |> Option.bind(Seq.tryFind(function | :? IMethod -> true | _ -> false))
            |> Option.bind(function | :? IMethod as method -> Some method | _ -> None)
        // TODO MC: TO get types, do method.Parameters() to get params, use .FSharpSymbol.FullName on it for name and .Type for type properties!
    override this.Initialize (context : IDataContext) =
        match this.GetMethod(context) with
        | Some method ->
            MethodPointer <- method.CreateElementPointer()
            true
        | None ->
            false
    override this.IsAvailable (context : IDataContext) =
        // TODO: Must also be the let binding defining the method
        this.GetMethod context |> Option.isSome
    
    override this.get_Title () = "Annotate function types"
    override this.get_ActionGroup () = RefactoringActionGroup.Convert
    
and AnnotateFunctionRefactoring(workflow, solution, driver) =
    inherit DrivenRefactoringBase<AnnotateFunctionWorkflow>(workflow, solution, driver)
    
    override this.Execute progressIndicator =
        if workflow.GetMethodPointer = null then false else
        let method = workflow.GetMethodPointer.FindDeclaredElement() // TODO: is is an IFSharpIdentifier?
        let declaration = MultyPsiDeclarations(method).AllDeclarations |> Seq.exactlyOne
        let fsharpDeclaration = declaration.As<IFSharpDeclaration>()
        let methodSymbol = fsharpDeclaration.GetFSharpSymbol()
        let fSharpFunction =
            match methodSymbol with
            | :? FSharpMemberOrFunctionOrValue as x -> x
                // fSharpFunction.CurriedParameterGroups for parameters then .Type.TypeDefinition for type of the param
                // fSharpFunction.ReturnParameter.Type.TypeDefinition for type of return
            | _ -> failwith "Expected function here"
        let parameters = fSharpFunction.CurriedParameterGroups
        let methodNode = method.GetDeclarations() |> Seq.exactlyOne
        printfn
            "Method name: %s return: %s" fSharpFunction.DisplayName fSharpFunction.ReturnParameter.Type.TypeDefinition.DisplayName
            
        use _writeLock = WriteLockCookie.Create(methodNode.IsPhysical())
        // Delete all argument parameters on the method
        JetBrains.ReSharper.Psi.ExtensionsAPI.Tree.ModificationUtil.DeleteChildRange(methodNode.FirstChild.NextSibling, methodNode.LastChild)
        
        // Recreate all type parameters on the method
        let mutable anchor = methodNode :> ITreeNode
        parameters
        |> Seq.iter (fun prm ->
            let parameter = prm |> Seq.exactlyOne
            let name = (parameter.Name |> Option.get)
            let typeName = (parameter.Type.TypeDefinition.DisplayName)
            printfn "Parameter Name: %s, Type: %s" name typeName
            let annotatedTypeElements = [
                Parsing.FSharpTokenType.LPAREN.Create("(")
                Parsing.FSharpTokenType.IDENTIFIER.Create(name)
                Parsing.FSharpTokenType.COLON.Create(":")
                Parsing.FSharpTokenType.IDENTIFIER.Create(typeName)
                Parsing.FSharpTokenType.RPAREN.Create(")")
            ]
            
            for elem in annotatedTypeElements do
                anchor <- JetBrains.ReSharper.Psi.ExtensionsAPI.Tree.ModificationUtil.AddChildAfter(anchor, elem)
            )
        
        let returnTypeElements = [
                Parsing.FSharpTokenType.COLON.Create(":")
                Parsing.FSharpTokenType.IDENTIFIER.Create(fSharpFunction.ReturnParameter.Type.TypeDefinition.DisplayName)
            ]
        for elem in returnTypeElements do
            anchor <- JetBrains.ReSharper.Psi.ExtensionsAPI.Tree.ModificationUtil.AddChildAfter(anchor, elem)
        // TODO: maybe use these when tidying up
//        let elementFactory = fsFile.CreateElementFactory()
//            elementFactory.CreateParenExpr
        
        true
    