using System.Collections.Generic;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Common.Checker;
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

    public FSharpCacheProvider(FSharpCheckerService checkerService)
    {
      myCheckerService = checkerService;
    }

    public void BuildCache(IFile file, ICacheBuilder builder)
    {
      var sourceFile = file.GetSourceFile();
      Assertion.AssertNotNull(sourceFile, "sourceFile != null");

      var declarationProcessor = new FSharpCacheDeclarationProcessor(builder, myCheckerService);
      (file as IFSharpFile)?.Accept(declarationProcessor);
    }

    public ProjectFilePart LoadProjectFilePart(IPsiSourceFile sourceFile, ProjectFilePartsTree tree, IReader reader) =>
      new FSharpProjectFilePart(sourceFile, reader, sourceFile.GetFSharpFileKind(),
        sourceFile != null && myCheckerService.HasPairFile(sourceFile));

    public Part ReadPart(byte tag, IReader reader)
    {
      switch ((FSharpPartKind) tag)
      {
        case FSharpPartKind.DeclaredNamespace:
          return new DeclaredNamespacePart(reader);
        case FSharpPartKind.QualifiedNamespace:
          return new QualifiedNamespacePart(reader);
        case FSharpPartKind.TopLevelModule:
          return new TopLevelModulePart(reader);
        case FSharpPartKind.NestedModule:
          return new NestedModulePart(reader);
        case FSharpPartKind.Exception:
          return new ExceptionPart(reader);
        case FSharpPartKind.Enum:
          return new EnumPart(reader);
        case FSharpPartKind.Record:
          return new RecordPart(reader);
        case FSharpPartKind.Union:
          return new UnionPart(reader);
        case FSharpPartKind.UnionCase:
          return new UnionCasePart(reader);
        case FSharpPartKind.HiddenType:
          return new HiddenTypePart(reader);
        case FSharpPartKind.Class:
          return new ClassPart(reader);
        case FSharpPartKind.Interface:
          return new InterfacePart(reader);
        case FSharpPartKind.Struct:
          return new StructPart(reader);
        case FSharpPartKind.StructRecord:
          return new StructRecordPart(reader);
        case FSharpPartKind.StructUnion:
          return new StructUnionPart(reader);
        case FSharpPartKind.ClassExtension:
          return new ClassExtensionPart(reader);
        case FSharpPartKind.StructExtension:
          return new StructExtensionPart(reader);
        case FSharpPartKind.Delegate:
          return new DelegatePart(reader);
        default:
          throw new SerializationError("Unknown tag:" + tag);
      }
    }

    public bool NeedCacheUpdate(ITreeNode elementContainingChanges, PsiChangedElementType type) => true;
    public bool IsCaseSensitive(IPsiModule module) => true;

    public IEnumerable<IPsiSourceFile> GetAffectedOnPsiModulePropertiesChange(IPsiModule module) =>
      EmptyList<IPsiSourceFile>.Instance; // todo: check this

    public void BuildCache(ISandBox sandBox, ICacheBuilder builder)
    {
    }
  }
}
