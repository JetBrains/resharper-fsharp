using System;
using System.Collections.Generic;
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

    public virtual bool ExistsFile(FileSystemPath path) =>
      myFileSystem is DelegatingFileSystemShim shim
        ? shim.ExistsFile(path)
        : myFileSystem.FileExistsShim(path.FullPath);

    public virtual DateTime GetLastWriteTime(FileSystemPath path) =>
      myFileSystem is DelegatingFileSystemShim shim
        ? shim.GetLastWriteTime(path)
        : myFileSystem.GetLastWriteTimeShim(path.FullPath);

    public virtual bool IsStableFile(FileSystemPath path) =>
      myFileSystem is DelegatingFileSystemShim shim
        ? shim.IsStableFile(path)
        : myFileSystem.IsStableFileHeuristic(path.FullPath);

    public virtual string GetDirectoryNameShim(string path) =>
      myFileSystem.GetDirectoryNameShim(path);

    public virtual DateTime GetLastWriteTimeShim(string fileName)
    {
      var path = FileSystemPath.TryParse(fileName);
      var lastWriteTime = path.IsEmpty
        ? myFileSystem.GetLastWriteTimeShim(fileName)
        : GetLastWriteTime(path);

      myLogger.Trace("Last write: {0}: {1}", fileName, lastWriteTime);
      return lastWriteTime;
    }

    public virtual DateTime GetCreationTimeShim(string path) =>
      myFileSystem.GetCreationTimeShim(path);

    public virtual void CopyShim(string src, string dest, bool overwrite) =>
      myFileSystem.CopyShim(src, dest, overwrite);

    public virtual bool FileExistsShim(string fileName)
    {
      var path = FileSystemPath.TryParse(fileName);
      var exists = path.IsEmpty
        ? myFileSystem.FileExistsShim(fileName)
        : ExistsFile(path);

      myLogger.Trace("Exists: {0}: {1}", fileName, exists);
      return exists;
    }

    public virtual void FileDeleteShim(string fileName) => myFileSystem.FileDeleteShim(fileName);
    public virtual DirectoryInfo DirectoryCreateShim(string path) => myFileSystem.DirectoryCreateShim(path);
    public virtual bool DirectoryExistsShim(string path) => myFileSystem.DirectoryExistsShim(path);
    public virtual void DirectoryDeleteShim(string path) => myFileSystem.DirectoryDeleteShim(path);

    public virtual IEnumerable<string> EnumerateFilesShim(string path, string pattern) =>
      myFileSystem.EnumerateFilesShim(path, path);

    public virtual IEnumerable<string> EnumerateDirectoriesShim(string path) =>
      myFileSystem.EnumerateDirectoriesShim(path);

    public virtual ByteMemory OpenFileForReadShim(string filePath, FSharpOption<bool> useMemoryMappedFile, 
      FSharpOption<bool> shouldShadowCopy) =>
      myFileSystem.OpenFileForReadShim(filePath, useMemoryMappedFile, shouldShadowCopy);

    public virtual Stream OpenFileForWriteShim(string filePath, FSharpOption<FileMode> fileMode,
      FSharpOption<FileAccess> fileAccess, FSharpOption<FileShare> fileShare) =>
      myFileSystem.OpenFileForWriteShim(filePath, fileMode, fileAccess, fileShare);

    public virtual string GetFullPathShim(string fileName) => myFileSystem.GetFullPathShim(fileName);

    public virtual string GetFullFilePathInDirectoryShim(string dir, string fileName) =>
      myFileSystem.GetFullFilePathInDirectoryShim(dir, fileName);

    public virtual bool IsPathRootedShim(string path) => myFileSystem.IsPathRootedShim(path);
    public virtual string NormalizePathShim(string path) => myFileSystem.NormalizePathShim(path);
    public virtual bool IsInvalidPathShim(string filename) => myFileSystem.IsInvalidPathShim(filename);
    public virtual string GetTempPathShim() => myFileSystem.GetTempPathShim();
    public virtual bool IsStableFileHeuristic(string fileName) => myFileSystem.IsStableFileHeuristic(fileName);
    public virtual IAssemblyLoader AssemblyLoader => myFileSystem.AssemblyLoader;
  }
}
