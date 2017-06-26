using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Impl.Special;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement.CompilerGenerated
{
  internal class FSharpGeneratedConstructor : FSharpGeneratedMemberBase, IConstructor, IFSharpTypeMember
  {
    internal FSharpGeneratedConstructor([NotNull] Class containingType,
      FSharpFieldProperty[] fields) : base(containingType)
    {
      var parameters = new List<IParameter>(fields.Length);
      for (var i = 0; i < fields.Length; i++)
      {
        var field = fields[i];
        parameters.Add(new Parameter(this, i, ParameterKind.VALUE, field.Type, field.ShortName));
      }
      Parameters = parameters;
    }

    public override DeclaredElementType GetElementType()
    {
      return CLRDeclaredElementType.CONSTRUCTOR;
    }

    public override string ShortName => ".ctor";

    public InvocableSignature GetSignature(ISubstitution substitution)
    {
      return new InvocableSignature(this, substitution);
    }

    public IEnumerable<IParametersOwnerDeclaration> GetParametersOwnerDeclarations()
    {
      return EmptyList<IParametersOwnerDeclaration>.Instance;
    }

    public IList<IParameter> Parameters { get; }

    public IType ReturnType => Module.GetPredefinedType().Void;
    public bool IsRefReturn => false;

    public override MemberHidePolicy HidePolicy => MemberHidePolicy.HIDE_BY_SIGNATURE;
    public bool IsPredefined => false;
    public bool IsIterator => false;
    public IAttributesSet ReturnTypeAttributes => EmptyAttributesSet.Instance;

    public override bool IsVisibleFromFSharp => false;
    public bool IsDefault => false;
    public bool IsParameterless => false;
    public bool IsImplicit => true;
  }
}