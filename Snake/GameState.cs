using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Snake
{
    public class GameState
    {
        public int Rows { get; }
        public int Cols { get; }
        public GridValue[,] Grid { get; }
        public Direction Dir { get; private set; }
        public int Score { get; private set; }
        public bool GameOver{ get; private set; }

        private readonly LinkedList<Direction> dirChanges = new();
        private readonly LinkedList<Position> snakePostions = new();
        private readonly Random random = new();

        public GameState(int rows, int cols)
        {
            Rows = rows;
            Cols = cols;
            Grid = new GridValue[rows, cols];
            Dir = Direction.Right;

            AddSnake();
            AddFood();
        }

        private void AddSnake()
        {
            int r = Rows / 2;
            int initSnakeLength = Cols * 7 / 10;
            for(int c = 1; c <= initSnakeLength; c++)
            {
                Grid[r, c] = GridValue.Snake;
                snakePostions.AddFirst(new Position(r, c));
            }
        }

        private IEnumerable<Position> EmptyPositions()
        {
            for(int r=0; r < Rows; r++)
            {
                for(int c=0; c< Cols; c++)
                {
                    if (Grid[r,c] == GridValue.Empty)
                    {
                        yield return new Position(r, c);
                    }
                }

            }
        }

        private void AddFood()
        {
            List<Position> empty = new List<Position>(EmptyPositions());

            if (empty.Count <= 0)
            {
                return;
            }

            Position pos = empty[random.Next(empty.Count)];
            Grid[pos.Row, pos.Col] = GridValue.Food;
        }

        public Position HeadPosition()
        {
            return snakePostions.First.Value;
        }

        public Position TailPosition()
        {
            return snakePostions.Last.Value;
        }

        public IEnumerable<Position> SnakePositions()
        {
            return snakePostions;
        }

        public void AddHead(Position pos)
        {
            snakePostions.AddFirst(pos);
            Grid[pos.Row, pos.Col] = GridValue.Snake;
        }

        public void RemoveTail()
        {
            Position tail = snakePostions.Last.Value;
            Grid[tail.Row, tail.Col] = GridValue.Empty;
            snakePostions.RemoveLast();
        }

        private Direction GetLastDirection()
        {
            if(dirChanges.Count <= 0)
            {
                return Dir;
            }
            else
            {
                return dirChanges.Last.Value;
            }
        }

        private bool CanChangesDirection(Direction newDir)
        {
            if(dirChanges.Count >= 2)
            {
                return false;
            }
            else
            {
                Direction lastDir = GetLastDirection();
                return newDir != lastDir && newDir != lastDir.Opposite();
            }
        }

        public void ChangeDirection(Direction dir)
        {
            if (CanChangesDirection(dir))
            {
                dirChanges.AddLast(dir);
            }
        }

        private bool OutsideGrid(Position pos)
        {
            return pos.Row < 0 || pos.Row >= Rows || pos.Col < 0 || pos.Col >= Cols;
        }

        private GridValue WillHit(Position newHeadPos)
        {
            if (OutsideGrid(newHeadPos))
            {
                return GridValue.Outside;
            }

            if (newHeadPos == TailPosition())
            {
                return GridValue.Empty;
            }

            return Grid[newHeadPos.Row, newHeadPos.Col];
        }

        public void Move()
        {
            if(dirChanges.Count > 0)
            {
                Dir = dirChanges.First.Value;
                dirChanges.RemoveFirst();
            }

            Position newHeadPos = HeadPosition().Translate(Dir);
            GridValue hit = WillHit(newHeadPos);

            if (hit == GridValue.Outside || hit == GridValue.Snake)
            {
                GameOver = true;
            }
            else if (hit == GridValue.Empty)
            {
                RemoveTail();
                AddHead(newHeadPos);
            }
            else if (hit == GridValue.Food)
            {
                AddHead(newHeadPos);
                Score++;
                AddFood();
            }
        }
    }
}
