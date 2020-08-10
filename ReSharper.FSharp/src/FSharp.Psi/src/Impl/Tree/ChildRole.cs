namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  public static class ChildRole
  {
    public const short NONE = 0;

    public const short FSHARP_COMMA = 1;
    public const short FSHARP_AND = 2;
    public const short FSHARP_SEMI = 3;
    public const short FSHARP_EQUALS = 4;
    public const short FSHARP_DELIMITER = 5;
    public const short FSHARP_COLON = 6;

    public const short LAST = 100;
  }
}
