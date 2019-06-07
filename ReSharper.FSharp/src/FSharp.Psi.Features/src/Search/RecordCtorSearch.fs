module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Search.RecordCtorSearch

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util.FSharpPredefinedType
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Finder
open JetBrains.ReSharper.Psi.Files
open JetBrains.ReSharper.Psi.Search
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

let getCompilationMappingAttrInstanceFlag (attrInstance: IAttributeInstance) =
    match Seq.tryHead (attrInstance.PositionParameters()) with
    | None -> SourceConstructFlags.None
    | Some parameter -> parameter.ConstantValue.Value :?> SourceConstructFlags

let getCompilationMappingFlag (attrsOwner: IAttributesOwner) =
    attrsOwner.GetAttributeInstances(compilationMappingAttrTypeName, false)
    |> Seq.tryHead
    |> Option.map getCompilationMappingAttrInstanceFlag
    |> Option.defaultValue SourceConstructFlags.None

let isFSharpField (property: IProperty) =
    getCompilationMappingFlag property = SourceConstructFlags.Field

let isRecord (typeElement: ITypeElement) =
    match typeElement with
    | :? IFSharpTypeElement as fsTypeElement ->
        isNotNull (fsTypeElement.GetPart<IRecordPart>())

    | :? ICompiledElement ->
        getCompilationMappingFlag typeElement = SourceConstructFlags.RecordType

    | _ -> false

let getRecordFieldNames (typeElement: ITypeElement) =
    match typeElement.GetPart<IRecordPart>() with
    | null ->
        typeElement.Properties
        |> Seq.filter isFSharpField
        |> Array.ofSeq
        |> Array.map (fun property -> property.ShortName)

    | recordPart ->
        recordPart.Fields
        |> Array.ofSeq
        |> Array.map (fun field -> field.ShortName)


[<PsiSharedComponent>]
type RecordCtorSearchFactory() =
    inherit DomainSpecificSearcherFactoryBase()

    override x.IsCompatibleWithLanguage(language) =
        language.Is<FSharpLanguage>()

    override x.GetAllPossibleWordsInFile(declaredElement) =
        match declaredElement with
        | :? ITypeElement as typeElement when isRecord typeElement -> getRecordFieldNames typeElement :> _
        | _ -> EmptyList.Instance :> _

    override x.CreateReferenceSearcher(declaredElements, findCandidates) =
        let recordTypeElements =
            declaredElements.FilterByType<ITypeElement>()
            |> Seq.filter isRecord
            |> Array.ofSeq

        if recordTypeElements.IsEmpty() then null else
        RecordCtorReferenceSearcher(recordTypeElements, findCandidates) :> _


and RecordCtorReferenceSearcher(recordTypeElements, findCandidates) =
    member x.ProcessElement<'TResult>(treeNode: ITreeNode, consumer: IFindResultConsumer<'TResult>) =
        let names =
            recordTypeElements
            |> Array.map getRecordFieldNames
            |> Array.concat
            |> Array.distinct

        let elements = DeclaredElementsSet(Seq.cast recordTypeElements)
        let processor = RecordCtorReferenceProcessor(treeNode, findCandidates, consumer, elements, names)
        processor.Run() = FindExecution.Stop
    
    interface IDomainSpecificSearcher with
        member x.ProcessProjectItem(sourceFile, consumer) =
            sourceFile.GetPsiFiles<FSharpLanguage>()
            |> Seq.exists (fun file -> x.ProcessElement(file, consumer))

        member x.ProcessElement(treeNode, consumer) =
            x.ProcessElement(treeNode, consumer)


and RecordCtorReferenceProcessor<'TResult>(treeNode, findCandidates, resultConsumer, elements, referenceNames) =
    inherit ReferenceSearchSourceFileProcessor<'TResult>(treeNode, findCandidates, resultConsumer, elements, referenceNames, referenceNames)

    override x.PreFilterReference(reference) =
        reference :? RecordCtorReference
