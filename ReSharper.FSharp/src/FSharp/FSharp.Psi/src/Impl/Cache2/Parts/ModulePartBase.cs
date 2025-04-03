using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Compiled;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2.ExtensionMethods;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;
using Microsoft.FSharp.Collections;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal abstract class ModulePartBase<T> : FSharpTypePart<T>, IModulePart
    where T : class, IModuleDeclaration
  {
    private SourceExtensionMemberInfo[] FSharpExtensionMemberInfos { get; } = EmptyArray<SourceExtensionMemberInfo>.Instance;

    public string[] ValueNames { get; }
    public string[] FunctionNames { get; }
    public string[] LiteralNames { get; }
    public string[] ActivePatternNames { get; }
    public string[] ActivePatternCaseNames { get; }

    protected ModulePartBase([NotNull] T declaration, [NotNull] string shortName, MemberDecoration memberDecoration,
      [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, shortName, memberDecoration, 0, cacheBuilder)
    {
      var extensionMemberInfos = new LocalList<SourceExtensionMemberInfo>();

      foreach (var fsDecl in EnumerateExtensionMembers(declaration))
      {
        // todo: use candidate type
        var sourceName = fsDecl.SourceName;
        var extensionMemberInfo = new SourceExtensionMemberInfo(TypeDescriptor.ANY, fsDecl.GetTreeStartOffset(), sourceName, FSharpExtensionMemberKind.INSTANCE, this);
        extensionMemberInfos.Add(extensionMemberInfo);
      }

      FSharpExtensionMemberInfos = extensionMemberInfos.ToArray();

      var valueNames = new LocalList<string>();
      var functionNames = new LocalList<string>();
      var literalNames = new LocalList<string>();
      var activePatternNames = new LocalList<string>();
      var activePatternCaseNames = new LocalList<string>();
      
      foreach (var moduleMember in declaration.MembersEnumerable)
      {
        if (moduleMember is not ILetBindingsDeclaration letBindings) continue;

        foreach (var binding in letBindings.BindingsEnumerable)
        {
          if (binding.HeadPattern is not { } headPattern) continue;

          foreach (var pat in headPattern.NestedPatterns)
          {
            // todo: complex patterns
            if (pat is not IReferencePat refPat || refPat.GetAccessRights() != AccessRights.PUBLIC) continue;

            var name = refPat.SourceName;
            if (refPat.NameIdentifier is IActivePatternId activePatternId)
            {
              activePatternNames.Add(name);
              foreach (var activePatternCase in activePatternId.CasesEnumerable)
              {
                if (activePatternCase is IActivePatternCaseName {Identifier.Name: var caseName })
                  activePatternCaseNames.Add(caseName);
              }
            }
            else
            {
              if (binding.ParametersDeclarationsEnumerable.IsEmpty())
              {
                valueNames.Add(name);
                if (binding.IsLiteral)
                  literalNames.Add(name);
              }
              else
              {
                functionNames.Add(name);
              }
            }
          }
        }
      }

      ValueNames = valueNames.ToArray();
      FunctionNames = functionNames.ToArray();
      LiteralNames = literalNames.ToArray();
      ActivePatternNames = activePatternNames.ToArray();
      ActivePatternCaseNames = activePatternCaseNames.ToArray();
    }

    private static IEnumerable<IFSharpDeclaration> EnumerateExtensionMembers(T declaration)
    {
      foreach (var moduleMember in declaration.MembersEnumerable)
      {
        if (moduleMember is not ITypeDeclarationGroup typeDeclGroup)
          continue;

        foreach (var typeDecl in typeDeclGroup.TypeDeclarations)
        {
          if (typeDecl is not ITypeExtensionDeclaration { IsTypePartDeclaration: false } typeExtensionDecl)
            continue;

          foreach (var memberDecl in typeExtensionDecl.MemberDeclarations)
          {
            if (memberDecl is not IFSharpDeclaration fsDecl)
              continue;

            yield return fsDecl;
          }
        }
      }
    }

    protected override void Write(IWriter writer)
    {
      base.Write(writer);

      writer.WriteOftenSmallPositiveInt(FSharpExtensionMemberInfos.Length);
      foreach (var info in FSharpExtensionMemberInfos)
        info.Write(writer);

      writer.WriteStringArray(ValueNames);
      writer.WriteStringArray(FunctionNames);
      writer.WriteStringArray(LiteralNames);
      writer.WriteStringArray(ActivePatternNames);
      writer.WriteStringArray(ActivePatternCaseNames);
    }

    protected ModulePartBase(IReader reader) : base(reader)
    {
      var extensionMemberCount = reader.ReadOftenSmallPositiveInt();
      if (extensionMemberCount != 0)
      {
        var methods = new SourceExtensionMemberInfo[extensionMemberCount];
        for (var i = 0; i < extensionMemberCount; i++)
          methods[i] = new SourceExtensionMemberInfo(reader, FSharpExtensionMemberKind.INSTANCE, this);
        FSharpExtensionMemberInfos = methods;
      }

      ValueNames = reader.ReadStringArray();
      FunctionNames = reader.ReadStringArray();
      LiteralNames = reader.ReadStringArray();
      ActivePatternNames = reader.ReadStringArray();
      ActivePatternCaseNames = reader.ReadStringArray();
    }

    public override ITypeMember FindExtensionMember(SourceExtensionMemberInfo info)
    {
      if (FSharpExtensionMemberInfos.Length > 0 && GetDeclaration() is { } decl)
        foreach (var fsDecl in EnumerateExtensionMembers(decl))
          if (fsDecl.GetTreeStartOffset() == info.StartOffset)
            return fsDecl.DeclaredElement as ITypeMember;

      return base.FindExtensionMember(info);
    }

    public override TypeElement CreateTypeElement() =>
      new FSharpModule(this);

    public IEnumerable<ITypeMember> GetTypeMembers() =>
      GetDeclaration() is { } declaration
        ? declaration.MemberDeclarations.Select(d => d.DeclaredElement).WhereNotNull()
        : EmptyList<ITypeMember>.Instance;

    public IEnumerable<IDeclaredType> GetSuperTypes() => new[] {GetBaseClassType()};
    public IDeclaredType GetBaseClassType() => GetPsiModule().GetPredefinedType().Object;

    public MemberPresenceFlag GetMemberPresenceFlag() => MemberPresenceFlag.NONE;

    public override SourceExtensionMemberInfo[] ExtensionMemberInfos =>
      ArrayModule.Append(CSharpExtensionMemberInfos, FSharpExtensionMemberInfos);

    public override MemberDecoration Modifiers
    {
      get
      {
        var modifiers = base.Modifiers;
        modifiers.IsAbstract = true;
        modifiers.IsSealed = true;
        modifiers.IsStatic = true;

        return modifiers;
      }
    }

    public override IDeclaration GetTypeParameterDeclaration(int index) => throw new InvalidOperationException();
    public override string GetTypeParameterName(int index) => throw new InvalidOperationException();
    public override TypeParameterVariance GetTypeParameterVariance(int index) => throw new InvalidOperationException();
    public override IEnumerable<IType> GetTypeParameterSuperTypes(int index) => throw new InvalidOperationException();
    public override bool IsNullableContextEnabledForTypeParameter(int index) => throw new InvalidOperationException();


    public override TypeParameterConstraintFlags GetTypeParameterConstraintFlags(int index) =>
      throw new InvalidOperationException();

    public virtual bool IsAnonymous => false;

    public ITypeElement AssociatedTypeElement =>
      GetDeclaration() is INestedModuleDeclaration moduleDeclaration
        ? ((ITypeDeclaration) moduleDeclaration.GetAssociatedTypeDeclaration(out _))?.DeclaredElement
        : null;
  }
}
