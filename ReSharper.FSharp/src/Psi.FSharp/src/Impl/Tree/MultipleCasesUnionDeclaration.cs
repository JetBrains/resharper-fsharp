namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class MultipleCasesUnionDeclaration
  {
    public override string DeclaredName => FSharpImplUtil.GetCompiledName(Identifier, Attributes);
    public override string SourceName => FSharpImplUtil.GetSourceName(Identifier);

    public override TreeTextRange GetNameRange()
    {
      return Identifier.GetNameRange();
    }
  }
}