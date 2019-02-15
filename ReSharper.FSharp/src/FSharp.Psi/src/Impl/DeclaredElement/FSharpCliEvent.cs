using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl.Special;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpCliEvent<TDeclaration> : FSharpMemberBase<TDeclaration>, IEvent
    where TDeclaration : FSharpDeclarationBase, IFSharpDeclaration, IAccessRightsOwnerDeclaration,
    IModifiersOwnerDeclaration
  {
    public FSharpCliEvent([NotNull] ITypeMemberDeclaration declaration, FSharpMemberOrFunctionOrValue mfv)
      : base(declaration, mfv)
    {
      try
      {
        ReturnType = FSharpTypesUtil.GetType(mfv.FullType, declaration, Module) ??
                     TypeFactory.CreateUnknownType(Module);
      }
      catch (ErrorLogger.ReportedError e)
      {
        ReturnType = TypeFactory.CreateUnknownType(Module);
      }
    }

    public override IType ReturnType { get; }

    public override DeclaredElementType GetElementType() => CLRDeclaredElementType.EVENT;

    public IAccessor Adder => new ImplicitAccessor(this, AccessorKind.ADDER);
    public IAccessor Remover => new ImplicitAccessor(this, AccessorKind.REMOVER);
    public IAccessor Raiser => null;

    public bool IsFieldLikeEvent => false;
    public IType Type => ReturnType;
  }
}
