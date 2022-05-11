using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl.Special;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public class FSharpProvidedEvent : FSharpProvidedMember<ProvidedEventInfo>, IEvent
  {
    public FSharpProvidedEvent(ProvidedEventInfo info, ITypeElement containingType) : base(info, containingType)
    {
    }

    public override DeclaredElementType GetElementType() => CLRDeclaredElementType.EVENT;
    public IAccessor Adder => new ImplicitAccessor(this, AccessorKind.ADDER);
    public IAccessor Remover => new ImplicitAccessor(this, AccessorKind.REMOVER);
    public IAccessor Raiser => new ImplicitAccessor(this, AccessorKind.RAISER);
    public IType Type => Info.EventHandlerType.MapType(Module);
    public bool IsFieldLikeEvent => false;
  }
}
