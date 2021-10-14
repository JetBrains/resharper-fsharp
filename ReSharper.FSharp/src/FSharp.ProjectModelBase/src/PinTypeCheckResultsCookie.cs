using System;
using FSharp.Compiler.CodeAnalysis;
using JetBrains.Diagnostics;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Plugins.FSharp
{
  public class PinTypeCheckResultsCookie : IDisposable
  {
    private readonly IDisposable myProhibitTypeCheckCookie;

    public PinTypeCheckResultsCookie(FSharpParseFileResults parseResults, FSharpCheckFileResults checkResults,
      bool prohibitTypeCheck)
    {
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
    }
  }
}
