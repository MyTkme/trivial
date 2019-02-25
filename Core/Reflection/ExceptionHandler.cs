﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Trivial.Reflection
{
    /// <summary>
    /// The exception handler instance.
    /// </summary>
    public class ExceptionHandler
    {
        /// <summary>
        /// The item handler.
        /// </summary>
        /// <param name="ex">The exception to test.</param>
        /// <param name="handled">true if handled; otherwise, false.</param>
        /// <returns>The exception need throw; or null, if ignore.</returns>
        public delegate Exception ItemHandler(Exception ex, out bool handled);

        /// <summary>
        /// The item of exception handlers registered.
        /// </summary>
        public class Item
        {
            /// <summary>
            /// Initializes a new instance of the ExceptionHandler.Item class.
            /// </summary>
            /// <param name="type">The exception type.</param>
            /// <param name="handler">The catch handler.</param>
            public Item(Type type, ItemHandler handler)
            {
                Type = type;
                Handler = handler;
            }

            /// <summary>
            /// Gets the exception type.
            /// </summary>
            public Type Type { get; private set; }

            /// <summary>
            /// Gets the catch handler.
            /// </summary>
            public ItemHandler Handler { get; private set; }
        }

        /// <summary>
        /// The item of exception handlers registered.
        /// </summary>
        public class Item<T> : Item, IEquatable<Item>, IEquatable<Item<T>> where T : Exception
        {
            /// <summary>
            /// Initializes a new instance of the ExceptionHandler.Item class.
            /// </summary>
            /// <param name="handler">The catch handler.</param>
            public Item(Func<T, Exception> handler) : base(typeof(T), (Exception ex, out bool handled) =>
            {
                if (ex is T exConverted)
                {
                    handled = true;
                    return handler(exConverted);
                }

                handled = false;
                return ex;
            })
            {
                Handler = handler;
            }

            /// <summary>
            /// Gets the catch handler.
            /// </summary>
            public new Func<T, Exception> Handler { get; private set; }

            /// <summary>
            /// Tests if the given item equals the current one.
            /// </summary>
            /// <param name="other">The given item.</param>
            /// <returns>true if the given item equals the current one; otherwise, false.</returns>
            public bool Equals(Item<T> other)
            {
                return other.Handler == Handler;
            }

            /// <summary>
            /// Tests if the given item equals the current one.
            /// </summary>
            /// <param name="other">The given item.</param>
            /// <returns>true if the given item equals the current one; otherwise, false.</returns>
            public bool Equals(Item other)
            {
                return other is Item<T> item && item.Handler == Handler;
            }
        }

        /// <summary>
        /// The catch handler list.
        /// </summary>
        private IList<Item> list = new List<Item>();

        /// <summary>
        /// Gets a value indicating whether need test all catch handler rather than try-catch logic.
        /// </summary>
        public bool TestAll { get; set; }

        /// <summary>
        /// Gets the count of the catch handler.
        /// </summary>
        public int Count => list.Count;

        /// <summary>
        /// Tests if need throw an exception.
        /// </summary>
        /// <param name="ex">The exception catched.</param>
        /// <returns>The exception needed to throw.</returns>
        public Exception GetException(Exception ex)
        {
            foreach (var item in list)
            {
                var result = item.Handler(ex, out bool handled);
                if (handled && (!TestAll || result != null)) return result;
            }

            return ex;
        }

        /// <summary>
        /// Adds a catch handler.
        /// </summary>
        /// <typeparam name="T">The type of exception to try to catch.</typeparam>
        /// <param name="catchHandler">The handler to return if need throw an exception.</param>
        public void Add<T>(Func<T, Exception> catchHandler) where T : Exception
        {
            var type = typeof(T);
            foreach (var item in list)
            {
                if (item is Item<T> itemConverted && itemConverted.Handler == catchHandler) return;
            }

            list.Add(new Item<T>(catchHandler));
        }

        /// <summary>
        /// Removes a catch handler.
        /// </summary>
        /// <typeparam name="T">The type of exception to try to catch.</typeparam>
        /// <param name="catchHandler">The handler to return if need throw an exception.</param>
        public bool Remove<T>(Func<T, Exception> catchHandler) where T : Exception
        {
            var type = typeof(T);
            var removing = new List<Item>();
            foreach (var item in list)
            {
                if (item is Item<T> itemConverted && itemConverted.Handler == catchHandler) removing.Add(item);
            }

            var count = removing.Count;
            foreach (var item in removing)
            {
                list.Remove(item);
            }

            return count > 0;
        }

        /// <summary>
        /// Removes a catch handler.
        /// </summary>
        /// <typeparam name="T">The type of exception to try to catch.</typeparam>
        public bool Remove<T>() where T : Exception
        {
            var type = typeof(T);
            var removing = new List<Item>();
            foreach (var item in list)
            {
                if (item is Item<T> itemConverted) removing.Add(item);
            }

            var count = removing.Count;
            foreach (var item in removing)
            {
                list.Remove(item);
            }

            return count > 0;
        }

        /// <summary>
        /// Clears all catch handlers.
        /// </summary>
        public void Clear()
        {
            list.Clear();
        }
    }
}