using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Common.Checker;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  /// <summary>
  /// Provides symbol cache constisting of namespaces and types trie and members names.
  /// It should work fast and currently uses parse results only.
  /// Some heuristics are applied for type kind inferrence, look at ObjectTypeDeclaration.
  /// Info about cases like single-case union and provided generated types should probably be cached separately.  
  /// </summary>
  public class FSharpCacheProvider : ILanguageCacheProvider
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
        myCheckerService.HasPairFile(sourceFile));

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
        default:
          throw new SerializationError("Unknown tag:" + tag);
      }
    }

    public bool NeedCacheUpdate(ITreeNode elementContainingChanges, PsiChangedElementType type) => true;
    public bool IsCaseSensitive(IPsiModule module) => true;

    public IEnumerable<IPsiSourceFile> GetAffectedOnPsiModulePropertiesChange(IPsiModule module) =>
      EmptyList<IPsiSourceFile>.InstanceList; // todo: check this

    public bool IsCachableTreeNode(ITreeNode treeNode) => false;
    public NodeType GetNodeType(int index) => FSharpNodeTypeIndexer.Instance.GetNodeType(index);

    public string TokenCacheUniqueId => null;
    public string BufferCacheUniqueId => null;
    public string StubTreeNodeCacheUniqueId => null;
    public string PersistentTreeNodeCacheUniqueId => null;

    public void SerializeMetadata(IFile file, UnsafeWriter bufferWriter)
    {
    }

    public void RestoreMetadata(IFile file, UnsafeReader bufferReader)
    {
    }

    public bool IsInternableToken(TokenNodeType tokenNodeType) => throw new NotImplementedException();
    public void BuildCache(ISandBox sandBox, ICacheBuilder builder) => throw new NotImplementedException();
    public bool IsCacheableInClosedForm(IChameleonNode node) => throw new NotImplementedException();

    public TreeElement CreateChameleonNode(NodeType nodeType, TreeOffset startOffset, TreeOffset endOffset) =>
      throw new NotImplementedException();
  }
}