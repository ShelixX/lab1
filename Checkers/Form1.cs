using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Checkers
{
    public partial class Form1 : Form
    {
        const int mapSize = 6; //Размер игрового поля
        const int cellSize = 50; //Размер ячейки
        int[,] map = new int[mapSize, mapSize]; //Игровое поле
        Button[,] buttons = new Button[mapSize, mapSize]; //Массив кнопок
        int currentPlayer; //Текущий ход
        int player = 1;
        Image whiteFigures; //Картинка белой фигуры
        Image blackFigures; //Картинка чёрной фигуры
        Button prevButton; //Предыдущая кнопка
        Button pressedButton; //Текущая кнопка
        bool isContinue = false; //Повторный ход
        bool isMoving; //Движение шашки
        List<Button> simpleSteps = new List<Button>(); //Доступные кнопки
        int countEatSteps = 0; //Количество ходов для битья вражеской шашки
        int stepCount = 0;
        Random r;
        bool draw = false; //Ничья

        public Form1()
        {
            InitializeComponent();
            this.Text = "Checkers";
            //Картинки шашек
            whiteFigures = new Bitmap(new Bitmap(@"Images\Белая шашка.png"), new Size(cellSize - 10, cellSize - 10));
            blackFigures = new Bitmap(new Bitmap(@"Images\Чёрная шашка.png"), new Size(cellSize - 10, cellSize - 10));
            this.FormBorderStyle = FormBorderStyle.FixedSingle; //Запрет на изменение размеров окна
            Init();
        }

        public void Init()
        {
            currentPlayer = 1;
            isMoving = false;
            prevButton = null; //Предыдущая нажатая кнопка
            //Карта
            map = new int[mapSize, mapSize] {
                { 0,2,0,2,0,2 },
                { 2,0,2,0,2,0 },
                { 0,0,0,0,0,0 },
                { 0,0,0,0,0,0 },
                { 0,1,0,1,0,1 },
                { 1,0,1,0,1,0 }
            };
            this.Width = (mapSize + 1) * cellSize; //Длина окна
            this.Height = (mapSize + 1) * cellSize; //Ширина окна
            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    //Создание клеток (кнопок)
                    Button button = new Button();
                    button.Location = new Point(j * cellSize, i * cellSize);
                    button.Size = new Size(cellSize, cellSize);
                    button.Click += new EventHandler(OnFigurePress);
                    if (map[i, j] == 1)
                        button.Image = whiteFigures;
                    else if (map[i, j] == 2)
                        button.Image = blackFigures;
                    button.BackColor = StandardButtonColor(button);
                    button.ForeColor = Color.Red;
                    buttons[i, j] = button;
                    this.Controls.Add(button);
                }
            }
        }

        public void ResetGame() //Метод, выводящий на экран сообщение с цветом победителя и перезапускающий игру
        {
            bool player1 = false;
            bool player2 = false;
            List<Button> blackButtons = new List<Button>();
            List<Button> whiteButtons = new List<Button>();
            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    if (map[i, j] == 1)
                    {
                        player1 = true;
                        whiteButtons.Add(buttons[i, j]);
                    } 
                    if (map[i, j] == 2)
                    {
                        player2 = true;
                        blackButtons.Add(buttons[i, j]);
                    }
                }
            }
            if (!player1 || !player2) //Если у кого-то закончились фигуры
            {
                string text = !player1 ? "Чёрные шашки победили" : "Белые шашки победили";
                var mb = MessageBox.Show(text, "Конец игры", MessageBoxButtons.OK);
                if (mb == DialogResult.OK)
                {
                    this.Controls.Clear();
                    Init();
                }
            }
            else if (blackButtons.Count == 1 && currentPlayer == 2) //Если осталась одна чёрная шашка
            {
                //Здесь проводится проверка на наличие доступных ходов у шашки
                bool d = blackButtons[0].Text == "Дамка" ? false : true;
                ShowDiagonal(blackButtons[0].Location.Y / cellSize, blackButtons[0].Location.X / cellSize, d);
                ClearSteps();
                if (stepCount == 0 && !IsButtonCanEat(blackButtons[0].Location.Y / cellSize, blackButtons[0].Location.X / cellSize, d, new int[2] { 0, 0 })) //Если доступных ходов нет
                {
                    var mb = MessageBox.Show("Белые шашки победили", "Конец игры", MessageBoxButtons.OK);
                    if (mb == DialogResult.OK)
                    {
                        this.Controls.Clear();
                        Init();
                    }
                }
            }
            else if (whiteButtons.Count == 1 && currentPlayer == 1) //Если осталась одна белая шашка
            {
                //Здесь проводится проверка на наличие доступных ходов у шашки
                bool d = whiteButtons[0].Text == "Дамка" ? false : true;
                ShowDiagonal(whiteButtons[0].Location.Y / cellSize, whiteButtons[0].Location.X / cellSize, d);
                ClearSteps();
                if (stepCount == 0 && !IsButtonCanEat(whiteButtons[0].Location.Y / cellSize, whiteButtons[0].Location.X / cellSize, d, new int[2] { 0, 0 })) //Если доступных ходов нет
                {
                    var mb = MessageBox.Show("Чёрные шашки победили", "Конец игры", MessageBoxButtons.OK);
                    if (mb == DialogResult.OK)
                    {
                        this.Controls.Clear();
                        Init();
                    }
                }
            }
        }

        public void BotStep() //Ход компьютера
        {
            List<Button> btns = new List<Button>();
            List<Button> btns2 = new List<Button>();
            int x = 0;
            int n;
            r = new Random();
            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    if (map[i, j] != player && map[i, j] != 0)
                    {
                        if (buttons[i, j].Enabled == true)
                            btns.Add(buttons[i, j]); //Если имеется доступная шашка, то добавляем её в список
                    }
                }
            }
            while (true)
            {
                if (btns.Count > 0)
                {
                    n = r.Next(0, btns.Count);
                    btns[n].PerformClick();
                    for (int i = 0; i < mapSize; i++)
                    {
                        for (int j = 0; j < mapSize; j++)
                        {
                            if (buttons[i, j].BackColor == Color.Yellow) //Если у шашки из списка есть возможность сделать ход
                            {
                                btns2.Add(buttons[i, j]);
                                x++;
                            }
                        }
                    }
                    if ((btns.Count == 1) && btns2.Count == 0) //В случае если последняя шашка в тупике
                    {
                        btns[n].PerformClick();
                        break;
                    }        
                    if (x == 0) //Если возможности сделать ход у шашки нет, то переходим к следующей итерации
                    {
                        btns[n].PerformClick();
                        continue;
                    }
                    else //Если возможность сделать ход у шашки есть
                    {
                        btns2[r.Next(0, btns2.Count)].PerformClick();
                        break;
                    }
                }
                else
                    break;
            }
            if (pressedButton.BackColor == Color.Red) //Условие для повторного хода
            {
                for (int i = 0; i < mapSize; i++)
                {
                    for (int j = 0; j < mapSize; j++)
                    {
                        if (buttons[i, j].BackColor == Color.Yellow)
                            buttons[i, j].PerformClick();
                    }
                }
            }
        }

        public void SwitchPlayer() //Метод переключает ход и вызывает другой метод, который в случае необходимости заканчивает игру
        {
            currentPlayer = currentPlayer == 1 ? 2 : 1;
            ResetGame();
        }

        public Color StandardButtonColor(Button prevButton) //Метод, который возвращает стандартный цвет фона клетки
        {
            if ((prevButton.Location.Y / cellSize % 2) != 0)
            {
                if ((prevButton.Location.X / cellSize % 2) == 0)
                    return Color.Gray;
            }
            if ((prevButton.Location.Y / cellSize) % 2 == 0)
            {
                if ((prevButton.Location.X / cellSize) % 2 != 0)
                    return Color.Gray;
            }
            return Color.White;
        }

        public void Draw_Click(object sender, EventArgs e) //Обработка нажатия кнопки (Завершение игры ничьей)
        {
            var mb = MessageBox.Show("Ничья", "Конец игры", MessageBoxButtons.OK);
            if (mb == DialogResult.OK)
            {
                this.Controls.Clear();
                Init();
            }
        }

        public void Draw() //Метод для запуска возможности закончить игру ничьей
        {
            List<Button> crownedListWhite = new List<Button>(); //Список белых дамок
            List<Button> crownedListBlack = new List<Button>(); //Список чёрных дамок
            int count = 0; //Количество шашек на поле

            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    if (map[i, j] == 1 || map[i, j] == 2)
                        count += 1;
                    if (map[i, j] == 1 && buttons[i, j].Text == "Дамка")
                        crownedListWhite.Add(buttons[i, j]);
                    if (map[i, j] == 2 && buttons[i, j].Text == "Дамка")
                        crownedListBlack.Add(buttons[i, j]);
                }
            }

            if (count == 2 && crownedListBlack.Count == 1 && crownedListWhite.Count == 1) //Если остались две дамки двух цветов, то появляется возможность закочнить игру ничьей
            {
                if (!draw)
                {
                    int width = this.Width;
                    this.Width = width + 100;
                    Button button = new Button();
                    button.Location = new Point(this.Width - 130, 135);
                    button.Size = new Size(100, 50);
                    button.Text = "Ничья";
                    button.Click += new EventHandler(Draw_Click);
                    this.Controls.Add(button);
                    draw = true;
                }
            }
        }

        public void OnFigurePress(object sender, EventArgs e) //Обрабатывает нажатие на кнопку, соответствующую шашке
        {
            pressedButton = sender as Button;
            //Если выбрана кнопка с фигурой игрока 
            if (map[pressedButton.Location.Y / cellSize, pressedButton.Location.X / cellSize] != 0 && map[pressedButton.Location.Y / cellSize, pressedButton.Location.X / cellSize] == currentPlayer)
            {
                ClearSteps();
                pressedButton.BackColor = Color.Red;
                DeactivateAllButtons(); //Закрытие доступа ко всем кнопкам
                pressedButton.Enabled = true; //Открытие доступа к выбранной кнопке
                countEatSteps = 0;
                if (pressedButton.Text == "Дамка") //Если фигура дамка
                    ShowDiagonal(pressedButton.Location.Y / cellSize, pressedButton.Location.X / cellSize, false); //Показать доступные ходы без ограничения на количество клеток
                else ShowDiagonal(pressedButton.Location.Y / cellSize, pressedButton.Location.X / cellSize, true); //Показать доступные ходы c ограничением на количество клеток
                if (isMoving) //Часть кода для отмены хода данной фигуры при повторном нажатии
                {
                    ClearSteps();
                    pressedButton.BackColor = StandardButtonColor(pressedButton);
                    ShowPossibleSteps();
                    isMoving = false;
                }
                else
                    isMoving = true;
            }
            else //Если выбрана доступная для хода клетка
            {
                if (isMoving)
                {
                    isContinue = false;
                    if (Math.Abs(pressedButton.Location.X / cellSize - prevButton.Location.X / cellSize) > 1) //Если расстояние по диагонали больше одной шашки
                    {
                        DeleteEaten(pressedButton, prevButton); //Удаление шашки
                    }
                    //Перенос шашки на другую клетку
                    int temp = map[pressedButton.Location.Y / cellSize, pressedButton.Location.X / cellSize]; 
                    map[pressedButton.Location.Y / cellSize, pressedButton.Location.X / cellSize] = map[prevButton.Location.Y / cellSize, prevButton.Location.X / cellSize];
                    map[prevButton.Location.Y / cellSize, prevButton.Location.X / cellSize] = temp;
                    pressedButton.Image = prevButton.Image;
                    prevButton.Image = null;
                    pressedButton.Text = prevButton.Text;
                    prevButton.Text = "";
                    CrownButton(pressedButton); //Вызов метода, где фигура станет дамкой в случае выполнения условия
                    countEatSteps = 0; //Количество возможных ходов для поедания
                    isMoving = false; //Отключение хода у шашки
                    ClearSteps(); //Смена цвета фона кнопки на стандартный
                    DeactivateAllButtons(); //Отключение всех кнопок
                    if (pressedButton.Text == "Дамка") //Если фигура дамка
                        ShowDiagonal(pressedButton.Location.Y / cellSize, pressedButton.Location.X / cellSize, false); //Показать доступные ходы без ограничения на количество клеток
                    else ShowDiagonal(pressedButton.Location.Y / cellSize, pressedButton.Location.X / cellSize, true); //Показать доступные ходы c ограничением на количество клеток
                    if (countEatSteps == 0 || !isContinue) //Если есть возможность побить вражескую шашку или повторный ход отключен
                    {
                        isContinue = false; //Отключение доступа к повторному ходу
                        ClearSteps(); //Смена цвета фона у кнопок на стандартный
                        SwitchPlayer(); //Смена хода
                        ShowPossibleSteps(); //Отображение доступных ходов
                        if (currentPlayer != player) //Если ход не игрока, а компьютера
                            BotStep();
                        Draw(); //Вызов метода для завершения игру ничьей
                    }
                    else if (isContinue) //Если есть возможность сделать повторный ход
                    {
                        pressedButton.BackColor = Color.Red;
                        pressedButton.Enabled = true;
                        isMoving = true;
                    }
                }
            }
            prevButton = pressedButton; 
        }

        public void ShowPossibleSteps() //Отображает доступные для хода шашки
        {
            bool isOneStep; //Определение статуса "Дамки" у шашки
            bool isEatStep = false; //Определение наличия хода, в котором можно побить вражескую шашку
            DeactivateAllButtons(); //Отключение всех кнопок
            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    if (map[i, j] == currentPlayer)
                    {
                        if (buttons[i, j].Text == "Дамка")
                            isOneStep = false;
                        else isOneStep = true;
                        if (IsButtonCanEat(i, j, isOneStep, new int[2] { 0, 0 })) //Проверка возможности поедания вражеской шашки
                        {
                            isEatStep = true;
                            buttons[i, j].Enabled = true; //Получение доступа к той шашке, которая может побить вражескую
                        }
                    }
                }
            }
            if (!isEatStep) //Если хода/ходов для поедания нет, то даём доступ ко всем фигурам
                ActivateAllButtons();
        }

        public void CrownButton(Button button)  //Становление шашки дамкой, если она достигла противоположного края поля.
        {
            if (map[button.Location.Y / cellSize, button.Location.X / cellSize] == 2 && button.Location.Y / cellSize == mapSize - 1)
            {
                button.Text = "Дамка";
            }
            if (map[button.Location.Y / cellSize, button.Location.X / cellSize] == 1 && button.Location.Y / cellSize == 0)
            {
                button.Text = "Дамка";
            }
        }

        public void DeleteEaten(Button endButton, Button startButton) //Удаляет шашку противника, если она была побита в результате хода
        {
            int count = Math.Abs(endButton.Location.Y / cellSize - startButton.Location.Y / cellSize); //Расстояние между кнопками
            int startIndexX = endButton.Location.Y / cellSize - startButton.Location.Y / cellSize;
            int startIndexY = endButton.Location.X / cellSize - startButton.Location.X / cellSize;
            startIndexX = startIndexX < 0 ? -1 : 1; //-1 если ход был сделан влево, 1 если ход был сделан вправо
            startIndexY = startIndexY < 0 ? -1 : 1; //-1 если ход был сделан вниз, 1 если ход был сделан вверх
            int currCount = 0;
            int i = startButton.Location.Y / cellSize + startIndexX; //Позиция съеденной фигуры
            int j = startButton.Location.X / cellSize + startIndexY; //Позиция съеденной фигуры
            while (currCount < count - 1)
            {
                if (map[i, j] != 0)
                    isContinue = true; //Получение доступа к повторному ходу
                map[i, j] = 0;
                buttons[i, j].Image = null;
                buttons[i, j].Text = "";
                i += startIndexX;
                j += startIndexY;
                currCount++;
            }

        }


        public void ShowDiagonal(int IcurrFigure, int JcurrFigure, bool isOneStep)  //Метод, отображающий доступные ходы по диагонали для шашки на клетке
        {
            simpleSteps.Clear(); //Очистка доступных кнопок
            stepCount = 0; //Количество доступных ходов
            //Проверка правой верхней клетки для шашки
            int j = JcurrFigure + 1;
            for (int i = IcurrFigure - 1; i >= 0; i--)
            {
                if (currentPlayer == 2 && isOneStep && !IsButtonCanEat(IcurrFigure, JcurrFigure, isOneStep, new int[2] { 0, 0 })) break; //Если шашка чёрная, она не дамка и у неё нет съедобного хода, то выходим из цикла
                if (IsInsideBorders(i, j)) //Если координаты в пределах поля
                {
                    if (!AvailablePaths(i, j)) //Если доступных ходов нет, то выходим из цикла
                        break;
                    else
                        stepCount++; //Количество доступных ходов
                }
                if (j < mapSize - 1)
                    j++;
                else break;

                if (isOneStep)
                    break;
            }

            j = JcurrFigure - 1; //Проверка левой верхней клетки
            for (int i = IcurrFigure - 1; i >= 0; i--)
            {
                if (currentPlayer == 2 && isOneStep && !IsButtonCanEat(IcurrFigure, JcurrFigure, isOneStep, new int[2] { 0, 0 })) break;
                if (IsInsideBorders(i, j))
                {
                    if (AvailablePaths(i, j))
                        break;
                    else
                        stepCount++;
                }
                if (j > 0)
                    j--;
                else break;

                if (isOneStep)
                    break;
            }

            //Проверка левой нижней клетки
            j = JcurrFigure - 1;
            for (int i = IcurrFigure + 1; i < mapSize; i++)
            {
                if (currentPlayer == 1 && isOneStep && !IsButtonCanEat(IcurrFigure, JcurrFigure, isOneStep, new int[2] { 0, 0 })) break;
                if (IsInsideBorders(i, j))
                {
                    if (!AvailablePaths(i, j))
                        break;
                    else
                        stepCount++;
                }
                if (j > 0)
                    j--;
                else break;

                if (isOneStep)
                    break;
            }

            //Проверка правой нижней клетки
            j = JcurrFigure + 1;
            for (int i = IcurrFigure + 1; i < mapSize; i++)
            {
                if (currentPlayer == 1 && isOneStep && !IsButtonCanEat(IcurrFigure, JcurrFigure, isOneStep, new int[2] { 0, 0 })) break;
                if (IsInsideBorders(i, j))
                {
                    if (!AvailablePaths(i, j))
                        break;
                    else
                        stepCount++;
                }
                if (j < mapSize - 1)
                    j++;
                else break;

                if (isOneStep)
                    break;
            }
            if (countEatSteps > 0) //Отключение доступных кнопок при возможности поедания вражеской шашки
                CloseSimpleSteps(simpleSteps);
        }

        public bool AvailablePaths(int ti, int tj) //Метод, определяющий наличие доступных ходов для шашки на клетке
        {

            if (map[ti, tj] == 0 && !isContinue) //Если клетка пустая и это не повторный ход
            {
                buttons[ti, tj].BackColor = Color.Yellow;
                buttons[ti, tj].Enabled = true;
                simpleSteps.Add(buttons[ti, tj]);
            }
            else
            {

                if (map[ti, tj] != currentPlayer) //Если на клетке вражеская фигура
                {
                    if (pressedButton.Text == "Дамка")
                        ShowProceduralEat(ti, tj, false);
                    else ShowProceduralEat(ti, tj);
                }

                return false;
            }
            return true;
        }

        public void CloseSimpleSteps(List<Button> simpleSteps) //Метод, закрывающий ходы для шашек из списка
        {
            if (simpleSteps.Count > 0)
            {
                for (int i = 0; i < simpleSteps.Count; i++)
                {
                    simpleSteps[i].BackColor = StandardButtonColor(simpleSteps[i]);
                    simpleSteps[i].Enabled = false;
                }
            }
        }
        public void ShowProceduralEat(int i, int j, bool isOneStep = true) //Метод для построения хода, при котором текущая шашка бьёт вражескую шашку
        {
            //Определение направления к вражеской шашке
            int dirX = i - pressedButton.Location.Y / cellSize;
            int dirY = j - pressedButton.Location.X / cellSize;
            dirX = dirX < 0 ? -1 : 1;
            dirY = dirY < 0 ? -1 : 1;
            int il = i;
            int jl = j;
            bool isEmpty = true;
            while (IsInsideBorders(il, jl))
            {
                if (map[il, jl] != 0 && map[il, jl] != currentPlayer) //Если на клетке вражеская шашка
                {
                    isEmpty = false;
                    break;
                }
                il += dirX;
                jl += dirY;

                if (isOneStep)
                    break;
            }
            if (isEmpty)
                return;
            List<Button> toClose = new List<Button>(); //Список доступных кнопок, которые будут закрыты
            bool closeSimple = false;
            int ik = il + dirX;
            int jk = jl + dirY;
            while (IsInsideBorders(ik, jk))
            {
                if (map[ik, jk] == 0) //Если за шашкой есть пустая клетка
                {
                    if (IsButtonCanEat(ik, jk, isOneStep, new int[2] { dirX, dirY }))
                    {
                        closeSimple = true; //Имеется ход для битья вражеской шашки
                    }
                    else
                    {
                        toClose.Add(buttons[ik, jk]); //Запись остальных ходов в список (они будут закрыты)
                    }
                    buttons[ik, jk].BackColor = Color.Yellow;
                    buttons[ik, jk].Enabled = true;
                    countEatSteps++;
                }
                else break;
                if (isOneStep)
                    break;
                jk += dirY;
                ik += dirX;
                if (closeSimple && toClose.Count > 0)
                {
                    CloseSimpleSteps(toClose);
                }
            }
        }

        public bool IsButtonCanEat(int IcurrFigure, int JcurrFigure, bool isOneStep, int[] dir) //Метод, проверяющий наличие хода для битья у шашки
        {
            bool eatStep = false;
            int j = JcurrFigure + 1;
            for (int i = IcurrFigure - 1; i >= 0; i--)
            {
                if (dir[0] == 1 && dir[1] == -1 && !isOneStep) break;
                if (IsInsideBorders(i, j)) 
                {
                    if (map[i, j] != 0 && map[i, j] != currentPlayer)
                    {
                        eatStep = true;
                        if (!IsInsideBorders(i - 1, j + 1))
                            eatStep = false;
                        else if (map[i - 1, j + 1] != 0) 
                        {
                            eatStep = false;
                            if (!isOneStep)
                            {
                                break;
                            }
                        } 
                        else return eatStep;
                    }
                    else if (map[i, j] == currentPlayer)
                        break;
                }
                if (j < mapSize - 1)
                    j++;
                else break;

                if (isOneStep)
                    break;
            }

            j = JcurrFigure - 1;
            for (int i = IcurrFigure - 1; i >= 0; i--)
            {
                if (dir[0] == 1 && dir[1] == 1 && !isOneStep) break;
                if (IsInsideBorders(i, j))
                {
                    if (map[i, j] != 0 && map[i, j] != currentPlayer)
                    {
                        eatStep = true;
                        if (!IsInsideBorders(i - 1, j - 1))
                            eatStep = false;
                        else if (map[i - 1, j - 1] != 0)
                        {
                            eatStep = false;
                            if (!isOneStep)
                            {
                                break;
                            }
                        }
                        else return eatStep;
                    }
                    else if (map[i, j] == currentPlayer)
                        break;
                }
                if (j > 0)
                    j--;
                else break;

                if (isOneStep)
                    break;
            }

            j = JcurrFigure - 1;
            for (int i = IcurrFigure + 1; i < mapSize; i++)
            {
                if (dir[0] == -1 && dir[1] == 1 && !isOneStep) break;
                if (IsInsideBorders(i, j))
                {
                    if (map[i, j] != 0 && map[i, j] != currentPlayer)
                    {
                        eatStep = true;
                        if (!IsInsideBorders(i + 1, j - 1))
                            eatStep = false;
                        else if(map[i + 1, j - 1] != 0)
                        {
                            eatStep = false;
                            if (!isOneStep)
                            {
                                break;
                            }
                        } 
                        else return eatStep;
                    }
                    else if (map[i, j] == currentPlayer)
                        break;
                }
                if (j > 0)
                    j--;
                else break;

                if (isOneStep)
                    break;
            }

            j = JcurrFigure + 1;
            for (int i = IcurrFigure + 1; i < mapSize; i++)
            {
                if (dir[0] == -1 && dir[1] == -1 && !isOneStep) break;
                if (IsInsideBorders(i, j))
                {
                    if (map[i, j] != 0 && map[i, j] != currentPlayer)
                    {
                        eatStep = true;
                        if (!IsInsideBorders(i + 1, j + 1))
                            eatStep = false;
                        else if (map[i + 1, j + 1] != 0)
                        {
                            eatStep = false;
                            if (!isOneStep)
                            {
                                break;
                            }
                        }
                        else return eatStep;
                    }
                    else if (map[i, j] == currentPlayer)
                        break;
                }
                if (j < mapSize - 1)
                    j++;
                else break;

                if (isOneStep)
                    break;
            }
            return eatStep;
        }

        public void ClearSteps() //Метод для смены цвета всех кнопок на стандартный
        {
            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    buttons[i, j].BackColor = StandardButtonColor(buttons[i, j]); //Смена цвета кнопки на стандартный
                }
            }
        }

        public bool IsInsideBorders(int ti, int tj) //Проверяет, находятся ли переданные координаты в пределах игрового поля.
        {
            if (ti >= mapSize || tj >= mapSize || ti < 0 || tj < 0)
            {
                return false;
            }
            return true;
        }

        public void ActivateAllButtons() //Делает все кнопки на игровом поле доступными для нажатия.
        {
            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    buttons[i, j].Enabled = true;
                }
            }
        }

        public void DeactivateAllButtons() //Делает все кнопки на игровом поле недоступными для нажатия
        {
            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    buttons[i, j].Enabled = false;
                }
            }
        }
    }
}