using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;

namespace snek
{
    public partial class MainWindow : Window
    {
        private int fieldHeight;
        private int fieldWidth;
        private int snakeSize;
        private Snake snake;
        private System.Timers.Timer gameTimer;
        private Direction nextDirection;
        private Point currentFruitPos;
        private bool goNext;
        private long fixedInterval = 100;
        private bool isGameRunning;
        private bool gameThreadIsRunning;
        private bool hitWall;
        private bool isSuicide;
        private List<Point> wallPositions;
        private int points;
        private string Httpresponse;
        private bool legendaryBombaFruit;
        private Level currentLevel = Level.FIRST;
        private Process serverProcess;
        public MainWindow()
        {
            serverInit();
            InitializeComponent();
            this.Closing += MainWindow_Closing;
            InitializeGame();
        }

        private void serverInit(){
            try{
            ProcessStartInfo startInfo = new ProcessStartInfo("svr.exe");
            startInfo.UseShellExecute = false;
            serverProcess = new Process();
            startInfo.CreateNoWindow = true;
            serverProcess.StartInfo = startInfo;
            serverProcess.Start();
            }
            catch (Exception ex)
            {
                // Obsługa błędów
                MessageBox.Show("Cannot open svr.exe. The file must be included in the same directory as the game.");
                Task.Run(() =>
                {
                while (gameThreadIsRunning) ;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Close();
                });
                });
                return;
            }
        }

        private void InitializeGame()
        {

            restartButton.IsEnabled = false;
            isGameRunning = true;
            gameThreadIsRunning = true;
            legendaryBombaFruit = false;

            hitWall = false;
            isSuicide = false;
            points = 0;
            snake = new Snake();
            nextDirection = Direction.Right;
            snakeSize = 20;
            fieldWidth = (int)gameCanvas.Width / snakeSize;
            fieldHeight = (int)gameCanvas.Height / snakeSize;
            wallPositions = new List<Point>();
            InitializeWallPlacement();
            DetermineFruitPos();

            gameTimer = new System.Timers.Timer(fixedInterval);
            gameTimer.Elapsed += GameLoop;
            gameTimer.Start();
        }

        private void GameLoop(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!isGameRunning)
            {
                gameTimer.Stop();
                gameThreadIsRunning = false;
                return;
            }

            Task.Run(() =>
            {
                snake.Move(fieldWidth, fieldHeight, nextDirection);
                CheckCollisions();
                CheckFruit();
            }).ContinueWith(t =>
            {
                Dispatcher.Invoke(() =>
                {
                    UpdateUI();
                });
            });
        }

        private void PauseGame()
        {
            isGameRunning = false;
        }

        private void CheckCollisions()
        {
            var snakeHead = snake.SnakeSegments.First.Value;
            var snakeHeadPosition = snakeHead.Position;

            if (CheckCollisionWithWall(snakeHeadPosition))
            {
                hitWall = true;
            }

            if (CheckCollisionWithSnakeBody(snakeHeadPosition))
            {
                isSuicide = true;
            }

            if (hitWall || isSuicide)
            {
                isGameRunning = false;
                Dispatcher.Invoke(() =>
                {
                    restartButton.IsEnabled = true;
                });
            }
        }

        private bool CheckCollisionWithWall(Point snakeHeadPosition)
        {
            if (wallPositions.Any(wallPosition => wallPosition.Equals(snakeHeadPosition)))
            {
                return true;
            }
            return false;
        }

        private bool CheckCollisionWithSnakeBody(Point snakeHeadPosition)
        {
            var snakeHead = snake.SnakeSegments.First.Value;
            return snake.SnakeSegments.Any(segment => !segment.Equals(snakeHead) && segment.Position.Equals(snakeHeadPosition));
        }


        private void UpdateUI()
        {
            gameCanvas.Children.Clear();
            UpdateQuest();
            wallPositions.ForEach(position => DrawWall(position));
            foreach (var segment in snake.SnakeSegments)
            {
                DrawSnakeSegment(segment);
            }
            DrawFruit();
            UpdateScore();
            if (isSuicide)
            {
                MessageBox.Show($"Snek unalived itself\nSnek snacked {points} snacks.");
            }
            if (hitWall)
            {
                MessageBox.Show($"Snek passionately dashed his head against a wall\nSnek snacked {points} snacks.");
            }
        }

        private void UpdateQuest()
        {
            AccessText accessText = new();
            accessText.TextWrapping = TextWrapping.Wrap;
            switch (currentLevel)
            {
                case Level.FIRST:
                    levelHeader.Content = "LEVEL 1";
                    accessText.Text = "Snek must eat bombastik fruit, \nbut it's hidden very well.\nTry to tinker at its root, \nwhere it is I'll never tell";
                    break;
                case Level.SECOND:
                    levelHeader.Content = "LEVEL 2";
                    accessText.Text = "Snek has eaten all he could. \nThousands and milions of fruit. \nHe won't stop, he'd sooner die. \nHelp him please, you have to try!";
                    break;
                case Level.THIRD:
                    levelHeader.Content = "LEVEL 3";
                    accessText.Text = "Snek's snack's hidden in that wall.\nHe can't climb it, he's too small.";
                    break;
            }
            levelDesc.Content = accessText;
        }

        private void CheckFruit()
        {
            if (snake.SnakeSegments.First.Value.Position == currentFruitPos)
            {
                points += 1; // In the release version this can be put below CheckWinCondition() to increase difficulty slightly
                CheckWinCondition();
                snake.PickedUpFruit = true;
                DetermineFruitPos();
            }
        }

       
        private async void Request(string json, string endpoint, bool showbox, bool block)
        {
            try
            {
                json = json.Replace(';', ',');
                if (block)
                goNext = false;
                // Adres URL serwera, do którego wysyłamy żądanie POST
                string url = "http://localhost:5000/" + endpoint;

                // Tworzenie obiektu HttpClient
                using (HttpClient client = new HttpClient())
                {
                    Console.WriteLine(json);

                    // Tworzenie obiektu StringContent na podstawie danych
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // Wysłanie żądania POST na określony adres URL
                    HttpResponseMessage response = await client.PostAsync(url, content);

                    // Odczytanie odpowiedzi z serwera
                    string responseBody = await response.Content.ReadAsStringAsync();

                    // Wyświetlenie odpowiedzi
                    Console.WriteLine("Odpowiedź serwera:");
                    Console.WriteLine(responseBody);
                    if (showbox)
                    MessageBox.Show(responseBody);
                    Httpresponse = responseBody;
                    if (block)
                    goNext = true;
                
                    return;
                }
                //helloProcess.WaitForExit(); // czekaj na zakończenie procesu

            }
            catch (Exception ex)
            {
                // Obsługa błędów
                Console.WriteLine($"Wystąpił błąd: {ex.Message}");
            }
        }

        

        private void CheckWinCondition()
        {
            switch (currentLevel)
            {
                case Level.FIRST:
                    if (legendaryBombaFruit)
                    {
                        PauseGame();
                        Request("{ \"last_position\": [" + snake.SnakeSegments.First.Value.Position.ToString() + "]}", "level1_last_pos", true, true);
                        while (!goNext){};
                        RestartGame();
                        
                        if (Httpresponse.Contains("Cheater"))
                        {
                            currentLevel = Level.FIRST;
                        }
                        else
                            currentLevel = Level.SECOND;
                    }
                    break;
                case Level.SECOND:
                    if (points.Equals(1337))
                    {
                        PauseGame();
                        Request("{ \"points\": " + points.ToString() + "}", "level2", true, true);
                        while (!goNext){};
                        RestartGame();
                        
                        if (Httpresponse.Contains("Cheater"))
                        {
                            currentLevel = Level.SECOND;
                        }
                        else
                            currentLevel = Level.THIRD;
                    }
                    break;
                case Level.THIRD:
                    PauseGame();
                    Request("{ \"last_position\": [" + snake.SnakeSegments.First.Value.Position.ToString() + "]}", "level3", true, true);
                    RestartGame();
                    while (!goNext){};
                    RestartGame();
                        
                    if (Httpresponse.Contains("Cheater"))
                    {
                        currentLevel = Level.THIRD;
                    }
                    else
                    currentLevel = Level.FIRST;
                    break;
            }
        }

        private void DetermineFruitPos()
        {
            if (currentLevel.Equals(Level.FIRST) || currentLevel.Equals(Level.SECOND))
            {
                RandomiseFruitPos();
            }
            else
            {
                currentFruitPos = new Point(7, 6);
            }
            Request("{ \"special_position\": [" + currentFruitPos.ToString() + "]}", "level1_special_pos", false, false);
        }

        private void RandomiseFruitPos()
        {
            Random random = new Random();

            bool validPositionFound = false;
            int fruitX = 0, fruitY = 0;

            while (!validPositionFound)
            {
                fruitX = random.Next(0, fieldWidth);
                fruitY = random.Next(0, fieldHeight);
                var fruitPos = new Point(fruitX, fruitY);

                if (!snake.SnakeSegments.Any(s => s.Position.Equals(fruitPos)) &&
                    !wallPositions.Any(w => w.Equals(fruitPos)))
                {
                    validPositionFound = true;
                    if (currentLevel.Equals(Level.FIRST))
                    {
                        var luckyDraw = random.Next(0xC0FFEE);
                        if (luckyDraw.Equals(1))
                        {
                            legendaryBombaFruit = true;
                        }
                    }

                }
            }

            currentFruitPos = new Point(fruitX, fruitY);       
        }

        private void InitializeWallPlacement()
        {
            if (currentLevel.Equals(Level.FIRST))
            {
                for (int i = 3; i < fieldWidth - 3; i++)
                {

                    wallPositions.Add(new Point(i, 0));
                    wallPositions.Add((new Point(i, fieldHeight - 1)));
                }
            }

            if (currentLevel.Equals(Level.SECOND))
            {
                for (int i = 4; i < fieldHeight - 4; i++)
                {
                    if (Math.Abs(i - (fieldHeight - 1) / 2.0) > 2)
                    {
                        wallPositions.Add(new Point(7, i));
                        wallPositions.Add(new Point(fieldWidth - 8, i));
                    }
                }
            }
            if (currentLevel.Equals(Level.THIRD))
            {
                for (int i = 0; i < fieldWidth; i++)
                {
                    wallPositions.Add(new Point(i, 0));
                    wallPositions.Add((new Point(i, fieldHeight - 1)));
                }
                for (int i = 0; i < fieldHeight; i++)
                {
                    if (Math.Abs(i - (fieldHeight - 1) / 2.0) < 4)
                    {
                        wallPositions.Add(new Point(0, i));
                        wallPositions.Add((new Point(fieldWidth - 1, i)));
                    }
                }
                DrawCageAt(new Point(6, 5));
                DrawCageAt(new Point(fieldWidth - 9, 5));
                for (int i = 5; i < fieldWidth - 5; i++)
                {
                    wallPositions.Add(new Point(i, 14));
                }
            }

        }

        private void DrawCageAt(Point cagePos)
        {
            for (int i = (int)cagePos.X; i <= cagePos.X + 2; i++)
            {
                wallPositions.Add(new Point(i, cagePos.Y));
                wallPositions.Add(new Point(i, cagePos.Y + 2));
            }
            for (int i = (int)cagePos.Y; i <= cagePos.Y + 2; i++)
            {
                wallPositions.Add(new Point(cagePos.X, i));
                wallPositions.Add(new Point(cagePos.X + 2, i));
            }
        }

        private void DrawSnakeSegment(SnakeSegment segment)
        {
            var position = segment.Position;
            Rectangle rect = new();
            rect.Width = snakeSize;
            rect.Height = snakeSize;
            rect.Fill = Brushes.Green;
            Canvas.SetLeft(rect, position.X * snakeSize);
            Canvas.SetTop(rect, position.Y * snakeSize);

            gameCanvas.Children.Add(rect);
        }

        private void DrawFruit()
        {
            Rectangle fruitRect = new();
            fruitRect.Width = snakeSize;
            fruitRect.Height = snakeSize;
            fruitRect.Fill = legendaryBombaFruit ? Brushes.Gold : Brushes.Red;
            Canvas.SetLeft(fruitRect, currentFruitPos.X * snakeSize);
            Canvas.SetTop(fruitRect, currentFruitPos.Y * snakeSize);

            gameCanvas.Children.Add(fruitRect);
        }

        private void UpdateScore()
        {
            scoreLabel.Content = $"Score: {points}";
        }

        private void DrawWall(Point position)
        {
            Rectangle wall = new();
            wall.Width = snakeSize;
            wall.Height = snakeSize;
            wall.Fill = Brushes.Gray;
            Canvas.SetLeft(wall, position.X * snakeSize);
            Canvas.SetTop(wall, position.Y * snakeSize);

            gameCanvas.Children.Add(wall);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            nextDirection = directionByKey(e.Key);
        }

        private Direction directionByKey(Key k)
        {
            var currentDirection = snake.CurrentDirection;
            switch (k)
            {
                case Key.Up:
                    if (currentDirection != Direction.Down)
                        return Direction.Up;
                    break;
                case Key.Down:
                    if (currentDirection != Direction.Up)
                        return Direction.Down;
                    break;
                case Key.Left:
                    if (currentDirection != Direction.Right)
                        return Direction.Left;
                    break;
                case Key.Right:
                    if (currentDirection != Direction.Left)
                        return Direction.Right;
                    break;
            }
            return nextDirection;
        }

        private void ExitGameButton_Click(object sender, RoutedEventArgs e)
        {
            isGameRunning = false;
            if (!serverProcess.HasExited){
                    serverProcess.Kill(entireProcessTree: true);
                    Console.WriteLine("Proces został zakończony");
                    serverProcess.WaitForExit();
                }
            Task.Run(() =>
            {
                while (gameThreadIsRunning) ;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Close();
                });
            });
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            isGameRunning = false;
            if (!serverProcess.HasExited){
                    serverProcess.Kill(entireProcessTree: true);
                    Console.WriteLine("Proces został zakończony");
                    serverProcess.WaitForExit();
                }
            Task.Run(() =>
            {
                while (gameThreadIsRunning) ;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Close();
                });
            });
        }

        private void RestartGame_Click(object sender, RoutedEventArgs e)
        {
            RestartGame();
        }

        private void RestartGame()
        {
            isGameRunning = false;
            gameTimer.Stop();

            Task.Run(() =>
            {
                while (gameThreadIsRunning) ;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    InitializeGame();
                });
            });
        }
    }



}
