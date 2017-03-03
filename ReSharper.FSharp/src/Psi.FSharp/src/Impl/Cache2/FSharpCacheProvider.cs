using System.Collections.Generic;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  public class FSharpCacheProvider : ILanguageCacheProvider
  {
    public Part ReadPart(byte tag, IReader reader)
    {
      switch ((FSharpSerializationTag) tag)
      {
        case FSharpSerializationTag.DeclaredNamespacePart:
          return new DeclaredNamespacePart(reader);
        case FSharpSerializationTag.QualifiedNamespacePart:
          return new QualifiedNamespacePart(reader);
        case FSharpSerializationTag.ModulePart:
          return new ModulePart(reader);
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
        case FSharpSerializationTag.TypedUnionCasePart:
          return new TypedUnionCasePart(reader);
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
      return new FSharpProjectFilePart(sourceFile, reader);
    }

    public void BuildCache(IFile file, ICacheBuilder builder)
    {
      (file as IFSharpFile)?.Accept(new FSharpDeclarationProcessor(builder));
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
      return treeNode is IFSharpDeclaration;
    }

    public NodeType GetNodeType(int index)
    {
      return FSharpNodeTypeIndexer.Instance.GetNodeType(index);
    }

    public string PersistentTreeNodeCacheUniqueId => "FSharpPersistentTreeNodeCache";
    public string TokenCacheUniqueId => "FSharpTokenCache";
    public string BufferCacheUniqueId => "FSharpBufferCache";
    public string StubTreeNodeCacheUniqueId => "FSharpStubTreeNodeCache";

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