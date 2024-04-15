using System.Windows;

namespace snek
{
    public class SnakeSegment
    {
        public Point Position { get; set; }

        public SnakeSegment(Point position)
        {
            Position = position;
        }
    }
}
