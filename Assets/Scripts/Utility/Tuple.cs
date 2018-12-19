using System;

namespace DaggerfallWorkshop.Utility
{
    /// <summary>
    /// A tuple/pair class because the version of C# that Unity uses doesn't have this yet.
    /// </summary>
    /// <typeparam name="T1">First/left type</typeparam>
    /// <typeparam name="T2">Second/right type</typeparam>
    [Serializable]
    public class Tuple<T1, T2> : IEquatable <Tuple<T1, T2>>
    {
        public T1 First;
        public T2 Second;

        public Tuple(T1 a, T2 b)
        {
            First = a;
            Second = b;
        }

        public static Tuple<T1, T2> Make(T1 a, T2 b)
        {
            return new Tuple<T1, T2>(a, b);
        }

        public override string ToString()
        {
            return string.Format("First: {0}, Second: {1}", First, Second);
        }

        public override int GetHashCode()
        {
            return First.GetHashCode() ^ Second.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            return Equals((Tuple<T1, T2>)obj);
        }

        public bool Equals(Tuple<T1, T2> other)
        {
            return other.First.Equals(First) && other.Second.Equals(Second);
        }
    }
}
