namespace JetBrains.ReSharper.Plugins.FSharp.Services.Formatter

open System
open System.Linq.Expressions
open JetBrains.Application.Components
open JetBrains.Application.UI.Options
open JetBrains.ReSharper.Feature.Services.OptionPages.CodeStyle
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Resources.Resources.Icons

[<CodePreviewPreparatorComponent>]
type FSharpCodePreviewPreparator() =
    inherit CodePreviewPreparator()

    override x.Language = FSharpLanguage.Instance :> _
    override x.ProjectFileType = FSharpProjectFileType.Instance :> _
    override x.Parse(parser,_) = parser.ParseFile() :> _


[<FormattingSettingsPresentationComponent>]
type FSharpCodeStylePageSchema(lifetime, smartContext, itemViewModelFactory, container, settingsToHide) =
    inherit CodeStylePageSchema<FSharpFormatSettingsKey, FSharpCodePreviewPreparator>(lifetime, smartContext,
        itemViewModelFactory, container, settingsToHide)

    override x.Language = FSharpLanguage.Instance :> _
    override x.PageName = "Formatting Style"

    member x.GetItem(getter: Expression<Func<FSharpFormatSettingsKey,_>>) =
        base.ItemFor(getter)

    override x.Describe() =
        [| x.GetItem(fun key -> key.WRAP_LIMIT)
           x.GetItem(fun key -> key.INDENT_SIZE)
           x.GetItem(fun key -> key.SemicolonAtEndOfLine)
           x.GetItem(fun key -> key.SpaceBeforeParameter)
           x.GetItem(fun key -> key.SpaceBeforeLowercaseInvocation)
           x.GetItem(fun key -> key.SpaceBeforeUppercaseInvocation)
           x.GetItem(fun key -> key.SpaceBeforeClassConstructor)
           x.GetItem(fun key -> key.SpaceBeforeMember)
           x.GetItem(fun key -> key.SpaceBeforeColon)
           x.GetItem(fun key -> key.SpaceAfterComma)
           x.GetItem(fun key -> key.SpaceBeforeSemicolon)
           x.GetItem(fun key -> key.SpaceAfterSemicolon)
           x.GetItem(fun key -> key.IndentOnTryWith)
           x.GetItem(fun key -> key.SpaceAroundDelimiter)
           x.GetItem(fun key -> key.MaxIfThenElseShortWidth)
           x.GetItem(fun key -> key.MaxInfixOperatorExpression)
           x.GetItem(fun key -> key.MaxRecordWidth)
           x.GetItem(fun key -> key.MaxArrayOrListWidth)
           x.GetItem(fun key -> key.MaxValueBindingWidth)
           x.GetItem(fun key -> key.MaxFunctionBindingWidth)
           x.GetItem(fun key -> key.MultilineBlockBracketsOnSameColumn)
           x.GetItem(fun key -> key.NewlineBetweenTypeDefinitionAndMembers)
           x.GetItem(fun key -> key.KeepIfThenInSameLine)
           x.GetItem(fun key -> key.MaxElmishWidth)
           x.GetItem(fun key -> key.SingleArgumentWebMode)
           x.GetItem(fun key -> key.AlignFunctionSignatureToIndentation)
           x.GetItem(fun key -> key.AlternativeLongMemberDefinitions)
           x.GetItem(fun key -> key.PlaceCommentsAtFirstColumn)
           x.GetItem(fun key -> key.StickComment)
           x.GetItem(fun key -> key.OutdentBinaryOperators)
           x.GetItem(fun key -> key.NeverOutdentPipeOperators) |] :> _


[<OptionsPage("FSharpCodeStylePage", "Formatting Style", typeof<PsiFeaturesUnsortedOptionsThemedIcons.Indent>)>]
type FSharpCodeStylePage(lifetime, smartContext: OptionsSettingsSmartContext, env,
                         schema: FSharpCodeStylePageSchema, preview, componentContainer: IComponentContainer) =
    inherit CodeStylePage(lifetime, smartContext, env, schema, preview, componentContainer)
    let _ = PsiFeaturesUnsortedOptionsThemedIcons.Indent // workaround to create assembly reference (dotnet/fsharp#3522)

    override x.Id = "FSharpIndentStylePage"
