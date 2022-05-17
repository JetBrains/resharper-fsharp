using System.Xml;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Util;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public abstract class FSharpProvidedMember<T> : FSharpGeneratedMemberBase, IOverridableMember,
    ISecondaryDeclaredElement where T : ProvidedMemberInfo
  {
    private readonly ITypeElement myContainingType;
    private IClrDeclaredElement myOriginElement;
    protected T Info { get; }

    protected FSharpProvidedMember(T info, ITypeElement containingType)
    {
      myContainingType = containingType;
      Info = info;
      Module = containingType.Module;
    }

    public override XmlNode GetXMLDoc(bool inherit) => Info.GetXmlDoc(this);
    public override IPsiModule Module { get; }
    public override string XMLDocId => XMLDocUtil.GetTypeMemberXmlDocId(this, ShortName);
    public override ITypeElement GetContainingType() => myContainingType;
    public override ITypeMember GetContainingTypeMember() => ContainingType as ITypeMember;
    public override string ShortName => Info.Name;
    protected override IClrDeclaredElement ContainingElement => myContainingType;
    public override ISubstitution IdSubstitution => EmptySubstitution.INSTANCE;

    public IClrDeclaredElement OriginElement => myOriginElement ??= GetOriginElement();
    public bool IsReadOnly => true;

    private IClrDeclaredElement GetOriginElement()
    {
      var declaringType = Info.DeclaringType;
      while (declaringType.DeclaringType != null)
        declaringType = declaringType.DeclaringType;

      return declaringType.MapType(Module).GetTypeElement().NotNull();
    }
  }
}
