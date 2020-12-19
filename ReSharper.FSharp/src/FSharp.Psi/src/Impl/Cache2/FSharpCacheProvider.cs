using System.Collections.Generic;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Checker;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  [Language(typeof(FSharpLanguage))]
  public class FSharpCacheProvider : ILanguageCacheProvider, ILanguageCacheInvalidator
  {
    private readonly FSharpCheckerService myCheckerService;

    public FSharpCacheProvider(FSharpCheckerService checkerService) =>
      myCheckerService = checkerService;

    public void BuildCache(IFile file, ICacheBuilder builder)
    {
      using var cookie = ProhibitTypeCheckCookie.Create();

      var sourceFile = file.GetSourceFile();
      Assertion.AssertNotNull(sourceFile, "sourceFile != null");

      var declarationProcessor = new FSharpCacheDeclarationProcessor(builder, myCheckerService);
      (file as IFSharpFile)?.Accept(declarationProcessor);
    }

    public ProjectFilePart LoadProjectFilePart(IPsiSourceFile sourceFile, ProjectFilePartsTree tree, IReader reader) =>
      new FSharpProjectFilePart(sourceFile, reader, sourceFile.GetFSharpFileKind(),
        sourceFile != null && myCheckerService.FcsProjectProvider.HasPairFile(sourceFile));

    public Part ReadPart(byte tag, IReader reader) =>
      (FSharpPartKind) tag switch
      {
        FSharpPartKind.DeclaredNamespace => new DeclaredNamespacePart(reader),
        FSharpPartKind.QualifiedNamespace => new QualifiedNamespacePart(reader),
        FSharpPartKind.NamedModule => new NamedModulePart(reader),
        FSharpPartKind.NestedModule => new NestedModulePart(reader),
        FSharpPartKind.AnonModule => new AnonModulePart(reader),
        FSharpPartKind.Exception => new ExceptionPart(reader),
        FSharpPartKind.Enum => new EnumPart(reader),
        FSharpPartKind.Record => new RecordPart(reader),
        FSharpPartKind.Union => new UnionPart(reader),
        FSharpPartKind.UnionCase => new UnionCasePart(reader),
        FSharpPartKind.Class => new ClassPart(reader),
        FSharpPartKind.Interface => new InterfacePart(reader),
        FSharpPartKind.Struct => new StructPart(reader),
        FSharpPartKind.StructRecord => new StructRecordPart(reader),
        FSharpPartKind.StructUnion => new StructUnionPart(reader),
        FSharpPartKind.ClassExtension => new ClassExtensionPart(reader),
        FSharpPartKind.StructExtension => new StructExtensionPart(reader),
        FSharpPartKind.Delegate => new DelegatePart(reader),
        FSharpPartKind.ObjectExpression => new ObjectExpressionTypePart(reader),
        FSharpPartKind.AbbreviationOrSingleCaseUnion => new TypeAbbreviationOrDeclarationPart(reader),
        FSharpPartKind.StructAbbreviationOrSingleCaseUnion => new StructTypeAbbreviationOrDeclarationPart(reader),
        FSharpPartKind.HiddenType => new HiddenTypePart(reader),
        _ => throw new SerializationError("Unknown tag:" + tag)
      };

    public bool NeedCacheUpdate(ITreeNode elementContainingChanges, PsiChangedElementType type) => true;
    public bool IsCaseSensitive(IPsiModule module) => true;

    public IEnumerable<IPsiSourceFile> GetAffectedOnPsiModulePropertiesChange(IPsiModule module) =>
      EmptyList<IPsiSourceFile>.Instance; // todo: check this

    public void BuildCache(ISandBox sandBox, ICacheBuilder builder)
    {
    }
  }
}
