using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.Common.Checker;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public class FSharpCacheDeclarationProcessor : TreeNodeVisitor
  {
    protected readonly ICacheBuilder Builder;
    private readonly FSharpCheckerService myCheckerService;

    public FSharpCacheDeclarationProcessor(ICacheBuilder builder, FSharpCheckerService checkerService)
    {
      Builder = builder;
      myCheckerService = checkerService;
    }

    private static FSharpFileKind GetFSharpFileKind(IFSharpFile file)
    {
      if (file is IFSharpImplFile) return FSharpFileKind.ImplFile;
      if (file is IFSharpSigFile) return FSharpFileKind.SigFile;
      throw new ArgumentOutOfRangeException();
    }

    public override void VisitFSharpFile(IFSharpFile fsFile)
    {
      fsFile.TokenBuffer = fsFile.ActualTokenBuffer; // todo: remove this when/if a proper lexer is implemented
      var sourceFile = fsFile.GetSourceFile();
      var fileKind = GetFSharpFileKind(fsFile);
      var hasPairFile = myCheckerService.HasPairFile(sourceFile);

      Builder.CreateProjectFilePart(new FSharpProjectFilePart(sourceFile, fileKind, hasPairFile));

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

    public override void VisitFSharpGlobalNamespaceDeclaration(IFSharpGlobalNamespaceDeclaration decl)
    {
      foreach (var memberDecl in decl.MembersEnumerable)
        memberDecl.Accept(this);
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

    public override void VisitMemberDeclaration(IMemberDeclaration decl)
    {
      Builder.AddDeclaredMemberName(decl.DeclaredName);
    }

    public override void VisitLet(ILet letParam)
    {
      Builder.AddDeclaredMemberName(letParam.DeclaredName);
    }

    public override void VisitExceptionDeclaration(IExceptionDeclaration decl)
    {
      Builder.StartPart(new ExceptionPart(decl, Builder));
      ProcessTypeMembers(decl.MemberDeclarations);
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
      ProcessTypeMembers(decl.MemberDeclarations);
      Builder.EndPart();
    }

    public void FinishUnionDeclaration(IUnionDeclaration decl)
    {
      foreach (var unionCase in decl.UnionCasesEnumerable)
        unionCase.Accept(this);
      ProcessTypeMembers(decl.MemberDeclarations);
      Builder.EndPart();
    }

    public override void VisitMultipleCasesUnionDeclaration(IMultipleCasesUnionDeclaration decl)
    {
      Builder.StartPart(new UnionPart(decl, Builder));
      FinishUnionDeclaration(decl);
    }

    public override void VisitSingleCaseUnionDeclaration(ISingleCaseUnionDeclaration decl)
    {
      Builder.StartPart(new UnionPart(decl, Builder));
      var theOnlyCase = decl.UnionCases.SingleOrDefault();
      if (theOnlyCase != null)
        ProcessTypeMembers(theOnlyCase.MemberDeclarations);
      FinishUnionDeclaration(decl);
    }

    public override void VisitUnionCaseDeclaration(IUnionCaseDeclaration decl)
    {
      Builder.StartPart(new UnionCasePart(decl, Builder, decl.Parent is ISingleCaseUnionDeclaration));
      ProcessTypeMembers(decl.MemberDeclarations);
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

    public override void VisitTypeExtension(ITypeExtension typeExtension) =>
      ProcessTypeMembers(typeExtension.TypeMembers.OfType<ITypeMemberDeclaration>().AsIReadOnlyList());

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

    private void ProcessTypeMembers(IReadOnlyList<ITypeMemberDeclaration> memberDeclarations)
    {
      foreach (var typeMemberDeclaration in memberDeclarations)
      {
        var declaredName = typeMemberDeclaration.DeclaredName;
        if (declaredName != SharedImplUtil.MISSING_DECLARATION_NAME)
          Builder.AddDeclaredMemberName(declaredName);
      }
    }
  }
}