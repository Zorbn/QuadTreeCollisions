using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace QuadTreeCollisions;

public struct Quad
{
    public const int PreferredLen = 10;
    public List<Collider> ContainedColliders;
    public const int MaxLevel = 3;
    private readonly int level;
    private readonly Rectangle rectangle;
    private Quad[] children = null;
    
    public Quad(Rectangle newRectangle, int level, QuadTree tree)
    {
        ContainedColliders = tree.GetColliderArray();
        rectangle = newRectangle;
        this.level = level;
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
        if (level == MaxLevel || ContainedColliders.Count <= PreferredLen)
        {
            // Hold this rectangle.
            ContainedColliders.Add(collider);
        }
        else // This node has no children, isn't at max level, and is at full capacity, so try to split it.
        {
            // Split, move this node's contained rectangles into the new children.
            children = new Quad[4];
            int childLevel = level + 1;
            int childWidth = rectangle.Width / 2;
            int childHeight = rectangle.Height / 2;
            children[0] = new Quad(new Rectangle(rectangle.X, rectangle.Y, childWidth, childHeight), childLevel, tree);
            children[1] = new Quad(new Rectangle(rectangle.X + childWidth, rectangle.Y, childWidth, childHeight), childLevel, tree);
            children[2] = new Quad(new Rectangle(rectangle.X, rectangle.Y + childHeight, childWidth, childHeight), childLevel, tree);
            children[3] = new Quad(new Rectangle(rectangle.X + childWidth, rectangle.Y + childHeight, childWidth, childHeight), childLevel, tree);
            
            foreach (Collider containedCollider in ContainedColliders)
            {
                for (var j = 0; j < 4; j++)
                {
                    if (!containedCollider.Intersects(children[j].rectangle)) continue;
                    children[j].Add(containedCollider, tree);
                }
            }
            
            ContainedColliders.Clear();

            for (var i = 0; i < 4; i++)
            {
                if (!collider.Intersects(children[i].rectangle)) continue;
                
                children[i].Add(collider, tree);
                return;
            }
        }
    }

    public void GetPotentialCollisions(List<Collider> list, Collider collider)
    {
        if (children is null) return;
        
        for (var i = 0; i < 4; i++)
        {
            if (!collider.Intersects(children[i].rectangle)) continue;
                
            children[i].GetPotentialCollisions(list, collider);
        }

        AddIntersectingChildren(list, collider);
    }

    private void AddIntersectingChildren(List<Collider> list, Collider collider)
    {
        if (children is null) return;

        for (var i = 0; i < 4; i++) 
        {
            if (!collider.Intersects(children[i].rectangle)) continue;
            
            for (var j = 0; j < children[i].ContainedColliders.Count; j++)
            {
                list.Add(children[i].ContainedColliders[j]);
            }
            
            children[i].AddIntersectingChildren(list, collider);
        }
    }

    public void Clear()
    {
        ContainedColliders.Clear();

        if (children is null) return;

        for (var i = 0; i < 4; i++)
        {
            children[i].Clear();
        }

        children = null;
    }
}

public class QuadTree
{
    private Quad root;
    private readonly List<Collider>[] colliderArrayCache;
    private int colliderArrayCount;

    public QuadTree(int x, int y, int width, int height)
    {
        var colliderCacheCount = (int)Math.Pow(5, Quad.MaxLevel);
        colliderArrayCache = new List<Collider>[colliderCacheCount];
        for (var i = 0; i < colliderCacheCount; i++)
        {
            colliderArrayCache[i] = new List<Collider>();
            colliderArrayCache[i].EnsureCapacity(Quad.PreferredLen);
        }
        
        root = new Quad(new Rectangle(x, y, width, height), 0, this);
    }
    
    public void Add(Collider collider)
    {
        root.Add(collider, this);
    }

    public void GetPotentialCollisions(List<Collider> list, Collider collider)
    {
        root.GetPotentialCollisions(list, collider);
    }

    public void Clear()
    {
        root.Clear();
        colliderArrayCount = 0;
        root.ContainedColliders = GetColliderArray();
    }

    public List<Collider> GetColliderArray()
    {
        return colliderArrayCache[colliderArrayCount++];
    }
}