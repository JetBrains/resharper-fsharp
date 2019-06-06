namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
{
  public class ParserMessages
  {
    public const string IDS_F_SHARP_DECLARATION = "";
    public const string IDS_MODULE_LIKE_DECLARATION = "";
    public const string IDS_NAMED_MODULE_LIKE_DECLARATION = "";
    public const string IDS_TOP_LEVEL_MODULE_LIKE_DECLARATION = "";
    public const string IDS_MODULE_DECLARATION = "";
    public const string IDS_MODULE_MEMBER = "";
    public const string IDS_MODULE_MEMBER_DECLARATION = "";
    public const string IDS_MODULE_MEMBER_STATEMENT = "";
    public const string IDS_F_SHARP_TYPE_DECLARATION = "";
    public const string IDS_UNION_CASE_DECLARATION = "";
    public const string IDS_CASE_FIELD_DECLARATION = "";
    public const string IDS_F_SHARP_TYPE_MEMBER_DECLARATION = "";
    public const string IDS_OBJECT_MODEL_TYPE_DECLARATION = "";
    public const string IDS_INHERIT_MEMBER = "";
    public const string IDS_MODIFIER = "";
    public const string IDS_NOT_COMPILED_TYPE_DECLARATION = "";
    public const string IDS_SIMPLE_TYPE_DECLARATION = "";
    public const string IDS_HASH_DIRECTIVE = "";
    public const string IDS_CONST_PAT = "";
    public const string IDS_LONG_IDENT_PAT = "";
    public const string IDS_NAMED_PAT = "";
    public const string IDS_ARRAY_OR_LIST_PAT = "";
    public const string IDS_BINDING = "";
    public const string IDS_IDENT_OR_OP_NAME = "";
    public const string IDS_ACTIVE_PATTERN_CASE_DECLARATION = "";
    public const string IDS_ACTIVE_PATTERN_NAMED_CASE_DECLARATION = "";
    public const string IDS_QUOTE_EXPR = "";
    public const string IDS_ARRAY_OR_LIST_OF_SEQ_EXPR = "";
    public const string IDS_SET_EXPR = "";
    public const string IDS_CAST_EXPR = "";
    public const string IDS_SYN_TYPE = "";

    public static string GetString(string id) => id;
    public static string GetExpectedMessage(string s) => string.Empty;
    public static string GetExpectedMessage(string s1, string s2) => string.Empty;
    public static string GetUnexpectedTokenMessage() => string.Empty;
  }
}
