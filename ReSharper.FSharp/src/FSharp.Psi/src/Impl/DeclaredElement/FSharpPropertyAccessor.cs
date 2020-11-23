using System.Collections.Generic;
using FSharp.Compiler.SourceCodeServices;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Impl.Special;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  public class FSharpPropertyAccessor : SimpleAccessorBase, IFSharpTypeParametersOwner
  {
    private readonly FSharpMemberOrFunctionOrValue myMfv;

    public FSharpPropertyAccessor(FSharpMemberOrFunctionOrValue mfv, IOverridableMember owner)
      : base(owner, mfv.IsPropertyGetterMethod ? AccessorKind.GETTER : AccessorKind.SETTER)
    {
      myMfv = mfv;
    }

    public override IList<IParameter> Parameters => this.GetParameters(myMfv);
    public override AccessRights GetAccessRights() => myMfv.GetAccessRights();

    public string SourceName => SharedImplUtil.MISSING_DECLARATION_NAME;
    public IList<ITypeParameter> AllTypeParameters => GetContainingType().GetAllTypeParametersReversed();
  }
}
