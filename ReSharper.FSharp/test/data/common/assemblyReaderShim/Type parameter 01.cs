public class Class1<T>
{
  public static T StaticField;
}

public class Class2<T1, T2, T3>
{
  public static T1 Field1;
  public static T2 Field2;
  public static T3 Field3;

  public static Class2<T1, T2, T3> StaticField;

  public class Nested<T4, T5>
  {
    public static T1 NestedField1;
    public static T2 NestedField2;
    public static T3 NestedField3;

    public static T4 NestedField4;
    public static T5 NestedField5;

    public static Class2<T1, T2, T3>.Nested<T4, T5> StaticField;
  }
}


namespace Ns1
{
  public class NsClass1<T>
  {
    public static T StaticField;
  }

  namespace Ns2
  {
    public class NsClass2<T1, T2>
    {
      public static NsClass2<T1, T2> StaticField;

      public class Nested<T3, T4, T5>
      {
        public static NsClass2<T1, T2>.Nested<T3, T4, T5> StaticField;
      }
    }
  }
}

public class Class3<T1, T2>
{
  public class Nested1<T3, T4>
  {
    public class Nested2<T5, T6>
    {
      public class Nested3<T7, T8>
      {
        public static Class3<T1, T2>.Nested1<T3, T4>.Nested2<T5, T6>.Nested3<T7, T8> StaticField;
      }
    }
  }
}

public class Class4<T, T>
{
  public static T StaticField;
  public virtual T Property { get; }
}
