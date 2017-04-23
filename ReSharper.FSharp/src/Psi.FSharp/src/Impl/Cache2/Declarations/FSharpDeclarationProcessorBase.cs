using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2.Declarations
{
  public abstract class FSharpCacheDeclarationProcessorBase : TreeNodeVisitor
  {
    protected readonly ICacheBuilder Builder;
    protected readonly int CacheVersion;

    protected FSharpCacheDeclarationProcessorBase(ICacheBuilder builder, int cacheVersion = -1)
    {
      Builder = builder;
      CacheVersion = cacheVersion;
    }

    internal abstract void StartTypePart(IFSharpTypeElementDeclaration fSharpTypeElementDeclaration,
      FSharpPartKind partKind);

    internal virtual void StartNamespacePart([NotNull] Part part)
    {
      Builder.StartPart(part);
    }


    [CanBeNull]
    protected static string MakeClrName([NotNull] IFSharpTypeElementDeclaration declaration)
    {
      var typeDeclaration = declaration as IFSharpTypeDeclaration;
      return typeDeclaration != null ? FSharpImplUtil.MakeClrName(typeDeclaration) : null;
    }


    public override void VisitFSharpFile(IFSharpFile fsFile)
    {
      Builder.CreateProjectFilePart(new FSharpProjectFilePart(fsFile.GetSourceFile(), CacheVersion));

      foreach (var declaration in fsFile.DeclarationsEnumerable)
      {
        var qualifiers = declaration.LongIdentifier.Qualifiers;
        foreach (var qualifier in qualifiers)
        {
          var qualifierName = FSharpNamesUtil.RemoveBackticks(qualifier.GetText());
          StartNamespacePart(new QualifiedNamespacePart(qualifier.GetTreeStartOffset(), qualifierName));
        }

        declaration.Accept(this);

        foreach (var _ in qualifiers)
          Builder.EndPart();
      }
    }

    public override void VisitFSharpNamespaceDeclaration(IFSharpNamespaceDeclaration decl)
    {
      StartNamespacePart(new DeclaredNamespacePart(decl));
      FinishModuleLikeDeclaraion(decl);
    }

    public override void VisitTopLevelModuleDeclaration(ITopLevelModuleDeclaration decl)
    {
      StartTypePart(decl, FSharpPartKind.TopLevelModule);
      FinishModuleLikeDeclaraion(decl);
    }

    public override void VisitNestedModuleDeclaration(INestedModuleDeclaration decl)
    {
      StartTypePart(decl, FSharpPartKind.NestedModule);
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

    public override void VisitFSharpExceptionDeclaration(IFSharpExceptionDeclaration decl)
    {
      StartTypePart(decl, FSharpPartKind.Exception);
      Builder.EndPart();
    }

    public override void VisitFSharpEnumDeclaration(IFSharpEnumDeclaration decl)
    {
      StartTypePart(decl, FSharpPartKind.Enum);
      foreach (var memberDecl in decl.EnumMembersEnumerable)
        Builder.AddDeclaredMemberName(memberDecl.DeclaredName);
      Builder.EndPart();
    }

    public override void VisitFSharpRecordDeclaration(IFSharpRecordDeclaration decl)
    {
      StartTypePart(decl, FSharpPartKind.Record);
      foreach (var fieldDeclaration in decl.FieldsEnumerable)
        Builder.AddDeclaredMemberName(fieldDeclaration.DeclaredName);
      ProcessTypeMembers(decl.MemberDeclarations);
      Builder.EndPart();
    }

    public override void VisitFSharpUnionDeclaration(IFSharpUnionDeclaration decl)
    {
      StartTypePart(decl, FSharpPartKind.Union);
      foreach (var unionCase in decl.UnionCasesEnumerable)
        unionCase.Accept(this);
      ProcessTypeMembers(decl.MemberDeclarations);
      Builder.EndPart();
    }

    public override void VisitFSharpUnionCaseDeclaration(IFSharpUnionCaseDeclaration decl)
    {
      StartTypePart(decl, FSharpPartKind.UnionCase);
      Builder.EndPart();
    }

    public override void VisitFSharpTypeAbbreviationDeclaration(IFSharpTypeAbbreviationDeclaration decl)
    {
      StartTypePart(decl, FSharpPartKind.HiddenType);
      Builder.EndPart();
    }

    public override void VisitFSharpObjectModelTypeDeclaration(IFSharpObjectModelTypeDeclaration decl)
    {
      StartTypePart(decl, decl.TypePartKind);
      ProcessTypeMembers(decl.MemberDeclarations);
      Builder.EndPart();
    }

    public override void VisitFSharpAbstractTypeDeclaration(IFSharpAbstractTypeDeclaration decl)
    {
      StartTypePart(decl, FSharpPartKind.HiddenType);
      Builder.EndPart();
    }

    private void ProcessTypeMembers(TreeNodeCollection<ITypeMemberDeclaration> memberDeclarations)
    {
      foreach (var typeMemberDeclaration in memberDeclarations)
        Builder.AddDeclaredMemberName(typeMemberDeclaration.DeclaredName);
    }

    [NotNull]
    internal static TypePart CreateTypePart([NotNull] IFSharpTypeElementDeclaration decl, FSharpPartKind partKind,
      bool isHidden = false)
    {
      switch (partKind)
      {
        case FSharpPartKind.TopLevelModule:
          return new TopLevelModulePart((ITopLevelModuleDeclaration) decl, isHidden);
        case FSharpPartKind.NestedModule:
          return new NestedModulePart((INestedModuleDeclaration) decl, isHidden);
        case FSharpPartKind.Exception:
          return new ExceptionPart((IFSharpTypeDeclaration) decl, isHidden);
        case FSharpPartKind.Enum:
          return new EnumPart((IFSharpEnumDeclaration) decl, isHidden);
        case FSharpPartKind.Record:
          return new RecordPart((IFSharpTypeDeclaration) decl, isHidden);
        case FSharpPartKind.Union:
          return new UnionPart((IFSharpTypeDeclaration) decl, isHidden);
        case FSharpPartKind.UnionCase:
          return new UnionCasePart((IFSharpUnionCaseDeclaration) decl, isHidden);
        case FSharpPartKind.HiddenType:
          return new HiddenTypePart((IFSharpTypeDeclaration) decl, isHidden);
        case FSharpPartKind.Interface:
          return new InterfacePart((IFSharpTypeDeclaration) decl, isHidden);
        case FSharpPartKind.Class:
          return new ClassPart((IFSharpTypeDeclaration) decl, isHidden);
        case FSharpPartKind.Struct:
          return new StructPart((IFSharpTypeDeclaration) decl, isHidden);
        default:
          throw new ArgumentOutOfRangeException(nameof(partKind), partKind, null);
      }
    }
  }
}