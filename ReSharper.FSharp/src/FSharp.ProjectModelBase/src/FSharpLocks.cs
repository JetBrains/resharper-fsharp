using System;
using System.Threading;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Threading;

namespace JetBrains.ReSharper.Plugins.FSharp
{
  [ShellComponent]
  public class FSharpLocks
  {
    public static volatile Thread ThreadRequestingWriteLock;

    private IShellLocks ShellLocks { get; }

    public FSharpLocks(IShellLocks shellLocks) =>
      ShellLocks = shellLocks;

    public FSharpReadLockCookie CreateReadLock() =>
      CreateReadLock(ShellLocks);

    public static FSharpReadLockCookie CreateReadLock(IShellLocks locks)
    {
      if (locks.IsReadAccessAllowed())
        // We already have read access from the callee and don't need to transfer the write lock.
        return new FSharpReadLockCookie(null);

      while (!locks.ContentModelLocks.TryAcquireReadLock(50))
        // Request transferring write lock in case it's held by main thread waiting for FCS.
        ThreadRequestingWriteLock = Thread.CurrentThread;

      ThreadRequestingWriteLock = null;
      return new FSharpReadLockCookie(locks.ContentModelLocks);
    }
  }

  public class FSharpReadLockCookie : IDisposable
  {
    [CanBeNull]
    private ContentModelReadWriteLock ReadWriteLock { get; }

    public FSharpReadLockCookie([CanBeNull] ContentModelReadWriteLock readWriteLock) =>
      ReadWriteLock = readWriteLock;

    public void Dispose()
    {
      if (ReadWriteLock == null)
        // The lock object is null when we didn't do anything to get the lock.
        return;

      ReadWriteLock.ReleaseReadLock();

      if (ReadWriteLock.IsWriteAccessAllowed)
        ReadWriteLock.RestoreWriteLock();
    }
  }
}
