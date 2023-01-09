namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion

open System.Text.RegularExpressions
open System.Threading
open System.Threading.Tasks
open JetBrains.Application.Environment
open JetBrains.Application.Environment.Helpers
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ProjectModel.NuGet.Completion
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Impl
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl
open JetBrains.ReSharper.Feature.Services.Lookup
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.FSharpCompletionUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util


type FSharpScriptReferenceCompletionContext(context, token) =
    inherit SpecificCodeCompletionContext(context)

    override x.ContextId = "FSharpReferenceDirectiveCompletionContext"
    member x.Token = token


[<IntellisensePart>]
type FSharpScriptReferenceCompletionContextProvider() =
    inherit CodeCompletionContextProviderBase()

    let stringTypes =
        NodeTypeSet(
            FSharpTokenType.STRING,
            FSharpTokenType.VERBATIM_STRING,
            FSharpTokenType.TRIPLE_QUOTED_STRING)

    override this.IsApplicable(context) =
        let fsFile = context.File.As<IFSharpFile>()
        isNotNull fsFile &&

        let caretOffset = context.CaretTreeOffset
        let token = fsFile.FindTokenAt(caretOffset - 1)
        isNotNull token && token.Parent :? IReferenceDirective &&

        let tokenType = token.GetTokenType()
        stringTypes[tokenType] &&

        // todo: better check
        caretOffset.Offset > token.GetTreeStartOffset().Offset && caretOffset.Offset < token.GetTreeEndOffset().Offset

    override this.GetCompletionContext(context) =
        let fsFile = context.File :?> IFSharpFile
        let token = fsFile.FindTokenAt(context.CaretTreeOffset - 1)
        FSharpScriptReferenceCompletionContext(context, token)


type ReferenceLookupItem(text, displayName, reschedule) =
    inherit TextLookupItem(text)

    override this.GetDisplayName() =
        displayName

    override this.Accept(textControl, nameRange, insertType, suffix, solution, keepCaretStill) =
        base.Accept(textControl, nameRange, insertType, suffix, solution, keepCaretStill)
        if reschedule then
            textControl.RescheduleCompletion(solution)


[<Language(typeof<FSharpLanguage>)>]
type FSharpScriptNugetReferencesCompletionProvider() =
    inherit ItemsProviderOfSpecificContext<FSharpScriptReferenceCompletionContext>()

    let dependencyProviderRegex = Regex(@"^(?<provider>[a-zA-Z]+)$")
    let packageReferenceRegex = Regex(@"^nuget:\s*(?<package>[a-zA-Z_0-9\-\.]*)\s*(,\s*(?<version>[a-zA-Z_0-9\-\.]*))?")

    override this.IsAvailable _ = true
    override this.GetLookupFocusBehaviour _ = LookupFocusBehaviour.Soft

    override this.AddLookupItems(context, collector) =
        let productConfigurations = Shell.Instance.GetComponent<RunsProducts.ProductConfigurations>()
        if not (productConfigurations.IsInternalMode()) then false else

        let document = context.BasicContext.Document

        // todo: different string types
        let argValueStartOffset = context.Token.GetTreeStartOffset().Offset + 1
        let argValueEndOffset = context.Token.GetTreeEndOffset().Offset - 1

        let caretOffset = context.BasicContext.CaretTreeOffset.Offset
        let caretOffsetInArg = caretOffset - argValueStartOffset

        let addNugetItem () =
            let nugetItem = ReferenceLookupItem("nuget: ", "nuget", true)
            let insertRange = DocumentRange(document, TextRange(argValueStartOffset, caretOffset))
            let replaceRange = DocumentRange(document, TextRange(argValueStartOffset, argValueEndOffset))
            let ranges = TextLookupRanges(insertRange, replaceRange)
            nugetItem.InitializeRanges(ranges, context.BasicContext)
            collector.Add(nugetItem)
            false

        let argValue = context.Token.GetText().TrimFromStart("\"").TrimFromEnd("\"")
        let reparsedArgValue = argValue.Insert(caretOffsetInArg, DummyIdentifier)

        if dependencyProviderRegex.IsMatch(reparsedArgValue) then
            addNugetItem () else

        let packageRefMatch = packageReferenceRegex.Match(reparsedArgValue)
        if not packageRefMatch.Success then false else

        let getGroup (name: string) =
            let group = packageRefMatch.Groups[name]
            group, TextRange(group.Index, group.Index + group.Length)

        let packageGroup, packageRange = getGroup "package"
        let _, versionRange = getGroup "version"

        let getItems (range: TextRange) (getInfo: NuGetCompletionService -> _ -> _ -> Task<_>) unwrapItem =
            if not (range.Contains(caretOffsetInArg)) then false else

            let service = context.BasicContext.Solution.GetComponent<NuGetCompletionService>()
            let arg = reparsedArgValue.Substring(range.StartOffset, caretOffsetInArg - range.StartOffset)

            use cts = new CancellationTokenSource()
            let task = getInfo service arg cts.Token
            task.Wait()
            let packages = task.Result

            packages
            |> Seq.map (unwrapItem >> TextLookupItem)
            |> Seq.iter (fun item ->
                let startOffset = argValueStartOffset + range.StartOffset
                let endOffset = argValueStartOffset + range.EndOffset - DummyIdentifier.Length

                let insertRange = DocumentRange(document, TextRange(startOffset, caretOffset))
                let replaceRange = DocumentRange(document, TextRange(startOffset, endOffset))
                let ranges = TextLookupRanges(insertRange, replaceRange)

                item.InitializeRanges(ranges, context.BasicContext)
                collector.Add(item)
            )

            true

        if getItems packageRange (fun service arg t -> service.CompleteIdAsync(arg, t)) id then false else

        getItems versionRange (fun service arg t -> service.CompleteVersionAsync(packageGroup.Value, arg, t))
              (fun nugetVersion -> nugetVersion.ToNormalizedString()) |> ignore

        false
