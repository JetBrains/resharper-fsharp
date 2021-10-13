using System;
using FSharp.Compiler.CodeAnalysis;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Plugins.FSharp
{
  public class PinTypeCheckResultsCookie : IDisposable
  {
    public PinTypeCheckResultsCookie(FSharpParseFileResults parseResults, FSharpCheckFileResults checkResults) =>
      PinnedResults = Tuple.Create(parseResults, checkResults);

    [field: ThreadStatic]
    public static FSharpOption<Tuple<FSharpParseFileResults, FSharpCheckFileResults>> PinnedResults { get; private set; }

    public void Dispose() =>
      PinnedResults = null;
  }
}
