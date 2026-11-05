using System;

// ReSharper disable once CheckNamespace
public static class Class
{
    public static void Int(int x = 1) { }
    public static void Decimal(decimal x = 4) { }
    public static void NullableDecimal(decimal? x = 4) { }
    public static void DateTimeDefault(DateTime d = default) { }
}
