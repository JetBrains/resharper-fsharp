using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Checker;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
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
      var sourceFile = fsFile.GetSourceFile();
      if (sourceFile == null)
        return;

      var fileKind = GetFSharpFileKind(fsFile);
      var hasPairFile = myCheckerService.FcsProjectProvider.HasPairFile(sourceFile);

      Builder.CreateProjectFilePart(new FSharpProjectFilePart(sourceFile, fileKind, hasPairFile));

      foreach (var declaration in fsFile.ModuleDeclarations)
        declaration.Accept(this);

      foreach (var objExpr in GetObjectExpressions(fsFile, sourceFile))
        objExpr.Accept(this);
    }

    public static IEnumerable<IObjExpr> GetObjectExpressions(IFSharpFile fsFile, IPsiSourceFile sourceFile)
    {
      var tokens = fsFile.CachingLexer.TokenBuffer.CachedTokens;
      if (tokens == null)
        return EmptyList<IObjExpr>.Instance;

      var objectExpressions = new List<IObjExpr>();

      var seenLBrace = false;
      for (var i = 0; i < tokens.Count; i++)
      {
        var token = tokens[i];
        var tokenType = token.Type;
        Assertion.Assert(tokenType != null, "tokenType != null");
        if (tokenType.IsFiltered)
          continue;

        if (tokenType == FSharpTokenType.LBRACE)
        {
          seenLBrace = true;
          continue;
        }

        if (seenLBrace && tokenType == FSharpTokenType.NEW)
          if (fsFile.FindNodeAt(new TreeOffset(token.Start)) is ITokenNode node && node.Parent is IObjExpr objExpr)
            objectExpressions.Add(objExpr);

        seenLBrace = false;
      }

      return objectExpressions;
    }

    public void ProcessQualifiableModuleLikeDeclaration(IQualifiableModuleLikeDeclaration decl, Part part)
    {
      StartNamespaceQualifier(decl.QualifierReferenceName);
      Builder.StartPart(part);
      FinishModuleLikeDeclaration(decl);
      EndNamespaceQualifier(decl.QualifierReferenceName);
    }

    private void StartNamespaceQualifier([CanBeNull] IReferenceName referenceName)
    {
      if (referenceName == null)
        return;

      StartNamespaceQualifier(referenceName.Qualifier);
      var qualifierName = Builder.Intern(referenceName.ShortName);
      Builder.StartPart(new QualifiedNamespacePart(referenceName.Identifier.GetTreeStartOffset(), qualifierName));
    }

    private void EndNamespaceQualifier([CanBeNull] IReferenceName referenceName)
    {
      if (referenceName == null)
        return;

      EndNamespaceQualifier(referenceName.Qualifier);
      Builder.EndPart();
    }

    public override void VisitNamedNamespaceDeclaration(INamedNamespaceDeclaration decl) =>
      ProcessQualifiableModuleLikeDeclaration(decl, new DeclaredNamespacePart(decl));

    public override void VisitGlobalNamespaceDeclaration(IGlobalNamespaceDeclaration decl)
    {
      foreach (var memberDecl in decl.MembersEnumerable)
        memberDecl.Accept(this);
    }

    public override void VisitAnonModuleDeclaration(IAnonModuleDeclaration decl)
    {
      Builder.StartPart(new AnonModulePart(decl, Builder));
      FinishModuleLikeDeclaration(decl);
    }

    public override void VisitNamedModuleDeclaration(INamedModuleDeclaration decl) =>
      ProcessQualifiableModuleLikeDeclaration(decl, new NamedModulePart(decl, Builder));

    public override void VisitNestedModuleDeclaration(INestedModuleDeclaration decl)
    {
      Builder.StartPart(new NestedModulePart(decl, Builder));
      FinishModuleLikeDeclaration(decl);
    }

    private void FinishModuleLikeDeclaration(IModuleLikeDeclaration decl)
    {
      foreach (var memberDecl in decl.MembersEnumerable)
        memberDecl.Accept(this);
      Builder.EndPart();
    }

    public override void VisitMemberDeclaration(IMemberDeclaration decl)
    {
      Builder.AddDeclaredMemberName(decl.CompiledName);
    }

    public override void VisitTypeDeclarationGroup(ITypeDeclarationGroup typeDeclarationGroupParam)
    {
      foreach (var typeDeclaration in typeDeclarationGroupParam.TypeDeclarations) 
        typeDeclaration.Accept(this);
    }

    public override void VisitLetModuleDecl(ILetModuleDecl letModuleDecl)
    {
      foreach (var binding in letModuleDecl.Bindings)
      {
        var headPattern = binding.HeadPattern;
        if (headPattern != null)
          ProcessTypeMembers(headPattern.Declarations);
      }
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
      var recordPart =
        decl.HasAttribute(FSharpImplUtil.Struct)
          ? (Part) new StructRecordPart(decl, Builder)
          : new RecordPart(decl, Builder);

      Builder.StartPart(recordPart);
      ProcessTypeMembers(decl.MemberDeclarations);
      ProcessTypeMembers(decl.FieldDeclarations);
      Builder.EndPart();
    }

    public override void VisitUnionDeclaration(IUnionDeclaration decl)
    {
      var unionCases = decl.UnionCases;

      var casesWithFieldsCount = 0;
      foreach (var unionCase in unionCases)
        if (unionCase is INestedTypeUnionCaseDeclaration)
          casesWithFieldsCount++;

      var casesCount = unionCases.Count;
      var hasPublicNestedTypes = casesWithFieldsCount > 0 && casesCount > 1;
      var isSingleCase = casesCount == 1;

      var unionPart =
        decl.HasAttribute(FSharpImplUtil.Struct)
          ? (Part) new StructUnionPart(decl, Builder, isSingleCase)
          : new UnionPart(decl, Builder, hasPublicNestedTypes, isSingleCase);

      Builder.StartPart(unionPart);
      foreach (var unionCase in unionCases)
        unionCase.Accept(this);
      ProcessTypeMembers(decl.MemberDeclarations);
      Builder.EndPart();
    }

    public override void VisitNestedTypeUnionCaseDeclaration(INestedTypeUnionCaseDeclaration decl)
    {
      Builder.StartPart(new UnionCasePart(decl, Builder));
      ProcessTypeMembers(decl.MemberDeclarations);
      Builder.EndPart();
    }

    public override void VisitTypeAbbreviationDeclaration(ITypeAbbreviationDeclaration decl)
    {
      var typePart = decl.TypePartKind == PartKind.Class
        ? (TypePart) new TypeAbbreviationOrDeclarationPart(decl, Builder)
        : new StructTypeAbbreviationOrDeclarationPart(decl, Builder);
      ProcessPart(typePart);

      var declaredName = decl.AbbreviatedTypeOrUnionCase?.GetSourceName();
      if (declaredName != SharedImplUtil.MISSING_DECLARATION_NAME)
        Builder.AddDeclaredMemberName(declaredName);
    }

    public override void VisitModuleAbbreviation(IModuleAbbreviation decl) =>
      ProcessHiddenTypeDeclaration(decl);

    public override void VisitAbstractTypeDeclaration(IAbstractTypeDeclaration decl) =>
      ProcessHiddenTypeDeclaration(decl);

    private void ProcessHiddenTypeDeclaration(IFSharpTypeDeclaration decl) =>
      ProcessPart(new HiddenTypePart(decl, Builder));

    public override void VisitObjectModelTypeDeclaration(IObjectModelTypeDeclaration decl)
    {
      Builder.StartPart(CreateObjectTypePart(decl, false));
      ProcessTypeMembers(decl.MemberDeclarations);
      Builder.EndPart();
    }

    public override void VisitDelegateDeclaration(IDelegateDeclaration decl) =>
      ProcessPart(new DelegatePart(decl, Builder));

    public override void VisitTypeExtensionDeclaration(ITypeExtensionDeclaration typeExtension)
    {
      if (typeExtension.IsTypePartDeclaration)
      {
        Builder.StartPart(CreateObjectTypePart(typeExtension, true));
        ProcessTypeMembers(typeExtension.MemberDeclarations);
        Builder.EndPart();
        return;
      }

      if (typeExtension.IsTypeExtensionAllowed)
        ProcessTypeMembers(typeExtension.MemberDeclarations);
    }

    private Part CreateObjectTypePart(IFSharpTypeDeclaration decl, bool isExtension)
    {
      switch (decl.TypePartKind)
      {
        case PartKind.Class:
          return isExtension ? (Part) new ClassExtensionPart(decl, Builder) : new ClassPart(decl, Builder);
        case PartKind.Struct:
          return isExtension ? (Part) new StructExtensionPart(decl, Builder) : new StructPart(decl, Builder);
        case PartKind.Interface:
          return new InterfacePart(decl, Builder);
        case PartKind.Enum:
          return new EnumPart(decl, Builder);
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    public override void VisitObjExpr(IObjExpr objExpr)
    {
      Builder.StartPart(new ObjectExpressionTypePart(objExpr, Builder));
      ProcessTypeMembers(objExpr.MemberDeclarations);
      ProcessTypeMembers(objExpr.InterfaceMembers);
      Builder.EndPart();
    }

    private void ProcessPart([NotNull] Part part)
    {
      Builder.StartPart(part);
      Builder.EndPart();
    }
    
    private void ProcessTypeMembers(IEnumerable<IDeclaration> declarations)
    {
      foreach (var declaration in declarations)
      {
        if (!(declaration is ITypeMemberDeclaration))
          continue;

        var declaredName = declaration.DeclaredName;
        if (declaredName != SharedImplUtil.MISSING_DECLARATION_NAME)
          Builder.AddDeclaredMemberName(declaredName);
      }
    }
  }
}
