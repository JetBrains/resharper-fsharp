using System;
using System.IO;
using System.Reflection;
using FSharp.Compiler.IO;
using JetBrains.Application.changes;
using JetBrains.Lifetimes;
using JetBrains.Util;
using JetBrains.Util.Logging;
using static FSharp.Compiler.IO.FileSystemAutoOpens;

namespace JetBrains.ReSharper.Plugins.FSharp
{
  public abstract class FileSystemShimChangeProvider : DelegatingFileSystemShim
  {
    protected FileSystemShimChangeProvider(Lifetime lifetime, ChangeManager changeManager) : base(lifetime) =>
      changeManager.Changed2.Advise(lifetime, Execute);

    public abstract void Execute(ChangeEventArgs changeEventArgs);
  }

  public class DelegatingFileSystemShim : IFileSystem
  {
    private readonly ILogger myLogger = Logger.GetLogger<DelegatingFileSystemShim>();
    private readonly IFileSystem myFileSystem;

    public DelegatingFileSystemShim(Lifetime lifetime)
    {
      myFileSystem = FileSystem;
      FileSystem = this;
      lifetime.OnTermination(() => FileSystem = myFileSystem);
    }

    public virtual bool Exists(FileSystemPath path) =>
      myFileSystem is DelegatingFileSystemShim shim
        ? shim.Exists(path)
        : myFileSystem.SafeExists(path.FullPath);

    public virtual DateTime GetLastWriteTime(FileSystemPath path) =>
      myFileSystem is DelegatingFileSystemShim shim
        ? shim.GetLastWriteTime(path)
        : myFileSystem.GetLastWriteTimeShim(path.FullPath);

    public virtual bool IsStableFile(FileSystemPath path) =>
      myFileSystem is DelegatingFileSystemShim shim
        ? shim.IsStableFile(path)
        : myFileSystem.IsStableFileHeuristic(path.FullPath);

    public virtual DateTime GetLastWriteTimeShim(string fileName)
    {
      var path = FileSystemPath.TryParse(fileName);
      var lastWriteTime = path.IsEmpty
        ? myFileSystem.GetLastWriteTimeShim(fileName)
        : GetLastWriteTime(path);

      myLogger.Trace("Last write: {0}: {1}", fileName, lastWriteTime);
      return lastWriteTime;
    }

    public virtual bool SafeExists(string fileName)
    {
      var path = FileSystemPath.TryParse(fileName);
      var exists = path.IsEmpty
        ? myFileSystem.SafeExists(fileName)
        : Exists(path);

      myLogger.Trace("Exists: {0}: {1}", fileName, exists);
      return exists;
    }

    public virtual byte[] ReadAllBytesShim(string fileName) => myFileSystem.ReadAllBytesShim(fileName);
    public virtual Stream FileStreamReadShim(string fileName) => myFileSystem.FileStreamReadShim(fileName);
    public virtual Stream FileStreamCreateShim(string fileName) => myFileSystem.FileStreamCreateShim(fileName);
    public virtual string GetFullPathShim(string fileName) => myFileSystem.GetFullPathShim(fileName);
    public virtual bool IsPathRootedShim(string path) => myFileSystem.IsPathRootedShim(path);
    public virtual bool IsInvalidPathShim(string filename) => myFileSystem.IsInvalidPathShim(filename);
    public virtual string GetTempPathShim() => myFileSystem.GetTempPathShim();
    public virtual void FileDelete(string fileName) => myFileSystem.FileDelete(fileName);
    public virtual Assembly AssemblyLoadFrom(string fileName) => myFileSystem.AssemblyLoadFrom(fileName);
    public virtual Assembly AssemblyLoad(AssemblyName assemblyName) => myFileSystem.AssemblyLoad(assemblyName);
    public virtual bool IsStableFileHeuristic(string fileName) => myFileSystem.IsStableFileHeuristic(fileName);

    public virtual Stream FileStreamWriteExistingShim(string fileName) =>
      myFileSystem.FileStreamWriteExistingShim(fileName);
  }
}
