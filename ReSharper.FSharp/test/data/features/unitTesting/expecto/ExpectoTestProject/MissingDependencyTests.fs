module ExpectoTestProject.MissingDependencyTests

type TypeWithMissingBaseType() =
    inherit FsCheck.Arbitrary<int>()

    override this.Generator = FsCheck.Gen.constant 1
