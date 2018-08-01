namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
{
  public class ParserMessages
  {
    public const string IDS_MODULE_MEMBER = "";
    public const string IDS_MODULE_MEMBER_DECLARATION = "";
    public const string IDS_MODULE_MEMBER_STATEMENT = "";
    public const string IDS_F_SHARP_TYPE_DECLARATION = "";
    public const string IDS_UNION_CASE_DECLARATION = "";
    public const string IDS_F_SHARP_TYPE_MEMBER_DECLARATION = "";
    public const string IDS_OBJECT_MODEL_TYPE_DECLARATION = "";
    public const string IDS_MODIFIER = "";
    public const string IDS_NOT_COMPILED_TYPE_DECLARATION = "";
    public const string IDS_SIMPLE_TYPE_DECLARATION = "";
    public const string IDS_TYPE_EXPRESSION = "";

    public static string GetString(string id) => id;
    public static string GetExpectedMessage(string expectedSymbol) => string.Empty;
  }
}