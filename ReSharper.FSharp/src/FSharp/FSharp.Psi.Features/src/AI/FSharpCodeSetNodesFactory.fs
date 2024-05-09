module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.AI.FSharpCodeSetNodesFactory

open System.Text
open JetBrains.Application
open JetBrains.Application.Parts;
open JetBrains.ReSharper.Feature.Services.AI.CodeSetsProviding
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Feature.Services.AI.CSharp.CodeSetsProviding


type FSharpGraphNode(graph:CodeSetsGraph, treeNode:ITreeNode) = 
    inherit CodeSetNode(graph, treeNode)
    let myFsharpNode = treeNode
    override this.Print(printer:StringBuilder, printingInfo:CodeSetsGraphPrintingInfo) = 
        myFsharpNode.Print(printer, graph, this, printingInfo)
    override this.GetCost() =
        myFsharpNode.GetCost(this.Graph.CodeSetNodesFactory, this.Graph.TokenCounter)

[<ShellComponent(Instantiation.DemandAnyThreadSafe)>]
type FSharpCodeSetNodesFactory() =
    interface ICodeSetNodesFactory with
        member _.CreateGraphNode(treeNode: ITreeNode, graph: CodeSetsGraph) = 
            FSharpGraphNode(graph, treeNode) :> CodeSetNode

        member _.CanCreateGraphNode(treeNode: ITreeNode) =
            match treeNode with
            | :? IFSharpDeclaration -> true
            | _ -> false
