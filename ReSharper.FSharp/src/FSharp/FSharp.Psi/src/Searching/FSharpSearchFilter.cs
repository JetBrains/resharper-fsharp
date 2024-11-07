using System.Linq;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.FSharp.Checker;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Finder;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Searching
{
  [PsiComponent(Instantiation.DemandAnyThreadUnsafe)]
  public sealed class FSharpSearchFilter(IFcsProjectProvider fsProjectProvider) : ISearchFilter
  {
    private record FSharpSearchFilterKey(
      bool IsTypePrivateMember,
      IPsiModule PsiModule,
      HybridCollection<IPsiSourceFile> SourceFiles,
      int FileIndex);

    public SearchFilterKind Kind => SearchFilterKind.Cache;

    public bool IsAvailable(SearchPattern pattern) => true;

    public static bool CanContainReference(IDeclaredElement declaredElement, IPsiSourceFile sourceFile, IFcsProjectProvider fsProjectProvider)
    {
      if (!sourceFile.LanguageType.Is<FSharpProjectFileType>()) return true;

      var key = TryGetKey(declaredElement, fsProjectProvider);

      if (key.SourceFiles.Contains(sourceFile)) return true;
      if (key.IsTypePrivateMember) return false;
      if (!sourceFile.PsiModule.Equals(key.PsiModule)) return true;

      var sourceFileIndex = fsProjectProvider.GetFileIndex(sourceFile);
      return sourceFileIndex >= key.FileIndex;
    }

    private static FSharpSearchFilterKey TryGetKey(IDeclaredElement declaredElement, IFcsProjectProvider fsProjectProvider)
    {
      if (declaredElement is not IFSharpDeclaredElement fsDeclaredElement) return null;

      var typeElement = fsDeclaredElement as ITypeElement
                        ?? fsDeclaredElement.GetContainingType();

      if (typeElement is null) return null;

      var sourceFiles = typeElement.GetSourceFiles();
      if (sourceFiles.IsEmpty) return null;

      if (declaredElement is ITypePrivateMember)
        return new FSharpSearchFilterKey(IsTypePrivateMember: true, fsDeclaredElement.Module, sourceFiles, FileIndex: -1);

      var fileIndex = sourceFiles.Select(fsProjectProvider.GetFileIndex).Min();

      return new FSharpSearchFilterKey(IsTypePrivateMember: false, fsDeclaredElement.Module, sourceFiles, fileIndex);
    }

    public object TryGetKey(IDeclaredElement declaredElement) =>
      TryGetKey(declaredElement, fsProjectProvider);

    public bool CanContainReferences(IPsiSourceFile sourceFile, object key)
    {
      if (!sourceFile.LanguageType.Is<FSharpProjectFileType>()) return true;

      var fsSearchFilterKey = (FSharpSearchFilterKey)key;

      if (fsSearchFilterKey.SourceFiles.Contains(sourceFile)) return true;
      if (fsSearchFilterKey.IsTypePrivateMember) return false;
      if (!sourceFile.PsiModule.Equals(fsSearchFilterKey.PsiModule)) return true;

      var sourceFileIndex = fsProjectProvider.GetFileIndex(sourceFile);
      return sourceFileIndex >= fsSearchFilterKey.FileIndex;
    }
  }
}
