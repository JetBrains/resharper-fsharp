using System;

namespace CSharpLib
{
    public enum MyEnum : Int16
    {
        A = 1
    }
 
    public class MyEnumEx
    {
        public const MyEnum A = MyEnum.A;
    }

    public class MyAttribute : Attribute
    {
        public MyAttribute(MyEnum myEnum)
        {
        }
    }
}
