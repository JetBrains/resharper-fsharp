using System.Collections.Generic;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Common.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpAttributeSet : IAttributesSet
  {
    private readonly IList<FSharpAttribute> myAttrs;
    private readonly IPsiModule myModule;

    public FSharpAttributeSet(IList<FSharpAttribute> attrs, IPsiModule module)
    {
      myAttrs = attrs;
      myModule = module;
    }

    public IList<IAttributeInstance> GetAttributeInstances(bool inherit) =>
      FSharpAttributeInstance.GetAttributeInstances(myAttrs, myModule);

    public IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName, bool inherit) =>
      FSharpAttributeInstance.GetAttributeInstances(myAttrs.GetAttributes(clrName), myModule);

    public bool HasAttributeInstance(IClrTypeName clrName, bool inherit) =>
      myAttrs.HasAttributeInstance(clrName);
  }
}