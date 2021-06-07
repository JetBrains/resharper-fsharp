namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TupleTypeUsage
  {
    public bool IsStruct => StructKeyword != null;
  }
}
