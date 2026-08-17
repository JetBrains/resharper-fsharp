// ${ABSENT_ITEM:ToString()}
module Module

type A() =
    override this.Equals(o) = false
    override this.ToString() = ""
    override this.{caret}
