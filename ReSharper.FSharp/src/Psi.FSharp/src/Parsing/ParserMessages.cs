namespace JetBrains.ReSharper.Psi.FSharp.Parsing
{
  public class ParserMessages
  {
    public const string IDS_F_SHARP_FILE = "";
    public const string IDS_F_SHARP_DECLARATION = "";
    public const string IDS_MODULE_OR_NAMESPACE_DECLARATION = "";

    public static string GetString(string id)
    {
      return id;
    }

    public static string GetExpectedMessage(string expectedSymbol)
    {
      return string.Empty;
    }

    public static string GetExpectedMessage(string firstExpectedSymbol, string secondExpectedSymbol)
    {
      return string.Empty;
    }
  }
}