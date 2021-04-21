module Module

open System

do
    use c1 = new Class1()
    c1.Dispose()
    (c1 :> IDisposable).Dispose()
    
    use c2 = new Class2()
    c2.Dispose()
    (c2 :> IDisposable).Dispose()

    use c3 = new Class3()
    c3.Dispose()
    (c3 :> IDisposable).Dispose()
