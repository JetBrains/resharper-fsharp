using System;

[AttributeUsage(AttributeTargets.Class)]
public class CustomClassAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class CustomClassAllowMultipleAttribute : Attribute
{
}
