using JetBrains.ReSharper.Plugins.FSharp.Common.Checker;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Threading;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  public class FSharpModuleResolvedSymbols : IFSharpModuleResolvedSymbols
  {
    private readonly FSharpFileResolvedSymbols[] myFileResolvedSymbols;

    public IPsiModule PsiModule { get; }
    public FSharpCheckerService CheckerService { get; }
    public IFSharpProjectOptionsProvider ProjectOptionsProvider { get; }

    private readonly JetFastSemiReenterableRWLock myLock = new JetFastSemiReenterableRWLock();

    public FSharpModuleResolvedSymbols(IPsiModule psiModule, int filesCount, FSharpCheckerService checkerService,
      IFSharpProjectOptionsProvider projectOptionsProvider)
    {
      myFileResolvedSymbols = new FSharpFileResolvedSymbols[filesCount];

      PsiModule = psiModule;
      CheckerService = checkerService;
      ProjectOptionsProvider = projectOptionsProvider;
    }

    public void Invalidate(IPsiSourceFile sourceFile)
    {
      using (myLock.UsingWriteLock())
      {
        var fileIndex = ProjectOptionsProvider.GetFileIndex(sourceFile);
        if (fileIndex == -1)
          return;

        var filesCount = myFileResolvedSymbols.Length;
        for (var i = fileIndex; i < filesCount; i++)
          myFileResolvedSymbols[i] = null;
      }
    }

    private FSharpFileResolvedSymbols TryGetResolvedSymbols(int fileIndex)
    {
      using (myLock.UsingReadLock())
        return myFileResolvedSymbols[fileIndex];
    }

    public IFSharpFileResolvedSymbols GetResolvedSymbols(IPsiSourceFile sourceFile)
    {
      var fileIndex = ProjectOptionsProvider.GetFileIndex(sourceFile);
      var fileResolvedSymbols = TryGetResolvedSymbols(fileIndex);
      if (fileResolvedSymbols != null)
        return fileResolvedSymbols;

      using (myLock.UsingWriteLock())
      {
        fileResolvedSymbols = TryGetResolvedSymbols(fileIndex);
        if (fileResolvedSymbols != null)
          return fileResolvedSymbols;

        fileResolvedSymbols = new FSharpFileResolvedSymbols(sourceFile, CheckerService);
        myFileResolvedSymbols[fileIndex] = fileResolvedSymbols;
      }

      return fileResolvedSymbols;
    }

    public void Invalidate()
    {
      using (myLock.UsingWriteLock())
        for (var i = 0; i < myFileResolvedSymbols.Length; i++)
          myFileResolvedSymbols[i] = null;
    }
  }
}
