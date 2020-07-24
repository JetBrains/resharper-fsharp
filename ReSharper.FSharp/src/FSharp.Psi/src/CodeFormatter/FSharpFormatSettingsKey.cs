using JetBrains.Application.Settings;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.EditorConfig;
using JetBrains.ReSharper.Psi.Format;

namespace JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
{
  [SettingsKey(typeof(CodeFormattingSettingsKey), "Code formatting in F#")]
  [EditorConfigKey("fsharp")]
  public class FSharpFormatSettingsKey : FormatSettingsKeyBase
  {
    [SettingsEntry(false, "Semicolon at end of line")]
    public bool SemicolonAtEndOfLine;

    [SettingsEntry(true, "Space before parameter")]
    public bool SpaceBeforeParameter;

    [SettingsEntry(true, "Space before lowercase function invocation")]
    public bool SpaceBeforeLowercaseInvocation;

    [SettingsEntry(false, "Space before uppercase function invocation")]
    public bool SpaceBeforeUppercaseInvocation;

    [SettingsEntry(false, "Space before class constructor")]
    public bool SpaceBeforeClassConstructor;

    [SettingsEntry(false, "Space before member")]
    public bool SpaceBeforeMember;

    [SettingsEntry(false, "Space before colon")]
    public bool SpaceBeforeColon;

    [SettingsEntry(true, "Space after comma")]
    public bool SpaceAfterComma;

    [SettingsEntry(false, "Space before semicolon")]
    public bool SpaceBeforeSemicolon;

    [SettingsEntry(true, "Space after semicolon")]
    public bool SpaceAfterSemicolon;

    [SettingsEntry(false, "Indent on try with")]
    public bool IndentOnTryWith;

    [SettingsEntry(true, "Space around delimiter")]
    public bool SpaceAroundDelimiter;

    [SettingsEntry(40, "Maximum width of longer (non multiline) if/then/else expressions")]
    public int MaxIfThenElseShortWidth;

    [SettingsEntry(50, "Maximum width of single line infix expressions")]
    public int MaxInfixOperatorExpression;

    [SettingsEntry(40, "Maximum width of single line record expressions")]
    public int MaxRecordWidth;

    [SettingsEntry(40, "Maximum width of single line array expressions")]
    public int MaxArrayOrListWidth;

    [SettingsEntry(40, "Maximum width of value binding expressions")]
    public int MaxValueBindingWidth;

    [SettingsEntry(40, "Maximum width of function binding expressions")]
    public int MaxFunctionBindingWidth;

    [SettingsEntry(false, "Align opening and closing braces of record, array and list expressions")]
    public bool MultilineBlockBracketsOnSameColumn;

    [SettingsEntry(false, "Newline between type definition and members")]
    public bool NewlineBetweenTypeDefinitionAndMembers;

    [SettingsEntry(false, "Keep if and then keywords in same line")]
    public bool KeepIfThenInSameLine;

    [SettingsEntry(40, "Maximum width of Elmish inspired expressions")]
    public int MaxElmishWidth;

    [SettingsEntry(false, "Single argument Elmish expressions")]
    public bool SingleArgumentWebMode;

    [SettingsEntry(false, "Align function signature to indentation")]
    public bool AlignFunctionSignatureToIndentation;

    [SettingsEntry(false, "Alternative long member definitions")]
    public bool AlternativeLongMemberDefinitions;

    [SettingsEntry(true, "Don't indent comments started at first column")]
    public bool StickComment;

    [SettingsEntry(true, "Outdent binary operators")]
    public bool OutdentBinaryOperators;

    [SettingsEntry(true, "Never outdent pipe operators")]
    public bool NeverOutdentPipeOperators;
  }
}
