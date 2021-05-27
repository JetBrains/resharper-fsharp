using JetBrains.Application.changes;
using JetBrains.Lifetimes;
using JetBrains.Util;
using static FSharp.Compiler.AbstractIL.ILBinaryReader;
using static FSharp.Compiler.AbstractIL.ILBinaryReader.Shim;

namespace JetBrains.ReSharper.Plugins.FSharp
{
  public abstract class AssemblyReaderShimBase : FileSystemShimChangeProvider, IAssemblyReader
  {
    public bool IsEnabled { get; }

    protected readonly IAssemblyReader DefaultReader;

    protected AssemblyReaderShimBase(Lifetime lifetime, ChangeManager changeManager, bool isEnabled)
      : base(lifetime, changeManager)
    {
      IsEnabled = isEnabled;
      if (!isEnabled)
        return;

      DefaultReader = AssemblyReader;
      AssemblyReader = this;
      lifetime.OnTermination(() => AssemblyReader = DefaultReader);
    }

    protected virtual ILModuleReader GetModuleReader(FileSystemPath path, ILReaderOptions readerOptions) =>
      DefaultReader.GetILModuleReader(path.FullPath, readerOptions);

    public ILModuleReader GetILModuleReader(string filename, ILReaderOptions readerOptions)
    {
      var path = FileSystemPath.TryParse(filename);
      return !path.IsEmpty
        ? GetModuleReader(path, readerOptions)
        : DefaultReader.GetILModuleReader(filename, readerOptions);
    }
  }

  public abstract class AssemblyReaderShimChangeListenerBase
  {
    protected AssemblyReaderShimChangeListenerBase(Lifetime lifetime, ChangeManager changeManager) =>
      changeManager.Changed2.Advise(lifetime, Execute);

    protected virtual void Execute(ChangeEventArgs obj)
    {
    }
  }
}
