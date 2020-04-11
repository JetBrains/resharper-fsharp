module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings.FunctionAnnotation

open JetBrains.Application.DataContext
open JetBrains.ReSharper.Feature.Services.Refactorings
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.DataContext
open JetBrains.ReSharper.Psi.Pointers
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree

//[<Language(typeof<FSharpLanguage>)>]
[<RefactoringWorkflowProvider>]
type AnnotateFunctionWorkflowProvider() =
    interface IRefactoringWorkflowProvider with
        member this.CreateWorkflow context =
            let solution = context.GetData(JetBrains.ProjectModel.DataContext.ProjectModelDataConstants.SOLUTION)
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
        let parameters = method.Parameters |> Seq.rev
        // TODO: To get the method return type, parameter names and return types in F# syntax use .GetMethods in FSharpIdentifierTooltipProvider.fs
        let methodDecl =
            match method with
            | :? IFSharpDeclaredElement as decl -> decl
            | unexpected -> failwithf "unexpected type %A" unexpected
            
        // TODO: inspect this, work out what type and how to use it properly
        let methodNode = method.GetDeclarations() |> Seq.exactlyOne
//        let rec getLastNode (node : ITreeNode) =
//            let nextNode = node.NextSibling
//            if nextNode = null then node else getLastNode nextNode
//        let lastArgNode = getLastNode methodNode
        printfn
            "Method name: %s return: %s" methodDecl.SourceName (method.ReturnType.GetPresentableName(FSharpLanguage.Instance))
            
        use _writeLock = WriteLockCookie.Create(methodNode.IsPhysical())
        // Delete all argument parameters on the method
        JetBrains.ReSharper.Psi.ExtensionsAPI.Tree.ModificationUtil.DeleteChildRange(methodNode.FirstChild.NextSibling, methodNode.LastChild)
        
        // Recreate all type parameters on the method
        parameters
        |> Seq.iter (fun prm ->
            printfn "Parameter Name: %s, Type: %s" prm.ShortName (prm.Type.ToString())
            let annotatedTypeElements = [
                Parsing.FSharpTokenType.LPAREN.Create("(")
                Parsing.FSharpTokenType.IDENTIFIER.Create(prm.ShortName)
                Parsing.FSharpTokenType.COLON.Create(":")
                Parsing.FSharpTokenType.IDENTIFIER.Create(prm.Type.ToString())
                Parsing.FSharpTokenType.RPAREN.Create(")")
            ]
            let mutable anchor = methodNode :> ITreeNode
            for elem in annotatedTypeElements do
                anchor <- JetBrains.ReSharper.Psi.ExtensionsAPI.Tree.ModificationUtil.AddChildAfter(anchor, elem)
            )
        // TODO: Add test that fails as first stage
        // TODO: Handle return type annotations
        // Call Children on methodNode ignore sibling nodes that are NodeType of "WHITE_SPACE", but care about types
        // which are NodeType LOCAL_REFERENCE_PAT, also has IsDeclaration as true
        
        // TODO: Work out how to replace text or symbols
        // Use FSHarpUtil.AddTokenBefore(, ) or maybe ModificationUtil.AddChildBefore, pass it the ITreeNode for each Parameter
        // Currently assuming it's possible to get an ITreeNode from each param
        
        // TODO: maybe use these when tidying up
//        let elementFactory = fsFile.CreateElementFactory()
//            elementFactory.CreateParenExpr
        
        true
    