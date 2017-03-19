using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.ReSharper.Psi.Impl.Special;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement
{
  /// <summary>
  /// Field in record or in discriminated union. Compiled as property.
  /// </summary>
  internal class FSharpFieldProperty : FSharpTypeMember<FSharpFieldDeclaration>, IProperty
  {
    public FSharpFieldProperty([NotNull] IFSharpFieldDeclaration declaration) : base(declaration)
    {
      // todo: check if this is called after set resolved symbols stage
      var fieldSymbol = declaration.GetFSharpSymbol() as FSharpField;
      if (fieldSymbol == null)
      {
        IsWritable = false;
        ReturnType = TypeFactory.CreateUnknownType(Module);
        return;
      }

      var psiModule = declaration.GetPsiModule();
      IsWritable = fieldSymbol.IsMutable;
      ReturnType = FSharpElementsUtil.GetType(fieldSymbol.FieldType, declaration, psiModule) ??
                   TypeFactory.CreateUnknownType(psiModule);
    }

    public override DeclaredElementType GetElementType()
    {
      return CLRDeclaredElementType.PROPERTY;
    }

    public bool IsExplicitImplementation => false;
    public IList<IExplicitImplementation> ExplicitImplementations => EmptyList<IExplicitImplementation>.Instance;
    public bool CanBeImplicitImplementation => false;

    public InvocableSignature GetSignature(ISubstitution substitution)
    {
      return new InvocableSignature(this, substitution);
    }

    public IEnumerable<IParametersOwnerDeclaration> GetParametersOwnerDeclarations()
    {
      return EmptyList<IParametersOwnerDeclaration>.Instance;
    }

    public IList<IParameter> Parameters => EmptyList<IParameter>.Instance;
    public IType ReturnType { get; }
    public bool IsRefReturn => false;
    public IType Type => ReturnType;

    public string GetDefaultPropertyMetadataName()
    {
      return ShortName;
    }

    public bool IsReadable => true;
    public bool IsWritable { get; }
    public IAccessor Getter => new ImplicitAccessor(this, AccessorKind.GETTER);
    public IAccessor Setter => IsWritable ? new ImplicitAccessor(this, AccessorKind.SETTER) : null;

    public bool IsAuto => false;
    public bool IsDefault => false;
  }
}