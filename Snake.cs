using System.Collections.Generic;
using System.Windows.Input;
using System.Windows;

namespace snek
{
    public class Snake
    {
        public LinkedList<SnakeSegment> SnakeSegments { get; }
        public Direction CurrentDirection { get; set;  }
        private HashSet<Point> occupiedPositions;
        public bool PickedUpFruit { get; set; }

        public Snake()
        {
            SnakeSegments = new LinkedList<SnakeSegment>();
            SnakeSegments.AddFirst(new SnakeSegment(new Point(0, 5)));
            CurrentDirection = Direction.Right;
            PickedUpFruit = false;
            occupiedPositions = new HashSet<Point>();
        }

        public void Move(int fieldWidth, int fieldHeight, Direction direction)
        {
            CurrentDirection = direction;
            var newPosition = NextHeadPosition(fieldWidth, fieldHeight);
            System.Diagnostics.Debug.WriteLine($"Next position: Width: {newPosition.X}, Height: {newPosition.Y}");
            System.Diagnostics.Debug.WriteLine($"Current direction: {CurrentDirection}");
            SnakeSegments.AddFirst(new SnakeSegment(newPosition));
            occupiedPositions.Add(newPosition);
            var tailPosition = SnakeSegments.Last.Value.Position;
            if (PickedUpFruit)
            {
                PickedUpFruit = false;
            }
            else 
            {
                SnakeSegments.RemoveLast();
            }
            
            occupiedPositions.Remove(tailPosition);
        }

        public void ChangeDirection(Key keyPressed)
        {
            switch (keyPressed)
            {
                case Key.Up:
                    if (CurrentDirection != Direction.Down)
                        CurrentDirection = Direction.Up;
                    break;
                case Key.Down:
                    if (CurrentDirection != Direction.Up)
                        CurrentDirection = Direction.Down;
                    break;
                case Key.Left:
                    if (CurrentDirection != Direction.Right)
                        CurrentDirection = Direction.Left;
                    break;
                case Key.Right:
                    if (CurrentDirection != Direction.Left)
                        CurrentDirection = Direction.Right;
                    break;
            }
        }

        private Point NextHeadPosition(int fieldWidth, int fieldHeight)
        {
            var headPos = SnakeSegments.First.Value.Position;
            switch (CurrentDirection)
            {
                case Direction.Left:
                    return new Point((headPos.X - 1) >= 0 ? (headPos.X - 1) : fieldWidth - 1, headPos.Y);
                case Direction.Right:
                    return new Point((headPos.X + 1) % fieldWidth, headPos.Y);
                case Direction.Up:
                    return new Point(headPos.X, (headPos.Y - 1) >= 0 ? (headPos.Y - 1) : fieldHeight - 1);
                case Direction.Down:
                    return new Point(headPos.X, (headPos.Y + 1) % fieldHeight);
                default:
                    return headPos;
            }
        }     
    }
}
