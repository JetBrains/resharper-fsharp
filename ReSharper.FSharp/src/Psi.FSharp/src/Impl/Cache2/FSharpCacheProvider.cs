using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Platform.ProjectModel.FSharp;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi.Caches.SymbolCache;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.FSharp.Impl.Cache2.Declarations;
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
    private readonly HashSet<FileSystemPath> myFilesWaitingForPairUpdate = new HashSet<FileSystemPath>();

    public void BuildCache(IFile file, ICacheBuilder builder)
    {
      if (file.GetProjectFileType().Equals(FSharpScriptProjectFileType.Instance))
        return;

      FileSystemPath pairFilePath;
      var pairFile = GetPairFile(file, out pairFilePath);
      var pairFileTypesInfo = pairFile != null
        ? GetTypesInfo(pairFile)
        : EmptyDictionary<string, FSharpTypeInfo>.InstanceDictionary;

      var implFile = file as IFSharpImplFile;
      implFile?.Accept(new FSharpCacheImplementationProcessor(builder, CacheVersion, pairFileTypesInfo));

      var sigFile = file as IFSharpSigFile;
      sigFile?.Accept(new FSharpCacheSignatureProcessor(builder, CacheVersion, pairFileTypesInfo));

      if (pairFile != null && !myFilesWaitingForPairUpdate.Contains(pairFilePath))
      {
        var symbolCache = file.GetSolution().GetComponent<SymbolCache>(); // todo: store it somewhere?
        myFilesWaitingForPairUpdate.Remove(pairFilePath);
        symbolCache.MarkAsDirty(pairFile.GetSourceFile());
        myFilesWaitingForPairUpdate.Add(file.GetSourceFile().GetLocation());
        return;
      }
      myFilesWaitingForPairUpdate.Remove(pairFilePath);
    }

    [CanBeNull]
    private static IFSharpFile GetPairFile([NotNull] IFile file, out FileSystemPath pairPath)
    {
      var sourceFile = file.GetSourceFile();
      Assertion.AssertNotNull(sourceFile, "sourceFile != null");
      pairPath = GetPairFilePath(sourceFile.GetLocation());
      var pairFile = file.GetSolution().FindProjectItemsByLocation(pairPath).FirstOrDefault() as IProjectFile;
      return pairFile?.GetPrimaryPsiFile() as IFSharpFile;
    }

    [NotNull]
    private static Dictionary<string, FSharpTypeInfo> GetTypesInfo([NotNull] IFSharpFile fsFile)
    {
      var namesCacheBuilder = new FSharpNamesCacheBuilder();
      fsFile.Accept(new FSharpCacheNamesProcessor(namesCacheBuilder));
      return namesCacheBuilder.Types;
    }

    [NotNull]
    private static FileSystemPath GetPairFilePath([NotNull] FileSystemPath filePath)
    {
      switch (filePath.ExtensionNoDot)
      {
        case "fs":
          return filePath.ChangeExtension("fsi");
        case "fsi":
          return filePath.ChangeExtension("fs");
        case "ml":
          return filePath.ChangeExtension("mli");
        case "mli":
          return filePath.ChangeExtension("ml");
      }
      throw new ArgumentOutOfRangeException();
    }

    public ProjectFilePart LoadProjectFilePart(IPsiSourceFile sourceFile, ProjectFilePartsTree tree, IReader reader)
    {
      var part = new FSharpProjectFilePart(sourceFile, reader);
      if (part.CacheVersion != CacheVersion)
        tree.ForceDirty();

      return part;
    }

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
        default:
          throw new SerializationError("Unknown tag:" + tag);
      }
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