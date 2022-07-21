using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public class FSharpGenerativeGenerativeProvidedMethod : FSharpGenerativeProvidedMethodBase<ProvidedMethodInfo>, IMethod
  {
    public FSharpGenerativeGenerativeProvidedMethod(ProvidedMethodInfo info, ITypeElement containingType) : base(info, containingType)
    {
    }

    public IList<ITypeParameter> TypeParameters => EmptyList<ITypeParameter>.InstanceList;
    public override DeclaredElementType GetElementType() => CLRDeclaredElementType.METHOD;
    public override IType ReturnType => Info.ReturnType.MapType(Module);
    public override bool IsAbstract => Info.IsAbstract;
    public override bool IsStatic => Info.IsStatic;
    public bool IsExtensionMethod => false;
    public bool IsAsync => false;
    public bool IsVarArg => false;
  }
}
