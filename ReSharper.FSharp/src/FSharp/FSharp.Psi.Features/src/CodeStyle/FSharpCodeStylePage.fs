namespace JetBrains.ReSharper.Plugins.FSharp.Services.Formatter

open System
open System.Linq.Expressions
open JetBrains.Application.Components
open JetBrains.Application.UI.Options
open JetBrains.ReSharper.Feature.Services.OptionPages.CodeStyle
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi.EditorConfig
open JetBrains.ReSharper.Resources.Resources.Icons

[<CodePreviewPreparatorComponent>]
type FSharpCodePreviewPreparator() =
    inherit CodePreviewPreparator()

    override x.Language = FSharpLanguage.Instance :> _
    override x.ProjectFileType = FSharpProjectFileType.Instance :> _
    override x.Parse(parser,_) = parser.ParseFile() :> _


[<FormattingSettingsPresentationComponent>]
type FSharpCodeStylePageSchema(lifetime, smartContext, itemViewModelFactory, container, settingsToHide, calculatedSettingsSchema) =
    inherit CodeStylePageSchema<FSharpFormatSettingsKey, FSharpCodePreviewPreparator>(lifetime, smartContext,
        itemViewModelFactory, container, settingsToHide, calculatedSettingsSchema)

    override x.Language = FSharpLanguage.Instance :> _
    override x.PageName = "Formatting Style"

    member x.GetItem(getter: Expression<Func<FSharpFormatSettingsKey,_>>) =
        base.ItemFor(getter)

    override x.Describe() =
        [| x.GetItem(_.WRAP_LIMIT)
           x.GetItem(_.INDENT_SIZE)
           x.GetItem(_.SemicolonAtEndOfLine)
           x.GetItem(_.SpaceBeforeParameter)
           x.GetItem(_.SpaceBeforeLowercaseInvocation)
           x.GetItem(_.SpaceBeforeUppercaseInvocation)
           x.GetItem(_.SpaceBeforeClassConstructor)
           x.GetItem(_.SpaceBeforeMember)
           x.GetItem(_.SpaceBeforeColon)
           x.GetItem(_.SpaceAfterComma)
           x.GetItem(_.SpaceBeforeSemicolon)
           x.GetItem(_.SpaceAfterSemicolon)
           x.GetItem(_.IndentOnTryWith)
           x.GetItem(_.SpaceAroundDelimiter)
           x.GetItem(_.MaxIfThenElseShortWidth)
           x.GetItem(_.MaxInfixOperatorExpression)
           x.GetItem(_.MaxRecordWidth)
           x.GetItem(_.MaxArrayOrListWidth)
           x.GetItem(_.MaxValueBindingWidth)
           x.GetItem(_.MaxFunctionBindingWidth)
           x.GetItem(_.MultilineBlockBracketsOnSameColumn)
           x.GetItem(_.NewlineBetweenTypeDefinitionAndMembers)
           x.GetItem(_.KeepIfThenInSameLine)
           x.GetItem(_.MaxElmishWidth)
           x.GetItem(_.SingleArgumentWebMode)
           x.GetItem(_.AlignFunctionSignatureToIndentation)
           x.GetItem(_.AlternativeLongMemberDefinitions)
           x.GetItem(_.PlaceCommentsAtFirstColumn)
           x.GetItem(_.StickComment)
           x.GetItem(_.OutdentBinaryOperators)
           x.GetItem(_.NeverOutdentPipeOperators) |] :> _


[<OptionsPage("FSharpCodeStylePage", "Formatting Style", typeof<PsiFeaturesUnsortedOptionsThemedIcons.Indent>,
              FilterTags = [|ConfigFileUtils.EditorConfigName|])>]
type FSharpCodeStylePage(lifetime, smartContext: OptionsSettingsSmartContext, env,
                         schema: FSharpCodeStylePageSchema, preview, componentContainer: IComponentContainer) =
    inherit CodeStylePage(lifetime, smartContext, env, schema, preview, componentContainer)
    let _ = PsiFeaturesUnsortedOptionsThemedIcons.Indent // workaround to create assembly reference (dotnet/fsharp#3522)

    override x.Id = "FSharpIndentStylePage"
