module Module

type T1() =
    inherit Class()

    override this.M1() = ()
    override this.M2() = ()


type T2() =
    inherit B()

    override this.M1() = ()
    override this.M2() = ()
    override this.M3() = ()
    override this.M4() = ()
