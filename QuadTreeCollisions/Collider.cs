using Microsoft.Xna.Framework;

namespace QuadTreeCollisions;

public struct Collider
{
    public Vector2 Pos;
    public Vector2 Size;
    public int Id;
    private static int NextId;

    public Collider(Vector2 pos, Vector2 size)
    {
        Pos = pos;
        Size = size;
        Id = NextId++;
    }
    
    public Collider(float x, float y, float width, float height)
    {
        Pos = new Vector2(x, y);
        Size = new Vector2(width, height);
        Id = NextId++;
    }

    public bool Contains(Rectangle value) => Pos.X <= value.X && value.X + value.Width <= Pos.X + Size.X && Pos.Y <= value.Y && value.Y + value.Height <= Pos.Y + Size.Y;
    public bool Contains(Collider value) => Pos.X <= value.Pos.X && value.Pos.X + value.Size.X <= Pos.X + Size.X && Pos.Y <= value.Pos.Y && value.Pos.Y + value.Size.Y <= Pos.Y + Size.Y;

    public override int GetHashCode() => (((17 * 23 + Pos.X.GetHashCode()) * 23 + Pos.Y.GetHashCode()) * 23 + Size.X.GetHashCode()) * 23 + Size.Y.GetHashCode();
    public bool Intersects(Rectangle value) => value.Left < Pos.X + Size.X && Pos.X < value.Right && value.Top < Pos.Y + Size.Y && Pos.Y < value.Bottom;
    public bool Intersects(Collider value) => value.Pos.X < Pos.X + Size.X && Pos.X < value.Pos.X + value.Size.X && value.Pos.Y < Pos.Y + Size.Y && Pos.Y < value.Pos.Y + value.Size.Y;
}