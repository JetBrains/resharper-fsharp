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
    private readonly FcsCheckerService myCheckerService;

    public FSharpCacheDeclarationProcessor(ICacheBuilder builder, FcsCheckerService checkerService)
    {
      Builder = builder;
      myCheckerService = checkerService;
    }

    private static FSharpFileKind GetFSharpFileKind(IFSharpFile file) =>
      file switch
      {
        IFSharpImplFile _ => FSharpFileKind.ImplFile,
        IFSharpSigFile _ => FSharpFileKind.SigFile,
        _ => throw new ArgumentOutOfRangeException()
      };

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
          if (fsFile.FindNodeAt(new TreeOffset(token.Start)) is ITokenNode { Parent: IObjExpr objExpr })
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

    public override void VisitMemberDeclaration(IMemberDeclaration decl) => 
      Builder.AddDeclaredMemberName(decl.CompiledName);

    public override void VisitMemberSignature(IMemberSignature decl) =>
      Builder.AddDeclaredMemberName(decl.CompiledName);

    public override void VisitTypeDeclarationGroup(ITypeDeclarationGroup typeDeclarationGroupParam)
    {
      foreach (var typeDeclaration in typeDeclarationGroupParam.TypeDeclarationsEnumerable) 
        typeDeclaration.Accept(this);
    }

    public override void VisitFSharpTypeDeclaration(IFSharpTypeDeclaration decl)
    {
      var allMembers = decl.MemberDeclarations;

      if (decl.TypeRepresentation is { } repr)
        repr.Accept(this);
      else
      {
        // todo: check "anon type" decl
        Builder.StartPart(CreateObjectTypePart(decl, false));
      }

      ProcessTypeMembers(allMembers);
      Builder.EndPart();
    }

    private void ProcessBinding(IBindingLikeDeclaration binding)
    {
      var headPattern = binding.HeadPattern;
      if (headPattern != null)
        ProcessTypeMembers(headPattern.NestedPatterns);
    }

    public override void VisitLetBindingsDeclaration(ILetBindingsDeclaration letBindings)
    {
      foreach (var binding in letBindings.Bindings) 
        ProcessBinding(binding);
    }

    public override void VisitBindingSignature(IBindingSignature binding) => 
      ProcessBinding(binding);

    public override void VisitExceptionDeclaration(IExceptionDeclaration decl)
    {
      Builder.StartPart(new ExceptionPart(decl, Builder));
      ProcessTypeMembers(decl.MemberDeclarations);
      Builder.EndPart();
    }

    public override void VisitObjectModelTypeRepresentation(IObjectModelTypeRepresentation repr) =>
      Builder.StartPart(CreateObjectTypePart(repr.TypeDeclaration, repr.TypePartKind, false));

    public override void VisitEnumRepresentation(IEnumRepresentation repr) => 
      Builder.StartPart(new EnumPart(repr.TypeDeclaration, Builder));

    public override void VisitRecordRepresentation(IRecordRepresentation decl)
    {
      var recordPart =
        decl.TypePartKind == PartKind.Struct
          ? (Part) new StructRecordPart(decl.TypeDeclaration, Builder)
          : new RecordPart(decl.TypeDeclaration, Builder);

      Builder.StartPart(recordPart);
    }

    public override void VisitUnionRepresentation(IUnionRepresentation repr)
    {
      var unionCases = repr.UnionCases;
      var isSingleCase = unionCases.Count == 1;

      if (repr.TypePartKind == PartKind.Struct)
      {
        Builder.StartPart(new StructUnionPart(repr.TypeDeclaration, Builder, isSingleCase));
        foreach (var unionCase in unionCases)
          ProcessTypeMembers(unionCase.Fields);
      }
      else
      {
        var hasNestedTypes = repr.HasNestedTypes;
        Builder.StartPart(new UnionPart(repr.TypeDeclaration, Builder, hasNestedTypes, isSingleCase));

        foreach (var unionCase in unionCases)
          if (hasNestedTypes && unionCase.HasFields)
            unionCase.Accept(this);
          else
            ProcessTypeMembers(unionCase.Fields);
      }
    }

    public override void VisitUnionCaseDeclaration(IUnionCaseDeclaration decl)
    {
      Builder.StartPart(new UnionCasePart(decl, Builder));
      ProcessTypeMembers(decl.MemberDeclarations);
      Builder.EndPart();
    }

    public override void VisitTypeAbbreviationRepresentation(ITypeAbbreviationRepresentation repr)
    {
      var decl = repr.TypeDeclaration;
      var typePart = decl.GetSimpleTypeKindFromAttributes() == PartKind.Struct
        ? new StructTypeAbbreviationOrDeclarationPart(decl, Builder)
        : (TypePart) new TypeAbbreviationOrDeclarationPart(decl, Builder);
      Builder.StartPart(typePart);

      var declaredName = repr.AbbreviatedTypeOrUnionCase?.GetSourceName();
      if (declaredName != SharedImplUtil.MISSING_DECLARATION_NAME)
        Builder.AddDeclaredMemberName(declaredName);
    }

    public override void VisitModuleAbbreviationDeclaration(IModuleAbbreviationDeclaration decl)
    {
      Builder.StartPart(new HiddenTypePart(decl, Builder));
      Builder.EndPart();
    }

    public override void VisitDelegateRepresentation(IDelegateRepresentation repr) =>
      Builder.StartPart(new DelegatePart(repr.TypeDeclaration, Builder));

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

    private Part CreateObjectTypePart(IFSharpTypeOrExtensionDeclaration decl, bool isExtension) =>
      CreateObjectTypePart(decl, decl.TypePartKind, isExtension);

    private Part CreateObjectTypePart(IFSharpTypeOrExtensionDeclaration decl, PartKind partKind, bool isExtension) =>
      partKind switch
      {
        PartKind.Class => isExtension ? (Part) new ClassExtensionPart(decl, Builder) : new ClassPart(decl, Builder),
        PartKind.Struct => isExtension ? (Part) new StructExtensionPart(decl, Builder) : new StructPart(decl, Builder),
        PartKind.Interface => new InterfacePart(decl, Builder),
        PartKind.Enum => new EnumPart(decl, Builder),
        _ => throw new ArgumentOutOfRangeException()
      };

    public override void VisitObjExpr(IObjExpr objExpr)
    {
      Builder.StartPart(new ObjectExpressionTypePart(objExpr, Builder));
      ProcessTypeMembers(objExpr.MemberDeclarations);
      ProcessTypeMembers(objExpr.InterfaceMembers);
      Builder.EndPart();
    }

    private void ProcessTypeMembers(IEnumerable<ITreeNode> declarations)
    {
      foreach (var declaration in declarations)
      {
        if (!(declaration is ITypeMemberDeclaration decl))
          continue;

        var declaredName = decl.DeclaredName;
        if (declaredName != SharedImplUtil.MISSING_DECLARATION_NAME)
          Builder.AddDeclaredMemberName(declaredName);
      }
    }
  }
}
