using System.Collections.Generic;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Common.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
{
  internal class FSharpAttributeSet : IAttributesSet
  {
    public readonly IList<FSharpAttribute> Attrs;
    public readonly IPsiModule Module;

    public FSharpAttributeSet(IList<FSharpAttribute> attrs, IPsiModule module)
    {
      Attrs = attrs;
      Module = module;
    }

    public IList<IAttributeInstance> GetAttributeInstances(bool inherit) =>
      Attrs.ToAttributeInstances(Module);

    public IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName, bool inherit) =>
      Attrs.GetAttributes(clrName).ToAttributeInstances(Module);

    public bool HasAttributeInstance(IClrTypeName clrName, bool inherit) =>
      Attrs.HasAttributeInstance(clrName);
  }
}
