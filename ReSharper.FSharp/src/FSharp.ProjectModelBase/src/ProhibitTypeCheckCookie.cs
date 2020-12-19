using System;
using System.Diagnostics;
using JetBrains.Diagnostics;

namespace JetBrains.ReSharper.Plugins.FSharp
{
  public struct ProhibitTypeCheckCookie : IDisposable
  {
    [ThreadStatic] private static bool IsAcquired;

    /// Prohibits type checking on the current thread.
    public static IDisposable Create()
    {
      AssertTypeCheckIsAllowed();

      IsAcquired = true;
      return new ProhibitTypeCheckCookie();
    }

    public void Dispose() => IsAcquired = false;

    [Conditional("JET_MODE_ASSERT")]
    public static void AssertTypeCheckIsAllowed() =>
      Assertion.Assert(!IsAcquired, "!IsAcquired");
  }
}
