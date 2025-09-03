using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Checker;
using JetBrains.ReSharper.Psi;
using JetBrains.Threading;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  public class FcsModuleCapturedInfo
  {
    private readonly FcsFileCapturedInfo[] myFileResolvedSymbols;
    private readonly JetFastSemiReenterableRWLock myLock = new();

    public bool IsScript { get; }
    [CanBeNull] public readonly FcsProject FcsProject;

    public static readonly FcsModuleCapturedInfo Empty = new(null);

    public FcsModuleCapturedInfo([CanBeNull] FcsProject fcsProject, bool isScript = false)
    {
      var filesCount = fcsProject?.ParsingOptions.SourceFiles.Length ?? (isScript ? 1 : 0);
      myFileResolvedSymbols = new FcsFileCapturedInfo[filesCount];

      FcsProject = fcsProject;
      IsScript = isScript;
    }

    private bool TryGetFileIndex(IPsiSourceFile sourceFile, out int index)
    {
      if (FcsProject != null)
        return FcsProject.FileIndices.TryGetValue(sourceFile.GetLocation(), out index);

      if (IsScript)
      {
        index = 0;
        return true;
      }

      index = 0;
      return false;
    }

    public void Invalidate(IPsiSourceFile sourceFile)
    {
      using (myLock.UsingWriteLock())
      {
        if (!TryGetFileIndex(sourceFile, out var fileIndex))
          return;

        var filesCount = myFileResolvedSymbols.Length;
        for (var i = fileIndex; i < filesCount; i++)
          myFileResolvedSymbols[i] = null;
      }
    }

    private FcsFileCapturedInfo TryGetResolvedSymbols(int fileIndex)
    {
      using (myLock.UsingReadLock())
        return myFileResolvedSymbols[fileIndex];
    }

    public IFcsFileCapturedInfo GetResolvedSymbols(IPsiSourceFile sourceFile)
    {
      if (!TryGetFileIndex(sourceFile, out var fileIndex))
        return EmptyFcsFileCapturedInfo.Instance;

      var fileResolvedSymbols = TryGetResolvedSymbols(fileIndex);
      if (fileResolvedSymbols != null)
        return fileResolvedSymbols;

      using (myLock.UsingWriteLock())
      {
        fileResolvedSymbols = TryGetResolvedSymbols(fileIndex);
        if (fileResolvedSymbols != null)
          return fileResolvedSymbols;

        fileResolvedSymbols = new FcsFileCapturedInfo(sourceFile);
        myFileResolvedSymbols[fileIndex] = fileResolvedSymbols;
      }

      return fileResolvedSymbols;
    }

    public void Invalidate()
    {
      if (FcsProject == null)
        return;

      using (myLock.UsingWriteLock())
        for (var i = 0; i < myFileResolvedSymbols.Length; i++)
          myFileResolvedSymbols[i] = null;
    }
  }
}
