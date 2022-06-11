using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public class FSharpGenerativeProvidedConstructor : FSharpGenerativeProvidedMethodBase<ProvidedConstructorInfo>, IConstructor
  {
    public FSharpGenerativeProvidedConstructor(ProvidedConstructorInfo info, [NotNull] ITypeElement containingType)
      : base(info, containingType)
    {
    }

    public override DeclaredElementType GetElementType() => CLRDeclaredElementType.CONSTRUCTOR;
    public override string ShortName => StandardMemberNames.Constructor;
    public override IType ReturnType => TypeFactory.CreateType(ContainingType);
    public bool IsParameterless => Info.GetParameters().IsEmpty();
    public bool IsDefault => IsParameterless;
    public bool IsImplicit => false;
    public bool IsValueTypeZeroInit => false;
  }
}
