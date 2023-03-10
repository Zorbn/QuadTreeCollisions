using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace QuadTreeCollisions;

public struct Quad
{
    public const int PreferredLen = 10;
    public const int MaxLevel = 3;
    private readonly int level;
    private readonly Rectangle rectangle;
    private Quad[] children = null;
    private List<Collider> containedColliders;

    public Quad(Rectangle newRectangle, int level, QuadTree tree)
    {
        containedColliders = tree.GetColliderList();
        rectangle = newRectangle;
        this.level = level;
    }

    public void Remove(Collider collider)
    {
        var wasInChildren = false;
        
        if (children is not null)
        {
            for (var i = 0; i < 4; i++)
            {
                if (!collider.Intersects(children[i].rectangle)) continue;
                
                children[i].Remove(collider);
                wasInChildren = true;
            }
        }

        if (wasInChildren) return;

        for (int i = containedColliders.Count - 1; i >= 0; i--)
        {
            if (containedColliders[i].Id != collider.Id) continue;
            
            containedColliders.RemoveAt(i);
            break;
        }
    }

    public void Add(Collider collider, QuadTree tree)
    {
        bool hasChildren = children is not null;
        
        if (hasChildren)
        {
            // Already has children, try to add the rectangle to those children instead.
            for (var i = 0; i < 4; i++)
            {
                if (!collider.Intersects(children[i].rectangle)) continue;
                
                children[i].Add(collider, tree);
            }

            return;
        }

        // No children, either this node needs to split or it can contain the rectangle.
        if (level == MaxLevel || containedColliders.Count <= PreferredLen)
        {
            // Hold this rectangle.
            containedColliders.Add(collider);
        }
        else // This node has no children, isn't at max level, and is at full capacity, so try to split it.
        {
            // Split, move this node's contained rectangles into the new children.
            int childLevel = level + 1;
            int childWidth = rectangle.Width / 2;
            int childHeight = rectangle.Height / 2;
            children = tree.GetChildrenArray();
            children[0] = new Quad(new Rectangle(rectangle.X, rectangle.Y, childWidth, childHeight), childLevel, tree);
            children[1] = new Quad(new Rectangle(rectangle.X + childWidth, rectangle.Y, childWidth, childHeight), childLevel, tree);
            children[2] = new Quad(new Rectangle(rectangle.X, rectangle.Y + childHeight, childWidth, childHeight), childLevel, tree);
            children[3] = new Quad(new Rectangle(rectangle.X + childWidth, rectangle.Y + childHeight, childWidth, childHeight), childLevel, tree);
            
            foreach (Collider containedCollider in containedColliders)
            {
                for (var j = 0; j < 4; j++)
                {
                    if (!containedCollider.Intersects(children[j].rectangle)) continue;
                    children[j].Add(containedCollider, tree);
                }
            }
            
            containedColliders.Clear();

            for (var i = 0; i < 4; i++)
            {
                if (!collider.Intersects(children[i].rectangle)) continue;
                
                children[i].Add(collider, tree);
            }
        }
    }

    public void GetPotentialCollisions(List<Collider> list, Collider collider, HashSet<int> duplicateCheckSet, bool removeDuplicates)
    {
        if (children is null)
        {
            if (removeDuplicates)
            {
                int count = containedColliders.Count;
                for (var i = 0; i < count; i++)
                {
                    if (duplicateCheckSet.Add(containedColliders[i].Id))
                    {
                        list.Add(containedColliders[i]);
                    }
                }
            }
            else
            {
                list.AddRange(containedColliders);
            }

            return;
        }
        
        for (var i = 0; i < 4; i++)
        {
            if (!collider.Intersects(children[i].rectangle)) continue;
                
            children[i].GetPotentialCollisions(list, collider, duplicateCheckSet, removeDuplicates);
        }

        AddIntersectingChildren(list, collider, duplicateCheckSet, removeDuplicates);
    }

    private void AddIntersectingChildren(List<Collider> list, Collider collider, HashSet<int> duplicateCheckSet, bool removeDuplicates)
    {
        if (children is null) return;

        for (var i = 0; i < 4; i++) 
        {
            if (!collider.Intersects(children[i].rectangle)) continue;

            if (removeDuplicates)
            {
                Quad child = children[i];
                int count = child.containedColliders.Count;
                for (var j = 0; j < count; j++)
                {
                    if (duplicateCheckSet.Add(child.containedColliders[j].Id))
                    {
                        list.Add(child.containedColliders[j]);
                    }
                }
            }
            else
            {
                list.AddRange(children[i].containedColliders);
            }

            children[i].AddIntersectingChildren(list, collider, duplicateCheckSet, removeDuplicates);
        }
    }

    public void Clear()
    {
        containedColliders.Clear();

        if (children is null) return;

        for (var i = 0; i < 4; i++)
        {
            children[i].Clear();
        }

        children = null;
    }

    public void NewColliderList(QuadTree tree)
    {
        containedColliders = tree.GetColliderList();
    }
}

public class QuadTree
{
    private Quad root;
    private readonly HashSet<int> duplicateCheckSet;
    private readonly List<Collider>[] colliderListCache;
    private readonly Quad[][] childrenArrayCache;
    private int usedColliderCacheCount;
    private int usedChildrenCacheCount;

    public QuadTree(int x, int y, int width, int height)
    {
        var cacheCount = (int)Math.Pow(5, Quad.MaxLevel);
        
        colliderListCache = new List<Collider>[cacheCount];
        for (var i = 0; i < cacheCount; i++)
        {
            colliderListCache[i] = new List<Collider>();
            colliderListCache[i].EnsureCapacity(Quad.PreferredLen);
        }
        
        childrenArrayCache = new Quad[cacheCount][];
        for (var i = 0; i < cacheCount; i++)
        {
            childrenArrayCache[i] = new Quad[4];
        }
        
        root = new Quad(new Rectangle(x, y, width, height), 0, this);
        duplicateCheckSet = new HashSet<int>();
    }

    public void Remove(Collider collider)
    {
        root.Remove(collider);
    }

    public void Add(Collider collider)
    {
        root.Add(collider, this);
    }

    public void GetPotentialCollisions(List<Collider> list, Collider collider, bool removeDuplicates)
    {
        list.Clear();
        if (removeDuplicates) duplicateCheckSet.Clear();
        root.GetPotentialCollisions(list, collider, duplicateCheckSet, removeDuplicates);
    }

    public void Clear()
    {
        root.Clear();
        usedColliderCacheCount = 0;
        usedChildrenCacheCount = 0;
        root.NewColliderList(this);
    }

    public List<Collider> GetColliderList()
    {
        return colliderListCache[usedColliderCacheCount++];
    }
    
    public Quad[] GetChildrenArray()
    {
        return childrenArrayCache[usedChildrenCacheCount++];
    }
}