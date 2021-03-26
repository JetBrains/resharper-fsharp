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

    private readonly IAssemblyReader myDefaultReader;

    protected AssemblyReaderShimBase(Lifetime lifetime, ChangeManager changeManager, bool isEnabled) 
      : base(lifetime, changeManager)
    {
      IsEnabled = isEnabled;
      if (!isEnabled)
        return;

      myDefaultReader = AssemblyReader;
      AssemblyReader = this;
      lifetime.OnTermination(() => AssemblyReader = myDefaultReader);
    }

    protected virtual ILModuleReader GetModuleReader(FileSystemPath path, ILReaderOptions readerOptions) =>
      myDefaultReader.GetILModuleReader(path.FullPath, readerOptions);

    public ILModuleReader GetILModuleReader(string filename, ILReaderOptions readerOptions)
    {
      var path = FileSystemPath.TryParse(filename);
      return !path.IsEmpty
        ? GetModuleReader(path, readerOptions)
        : myDefaultReader.GetILModuleReader(filename, readerOptions);
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
