using System.Collections.Generic;

namespace OtterStore
{
    /// <summary>
    /// Helps check if two objects are the same for the OtterStore.
    ///
    /// Why not just Equals? 
    /// In the store, we want to compare objects to notify subscribers data has changed.
    ///     Normally, Equals() is fine. Compares by Reference if it's a reference type, or value if it's a value type.
    ///
    /// However, if the user uses C# class records as an element of their store, Equals() treats it as a value type.
    ///     We can make it work like a reference via ReferenceEquals(). For value types (e.g. int) ReferenceEquals()
    ///     would box the values-so ReferenceEquals(1,1) = false.
    ///
    /// If the user uses C# struct records as an element of their store, they are treated as value types, it's what they chose.
    ///
    /// The solution is to check if it's a value type via type information and go from there. 
    /// </summary>
    /// <typeparam name="T">Type being compared</typeparam>
    internal class ReferenceEqualityComparer<T>
    {
        private readonly IEqualityComparer<T> comparer = EqualityComparer<T>.Default;
        private readonly bool isValueType = typeof(T).IsValueType;

        public bool CheckEquals(T x, T y)
        {
            if (isValueType)
            {
                // Value types: compare by value (default comparer)
                return comparer.Equals(x, y);
            }
            
            // Reference types (including records): compare by reference
            return ReferenceEquals(x, y);
        }
    }
}