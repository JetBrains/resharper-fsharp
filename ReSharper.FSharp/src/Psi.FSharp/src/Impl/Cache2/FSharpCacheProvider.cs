using System.Collections.Generic;
using JetBrains.Platform.ProjectModel.FSharp;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Impl.CodeStyle;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  public class FSharpCacheProvider : ILanguageCacheProvider
  {
    private const int CacheVersion = 1;

    public Part ReadPart(byte tag, IReader reader)
    {
      switch ((FSharpSerializationTag) tag)
      {
        case FSharpSerializationTag.DeclaredNamespacePart:
          return new DeclaredNamespacePart(reader);
        case FSharpSerializationTag.QualifiedNamespacePart:
          return new QualifiedNamespacePart(reader);
        case FSharpSerializationTag.ModulePart:
          return new TopLevelModulePart(reader);
        case FSharpSerializationTag.NestedModulePart:
          return new NestedModulePart(reader);
        case FSharpSerializationTag.ExceptionPart:
          return new ExceptionPart(reader);
        case FSharpSerializationTag.EnumPart:
          return new EnumPart(reader);
        case FSharpSerializationTag.RecordPart:
          return new RecordPart(reader);
        case FSharpSerializationTag.UnionPart:
          return new UnionPart(reader);
        case FSharpSerializationTag.UnionCasePart:
          return new UnionCasePart(reader);
        case FSharpSerializationTag.TypeAbbreviationPart:
          return new TypeAbbreviationPart(reader);
        case FSharpSerializationTag.ClassPart:
          return new ClassPart(reader);
        case FSharpSerializationTag.InterfacePart:
          return new InterfacePart(reader);
        case FSharpSerializationTag.StructPart:
          return new StructPart(reader);
        default:
          throw new SerializationError("Unknown tag:" + tag);
      }
    }

    public ProjectFilePart LoadProjectFilePart(IPsiSourceFile sourceFile, ProjectFilePartsTree tree, IReader reader)
    {
      var part = new FSharpProjectFilePart(sourceFile, reader);
      if (part.CacheVersion != CacheVersion)
        tree.ForceDirty();

      return part;
    }

    public void BuildCache(IFile file, ICacheBuilder builder)
    {
      if (file.GetProjectFileType().Equals(FSharpScriptProjectFileType.Instance))
        return;

      ((IFSharpFile) file).Accept(new FSharpDeclarationProcessor(builder, CacheVersion));
    }

    public bool NeedCacheUpdate(ITreeNode elementContainingChanges, PsiChangedElementType type)
    {
      return true;
    }

    public bool IsCaseSensitive(IPsiModule module)
    {
      return true;
    }

    public IEnumerable<IPsiSourceFile> GetAffectedOnPsiModulePropertiesChange(IPsiModule module)
    {
      return EmptyList<IPsiSourceFile>.InstanceList;
    }

    public bool IsCachableTreeNode(ITreeNode treeNode)
    {
      return false;
    }

    public NodeType GetNodeType(int index)
    {
      return FSharpNodeTypeIndexer.Instance.GetNodeType(index);
    }

    public string PersistentTreeNodeCacheUniqueId => null;
    public string TokenCacheUniqueId => null;
    public string BufferCacheUniqueId => null;
    public string StubTreeNodeCacheUniqueId => null;

    public void SerializeMetadata(IFile file, UnsafeWriter bufferWriter)
    {
    }

    public void RestoreMetadata(IFile file, UnsafeReader bufferReader)
    {
    }

    public bool IsInternableToken(TokenNodeType tokenNodeType)
    {
      throw new System.NotImplementedException();
    }

    public void BuildCache(ISandBox sandBox, ICacheBuilder builder)
    {
      throw new System.NotImplementedException();
    }

    public bool IsCacheableInClosedForm(IChameleonNode node)
    {
      throw new System.NotImplementedException();
    }

    public TreeElement CreateChameleonNode(NodeType nodeType, TreeOffset startOffset, TreeOffset endOffset)
    {
      throw new System.NotImplementedException();
    }
  }
}