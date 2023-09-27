using JetBrains.Application.changes;
using JetBrains.Lifetimes;
using JetBrains.Util;
using static FSharp.Compiler.AbstractIL.ILBinaryReader;
using static FSharp.Compiler.AbstractIL.ILBinaryReader.Shim;

namespace JetBrains.ReSharper.Plugins.FSharp
{
  public abstract class AssemblyReaderShimBase : FileSystemShimChangeProvider, IAssemblyReader
  {
    public readonly IAssemblyReader DefaultReader;

    protected AssemblyReaderShimBase(Lifetime lifetime, ChangeManager changeManager)
      : base(lifetime, changeManager)
    {
      DefaultReader = AssemblyReader;
      AssemblyReader = this;
      lifetime.OnTermination(() => AssemblyReader = DefaultReader);
    }

    protected virtual ILModuleReader GetModuleReader(VirtualFileSystemPath path, ILReaderOptions readerOptions) =>
      DefaultReader.GetILModuleReader(path.FullPath, readerOptions);

    public ILModuleReader GetILModuleReader(string filename, ILReaderOptions readerOptions)
    {
      var path = VirtualFileSystemPath.TryParse(filename, InteractionContext.SolutionContext);
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
