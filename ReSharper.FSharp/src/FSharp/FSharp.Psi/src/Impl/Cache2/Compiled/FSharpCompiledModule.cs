using System.Collections.Generic;
using System.Linq;
using FSharp.Compiler.Syntax;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Metadata;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2.ExtensionMethods;
using JetBrains.ReSharper.Psi.Impl.Reflection2;
using JetBrains.Util;
using Microsoft.FSharp.Collections;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Compiled
{
  public class FSharpCompiledModule : FSharpCompiledClassBase, IFSharpModule
  {
    public string[] LiteralNames { get; }
    public string[] ValueNames { get; }
    public string[] FunctionNames { get; }
    public string[] ActivePatternNames { get; }
    public string[] ActivePatternCaseNames { get; }

    private readonly ICompiledExtensionMemberProxy[] myExtensionMemberInfos;

    public override ICollection<ICompiledExtensionMemberProxy> ExtensionMembers =>
      ArrayModule.Append(base.ExtensionMembers.AsArray(), myExtensionMemberInfos);

    public FSharpCompiledModule([NotNull] FSharpCompiledTypeRepresentation.Module repr,
      [NotNull] FSharpMetadataEntity entity, [NotNull] ICompiledEntity parent,
      [NotNull] IReflectionBuilder builder, [NotNull] IMetadataTypeInfo info) : base(entity, parent, builder, info)
    {
      AccessKind = GetModuleMembersAccessKind(info);

      var valueNames = new LocalList<string>();
      var functionNames = new LocalList<string>();
      var literalNames = new LocalList<string>();
      var activePatternNames = new LocalList<string>();
      var activePatternCaseNames = new LocalList<string>();

      var extensionMemberInfos = new LocalList<ICompiledExtensionMemberProxy>();

      foreach (var value in repr.values)
      {
        if (value.IsExtensionMember)
        {
          extensionMemberInfos.Add(new FSharpCompiledExtensionMemberInfo(value, this));
        }
        // ApparentEnclosingTypeReference is set for every member, even for members coming from types inside the module
        else if (value.IsPublic && value.ApparentEnclosingTypeReference == null)
        {
          var name = value.LogicalName;
          if (!name.IsEmpty() && !PrettyNaming.IsIdentifierFirstCharacter(name[0]))
          {
            // todo: operators
            if (!PrettyNaming.IsActivePatternName(name)) continue;

            activePatternNames.Add(name);
            foreach (var activePatternCaseName in name.Split('|'))
            {
              if (!activePatternCaseName.IsEmpty() && activePatternCaseName != "_")
                activePatternCaseNames.Add(activePatternCaseName);
            }
          }
          else
          {
            if (value.IsFunction)
            {
              functionNames.Add(name);
            }
            else
            {
              valueNames.Add(name);
              if (value.IsLiteral)
                literalNames.Add(name);
            }
          }
        }
      }

      myExtensionMemberInfos = extensionMemberInfos.ToArray();

      ValueNames = valueNames.ToArray();
      FunctionNames = functionNames.ToArray();
      LiteralNames = literalNames.ToArray();
      ActivePatternNames = activePatternNames.ToArray();
      ActivePatternCaseNames = activePatternCaseNames.ToArray();
    }

    public override IEnumerable<string> MemberNames
    {
      get
      {
        foreach (var name in ExpressionNames)
          yield return name;
        foreach (var name in PatternNames)
          yield return name;
        foreach (var name in base.MemberNames)
          yield return name;
      }
    }

    public IEnumerable<string> ExpressionNames
    {
      get
      {
        foreach (var name in ValueNames)
          yield return name;
        foreach (var name in FunctionNames)
          yield return name;
        foreach (var name in ActivePatternNames)
          yield return name;
      }
    }

    public IEnumerable<string> PatternNames
    {
      get
      {
        foreach (var name in LiteralNames)
          yield return name;
        foreach (var name in ActivePatternCaseNames)
          yield return name;
      }
    }

    public override ModuleMembersAccessKind AccessKind { get; }

    public bool IsAnonymous =>
      Representation is FSharpCompiledTypeRepresentation.Module module && module.nameKind.IsAnon;

    public bool HasModuleSuffix =>
      Representation is FSharpCompiledTypeRepresentation.Module module && module.nameKind.IsHasModuleSuffix;

    public bool IsAutoOpen => AccessKind == ModuleMembersAccessKind.AutoOpen;
    public bool RequiresQualifiedAccess => AccessKind == ModuleMembersAccessKind.RequiresQualifiedAccess;
    public bool HasAssociatedType => AssociatedTypeElement != null;

    public ITypeElement AssociatedTypeElement => TryGetAssociatedTypeElement(); // todo: cache
    public string QualifiedSourceName => this.GetQualifiedName();

    private static ModuleMembersAccessKind GetModuleMembersAccessKind(IMetadataTypeInfo info)
    {
      if (info.HasCustomAttribute(FSharpPredefinedType.AutoOpenAttrTypeName.FullName))
        return ModuleMembersAccessKind.AutoOpen;

      if (info.HasCustomAttribute(FSharpPredefinedType.RequireQualifiedAccessAttrTypeName.FullName))
        return ModuleMembersAccessKind.RequiresQualifiedAccess;

      return ModuleMembersAccessKind.Normal;
    }

    private ITypeElement TryGetAssociatedTypeElement()
    {
      if (!HasModuleSuffix)
        return null;

      var sourceName = SourceName;

      var symbolScope = Module.GetModuleOnlySymbolScope(true);
      if (GetContainingType() is { } containingType)
        return containingType.NestedTypes.FirstOrDefault(HasSameName);

      var containingNamespace = GetContainingNamespace();
      return containingNamespace.GetNestedTypeElements(symbolScope).FirstOrDefault(HasSameName);

      bool HasSameName(ITypeElement typeElement) =>
        !Equals(typeElement) && typeElement.TypeParametersCount == 0 && typeElement.GetSourceName() == sourceName;
    }
  }
}
