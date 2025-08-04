using System;
using System.IO;
using FSharp.Compiler.IO;
using JetBrains.Application.changes;
using JetBrains.Lifetimes;
using JetBrains.Util;
using JetBrains.Util.Logging;
using Microsoft.FSharp.Core;
using static FSharp.Compiler.IO.FileSystemAutoOpens;

namespace JetBrains.ReSharper.Plugins.FSharp
{
  public abstract class FileSystemShimChangeProvider : DelegatingFileSystemShim
  {
    protected FileSystemShimChangeProvider(Lifetime lifetime, ChangeManager changeManager) : base(lifetime) =>
      changeManager.Changed.Advise(lifetime, Execute);

    public virtual void Execute(ChangeEventArgs changeEventArgs)
    {
    }
  }

  public class DelegatingFileSystemShim : DefaultFileSystem
  {
    private readonly ILogger myLogger = Logger.GetLogger<DelegatingFileSystemShim>();
    private readonly IFileSystem myFileSystem;

    public DelegatingFileSystemShim(Lifetime lifetime)
    {
      myFileSystem = FileSystem;
      FileSystem = this;
      lifetime.OnTermination(() => FileSystem = myFileSystem);
    }

    public virtual bool ExistsFile(VirtualFileSystemPath path) =>
      myFileSystem is DelegatingFileSystemShim shim
        ? shim.ExistsFile(path)
        : myFileSystem.FileExistsShim(path.FullPath);

    public virtual DateTime GetLastWriteTime(VirtualFileSystemPath path) =>
      myFileSystem is DelegatingFileSystemShim shim
        ? shim.GetLastWriteTime(path)
        : myFileSystem.GetLastWriteTimeShim(path.FullPath);

    public virtual bool IsStableFile(VirtualFileSystemPath path) =>
      myFileSystem is DelegatingFileSystemShim shim
        ? shim.IsStableFile(path)
        : myFileSystem.IsStableFileHeuristic(path.FullPath);

    public virtual Stream ReadFile(VirtualFileSystemPath path, bool useMemoryMappedFile, bool shouldShadowCopy) =>
      myFileSystem is DelegatingFileSystemShim shim
        ? shim.ReadFile(path, useMemoryMappedFile, shouldShadowCopy)
        : myFileSystem.OpenFileForReadShim(path.FullPath, useMemoryMappedFile, shouldShadowCopy);

    public override Stream OpenFileForReadShim(string filePath, FSharpOption<bool> useMemoryMappedFile,
      FSharpOption<bool> shouldShadowCopy)
    {
      // todo: don't set values, fix in FCS
      var memoryMappedFile = useMemoryMappedFile?.Value ?? false;
      var shadowCopy = shouldShadowCopy?.Value ?? false;

      var path = VirtualFileSystemPath.TryParse(filePath, InteractionContext.SolutionContext);
      var stream = path.IsEmpty
        ? myFileSystem.OpenFileForReadShim(filePath, useMemoryMappedFile, shouldShadowCopy)
        : ReadFile(path, memoryMappedFile, shadowCopy);

      myLogger.Trace("Read file: {0}", filePath);
      return stream;
    }

    public override DateTime GetLastWriteTimeShim(string fileName)
    {
      var path = VirtualFileSystemPath.TryParse(fileName, InteractionContext.SolutionContext);
      var lastWriteTime = path.IsEmpty
        ? myFileSystem.GetLastWriteTimeShim(fileName)
        : GetLastWriteTime(path);

      myLogger.Trace("Last write: {0}: {1}", fileName, lastWriteTime);
      return lastWriteTime;
    }

    public override bool FileExistsShim(string fileName)
    {
      var path = VirtualFileSystemPath.TryParse(fileName, InteractionContext.SolutionContext);
      var exists = path.IsEmpty
        ? myFileSystem.FileExistsShim(fileName)
        : ExistsFile(path);

      myLogger.Trace("Exists: {0}: {1}", fileName, exists);
      return exists;
    }

    public override bool IsStableFileHeuristic(string fileName)
    {
      var path = VirtualFileSystemPath.TryParse(fileName, InteractionContext.SolutionContext);
      var isStablePath = path.IsEmpty
        ? myFileSystem.IsStableFileHeuristic(fileName)
        : IsStableFile(path);
      myLogger.Trace("Exists: {0}: {1}", fileName, isStablePath);
      return base.IsStableFileHeuristic(fileName);
    }
  }
}
