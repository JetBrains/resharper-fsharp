using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Common.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  public class FSharpMethodParameter : IParameter
  {
    private const string OptionalTypeName = "System.Runtime.InteropServices.OptionalAttribute";

    private const string DefaultParameterValueTypeName =
      "System.Runtime.InteropServices.DefaultParameterValueAttribute";

    private readonly IParametersOwner myParametersOwner;
    private readonly int myParameterIndex;

    public FSharpMethodParameter(FSharpParameter fsParam, [NotNull] IParametersOwner parametersOwner,
      int parameterIndex, ParameterKind kind, IType type, string name)
    {
      FSharpSymbol = fsParam;
      Type = type;
      Kind = kind;
      ShortName = name;
      myParametersOwner = parametersOwner;
      myParameterIndex = parameterIndex;
    }

    public IPsiServices GetPsiServices() => myParametersOwner.GetPsiServices();
    public IList<IDeclaration> GetDeclarations() => EmptyList<IDeclaration>.InstanceList;
    public IList<IDeclaration> GetDeclarationsIn(IPsiSourceFile sourceFile) => EmptyList<IDeclaration>.InstanceList;
    public DeclaredElementType GetElementType() => CLRDeclaredElementType.PARAMETER;
    public XmlNode GetXMLDoc(bool inherit) => null;
    public XmlNode GetXMLDescriptionSummary(bool inherit) => null;
    public bool IsValid() => myParametersOwner.IsValid();
    public bool IsSynthetic() => false;
    public HybridCollection<IPsiSourceFile> GetSourceFiles() => HybridCollection<IPsiSourceFile>.Empty;
    public bool HasDeclarationsIn(IPsiSourceFile sourceFile) => false;

    public string ShortName { get; }
    public bool CaseSensitiveName => true;
    public PsiLanguageType PresentationLanguage => FSharpLanguage.Instance;
    public ITypeElement GetContainingType() => myParametersOwner.GetContainingType();
    public ITypeMember GetContainingTypeMember() => (ITypeMember) myParametersOwner;

    public IPsiModule Module => myParametersOwner.Module;
    public ISubstitution IdSubstitution => EmptySubstitution.INSTANCE;
    public FSharpParameter FSharpSymbol { get; }
    public IType Type { get; }

    public bool HasAttributeInstance(IClrTypeName clrName, bool inherit) =>
      FSharpSymbol.Attributes.HasAttributeInstance(clrName.FullName);

    public IList<IAttributeInstance> GetAttributeInstances(bool inherit) =>
      FSharpAttributeInstance.GetAttributeInstances(FSharpSymbol.Attributes, Module);

    public IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName, bool inherit) =>
      FSharpAttributeInstance.GetAttributeInstances(FSharpSymbol.Attributes.GetAttributes(clrName.FullName), Module);

    public DefaultValue GetDefaultValue()
    {
      try
      {
        // todo: implement DefaultValue in FCS
        var defaultValueAttr = FSharpSymbol.Attributes
          .FirstOrDefault(a => a.GetClrName() == DefaultParameterValueTypeName)
          ?.ConstructorArguments.FirstOrDefault();
        return defaultValueAttr == null
          ? new DefaultValue(Type)
          : new DefaultValue(new ConstantValue(defaultValueAttr.Item2, type: null));
      }
      // todo: change exception in FCS
      catch (Exception)
      {
        return DefaultValue.BAD_VALUE;
      }
    }

    public ParameterKind Kind { get; }
    public bool IsParameterArray => FSharpSymbol.IsParamArrayArg;
    public bool IsValueVariable => false;

    // todo: implement IsCliOptional in FCS
    public bool IsOptional =>
      FSharpSymbol.Attributes.HasAttributeInstance(OptionalTypeName);

    public bool IsVarArg => false;
    public IParametersOwner ContainingParametersOwner => myParametersOwner;

    public override bool Equals(object obj)
    {
      if (!(obj is FSharpMethodParameter parameter)) return false;

      return myParametersOwner.Equals(parameter.myParametersOwner) &&
             myParameterIndex == parameter.myParameterIndex;
    }

    public override int GetHashCode()
    {
      unchecked
      {
        return 197 * myParametersOwner.GetHashCode() + 47 * myParameterIndex;
      }
    }
  }
}