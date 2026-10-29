using System.ComponentModel;

namespace EditorBrowsableMembers
{
    [EditorBrowsable(EditorBrowsableState.Always)]
    public class Always { }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public class Advanced { }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class Never { }

    public class Unmarked { }

    public class Class
    {
        [EditorBrowsable(EditorBrowsableState.Always)]
        public int AlwaysProp => 1;

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public int AdvancedProp => 1;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public int NeverProp => 1;

        public int UnmarkedProp => 1;

        [EditorBrowsable(EditorBrowsableState.Always)]
        public int AlwaysMethod() => 1;

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public int AdvancedMethod() => 1;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public int NeverMethod() => 1;

        public int UnmarkedMethod() => 1;
    }
}
