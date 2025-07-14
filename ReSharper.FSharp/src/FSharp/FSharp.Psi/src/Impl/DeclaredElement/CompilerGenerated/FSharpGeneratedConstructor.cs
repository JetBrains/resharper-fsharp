using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated
{
  public class FSharpGeneratedConstructorFromFields([NotNull] TypePart typePart) : FSharpGeneratedConstructor(typePart)
  {
    public override IList<IParameter> Parameters
    {
      get
      {
        if (TypePart is not IFieldsOwnerPart typePart)
          return EmptyList<IParameter>.Instance;

        var fields = typePart.Fields;
        var result = new IParameter[fields.Count];
        for (var i = 0; i < fields.Count; i++)
          result[i] = new FSharpGeneratedParameter(this, fields[i], false);

        return result;
      }
    }
  }

  public abstract class FSharpGeneratedConstructor(TypePart typePart) : FSharpGeneratedFunctionBase, IConstructor
  {
    [NotNull] protected readonly TypePart TypePart = typePart;

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
    public bool IsValueTypeZeroInit => false;
  }
}
