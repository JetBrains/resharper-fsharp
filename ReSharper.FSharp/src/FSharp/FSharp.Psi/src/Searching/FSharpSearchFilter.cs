using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.FSharp.Checker;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Finder;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Searching
{
  [PsiComponent]
  public sealed class FSharpSearchFilter : ISearchFilter
  {
    private readonly IFcsProjectProvider myFsProjectProvider;

    public FSharpSearchFilter(IFcsProjectProvider fsProjectProvider)
    {
      myFsProjectProvider = fsProjectProvider;
    }

    private record FSharpSearchFilterKey(
      bool IsTypePrivateMember,
      IPsiModule PsiModule,
      HybridCollection<IPsiSourceFile> SourceFiles,
      int FileIndex);

    public SearchFilterKind Kind => SearchFilterKind.Cache;

    public bool IsAvailable(SearchPattern pattern) => true;

    public object TryGetKey(IDeclaredElement declaredElement)
    {
      if (declaredElement is not IFSharpDeclaredElement fsDeclaredElement) return null;

      var typeElement = fsDeclaredElement as ITypeElement
                        ?? fsDeclaredElement.GetContainingType();

      if (typeElement is null) return null;

      var sourceFiles = typeElement.GetSourceFiles();
      if (sourceFiles.IsEmpty) return null;

      if (declaredElement is ITypePrivateMember)
        return new FSharpSearchFilterKey(IsTypePrivateMember: true, fsDeclaredElement.Module, sourceFiles, FileIndex: -1);

      var fileIndex = 0;
      foreach (var psiSourceFile in sourceFiles)
      {
        var currentIndex = myFsProjectProvider.GetFileIndex(psiSourceFile);
        if (currentIndex < fileIndex)
          fileIndex = currentIndex;
      }

      if (fileIndex < 0) return null;

      return new FSharpSearchFilterKey(IsTypePrivateMember: false, fsDeclaredElement.Module, sourceFiles, fileIndex);
    }

    public bool CanContainReferences(IPsiSourceFile sourceFile, object key)
    {
      if (!sourceFile.LanguageType.Is<FSharpProjectFileType>()) return true;

      var fsSearchFilterKey = (FSharpSearchFilterKey)key;

      if (fsSearchFilterKey.SourceFiles.Contains(sourceFile)) return true;
      if (fsSearchFilterKey.IsTypePrivateMember) return false;
      if (!sourceFile.PsiModule.Equals(fsSearchFilterKey.PsiModule)) return true;

      var sourceFileIndex = myFsProjectProvider.GetFileIndex(sourceFile);
      return sourceFileIndex >= fsSearchFilterKey.FileIndex;
    }
  }
}