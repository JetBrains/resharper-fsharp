using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2.Declarations
{
  internal class FSharpNamesCacheBuilder : ICacheBuilder
  {
    private readonly Stack<FSharpTypeInfo> myTypesStack = new Stack<FSharpTypeInfo>();
    public readonly Dictionary<string, FSharpTypeInfo> Types = new Dictionary<string, FSharpTypeInfo>();

    public void CreateProjectFilePart(ProjectFilePart projectFilePart)
    {
    }

    public void StartTypePart([NotNull] FSharpTypeInfo typeInfo)
    {
      myTypesStack.Push(typeInfo);

      var typeClrName = typeInfo.ClrName;
      if (typeClrName != null && !Types.ContainsKey(typeClrName))
        Types.Add(typeInfo.ClrName, typeInfo);
    }

    public void StartPart(Part part)
    {
      Assertion.Assert(part is NamespacePart, "part is NamespacePart");
      myTypesStack.Push(null);
    }

    public void EndPart()
    {
      myTypesStack.Pop();
    }

    public void AddDeclaredMemberName(string name)
    {
    }

    public void AddImplicitMemberName(string name)
    {
    }

    public string Intern(string str)
    {
      throw new System.NotImplementedException();
    }
  }
}