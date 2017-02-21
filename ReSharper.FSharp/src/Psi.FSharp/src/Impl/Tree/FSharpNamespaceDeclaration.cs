using JetBrains.DocumentModel;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class FSharpNamespaceDeclaration
  {
    public FSharpSymbol Symbol { get; set; }

    public override string DeclaredName => QualifiedName;
    public string QualifiedName => LongIdentifier.QualifiedName;
    public string ShortName => LongIdentifier.ShortName;
    public bool IsModule => false;

    public override TreeTextRange GetNameRange()
    {
      return LongIdentifier.GetNameRange();
    }

    public DocumentRange GetDeclaredNameDocumentRange()
    {
      return LongIdentifier.GetDocumentRange();
    }

    public new INamespace DeclaredElement { get; set; }

    public override void SetName(string name)
    {
      // todo
    }

    public void SetQualifiedName(string qualifiedName)
    {
      // todo
    }
  }
}