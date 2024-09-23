module Module

type T() =
    inherit InheritedClass()

    override this.MPublic() = base.MPublic()
    override this.MProtected() = base.MProtected()
    override this.MPrivate() = base.MPrivate()

    override this.NonVirtual() = base.NonVirtual()
    override this.NonVirtualOverriden() = base.NonVirtualOverriden()
