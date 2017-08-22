using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Impl.Special;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.dataStructures;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal abstract class FSharpConstructorBase<TDeclaration> : FSharpTypeMember<TDeclaration>, IConstructor
    where TDeclaration : FSharpDeclarationBase, IFSharpDeclaration, IAccessRightsOwnerDeclaration,
    IModifiersOwnerDeclaration
  {
    protected FSharpConstructorBase([NotNull] ITypeMemberDeclaration declaration,
      [NotNull] FSharpMemberOrFunctionOrValue mfv) : base(declaration)
    {
      ReturnType = Module.GetPredefinedType().Void;

      var ctorParams = new FrugalLocalList<IParameter>();
      foreach (var paramsGroup in mfv.CurriedParameterGroups)
      foreach (var param in paramsGroup)
        ctorParams.Add(new Parameter(this, ctorParams.Count, FSharpTypesUtil.GetParameterKind(param),
          FSharpTypesUtil.GetType(param.Type, declaration, Module), param.DisplayName));

      Parameters = ctorParams.Count == 1 && ctorParams[0].Type.IsUnit(Module)
        ? EmptyList<IParameter>.InstanceList
        : ctorParams.ToList();
    }

    public override DeclaredElementType GetElementType()
    {
      return CLRDeclaredElementType.CONSTRUCTOR;
    }

    public IType ReturnType { get; }
    public bool IsRefReturn => false;

    public InvocableSignature GetSignature(ISubstitution substitution)
    {
      return new InvocableSignature(this, substitution);
    }

    public IEnumerable<IParametersOwnerDeclaration> GetParametersOwnerDeclarations()
    {
      return EmptyList<IParametersOwnerDeclaration>.Instance;
    }

    public IList<IParameter> Parameters { get; }
    public override string ShortName => GetContainingType()?.ShortName ?? SharedImplUtil.MISSING_DECLARATION_NAME;

    public bool IsPredefined => false;
    public bool IsIterator => false;
    public IAttributesSet ReturnTypeAttributes => EmptyAttributesSet.Instance;
    public bool IsDefault => false;
    public bool IsParameterless => Parameters.IsEmpty();
    public abstract bool IsImplicit { get; }
  }
}