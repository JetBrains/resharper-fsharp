public class Class1
{
  public Class1()
  {
    var f = new Module.Foo {Foo = 123};
    int foo = f.Foo;
    f.Foo = 1;
  }
}
