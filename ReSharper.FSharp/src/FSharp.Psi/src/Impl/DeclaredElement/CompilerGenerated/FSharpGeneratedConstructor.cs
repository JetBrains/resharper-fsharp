using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Impl.Special;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated
{
  internal class FSharpGeneratedConstructor : FSharpGeneratedMemberBase, IConstructor, IFSharpTypeMember
  {
    internal FSharpGeneratedConstructor([NotNull] Class containingType,
      FSharpFieldProperty[] fields) : base(containingType)
    {
      var parameters = new List<IParameter>(fields.Length);
      var fieldNames = new HashSet<string>();
      foreach (var field in fields)
        fieldNames.Add(field.ShortName);

      for (var i = 0; i < fields.Length; i++)
      {
        var field = fields[i];
        var fieldName = field.ShortName;
        var lowerName = !fieldName.IsEmpty() ? fieldName[0].ToLowerFast() + fieldName.Substring(1) : fieldName;
        var paramName = fieldNames.Contains(lowerName) ? fieldName : lowerName;
        parameters.Add(new Parameter(this, i, ParameterKind.VALUE, field.Type, paramName));
      }
      Parameters = parameters;
    }

    public override DeclaredElementType GetElementType()
    {
      return CLRDeclaredElementType.CONSTRUCTOR;
    }

    public override string ShortName => StandardMemberNames.Constructor;

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