﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Snake
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly int msGameLoop = 250;
        //private readonly int rows = 50, cols = 50; //huge   50 x 50 grid of 2500 cells
        //private readonly int rows = 32, cols = 32; //large  32 x 32 grid of 1024 cells
        private readonly int rows = 11, cols = 21;   //normal 11 x 21 grid of  231 cells
        private readonly Image[,] gridImages;

        private readonly Dictionary<GridValue, ImageSource> gridValToImage = new()
        {
            { GridValue.Empty, Images.Empty },
            { GridValue.Snake, Images.Body },
            { GridValue.Food, Images.Food }
        };

        private readonly Dictionary<Direction, int> dirToRotation = new Dictionary<Direction, int>()
        {
            {Direction.Up,         0 },
            {Direction.UpRight,    45 },
            {Direction.Right,      90 },
            {Direction.DownRight, 135 },
            {Direction.Down,      180 },
            {Direction.DownLeft,  225 },
            {Direction.Left,      270 },
            {Direction.UpLeft,    315 }
        };

        private GameState gameState;

        private MediaPlayer mediaPlayer;

        public MainWindow()
        {
            InitializeComponent();
            gridImages = SetupGrid();
            gameState = new GameState(rows, cols);
            mediaPlayer = new MediaPlayer();
        }

        private async Task RunGame()
        {
            Draw();
            await ShowCountdown();
            Overlay.Visibility = Visibility.Hidden;
            await GameLoop();
            await ShowGameOver();
            gameState = new GameState(rows, cols);
        }

        private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(Overlay.Visibility == Visibility.Visible && gameState.Mode != GameMode.Paused)
            {
                e.Handled = true;
            }

            if(gameState.Mode == GameMode.NotStarted)
            {
                gameState.Mode = GameMode.Started;
                await RunGame();
                gameState.Mode = GameMode.NotStarted;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if(gameState.Mode == GameMode.NotStarted)
            {
                return;
            }

            if (gameState.Mode == GameMode.Over)
            {
                return;
            }

            if (gameState.Mode == GameMode.Started && e.Key == Key.Space)
            {
                mediaPlayer.Open(new Uri("Assets/intermission.wav", UriKind.Relative));
                mediaPlayer.Play();
                gameState.Mode = GameMode.Paused;
                return;
            }

            if (gameState.Mode == GameMode.Paused && e.Key == Key.Space)
            {
                gameState.Mode = GameMode.Resuming;
                return;
            }

            switch (e.Key)
            {
                case Key.Left:
                case Key.NumPad4:
                    gameState.ChangeDirection(Direction.Left);
                    break;

                case Key.Right:
                case Key.NumPad6:
                    gameState.ChangeDirection(Direction.Right);
                    break;

                case Key.Up:
                case Key.NumPad8:
                    gameState.ChangeDirection(Direction.Up);
                    break;

                case Key.Down:
                case Key.NumPad2:
                    gameState.ChangeDirection(Direction.Down);
                    break;

                case Key.NumPad7:
                    gameState.ChangeDirection(Direction.UpLeft);
                    break;

                case Key.NumPad9:
                    gameState.ChangeDirection(Direction.UpRight);
                    break;

                case Key.NumPad3:
                    gameState.ChangeDirection(Direction.DownRight);
                    break;

                case Key.NumPad1:
                    gameState.ChangeDirection(Direction.DownLeft);
                    break;
            }
        }

        private async Task GameLoop()
        {
            int oldScore = gameState.Score;
            int newScore = 0;
            while (gameState.Mode != GameMode.NotStarted 
                && gameState.Mode != GameMode.Over)
            {
                await Task.Delay(msGameLoop);
                if (gameState.Mode == GameMode.Started)
                {
                    newScore = gameState.Move();
                    if(oldScore != newScore)
                    {
                        if (newScore % 50 == 0)
                        {
                            mediaPlayer.Open(new Uri("Assets/applause.wav", UriKind.Relative));
                        }
                        else
                        {
                            mediaPlayer.Open(new Uri("Assets/thwack.wav", UriKind.Relative));
                        }
                        mediaPlayer.Play();
                        oldScore = newScore;
                    }
                    Draw();
                }
                else if (gameState.Mode == GameMode.Paused)
                {
                    Overlay.Visibility = Visibility.Visible;
                    OverlayText.Text = "[ P A U S E D ]";
                }
                else if (gameState.Mode == GameMode.Resuming)
                {
                    mediaPlayer.Open(new Uri("Assets/eat_fruit.wav", UriKind.Relative));
                    mediaPlayer.Play();
                    Overlay.Visibility = Visibility.Visible;
                    OverlayText.Text = "[ R e s u m i n g . . . ]";
                    await Task.Delay(500);
                    Overlay.Visibility = Visibility.Hidden;
                    OverlayText.Text = "";
                    gameState.Mode = GameMode.Started;
                }
            }
        }

        private Image[,] SetupGrid()
        {
            Image[,] images = new Image[rows, cols];
            GameGrid.Rows = rows;
            GameGrid.Columns = cols;
            GameGrid.Width = GameGrid.Height * (cols / (double)rows);
            
            for(int r=0; r<rows; r++)
            {
                for (int c=0; c<cols; c++)
                {
                    Image image = new Image
                    {
                        Source = Images.Empty,
                        RenderTransformOrigin = new Point(0.5, 0.5)
                    };

                    images[r, c] = image;
                    GameGrid.Children.Add(image);
                }
            }

            return images;
        }

        private void Draw()
        {
            DrawGrid();
            DrawSnakeHead();
            ScoreText.Text = $"Score: {gameState.Score} : {(int) (100 * gameState.Score) / (gameState.Rows * gameState.Cols)}%";
        }

        private void DrawGrid()
        {
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    GridValue gridValue = gameState.Grid[r, c];
                    gridImages[r, c].Source = gridValToImage[gridValue];
                    gridImages[r, c].RenderTransform = Transform.Identity;
                }
            }

        }

        private void DrawSnakeHead()
        {
            Position headPos = gameState.HeadPosition();
            Image image = gridImages[headPos.Row, headPos.Col];
            image.Source = Images.Head;

            int rotation = dirToRotation[gameState.Dir];
            image.RenderTransform = new RotateTransform(rotation);
        }

        private async Task DrawDeadSnake()
        {
            var positions = new List<Position>(gameState.SnakePositions());

            for(int i=0; i<positions.Count; i++)
            {
                Position pos = positions[i];
                ImageSource source = (i == 0) ? Images.DeadHead : Images.DeadBody;
                gridImages[pos.Row, pos.Col].Source = source;
                await Task.Delay(50);
            }
        }

        private async Task ShowCountdown()
        {
            mediaPlayer.Open(new Uri("Assets/game_start.wav", UriKind.Relative));
            mediaPlayer.Play();
            for (int i = 5; i>=1; i--)
            {
                OverlayText.Text = $"[ {i} ]";
                await Task.Delay(1000);
            }
        }

        private Point startPoint;
        private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(null);
        }
        private void Window_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Point position = e.GetPosition(this);
            int minDisplacement = 40;
            string udlr = string.Empty;
            int rowOffset = 0;
            int colOffset = 0;
            if (Math.Abs(position.X - startPoint.X) > minDisplacement ||
                Math.Abs(position.Y - startPoint.Y) > minDisplacement)
            {
                if (position.Y < startPoint.Y - minDisplacement)
                {
                    udlr += "ud:UP ";
                    rowOffset = -1;
                }
                else if (position.Y > startPoint.Y + minDisplacement)
                {
                    udlr += "ud:DOWN ";
                    rowOffset = 1;
                }
                else
                {
                    udlr += "ud:-- ";
                }

                if (position.X < startPoint.X - minDisplacement)
                {
                    udlr += "lr:LEFT";
                    colOffset = -1;
                }
                else if (position.X > startPoint.X + minDisplacement)
                {
                    udlr += "lr:RIGHT";
                    colOffset = 1;
                }
                else
                {
                    udlr += "lr:--";
                }

                Direction dir = new(rowOffset, colOffset);
                if (gameState.Mode == GameMode.Started)
                {
                    gameState.ChangeDirection(dir);
                }

            }
        }

        private async Task ShowGameOver()
        {
            mediaPlayer.Open(new Uri("Assets/death_1.wav", UriKind.Relative));
            mediaPlayer.Play();
            await DrawDeadSnake();
            await Task.Delay(1000);
            Overlay.Visibility = Visibility.Visible;
            OverlayText.Text = "Press any key to Start";
            gameState.Mode = GameMode.NotStarted;
        }
    }
}
