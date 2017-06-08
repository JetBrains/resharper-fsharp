using System;
using JetBrains.ReSharper.Plugins.FSharp.Common.Checker;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Impl.Cache2.Parts;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  public class FSharpCacheDeclarationProcessor : TreeNodeVisitor
  {
    protected readonly int CacheVersion;
    protected readonly ICacheBuilder Builder;
    private readonly FSharpCheckerService myCheckerService;

    public FSharpCacheDeclarationProcessor(ICacheBuilder builder, FSharpCheckerService checkerService, int cacheVersion)
    {
      Builder = builder;
      myCheckerService = checkerService;
      CacheVersion = cacheVersion;
    }

    private static FSharpFileKind GetFSharpFileKind(IFSharpFile file)
    {
      if (file is IFSharpImplFile) return FSharpFileKind.ImplFile;
      if (file is IFSharpSigFile) return FSharpFileKind.SigFile;
      throw new ArgumentOutOfRangeException();
    }

    public override void VisitFSharpFile(IFSharpFile fsFile)
    {
      fsFile.TokenBuffer = fsFile.ActualTokenBuffer; // todo: remove this when a proper lexer implemented
      var sourceFile = fsFile.GetSourceFile();
      var fileKind = GetFSharpFileKind(fsFile);
      var hasPairFile = myCheckerService.HasPairFile(sourceFile);

      Builder.CreateProjectFilePart(new FSharpProjectFilePart(sourceFile, CacheVersion, fileKind, hasPairFile));

      foreach (var declaration in fsFile.DeclarationsEnumerable)
      {
        var qualifiers = declaration.LongIdentifier.Qualifiers;
        foreach (var qualifier in qualifiers)
        {
          var qualifierName = FSharpNamesUtil.RemoveBackticks(qualifier.GetText());
          Builder.StartPart(new QualifiedNamespacePart(qualifier.GetTreeStartOffset(), Builder.Intern(qualifierName)));
        }

        declaration.Accept(this);

        foreach (var _ in qualifiers)
          Builder.EndPart();
      }
    }
    
    public override void VisitFSharpNamespaceDeclaration(IFSharpNamespaceDeclaration decl)
    {
      Builder.StartPart(new DeclaredNamespacePart(decl));
      FinishModuleLikeDeclaraion(decl);
    }

    public override void VisitTopLevelModuleDeclaration(ITopLevelModuleDeclaration decl)
    {
      Builder.StartPart(new TopLevelModulePart(decl, Builder));
      FinishModuleLikeDeclaraion(decl);
    }

    public override void VisitNestedModuleDeclaration(INestedModuleDeclaration decl)
    {
      Builder.StartPart(new NestedModulePart(decl, Builder));
      FinishModuleLikeDeclaraion(decl);
    }

    private void FinishModuleLikeDeclaraion(IModuleLikeDeclaration decl)
    {
      foreach (var memberDecl in decl.MembersEnumerable)
        memberDecl.Accept(this);
      Builder.EndPart();
    }

    public override void VisitLet(ILet letParam)
    {
      Builder.AddDeclaredMemberName(letParam.DeclaredName);
    }

    public override void VisitExceptionDeclaration(IExceptionDeclaration decl)
    {
      Builder.StartPart(new ExceptionPart(decl, Builder));
      Builder.EndPart();
    }

    public override void VisitEnumDeclaration(IEnumDeclaration decl)
    {
      Builder.StartPart(new EnumPart(decl, Builder));
      foreach (var memberDecl in decl.EnumMembersEnumerable)
        Builder.AddDeclaredMemberName(memberDecl.DeclaredName);
      Builder.EndPart();
    }

    public override void VisitRecordDeclaration(IRecordDeclaration decl)
    {
      Builder.StartPart(new RecordPart(decl, Builder));
      foreach (var fieldDeclaration in decl.FieldsEnumerable)
        Builder.AddDeclaredMemberName(fieldDeclaration.DeclaredName);
      ProcessTypeMembers(decl.MemberDeclarations);
      Builder.EndPart();
    }

    public override void VisitUnionDeclaration(IUnionDeclaration decl)
    {
      Builder.StartPart(new UnionPart(decl, Builder));
      foreach (var unionCase in decl.UnionCasesEnumerable)
        unionCase.Accept(this);
      ProcessTypeMembers(decl.MemberDeclarations);
      Builder.EndPart();
    }

    public override void VisitUnionCaseDeclaration(IUnionCaseDeclaration decl)
    {
      Builder.StartPart(new UnionCasePart(decl, Builder));
      Builder.EndPart();
    }

    public override void VisitTypeAbbreviationDeclaration(ITypeAbbreviationDeclaration decl)
    {
      Builder.StartPart(new HiddenTypePart(decl, Builder));
      Builder.EndPart();
    }

    public override void VisitAbstractTypeDeclaration(IAbstractTypeDeclaration decl)
    {
      Builder.StartPart(new HiddenTypePart(decl, Builder));
      Builder.EndPart();
    }

    public override void VisitObjectModelTypeDeclaration(IObjectModelTypeDeclaration decl)
    {
      Builder.StartPart(CreateObjectTypePart(decl));
      ProcessTypeMembers(decl.MemberDeclarations);
      Builder.EndPart();
    }

    private Part CreateObjectTypePart(IObjectModelTypeDeclaration decl)
    {
      switch (decl.TypePartKind)
      {
        case FSharpPartKind.Class:
          return new ClassPart(decl, Builder);
        case FSharpPartKind.Interface:
          return new InterfacePart(decl, Builder);
        case FSharpPartKind.Struct:
          return new StructPart(decl, Builder);
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    private void ProcessTypeMembers(TreeNodeCollection<ITypeMemberDeclaration> memberDeclarations)
    {
      foreach (var typeMemberDeclaration in memberDeclarations)
        Builder.AddDeclaredMemberName(typeMemberDeclaration.DeclaredName);
    }
  }
}