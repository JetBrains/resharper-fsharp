using static YamlProviderLibrary;

namespace CSharpLib
{
    public static class Class
    {
        public static void Foo()
        {
            var config = new GeneratedConfig().Level2;
            var items = configInstance.Level1.Level12.Level13;
            var item = items[0].age;
            items[0].age = 3;

            configInstance.Changed += (sender, args) => { };

            funcWithNestedProvidedType(new GeneratedConfig.Level1_Type());
            funcWithNestedProvidedTypeArray(new[] { new GeneratedConfig.Level1_Type() });
            F(new GeneratedConfig.Level1_Type());
        }

        private static void F<T>(T x) where T : new() {}
    }
}
