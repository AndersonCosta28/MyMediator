using System.Runtime.CompilerServices;

namespace Mert1s.MyMediator;

internal sealed class ReferenceTupleComparer : IEqualityComparer<(Type, Type)>
{
    public static ReferenceTupleComparer Instance { get; } = new ReferenceTupleComparer();

    public bool Equals((Type, Type) x, (Type, Type) y) => ReferenceEquals(x.Item1, y.Item1) && ReferenceEquals(x.Item2, y.Item2);

    public int GetHashCode((Type, Type) obj)
    {
        // Use RuntimeHelpers.GetHashCode if available; otherwise combine metadata tokens as a fallback
        try
        {
            var h1 = RuntimeHelpers.GetHashCode(obj.Item1);
            var h2 = RuntimeHelpers.GetHashCode(obj.Item2);
            return System.HashCode.Combine(h1, h2);
        }
        catch
        {
            var t1 = obj.Item1;
            var t2 = obj.Item2;
            return System.HashCode.Combine(t1.MetadataToken, t2.MetadataToken);
        }
    }
}
