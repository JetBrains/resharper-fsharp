using System;
using FSharp.Compiler.CodeAnalysis;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Resources.Shell;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Plugins.FSharp
{
  // todo: limit to the UI thread? add an assert? may need to invalidate other files in future as well
  public class PinTypeCheckResultsCookie : IDisposable
  {
    private readonly IPsiSourceFile mySourceFile;
    private readonly IDisposable myProhibitTypeCheckCookie;

    public PinTypeCheckResultsCookie(IPsiSourceFile sourceFile, FSharpParseFileResults parseResults,
      FSharpCheckFileResults checkResults, bool prohibitTypeCheck)
    {
      mySourceFile = sourceFile;
      PinnedResults = Tuple.Create(parseResults, checkResults);
      if (prohibitTypeCheck)
        myProhibitTypeCheckCookie = ProhibitTypeCheckCookie.Create();
    }

    [field: ThreadStatic]
    public static FSharpOption<Tuple<FSharpParseFileResults, FSharpCheckFileResults>> PinnedResults
    {
      get;
      private set;
    }

    public void Dispose()
    {
      Assertion.Assert(PinnedResults != null, "PinnedResults == null");

      PinnedResults = null;
      myProhibitTypeCheckCookie?.Dispose();

      using var cookie = WriteLockCookie.Create(true);
      mySourceFile.GetPsiServices().Files.IncrementModificationTimestamp(null);
    }
  }
}
