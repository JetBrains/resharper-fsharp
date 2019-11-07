namespace JetBrains.ReSharper.Plugins.FSharp.Services.Formatter

open System
open System.Linq.Expressions
open JetBrains.Application.Components
open JetBrains.Application.Settings
open JetBrains.Application.UI.Options
open JetBrains.ReSharper.Feature.Services.OptionPages.CodeStyle
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.CodeStyle
open JetBrains.ReSharper.Psi.EditorConfig
open JetBrains.ReSharper.Psi.Impl.CodeStyle
open JetBrains.ReSharper.Psi.Format
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Resources.Resources.Icons

[<SettingsKey(typeof<CodeFormattingSettingsKey>, "Code formatting in F#"); EditorConfigKey("fsharp")>]
type FSharpFormatSettingsKey() =
    inherit FormatSettingsKeyBase()

    [<SettingsEntry(false, "Reorder open declarations"); DefaultValue>]
    val mutable ReorderOpenDeclarations: bool
    
    [<SettingsEntry(false, "Semicolon at end of line"); DefaultValue>]
    val mutable SemicolonAtEndOfLine: bool
    
    [<SettingsEntry(true, "Space before argument"); DefaultValue>]
    val mutable SpaceBeforeArgument: bool
    
    [<SettingsEntry(false, "Space before colon"); DefaultValue>]
    val mutable SpaceBeforeColon: bool
    
    [<SettingsEntry(true, "Space after comma"); DefaultValue>]
    val mutable SpaceAfterComma: bool
    
    [<SettingsEntry(true, "Space after semicolon"); DefaultValue>]
    val mutable SpaceAfterSemicolon: bool
    
    [<SettingsEntry(false, "Indent on try with"); DefaultValue>]
    val mutable IndentOnTryWith: bool
    
    [<SettingsEntry(true, "Space around delimiter"); DefaultValue>]
    val mutable SpaceAroundDelimiter: bool

    [<SettingsEntry(true, "Preserve end of line"); DefaultValue>]
    val mutable PreserveEndOfLine: bool

    [<SettingsEntry(true, "Don't indent comments started at first column"); DefaultValue>]
    val mutable StickComment: bool


[<Language(typeof<FSharpLanguage>)>]
type FSharpDummyCodeFormatter(fsLanguage: FSharpLanguage, formatterRequirements) =
    inherit CodeFormatterBase<FSharpFormatSettingsKey>(fsLanguage, formatterRequirements)

    override x.Format(first,last,_,_) = TreeRange(first, last) :> _
    override x.FormatFile(_,_,_) = ()
    override x.CanModifyInsideNodeRange(_,_,_) = false
    override x.CanModifyNode(_,_) = false

    override x.GetMinimalSeparator(_,_) = InvalidOperationException() |> raise
    override x.GetMinimalSeparatorByNodeTypes(_,_) = InvalidOperationException() |> raise
    override x.CreateNewLine(_,_) = InvalidOperationException() |> raise
    override x.CreateSpace(_,_) = InvalidOperationException() |> raise
    override x.FormatInsertedNodes(_,_,_) = ()
    override x.FormatInsertedRange(_,_,_) = InvalidOperationException() |> raise 
    override x.FormatReplacedRange(_,_,_) = ()
    override x.FormatDeletedNodes(_,_,_) = ()
    override x.FormatReplacedNode(_,_) = ()
    
    override x.CreateFormatterContext(profile, firstNode, lastNode, parameters, _) =
        let logger = formatterRequirements.FormatterLoggerProvider.FormatterLogger
        CodeFormattingContext(x, firstNode, lastNode, profile, logger, parameters)


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
           x.GetItem(fun key -> key.SpaceBeforeArgument)
           x.GetItem(fun key -> key.SpaceBeforeColon)
           x.GetItem(fun key -> key.SpaceAfterComma)
           x.GetItem(fun key -> key.SpaceAfterSemicolon)
           x.GetItem(fun key -> key.IndentOnTryWith)
           x.GetItem(fun key -> key.SpaceAroundDelimiter)
           x.GetItem(fun key -> key.ReorderOpenDeclarations)
           x.GetItem(fun key -> key.PreserveEndOfLine)
           x.GetItem(fun key -> key.StickComment) |] :> _


[<OptionsPage("FSharpCodeStylePage", "Formatting Style", typeof<PsiFeaturesUnsortedOptionsThemedIcons.Indent>)>]
type FSharpCodeStylePage(lifetime, smartContext: OptionsSettingsSmartContext, env,
                         schema: FSharpCodeStylePageSchema, preview, componentContainer: IComponentContainer) =
    inherit CodeStylePage(lifetime, smartContext, env, schema, preview, componentContainer)
    let _ = PsiFeaturesUnsortedOptionsThemedIcons.Indent // workaround to create assembly reference (dotnet/fsharp#3522)

    override x.Id = "FSharpIndentStylePage"