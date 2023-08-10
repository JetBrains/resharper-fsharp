using System;
using System.Collections;

public class Class1
{
  public Class1()
  {
    var s1 = new S1();
    var s2 = new S2();

    var s31 = new S3(1);
    var s32 = new S3();

    var structuralComparable = (IStructuralComparable) s1;
    var structuralEquatable = (IStructuralEquatable) s1;
    var comparable = (IComparable) s1;
    var comparableT = (IComparable<S1>) s1;
    var equatable = (IEquatable<S1>) s1;
  }
}
