using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace QuadTreeCollisions;

public struct Quad
{
    private const int PreferredLen = 10;
    private const int MaxLen = PreferredLen * 2;
    private const int MaxLevel = 3;
    private readonly Rectangle[] containedRectangles = new Rectangle[MaxLen];
    private int containedRectangleCount = 0;
    private readonly int level;
    private Rectangle rectangle;
    private Quad[] children = null;
    
    public Quad(Rectangle newRectangle, int level)
    {
        rectangle = newRectangle;
        this.level = level;
    }

    public void Add(Rectangle newRectangle)
    {
        bool hasChildren = children is not null;
        
        if (hasChildren)
        {
            // Already has children, try to add the rectangle to those children instead.
            for (var i = 0; i < 4; i++)
            {
                if (!children[i].rectangle.Intersects(newRectangle)) continue;
                
                children[i].Add(newRectangle);
            }

            return;
        }

        // No children, either this node needs to split or it can contain the rectangle.
        if (level == MaxLevel || containedRectangleCount <= PreferredLen)
        {
            // Hold this rectangle.
            if (containedRectangleCount < MaxLen)
            {
                containedRectangles[containedRectangleCount] = newRectangle;
                containedRectangleCount++;
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

            for (int i = containedRectangleCount - 1; i >= 0; i--)
            {
                for (var j = 0; j < 4; j++)
                {
                    if (!children[j].rectangle.Intersects(containedRectangles[i])) continue;
                
                    children[j].Add(containedRectangles[i]);
                }
            }

            containedRectangleCount = 0;

            for (var i = 0; i < 4; i++)
            {
                if (!children[i].rectangle.Intersects(newRectangle)) continue;
                
                children[i].Add(newRectangle);
                return;
            }
        }
    }

    public void GetPotentialCollisions(HashSet<Rectangle> list, Rectangle newRectangle)
    {
        if (children is null) return;
        
        for (var i = 0; i < 4; i++)
        {
            if (!children[i].rectangle.Intersects(newRectangle)) continue;
                
            children[i].GetPotentialCollisions(list, newRectangle);
        }

        AddIntersectingChildren(list, newRectangle);
    }

    private void AddIntersectingChildren(HashSet<Rectangle> list, Rectangle newRectangle)
    {
        if (children is null) return;

        for (var i = 0; i < 4; i++) 
        {
            if (!children[i].rectangle.Intersects(newRectangle)) continue;
            
            for (var j = 0; j < children[i].containedRectangleCount; j++)
            {
                list.Add(children[i].containedRectangles[j]);
            }
            
            children[i].AddIntersectingChildren(list, newRectangle);
        }
    }

    public void Clear()
    {
        containedRectangleCount = 0;

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
    
    public void Add(Rectangle rectangle)
    {
        root.Add(rectangle);
    }

    public void GetPotentialCollisions(HashSet<Rectangle> list, Rectangle rectangle)
    {
        root.GetPotentialCollisions(list, rectangle);
    }

    public void Clear()
    {
        root.Clear();
    }
}