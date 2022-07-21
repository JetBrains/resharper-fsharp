using System.Collections.Generic;
using System.Linq;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl.Special;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public class FSharpGenerativeProvidedProperty : FSharpGenerativeProvidedMember<ProvidedPropertyInfo>, IProperty
  {
    public FSharpGenerativeProvidedProperty(ProvidedPropertyInfo info, ITypeElement containingType) : base(info,
      containingType)
    {
    }

    public override DeclaredElementType GetElementType() => CLRDeclaredElementType.PROPERTY;

    public InvocableSignature GetSignature(ISubstitution substitution) => new(this, substitution);

    public IEnumerable<IParametersOwnerDeclaration> GetParametersOwnerDeclarations() =>
      EmptyList<IParametersOwnerDeclaration>.Instance;

    public IList<IParameter> Parameters => Info
      .GetIndexParameters()
      .Select(t => (IParameter)new FSharpGenerativeProvidedParameter(t, this))
      .ToList();

    public IType Type => Info.PropertyType.MapType(Module);
    public IType ReturnType => Type;
    public ReferenceKind ReturnKind => ReferenceKind.VALUE;

    public string GetDefaultPropertyMetadataName() => ShortName;

    public IAccessor Getter => new ImplicitAccessor(this, AccessorKind.GETTER);
    public IAccessor Setter => new ImplicitAccessor(this, AccessorKind.SETTER);
    public bool IsDefault => Info.Name == StandardMemberNames.DefaultIndexerName;
    public bool IsRequired => false;
    public bool IsReadable => Info.CanRead;
    public bool IsWritable => Info.CanWrite;
    public bool IsAuto => true;
  }
}
