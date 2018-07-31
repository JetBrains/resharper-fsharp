using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated
{
  public class FSharpGeneratedConstructorFromFields : FSharpGeneratedConstructor
  {
    public FSharpGeneratedConstructorFromFields([NotNull] TypePart typePart,
      IList<ITypeOwner> fields) : base(typePart)
    {
      var parameters = new IParameter[fields.Count];
      for (var i = 0; i < fields.Count; i++)
        parameters[i] = new FSharpGeneratedConstructorParameter(this, fields[i]);

      Parameters = parameters;
    }

    public override IList<IParameter> Parameters { get; }
  }

  public abstract class FSharpGeneratedConstructor : FSharpGeneratedFunctionBase, IConstructor
  {
    [NotNull] protected readonly TypePart TypePart;

    protected FSharpGeneratedConstructor(TypePart typePart) =>
      TypePart = typePart;

    public override string ShortName => StandardMemberNames.Constructor;

    public override DeclaredElementType GetElementType() =>
      CLRDeclaredElementType.CONSTRUCTOR;

    protected override IClrDeclaredElement ContainingElement => TypePart.TypeElement;
    public override ITypeElement GetContainingType() => (ITypeElement) ContainingElement;
    public override ITypeMember GetContainingTypeMember() => (ITypeMember) ContainingElement;

    public override IType ReturnType => PredefinedType.Void;


    public bool IsDefault => false;
    public bool IsParameterless => false;
    public bool IsImplicit => true;
  }
}
