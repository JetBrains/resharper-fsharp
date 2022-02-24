module Module

type R =
    { ///A1
      ///A2
      Field1: int;

      ///A1
      ///A2  
      mutable Field2: int;

      ///A1
      ///A2
      [<Foo>] Field3: int }
