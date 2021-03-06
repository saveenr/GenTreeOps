﻿using System.Collections.Generic;
using System.Linq;

namespace GenTreeOps
{
    public static class Algorithms
    {
        // Delegates
        public delegate IEnumerable<T> EnumerateChildren<T>(T item);

        private struct WalkState<T>
        {
            // this is an internal struct used when traversing the DOM
            // it preserves the state in the non-recursive, stack-based traversal of the DOM

            internal readonly T Node;
            internal readonly bool Entered;

            public WalkState(T node, bool entered)
            {
                this.Node = node;
                this.Entered = entered;
            }
        }

        /// <summary>
        // Walks a Node in a depth-first/pre-order manner without recursion.
        // It returns a series of "events" that indicate one of three things:
        // - whether it has enters into a node
        // - whether it has exited from a node (i.e. it is finished with that container and its children)
        // - caller can control which children get entered via the enum_children method
        /// </summary>
        public static IEnumerable<GenTreeOps.WalkEvent<T>> Walk<T>(T node, EnumerateChildren<T> enum_children)
        {
            var stack = new Stack<WalkState<T>>();

            // put the first item on the stack 
            stack.Push(new WalkState<T>(node,false));

            // As long as something is on the stack, we are not done
            while (stack.Count > 0)
            {
                var cur_item = stack.Pop();

                if (cur_item.Entered == false)
                {
                    var walkevent = WalkEvent<T>.CreateEnterEvent(cur_item.Node);
                    yield return walkevent;

                    stack.Push(new WalkState<T>(cur_item.Node,true));

                    // placing the children on the stack in reverse
                    // order means that the children be walked in 
                    // their "natural order"
                    foreach (var child in Algorithms.efficient_reverse(enum_children(cur_item.Node)))
                    {
                        stack.Push(new WalkState<T>(child,false));
                    }
                }
                else
                {
                    var walkevent = WalkEvent<T>.CreateExitEvent(cur_item.Node);
                    yield return walkevent;
                }
            }
        }

        public static IEnumerable<T> PreOrder<T>(T root, EnumerateChildren<T> enum_children)
        {
            return Algorithms.Walk(root, enum_children).Where(ev => ev.Type == WalkEventType.EventEnter).Select(ev => ev.Node);
        }

        public static IEnumerable<T> PostOrder<T>(T root, EnumerateChildren<T> enum_children)
        {
            return Algorithms.Walk(root, enum_children).Where(ev => ev.Type == WalkEventType.EventExit).Select(ev => ev.Node);
        }

        internal static IEnumerable<T> efficient_reverse<T>(IEnumerable<T> items)
        {
            if (items is IList<T>)
            {
                var item_col = (IList<T>) items;
                for (int i = item_col.Count - 1; i >= 0; i--)
                {
                    yield return item_col[i];
                }
            }
            else
            {
                foreach (var i in items.Reverse())
                {
                    yield return i;
                }
            }
        }

        public static IList<TDest> CopyTree<TSrc, TDest>(
            TSrc src_root_node,
            System.Func<TSrc, IEnumerable<TSrc>> enum_src_children,
            System.Func<TSrc, TDest> create_dest_node,
            System.Action<TDest, TDest> add_dest_child)
        {
            var stack = new Stack<TDest>();
            var dest_nodes = new List<TDest>();

            var walkevents = Algorithms.Walk<TSrc>(src_root_node, input_node => enum_src_children(input_node));
            foreach (var ev in walkevents)
            {
                if (ev.Type == WalkEventType.EventEnter)
                {
                    var new_dst_node = create_dest_node(ev.Node);

                    if (stack.Count > 0)
                    {
                        // if there is node on the stack, then that node is the current node's parent
                        var parent = stack.Peek();
                        add_dest_child(parent, new_dst_node);
                    }
                    else
                    {
                        // if there is nothing on the stack this is node without a parent (a root node)
                    }

                    stack.Push(new_dst_node);
                    dest_nodes.Add(new_dst_node);
                }
                else if (ev.Type == WalkEventType.EventExit)
                {
                    stack.Pop();
                }
            }

            return dest_nodes;
        }
    }
}