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
    [SettingsEntry(false, "Reorder open declarations")]
    public bool ReorderOpenDeclarations;

    [SettingsEntry(false, "Semicolon at end of line")]
    public bool SemicolonAtEndOfLine;

    [SettingsEntry(true, "Space before argument")]
    public bool SpaceBeforeArgument;

    [SettingsEntry(false, "Space before colon")]
    public bool SpaceBeforeColon;

    [SettingsEntry(true, "Space after comma")]
    public bool SpaceAfterComma;

    [SettingsEntry(true, "Space after semicolon")]
    public bool SpaceAfterSemicolon;

    [SettingsEntry(false, "Indent on try with")]
    public bool IndentOnTryWith;

    [SettingsEntry(true, "Space around delimiter")]
    public bool SpaceAroundDelimiter;

    [SettingsEntry(true, "Keep newline after")]
    public bool PreserveEndOfLine;

    [SettingsEntry(true, "Don't indent comments started at first column")]
    public bool StickComment;

    [SettingsEntry(true, "Outdent binary operators")]
    public bool OutdentBinaryOperators;

    [SettingsEntry(true, "Never outdent pipe operators")]
    public bool NeverOutdentPipeOperators;
  }
}
