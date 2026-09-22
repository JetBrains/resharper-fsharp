using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Checker;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public class FSharpCacheDeclarationProcessor(ICacheBuilder builder, FcsCheckerService checkerService)
    : TreeNodeVisitor<IEnumeratorWithEnd<IObjExpr>>
  {
    protected readonly ICacheBuilder Builder = builder;
    private bool myHasInternalsVisibleTo;

    private static FSharpFileKind GetFSharpFileKind(IFSharpFile file) =>
      file switch
      {
        IFSharpImplFile _ => FSharpFileKind.ImplFile,
        IFSharpSigFile _ => FSharpFileKind.SigFile,
        _ => throw new ArgumentOutOfRangeException()
      };

    public override void VisitFSharpFile(IFSharpFile fsFile, IEnumeratorWithEnd<IObjExpr> objectExpressions)
    {
      var sourceFile = fsFile.GetSourceFile();
      if (sourceFile == null)
        return;

      var fileKind = GetFSharpFileKind(fsFile);
      var hasPairFile = checkerService.FcsProjectProvider.HasPairFile(sourceFile);

      var filePart = new FSharpProjectFilePart(sourceFile, fileKind, hasPairFile);
      Builder.CreateProjectFilePart(filePart);

      foreach (var declaration in fsFile.ModuleDeclarations)
        declaration.Accept(this, objectExpressions);

      filePart.HasInternalsVisibleTo = myHasInternalsVisibleTo;
    }

    public static IEnumerable<IObjExpr> GetObjectExpressions(IFSharpFile fsFile)
    {
      foreach (var offset in fsFile.ObjectExpressionOffsets)
        if (fsFile.FindNodeAt(offset) is ITokenNode { Parent: IObjExpr objExpr })
          yield return objExpr;
    }

    public void ProcessQualifiableModuleLikeDeclaration(IQualifiableModuleLikeDeclaration decl, Part part, IEnumeratorWithEnd<IObjExpr> objectExpressions)
    {
      StartNamespaceQualifier(decl.QualifierReferenceName);
      Builder.StartPart(part);
      FinishModuleLikeDeclaration(decl, objectExpressions);
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

    public override void VisitNamedNamespaceDeclaration(INamedNamespaceDeclaration decl, IEnumeratorWithEnd<IObjExpr> objectExpressions) =>
      ProcessQualifiableModuleLikeDeclaration(decl, new DeclaredNamespacePart(decl), objectExpressions);

    public override void VisitGlobalNamespaceDeclaration(IGlobalNamespaceDeclaration decl, IEnumeratorWithEnd<IObjExpr> objectExpressions)
    {
      foreach (var memberDecl in decl.MembersEnumerable)
        memberDecl.Accept(this, objectExpressions);
    }

    public override void VisitAnonModuleDeclaration(IAnonModuleDeclaration decl, IEnumeratorWithEnd<IObjExpr> objectExpressions)
    {
      Builder.StartPart(new AnonModulePart(decl, Builder));
      FinishModuleLikeDeclaration(decl, objectExpressions);
    }

    public override void VisitNamedModuleDeclaration(INamedModuleDeclaration decl, IEnumeratorWithEnd<IObjExpr> objectExpressions) =>
      ProcessQualifiableModuleLikeDeclaration(decl, new NamedModulePart(decl, Builder), objectExpressions);

    public override void VisitNestedModuleDeclaration(INestedModuleDeclaration decl, IEnumeratorWithEnd<IObjExpr> objectExpressions)
    {
      Builder.StartPart(new NestedModulePart(decl, Builder));
      FinishModuleLikeDeclaration(decl, objectExpressions);
    }

    private void FinishModuleLikeDeclaration(IModuleLikeDeclaration decl, IEnumeratorWithEnd<IObjExpr> objectExpressions)
    {
      foreach (var memberDecl in decl.MembersEnumerable)
        memberDecl.Accept(this, objectExpressions);
      EndPart(decl, objectExpressions);
    }

    private void EndPart(IDeclaration decl, IEnumeratorWithEnd<IObjExpr> objectExpressions)
    {
      while (!objectExpressions.AtEnd && objectExpressions.Current is { } objExpr && decl != objExpr && decl.Contains(objExpr))
      {
        objExpr.Accept(this, objectExpressions);

        objectExpressions.MoveNext();
      }

      Builder.EndPart();
    }

    public override void VisitMemberDeclaration(IMemberDeclaration decl, IEnumeratorWithEnd<IObjExpr> objectExpressions) =>
      Builder.AddDeclaredMemberName(decl.CompiledName);

    public override void VisitMemberSignature(IMemberSignature decl, IEnumeratorWithEnd<IObjExpr> objectExpressions) =>
      Builder.AddDeclaredMemberName(decl.CompiledName);

    public override void VisitTypeDeclarationGroup(ITypeDeclarationGroup typeDeclarationGroupParam, IEnumeratorWithEnd<IObjExpr> objectExpressions)
    {
      foreach (var typeDeclaration in typeDeclarationGroupParam.TypeDeclarationsEnumerable)
        typeDeclaration.Accept(this, objectExpressions);
    }

    public override void VisitFSharpTypeDeclaration(IFSharpTypeDeclaration decl, IEnumeratorWithEnd<IObjExpr> objectExpressions)
    {
      var allMembers = decl.MemberDeclarations;

      if (decl.TypeRepresentation is { } repr)
        repr.Accept(this, objectExpressions);
      else
      {
        // todo: check "anon type" decl
        Builder.StartPart(CreateObjectTypePart(decl, false));
      }

      ProcessTypeMembers(allMembers);
      EndPart(decl, objectExpressions);
    }

    private void ProcessBinding(IBindingLikeDeclaration binding)
    {
      var headPattern = binding.HeadPattern;
      if (headPattern != null)
        ProcessTypeMembers(headPattern.NestedPatterns);
    }

    public override void VisitLetBindingsDeclaration(ILetBindingsDeclaration letBindings, IEnumeratorWithEnd<IObjExpr> objectExpressions)
    {
      foreach (var binding in letBindings.Bindings)
        ProcessBinding(binding);
    }

    public override void VisitBindingSignature(IBindingSignature bindingSignature, IEnumeratorWithEnd<IObjExpr> objectExpressions) =>
      ProcessBinding(bindingSignature);

    public override void VisitExceptionDeclaration(IExceptionDeclaration decl, IEnumeratorWithEnd<IObjExpr> objectExpressions)
    {
      Builder.StartPart(new FSharpExceptionPart(decl, Builder));
      ProcessTypeMembers(decl.MemberDeclarations);
      EndPart(decl, objectExpressions);
    }

    public override void VisitObjectModelTypeRepresentation(IObjectModelTypeRepresentation repr, IEnumeratorWithEnd<IObjExpr> objectExpressions) =>
      Builder.StartPart(CreateObjectTypePart(repr.TypeDeclaration, repr.TypePartKind, false));

    public override void VisitEnumRepresentation(IEnumRepresentation repr, IEnumeratorWithEnd<IObjExpr> objectExpressions) =>
      Builder.StartPart(new EnumPart(repr.TypeDeclaration, Builder));

    public override void VisitRecordRepresentation(IRecordRepresentation decl, IEnumeratorWithEnd<IObjExpr> objectExpressions)
    {
      var recordPart =
        decl.TypePartKind == PartKind.Struct
          ? (Part) new StructRecordPart(decl.TypeDeclaration, Builder)
          : new RecordPart(decl.TypeDeclaration, Builder);

      Builder.StartPart(recordPart);
    }

    public override void VisitUnionRepresentation(IUnionRepresentation repr, IEnumeratorWithEnd<IObjExpr> objectExpressions)
    {
      var unionCases = repr.UnionCases;
      var caseNames = unionCases.Select(caseDecl => caseDecl.SourceName).AsArray();

      var hasNestedTypes = repr.HasNestedTypes;
      if (repr.TypePartKind == PartKind.Struct)
        Builder.StartPart(new StructUnionPart(repr.TypeDeclaration, Builder, caseNames));
      else
        Builder.StartPart(new UnionPart(repr.TypeDeclaration, Builder, hasNestedTypes, caseNames));

      foreach (var unionCase in unionCases)
        if (hasNestedTypes && unionCase.HasFields)
          unionCase.Accept(this, objectExpressions);
        else
          ProcessTypeMembers(unionCase.Fields);

    }

    public override void VisitUnionCaseDeclaration(IUnionCaseDeclaration decl, IEnumeratorWithEnd<IObjExpr> objectExpressions)
    {
      Builder.StartPart(new UnionCasePart(decl, Builder));
      ProcessTypeMembers(decl.MemberDeclarations);
      EndPart(decl, objectExpressions);
    }

    public override void VisitTypeAbbreviationRepresentation(ITypeAbbreviationRepresentation repr, IEnumeratorWithEnd<IObjExpr> objectExpressions)
    {
      var decl = repr.TypeDeclaration;
      var identifier = repr.AbbreviatedTypeOrUnionCase?.NameIdentifier;
      var declaredName = identifier.GetSourceName();
      var caseNames =
        declaredName != SharedImplUtil.MISSING_DECLARATION_NAME
          ? [declaredName]
          : EmptyArray<string>.Instance;

      TypePart typePart = decl.GetSimpleTypeKindFromAttributes() == PartKind.Struct
        ? new StructTypeAbbreviationOrDeclarationPart(decl, Builder, caseNames)
        : new TypeAbbreviationOrDeclarationPart(decl, Builder, caseNames);
      Builder.StartPart(typePart);

      if (declaredName != SharedImplUtil.MISSING_DECLARATION_NAME)
        Builder.AddDeclaredMemberName(declaredName);
    }

    public override void VisitIlAssemblyRepresentation(IIlAssemblyRepresentation repr, IEnumeratorWithEnd<IObjExpr> objectExpressions) =>
      Builder.StartPart(new ILAssemblyTypeAbbreviationPart(repr.TypeDeclaration, Builder));

    public override void VisitModuleAbbreviationDeclaration(IModuleAbbreviationDeclaration decl, IEnumeratorWithEnd<IObjExpr> objectExpressions)
    {
      Builder.StartPart(new FSharpModuleAbbreviationPart(decl, Builder));
      EndPart(decl, objectExpressions);
    }

    public override void VisitDelegateRepresentation(IDelegateRepresentation repr, IEnumeratorWithEnd<IObjExpr> objectExpressions) =>
      Builder.StartPart(new DelegatePart(repr.TypeDeclaration, Builder));

    public override void VisitTypeExtensionDeclaration(ITypeExtensionDeclaration typeExtension, IEnumeratorWithEnd<IObjExpr> objectExpressions)
    {
      if (typeExtension.IsTypePartDeclaration)
      {
        Builder.StartPart(CreateObjectTypePart(typeExtension, true));
        ProcessTypeMembers(typeExtension.MemberDeclarations);
        EndPart(typeExtension, objectExpressions);
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
        PartKind.Class => isExtension ? new ClassExtensionPart(decl, Builder) : new ClassPart(decl, Builder),
        PartKind.Struct => isExtension ? new StructExtensionPart(decl, Builder) : new StructPart(decl, Builder),
        PartKind.Interface => new InterfacePart(decl, Builder),
        PartKind.Enum => new EnumPart(decl, Builder),
        _ => throw new ArgumentOutOfRangeException()
      };

    public override void VisitObjExpr(IObjExpr objExpr, IEnumeratorWithEnd<IObjExpr> objectExpressions)
    {
      Builder.StartPart(new ObjectExpressionTypePart(objExpr, Builder));
      ProcessTypeMembers(objExpr.MemberDeclarations);
      ProcessTypeMembers(objExpr.InterfaceMembers);
      EndPart(objExpr, objectExpressions);
    }

    public override void VisitDoStatement(IDoStatement doStmt, IEnumeratorWithEnd<IObjExpr> objectExpressions) => ProcessDoLikeStatement(doStmt);
    public override void VisitExpressionStatement(IExpressionStatement exprStmt, IEnumeratorWithEnd<IObjExpr> objectExpressions) => ProcessDoLikeStatement(exprStmt);

    private void ProcessDoLikeStatement(IDoLikeStatement doStmt)
    {
      foreach (var attribute in doStmt.Attributes)
        // Workaround for providing IVT attributes until attributes can be resolved in a better/faster way.
        if (attribute.ReferenceName.ShortName.DropAttributeSuffix() == "InternalsVisibleTo")
          myHasInternalsVisibleTo = true;
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
