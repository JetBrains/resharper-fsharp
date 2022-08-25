module M
   
type T() =
    static member M1(a: int) = ()
    static member M2(a: int, b) = ()
    
T.M1(a = 1)
T.M1((a) = 1)
T.M1(((a)) = 1)

T.M1((a = 1))
T.M1(((a = 1)))

T.M1((a) = 1)
T.M1(((a) = 1))

T.M2((a) = 1, b = 2)
T.M2((a = 1), b = 2)
T.M2(((a) = 1), b = 2)
