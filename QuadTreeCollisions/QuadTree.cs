using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace QuadTreeCollisions;

public struct Quad
{
    private const int PreferredLen = 10;
    private const int MaxLen = PreferredLen * 2;
    private const int MaxLevel = 3;
    private readonly Collider[] containedColliders = new Collider[MaxLen];
    private int containedColliderCount = 0;
    private readonly int level;
    private Rectangle rectangle;
    private Quad[] children = null;
    
    public Quad(Rectangle newRectangle, int level)
    {
        rectangle = newRectangle;
        this.level = level;
    }

    public void Add(Collider collider)
    {
        bool hasChildren = children is not null;
        
        if (hasChildren)
        {
            // Already has children, try to add the rectangle to those children instead.
            for (var i = 0; i < 4; i++)
            {
                if (!collider.Intersects(children[i].rectangle)) continue;
                
                children[i].Add(collider);
            }

            return;
        }

        // No children, either this node needs to split or it can contain the rectangle.
        if (level == MaxLevel || containedColliderCount <= PreferredLen)
        {
            // Hold this rectangle.
            if (containedColliderCount < MaxLen)
            {
                containedColliders[containedColliderCount] = collider;
                containedColliderCount++;
            }
        }
        else // This node has no children, isn't at max level, and is at full capacity, so try to split it.
        {
            // Split, move this node's contained rectangles into the new children.
            children = new Quad[4];
            int childLevel = level + 1;
            int childWidth = rectangle.Width / 2;
            int childHeight = rectangle.Height / 2;
            children[0] = new Quad(new Rectangle(rectangle.X, rectangle.Y, childWidth, childHeight), childLevel);
            children[1] = new Quad(new Rectangle(rectangle.X + childWidth, rectangle.Y, childWidth, childHeight), childLevel);
            children[2] = new Quad(new Rectangle(rectangle.X, rectangle.Y + childHeight, childWidth, childHeight), childLevel);
            children[3] = new Quad(new Rectangle(rectangle.X + childWidth, rectangle.Y + childHeight, childWidth, childHeight), childLevel);

            for (int i = containedColliderCount - 1; i >= 0; i--)
            {
                for (var j = 0; j < 4; j++)
                {
                    if (!containedColliders[i].Intersects(children[j].rectangle)) continue;
                
                    children[j].Add(containedColliders[i]);
                }
            }

            containedColliderCount = 0;

            for (var i = 0; i < 4; i++)
            {
                if (!collider.Intersects(children[i].rectangle)) continue;
                
                children[i].Add(collider);
                return;
            }
        }
    }

    public void GetPotentialCollisions(HashSet<Collider> list, Collider collider)
    {
        if (children is null) return;
        
        for (var i = 0; i < 4; i++)
        {
            if (!collider.Intersects(children[i].rectangle)) continue;
                
            children[i].GetPotentialCollisions(list, collider);
        }

        AddIntersectingChildren(list, collider);
    }

    private void AddIntersectingChildren(HashSet<Collider> list, Collider collider)
    {
        if (children is null) return;

        for (var i = 0; i < 4; i++) 
        {
            if (!collider.Intersects(children[i].rectangle)) continue;
            
            for (var j = 0; j < children[i].containedColliderCount; j++)
            {
                list.Add(children[i].containedColliders[j]);
            }
            
            children[i].AddIntersectingChildren(list, collider);
        }
    }

    public void Clear()
    {
        containedColliderCount = 0;

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

    public QuadTree(int x, int y, int width, int height)
    {
        root = new Quad(new Rectangle(x, y, width, height), 0);
    }
    
    public void Add(Collider collider)
    {
        root.Add(collider);
    }

    public void GetPotentialCollisions(HashSet<Collider> list, Collider collider)
    {
        root.GetPotentialCollisions(list, collider);
    }

    public void Clear()
    {
        root.Clear();
    }
}