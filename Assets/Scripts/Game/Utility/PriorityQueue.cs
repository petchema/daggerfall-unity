using System;
using System.Collections.Generic;
using UnityEngine;

namespace DaggerfallWorkshop.Game.Utility
{
        // Standard PriorityQueue, aka Heap datastructure
        public class PriorityQueue<T> where T : IComparable<T>
        {
            private List<T> data;

            public PriorityQueue()
            {
                this.data = new List<T>();
            }

            public void Enqueue(T item)
            {
                data.Add(item);
                int child_index = data.Count - 1;
                while (child_index > 0)
                {
                    int parent_index = (child_index - 1) / 2;
                    if (data[child_index].CompareTo(data[parent_index]) >= 0) 
                        break;
                    T tmp = data[child_index]; 
                    data[child_index] = data[parent_index]; 
                    data[parent_index] = tmp;
                    child_index = parent_index;
                }
            }

            public T Dequeue()
            {
                if (data.Count == 0)
                    throw new InvalidOperationException("PriorityQueue is empty");

                int last_index = data.Count - 1;
                T front_item = data[0];
                data[0] = data[last_index];
                data.RemoveAt(last_index);

                --last_index;
                int parent_index = 0;
                while (true)
                {
                    int left_child_index = parent_index * 2 + 1;
                    if (left_child_index > last_index) 
                        break;
                    int right_child_index = left_child_index + 1;
                    if (right_child_index <= last_index && data[right_child_index].CompareTo(data[left_child_index]) < 0)
                        left_child_index = right_child_index;
                    if (data[parent_index].CompareTo(data[left_child_index]) <= 0) 
                        break;
                    T tmp = data[parent_index]; 
                    data[parent_index] = data[left_child_index]; 
                    data[left_child_index] = tmp;
                    parent_index = left_child_index;
                }
                return front_item;
            }

            public T Peek()
            {
                if (data.Count == 0)
                    throw new InvalidOperationException("PriorityQueue is empty");

                return data[0];
            }

            public int Count()
            {
                return data.Count;
            }

            public void Clear()
            {
                data.Clear();
            }
        }
}