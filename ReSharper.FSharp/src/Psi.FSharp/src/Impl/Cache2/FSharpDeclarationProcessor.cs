using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  public class FSharpDeclarationProcessor : TreeNodeVisitor
  {
    private readonly ICacheBuilder myBuilder;

    public FSharpDeclarationProcessor(ICacheBuilder builder)
    {
      myBuilder = builder;
    }

    public override void VisitFSharpFile(IFSharpFile fsFile)
    {
      myBuilder.CreateProjectFilePart(new FSharpProjectFilePart(fsFile.GetSourceFile()));

      foreach (var declaration in fsFile.DeclarationsEnumerable)
      {
        var qualifiers = declaration.LongIdentifier.Qualifiers;
        foreach (var qualifier in qualifiers)
        {
          var qualifierName = FSharpNamesUtil.RemoveBackticks(qualifier.GetText());
          myBuilder.StartPart(new QualifiedNamespacePart(qualifier.GetTreeStartOffset(), qualifierName));
        }

        declaration.Accept(this);

        foreach (var _ in qualifiers)
          myBuilder.EndPart();
      }
    }

    public override void VisitFSharpNamespaceDeclaration(IFSharpNamespaceDeclaration decl)
    {
      ProcessModuleLikeDeclaraion(decl, new DeclaredNamespacePart(decl));
    }

    public override void VisitTopLevelModuleDeclaration(ITopLevelModuleDeclaration decl)
    {
      ProcessModuleLikeDeclaraion(decl, new TopLevelModulePart(decl));
    }

    public override void VisitNestedModuleDeclaration(INestedModuleDeclaration decl)
    {
      ProcessModuleLikeDeclaraion(decl, new NestedModulePart(decl));
    }

    private void ProcessModuleLikeDeclaraion(IModuleLikeDeclaration decl, Part part)
    {
      myBuilder.StartPart(part);
      foreach (var memberDecl in decl.MembersEnumerable)
        memberDecl.Accept(this);
      myBuilder.EndPart();
    }

    public override void VisitLet(ILet letParam)
    {
      myBuilder.AddDeclaredMemberName(letParam.DeclaredName);
    }

    public override void VisitFSharpExceptionDeclaration(IFSharpExceptionDeclaration decl)
    {
      myBuilder.StartPart(new ExceptionPart(decl));
      myBuilder.EndPart();
    }

    public override void VisitFSharpEnumDeclaration(IFSharpEnumDeclaration decl)
    {
      myBuilder.StartPart(new EnumPart(decl));
      foreach (var memberDecl in decl.EnumMembersEnumerable)
        myBuilder.AddDeclaredMemberName(memberDecl.DeclaredName);
      myBuilder.EndPart();
    }

    public override void VisitFSharpRecordDeclaration(IFSharpRecordDeclaration decl)
    {
      myBuilder.StartPart(new RecordPart(decl));
      foreach (var fieldDeclaration in decl.FieldsEnumerable)
        myBuilder.AddDeclaredMemberName(fieldDeclaration.DeclaredName);
      ProcessTypeMembers(decl.MemberDeclarations);
      myBuilder.EndPart();
    }

    public override void VisitFSharpUnionDeclaration(IFSharpUnionDeclaration decl)
    {
      myBuilder.StartPart(new UnionPart(decl));
      foreach (var unionCase in decl.UnionCasesEnumerable)
        unionCase.Accept(this);
      ProcessTypeMembers(decl.MemberDeclarations);
      myBuilder.EndPart();
    }

    public override void VisitFSharpUnionCaseDeclaration(IFSharpUnionCaseDeclaration decl)
    {
      myBuilder.StartPart(new UnionCasePart(decl));
      myBuilder.EndPart();
    }

    public override void VisitFSharpTypeAbbreviationDeclaration(IFSharpTypeAbbreviationDeclaration decl)
    {
      myBuilder.StartPart(new TypeAbbreviationPart(decl));
      myBuilder.EndPart();
    }

    public override void VisitFSharpObjectModelTypeDeclaration(IFSharpObjectModelTypeDeclaration decl)
    {
      myBuilder.StartPart(CreateObjectTypePart(decl));
      ProcessTypeMembers(decl.MemberDeclarations);
      myBuilder.EndPart();
    }

    private void ProcessTypeMembers(TreeNodeCollection<ITypeMemberDeclaration> memberDeclarations)
    {
      foreach (var typeMemberDeclaration in memberDeclarations)
        myBuilder.AddDeclaredMemberName(typeMemberDeclaration.DeclaredName);
    }

    private static Part CreateObjectTypePart(IFSharpObjectModelTypeDeclaration decl)
    {
      switch (decl.TypeKind)
      {
        case FSharpObjectModelTypeKind.Class:
          return new ClassPart(decl);
        case FSharpObjectModelTypeKind.Interface:
          return new InterfacePart(decl);
        case FSharpObjectModelTypeKind.Struct:
          return new StructPart(decl);
        default:
          throw new System.ArgumentOutOfRangeException();
      }
    }
  }
}