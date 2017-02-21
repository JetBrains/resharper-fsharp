namespace JetBrains.ReSharper.Psi.FSharp.Tree
{
  public sealed class ChildRole
  {
    public const short MODULE_OR_NAMESPACE_DECLARATION = 1;
    public const short MODULE_OR_NAMESPACE_SIGNATURE = 2;
    public const short MODULE_MEMBER = 3;
    public const short FS_IDENTIFIER = 4;
    public const short FS_KEYWORD = 5;
    public const short FS_ACCESS_MODIFIERS = 6;
    public const short FS_ENUM_MEMBER = 7;
    public const short FS_UNION_CASE = 8;

    public const short LAST = 100;
  }
}