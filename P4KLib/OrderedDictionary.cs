using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P4KLib
{
    class OrderedDictionary<TKey, TValue> : System.Collections.Specialized.OrderedDictionary
    {


        //
        // Summary:
        //     Gets or sets the value at the specified index.
        //
        // Parameters:
        //   index:
        //     The zero-based index of the value to get or set.
        //
        // Returns:
        //     The value of the item at the specified index.
        //
        // Exceptions:
        //   T:System.NotSupportedException:
        //     The property is being set and the System.Collections.Specialized.OrderedDictionary
        //     collection is read-only.
        //
        //   T:System.ArgumentOutOfRangeException:
        //     index is less than zero.-or- index is equal to or greater than System.Collections.Specialized.OrderedDictionary.Count.
        public new TValue this[int index]
        {
            get => (TValue)base[index];
            set => base[index] = value;
        }


        //
        // Summary:
        //     Gets or sets the value with the specified key.
        //
        // Parameters:
        //   key:
        //     The key of the value to get or set.
        //
        // Returns:
        //     The value associated with the specified key. If the specified key is not found,
        //     attempting to get it returns null, and attempting to set it creates a new element
        //     using the specified key.
        //
        // Exceptions:
        //   T:System.NotSupportedException:
        //     The property is being set and the System.Collections.Specialized.OrderedDictionary
        //     collection is read-only.
#pragma warning disable CS0109 // Member does not hide an inherited member; new keyword is not required
        public new TValue this[TKey key]
#pragma warning restore CS0109 // Member does not hide an inherited member; new keyword is not required
        {
            get => (TValue)base[key];
            set => base[key] = value;
        }


        //
        // Summary:
        //     Determines whether the System.Collections.Specialized.OrderedDictionary collection
        //     contains a specific key.
        //
        // Parameters:
        //   key:
        //     The key to locate in the System.Collections.Specialized.OrderedDictionary collection.
        //
        // Returns:
        //     true if the System.Collections.Specialized.OrderedDictionary collection contains
        //     an element with the specified key; otherwise, false.
        public bool ContainsValue(object key)
        {
            return base.Contains(key);
        }


    }
}
