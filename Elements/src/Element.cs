#pragma warning disable CS1591


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace Elements
{
    /// <summary>
    /// An object which is identified 
    /// with a unique identifier and a name.
    /// </summary>
    public abstract partial class Element
    {
        // This partial definition exists only to mark the class as abstract.

        /// <summary>
        /// Control the order of serialization by setting the sort priority >1.
        /// Elements are serialized in descending sort priorty order.
        /// Ex: An Element with sort priority 1 will be serialized before an element
        /// with sort priority 0.
        /// </summary>
        internal virtual int SortPriority => 0;

        protected Dictionary<Guid, Element> _children = new Dictionary<Guid, Element>();

        /// <summary>
        /// Get all children of this element including those
        /// that are not direct descendants.
        /// </summary>
        /// <returns>A dictionary of elements which have this element as an ancestor.</returns>
        public Dictionary<Guid, Element> AllChildren()
        {
            var allChildren = new Dictionary<Guid, Element>();
            foreach (var child in _children)
            {
                var subElements = child.Value.AllChildren();
                foreach (var subElement in subElements)
                {
                    if (!allChildren.ContainsKey(subElement.Key))
                    {
                        allChildren.Add(subElement.Key, subElement.Value);
                    }
                }
                if (!allChildren.ContainsKey(child.Value.Id))
                {
                    allChildren.Add(child.Value.Id, child.Value);
                }
            }
            return allChildren;
        }

        /// <summary>
        /// Update the element's children collection.
        /// </summary>
        internal void UpdateChildren()
        {
            _children.Clear();
            GatherSubElements(this, _children);
        }

        private static HashSet<Type> SkipTypes = new HashSet<Type>
        {
            typeof(Guid),
            typeof(string),
            typeof(int),
            typeof(uint),
            typeof(double),
            typeof(decimal),
            typeof(short),
            typeof(ushort),
            typeof(long),
            typeof(ulong)
        };

        private void GatherSubElements(object obj,
                                       Dictionary<Guid, Element> elements,
                                       bool notify = true)
        {
            if (obj == null)
            {
                return;
            }

            var t = obj.GetType();

            // Ignore value types and strings
            // as they won't have properties that
            // could be elements.
            if (!t.IsClass || SkipTypes.Contains(t))
            {
                return;
            }

            var e = obj as Element;

            t.GetCustomAttribute(typeof(JsonIgnoreAttribute));
            var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                         .Where(p => p.GetCustomAttribute(typeof(JsonIgnoreAttribute)) == null && !SkipTypes.Contains(p.PropertyType));
            foreach (var p in props)
            {
                var pValue = p.GetValue(obj, null);
                if (pValue == null)
                {
                    continue;
                }

                // TODO: This is not as precise as it could be.
                // List of things that are not supported will be
                // iterated.
                var elems = pValue as IList;
                if (elems != null)
                {
                    foreach (var item in elems)
                    {
                        if (typeof(Element).IsAssignableFrom(item.GetType()))
                        {
                            var subElement = (Element)item;
                            if (!elements.ContainsKey(subElement.Id))
                            {
                                elements.Add(subElement.Id, subElement);
                                if (notify)
                                {
                                    subElement.UpdateChildren();
                                }
                            }
                        }
                        else
                        {
                            GatherSubElements(item, elements);
                        }
                    }
                    continue;
                }

                var dict = pValue as IDictionary;
                if (dict != null)
                {
                    foreach (var value in dict.Values)
                    {
                        if (typeof(Element).IsAssignableFrom(value.GetType()))
                        {
                            var subElement = (Element)value;
                            if (!elements.ContainsKey(subElement.Id))
                            {
                                elements.Add(subElement.Id, subElement);
                                if (notify)
                                {
                                    subElement.UpdateChildren();
                                }
                            }
                        }
                        else
                        {
                            GatherSubElements(value, elements);
                        }
                    }
                    continue;
                }

                if (typeof(Element).IsAssignableFrom(pValue.GetType()))
                {
                    var subElement = (Element)pValue;
                    if (!elements.ContainsKey(subElement.Id))
                    {
                        elements.Add(subElement.Id, subElement);
                        if (notify)
                        {
                            subElement.UpdateChildren();
                        }
                    }
                }
            }
        }
    }
}