using System;
using System.Diagnostics;
using JetBrains.Diagnostics;

namespace JetBrains.ReSharper.Plugins.FSharp
{
  public class ProhibitTypeCheckCookie : IDisposable
  {
    private readonly bool myAcquiredByThisInstance;

    [ThreadStatic] private static bool IsAcquired;

    private ProhibitTypeCheckCookie(bool acquire) =>
      myAcquiredByThisInstance = acquire;

    /// Prohibits type checking on the current thread.
    public static IDisposable Create()
    {
      if (IsAcquired)
        return new ProhibitTypeCheckCookie(false);

      IsAcquired = true;
      return new ProhibitTypeCheckCookie(true);
    }

    public void Dispose()
    {
      if (!myAcquiredByThisInstance)
        return;

      IsAcquired = false;
    }

    [Conditional("JET_MODE_ASSERT")]
    public static void AssertTypeCheckIsAllowed() => Assertion.Assert(!IsAcquired);
  }
}
