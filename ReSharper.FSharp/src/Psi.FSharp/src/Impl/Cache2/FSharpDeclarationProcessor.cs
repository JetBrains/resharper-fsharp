using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  public class FSharpDeclarationProcessor : TreeNodeVisitor
  {
    private readonly ICacheBuilder myBuilder;

    public FSharpDeclarationProcessor(ICacheBuilder builder)
    {
      myBuilder = builder;
    }

    public override void VisitFSharpImplFile(IFSharpImplFile implFile)
    {
      myBuilder.CreateProjectFilePart(new FSharpProjectFilePart(implFile.GetSourceFile()));

      foreach (var declaration in implFile.DeclarationsEnumerable)
      {
        var qualifiers = declaration.LongIdentifier.Qualifiers;
        foreach (var qualifier in qualifiers)
          myBuilder.StartPart(new QualifiedNamespacePart(qualifier.GetTreeStartOffset(), qualifier.GetText()));

        declaration.Accept(this);

        foreach (var _ in qualifiers)
          myBuilder.EndPart();
      }
    }

    public override void VisitModuleDeclaration(IModuleDeclaration decl)
    {
      ProcessModuleLikeDeclaraion(decl, new ModulePart(decl));
    }

    public override void VisitFSharpNamespaceDeclaration(IFSharpNamespaceDeclaration decl)
    {
      ProcessModuleLikeDeclaraion(decl, new DeclaredNamespacePart(decl));
    }

    private void ProcessModuleLikeDeclaraion(IModuleOrNamespaceDeclaration decl, Part part)
    {
      myBuilder.StartPart(part);
      myBuilder.EndPart();
    }
  }
}