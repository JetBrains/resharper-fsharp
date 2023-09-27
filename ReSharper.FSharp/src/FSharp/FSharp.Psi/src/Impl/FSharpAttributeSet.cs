using System.Collections.Generic;
using FSharp.Compiler.Symbols;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;

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

    public IList<IAttributeInstance> GetAttributeInstances(AttributesSource attributesSource) =>
      Attrs.ToAttributeInstances(Module);

    public IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName, AttributesSource attributesSource) =>
      Attrs.GetAttributes(clrName).ToAttributeInstances(Module);

    public bool HasAttributeInstance(IClrTypeName clrName, AttributesSource attributesSource) =>
      Attrs.HasAttributeInstance(clrName);
  }
}
