using System;
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
        private readonly int rows = 32, cols = 32; //normal 32 x 32 grid of 1024 cells
        //private readonly int rows = 15, cols = 15; //min
        private readonly Image[,] gridImages;

        private readonly Dictionary<GridValue, ImageSource> gridValToImage = new()
        {
            { GridValue.Empty, Images.Empty },
            { GridValue.Snake, Images.Body },
            { GridValue.Food, Images.Food }
        };

        private readonly Dictionary<Direction, int> dirToRotation = new Dictionary<Direction, int>()
        {
            {Direction.Up, 0 },
            {Direction.Down, 180 },
            {Direction.Left, 270 },
            {Direction.Right, 90 }
        };

        private GameState gameState;

        public MainWindow()
        {
            InitializeComponent();
            gridImages = SetupGrid();
            gameState = new GameState(rows, cols);
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
                    gameState.ChangeDirection(Direction.Left);
                    break;
                case Key.Right:
                    gameState.ChangeDirection(Direction.Right);
                    break;
                case Key.Up:
                    gameState.ChangeDirection(Direction.Up);
                    break;
                case Key.Down:
                    gameState.ChangeDirection(Direction.Down);
                    break;
            }
        }

        private async Task GameLoop()
        {
            while (gameState.Mode != GameMode.NotStarted 
                && gameState.Mode != GameMode.Over)
            {
                await Task.Delay(msGameLoop);
                if (gameState.Mode == GameMode.Started)
                {
                    gameState.Move();
                    Draw();
                }
                else if (gameState.Mode == GameMode.Paused)
                {
                    Overlay.Visibility = Visibility.Visible;
                    OverlayText.Text = "[ P A U S E D ]";
                }
                else if (gameState.Mode == GameMode.Resuming)
                {
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
            ScoreText.Text = $"Score: {gameState.Score}";
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
            for (int i =3; i>=1; i--)
            {
                OverlayText.Text = $"[ {i} ]";
                await Task.Delay(500);
            }
        }

        private async Task ShowGameOver()
        {
            await DrawDeadSnake();
            await Task.Delay(1000);
            Overlay.Visibility = Visibility.Visible;
            OverlayText.Text = "Press any key to Start";
            gameState.Mode = GameMode.NotStarted;
        }
    }
}
