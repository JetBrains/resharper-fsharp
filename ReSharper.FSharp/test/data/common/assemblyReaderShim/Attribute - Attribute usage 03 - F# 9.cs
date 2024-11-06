using System;

[AttributeUsage(AttributeTargets.Class)]
public class CustomClassAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Struct)]
public  class CustomStructAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public  class CustomMethodAttribute : Attribute
{
}
