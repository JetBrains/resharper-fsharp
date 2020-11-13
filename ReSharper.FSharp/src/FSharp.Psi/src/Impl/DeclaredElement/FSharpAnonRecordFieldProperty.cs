using FSharp.Compiler.SourceCodeServices;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Resources.Shell;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpAnonRecordFieldProperty : FSharpDeclaredElementBase, IFSharpAnonRecordFieldProperty
  {
    public FSharpSymbolReference Reference { get; }

    public FSharpAnonRecordFieldProperty(FSharpSymbolReference reference) =>
      Reference = reference;

    public FSharpField FcsField => Reference.GetFSharpSymbol() as FSharpField;
    public FSharpAnonRecordTypeDetails AnonType => FcsField.AnonRecordFieldDetails.Item1;

    public override string ShortName => Reference.GetName();

    public override bool IsValid() => Reference.IsEmpty();

    public override ITypeMember GetContainingTypeMember() => null;

    public override ISubstitution IdSubstitution =>
      EmptySubstitution.INSTANCE;

    public override DeclaredElementType GetElementType() =>
      CommonDeclaredElementType.AnonymousTypeProperty;

    public override IPsiModule Module => Reference.GetElement().GetPsiModule();
    public override IPsiServices GetPsiServices() => Module.GetPsiServices();

    public override ITypeElement GetContainingType() => null;

    public IType Type =>
      FcsField is { } field
        ? field.FieldType.MapType(Reference.GetElement())
        : TypeFactory.CreateUnknownType(Module);

    public IFSharpAnonRecordFieldProperty SetName(string newName)
    {
      var referenceOwner = Reference.GetElement();
      using (WriteLockCookie.Create(referenceOwner.IsPhysical()))
        return new FSharpAnonRecordFieldProperty(referenceOwner.SetName(newName).Reference);
    }

    public int Index => FcsField.AnonRecordFieldDetails.Item3;

    public override bool Equals(object obj)
    {
      if (!(obj is FSharpAnonRecordFieldProperty field))
        return false;

      if (Module != field.Module)
        return false;

      return ShortName == field.ShortName && AnonType.CompiledName == field.AnonType.CompiledName;
    }

    public override int GetHashCode() =>
      ShortName.GetHashCode();

    public string SourceName => ShortName;
  }
}
