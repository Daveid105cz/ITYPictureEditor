using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
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
using System.Windows.Threading;

namespace ITYObraski
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int Scaling = 8;
        private bool isDrawing = false;
        Point startPoint;
        Line currentlyDrawnLine = null;

        List<MyDrawable> drawables = new List<MyDrawable>();
        int CanvasImagWidth;
        int CanvasImagHeigth;
        bool startupChange = false;
        SolidColorBrush currentGridBrush;
        public MainWindow()
        {
            InitializeComponent();
            startPoint = new Point(0,0);
            startupChange = true;
            currentGridBrush = new SolidColorBrush(Colors.Cyan);
            Button_Click(this, null);
            startupChange = false;
            GridColorCombo.SelectionChanged += GridColorCombo_SelectionChanged;
            
            Type colorsType = typeof(System.Windows.Media.Colors);
            PropertyInfo[] colorsTypePropertyInfos = colorsType.GetProperties(BindingFlags.Public | BindingFlags.Static);

            foreach (PropertyInfo colorsTypePropertyInfo in colorsTypePropertyInfos)
                GridColorCombo.Items.Add(colorsTypePropertyInfo.Name);
            
            GridColorCombo.SelectedItem = "Cyan";
        }
        private SolidColorBrush ColorNameToBrush(String name)
        {
            Type colorsType = typeof(System.Windows.Media.Colors);
            PropertyInfo[] colorsTypePropertyInfos = colorsType.GetProperties(BindingFlags.Public | BindingFlags.Static);

            PropertyInfo pi = colorsTypePropertyInfos.FirstOrDefault((item) => item.Name == name);
            if(pi!=null)
            {
                var color = pi.GetValue(null, null);
                if(color is Color colr)
                {
                    return new SolidColorBrush(colr);
                }
            }
            return new SolidColorBrush(Colors.Cyan);
        }
        private void GridColorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1)
            {
                currentGridBrush = ColorNameToBrush((String)e.AddedItems[0]);
                foreach (Line gridLine in gridLines)
                {
                    gridLine.Stroke = currentGridBrush;
                }
            }
        }

        private void NumericTextBlock_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            int temp = 0;
            if (int.TryParse(e.Text, out temp))
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void MainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(MainCanvas);
            StopDrawing(p.X, p.Y);
            StartDrawing(WindowToImaginary(p.X), WindowToImaginary(p.Y));
        }

        private void MainCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(MainCanvas);
            StopDrawing(p.X, p.Y);
        }

        private void MainCanvas_MouseMove(object sender, MouseEventArgs e)
        {            
            Point p = e.GetPosition(MainCanvas);
            int iX = WindowToImaginary(p.X);
            int iY = WindowToImaginary(p.Y);
            PosXTb.Text = "X: " + iX;
            PosYTb.Text = "Y: " + (CanvasImagHeigth - iY);
            Canvas.SetLeft(FocusRect, RoundWindowToScale(p.X));
            Canvas.SetTop(FocusRect, RoundWindowToScale(p.Y));
            if (deleting)
            {
                if (e.OriginalSource is Line clickedLine)
                {
                    if (clickedLine.Tag == null)
                    {
                        MyDrawable toDelete = null;
                        foreach (MyDrawable draw in drawables)
                        {
                            if (draw.CanvasObject == clickedLine)
                            {
                                toDelete = draw;
                                break;
                            }
                        }
                        if (toDelete != null)
                        {
                            MainCanvas.Children.Remove(toDelete.CanvasObject);
                            toDelete.CanvasObject = null;
                            drawables.Remove(toDelete);
                            toDelete = null;
                        }
                    }
                }
                return;
            }

            MakeMove(p.X, p.Y);
        }
        private double ImaginaryToWindow(double pos)
        {
            return pos * Scaling;
        }
        private int WindowToImaginary(double pos)
        {
            int iPos = (int)pos;
            return iPos / Scaling;
        }
        private double RoundWindowToScale(double pos)
        {
            int iPos = (int)pos;
            iPos /= Scaling;
            return iPos * Scaling;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (startupChange || MessageBox.Show("Změna velikosti a scalu smaže vše udělané!!\nChcete pokračovat ?", "Pozzor. Wurning!", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {

                int widt, hei, newScale;
                if (int.TryParse(WidthSizeTb.Text, out widt) && int.TryParse(HeightSizeTb.Text, out hei) && int.TryParse(ScaleTb.Text, out newScale))
                {
                    Scaling = newScale;
                    double winWid = ImaginaryToWindow(widt);
                    double winHei = ImaginaryToWindow(hei);
                    this.Width = winWid + 20;
                    this.Height = winHei + 80;
                    MainCanvas.Width = winWid;
                    MainCanvas.Height = winHei;
                    CanvasImagHeigth = hei;
                    CanvasImagWidth = widt;
                    FocusRect.Width = Scaling;
                    FocusRect.Height = Scaling;
                    ClearAll();
                    DrawGrid();
                }
                else
                {
                    MessageBox.Show("Fck off, špatný číslo");
                }
            }
        }

        private void StartDrawing(int X, int Y)
        {
            if(!isDrawing)
            {
                float thickness = 1;
                float.TryParse(sizeTb.Text, out thickness);
                isDrawing = true;
                startPoint = new Point(X, Y);
                currentlyDrawnLine = new Line();
                MainCanvas.Children.Add(currentlyDrawnLine);
                currentlyDrawnLine.X1 = ImaginaryToWindow(X)+(Scaling/2);
                currentlyDrawnLine.Y1 = ImaginaryToWindow(Y)+(Scaling/2);
                currentlyDrawnLine.X2 = ImaginaryToWindow(X)+20;
                currentlyDrawnLine.Y2 = ImaginaryToWindow(Y)+20;
                currentlyDrawnLine.StrokeThickness = thickness*(Scaling/2);
                currentlyDrawnLine.Stroke = new SolidColorBrush(Colors.Black);
            }
        }
        private void MakeMove(double X, double Y)
        {
            if (isDrawing)
            {
                Point disPoint = new Point(WindowToImaginary(X),WindowToImaginary(Y));
                Vector direction = disPoint - startPoint;
                double len = direction.Length;
                Vector snapped = SnapTo45(direction);
                Vector almFinal = snapped * (int)len * Scaling;
                
                double finalX = almFinal.X + ImaginaryToWindow(startPoint.X);
                double finalY = almFinal.Y + ImaginaryToWindow(startPoint.Y);
                if (double.IsNaN(finalX) || double.IsNaN(finalY))
                    return;
                currentlyDrawnLine.X2 = finalX + (Scaling / 2);
                currentlyDrawnLine.Y2 = finalY + (Scaling / 2);
                AngleTb.Text="Angl: "+ (int)Math.Round((float)Math.Atan2(almFinal.Y, almFinal.X)*180/Math.PI);
            }
        }
        private void StopDrawing(double X, double Y)
        {
            if (isDrawing)
            {
                MakeMove(X,Y);
                isDrawing = false;
                Point stopLinePoint = new Point(RoundWindowToScale(currentlyDrawnLine.X2), RoundWindowToScale( currentlyDrawnLine.Y2));
                Point startWindowPoint = new Point(ImaginaryToWindow(startPoint.X), ImaginaryToWindow(startPoint.Y));
                Vector lineVect = (stopLinePoint - startWindowPoint);
                double length = Math.Round(lineVect.Length / Scaling);
                lineVect.Normalize();
                MyLine myl = new MyLine() { CanvasObject = currentlyDrawnLine, Length = (int)length, Position = startPoint , Direction = lineVect,Thickness = (float)(currentlyDrawnLine.StrokeThickness/(Scaling / 2)) };
                drawables.Add(myl);
                currentlyDrawnLine = null;
                startPoint = new Point();
                AngleTb.Text = "Angl: ";
            }
        }
        private Vector SnapTo45(Vector normal)
        {
            double roundAngle = 45 * Math.PI / 180.0;
            float angle = (float)Math.Atan2(normal.Y, normal.X);
            Vector newNormal;

            if (angle % roundAngle != 0)
            {
                double newAngle = (float)Math.Round(angle / roundAngle) * roundAngle;
                newNormal = new Vector(Math.Cos(newAngle), Math.Sin(newAngle));
            }
            else
            {
                normal.Normalize();
                newNormal = normal;
            }
            return newNormal;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            String exportText = String.Empty;
            float lastThickness = 0;
            foreach(MyDrawable drawable in drawables)
            {
                if(drawable is MyLine lajn)
                {
                    int StartX, StartY;
                    int directionX, directionY;
                    int length = lajn.Length;
                    StartX = (int)lajn.Position.X;
                    StartY = CanvasImagHeigth-(int)lajn.Position.Y;
                    var directs = GetStupidDirectionFromVector(lajn.Direction);
                    bool angled = false;
                    if(IsAngled(directs))
                    {
                        length = (int)Math.Round((length * 0.68f));
                        angled = true;
                    }
                    directionX = directs.Item1;
                    directionY = directs.Item2;
                    String curLineText = "\\put("+StartX+ ", " + StartY + "){\\line(" + directionX + ", " + directionY + "){ " + length + " }}";
                    if(lastThickness!=lajn.Thickness)
                    {
                        exportText += "\\linethickness{"+(int)lajn.Thickness+"pt}\n";
                        lastThickness = lajn.Thickness;
                    }
                    exportText += curLineText;
                    if (angled)
                        exportText += " %";
                    exportText += "\n";
                    
                }
            }
            System.Windows.Clipboard.SetText(exportText);
            ShowMessageForTime( "Obrásek je v Clipboardu",8);
        }
        private void ShowMessageForTime(String message, int secs)
        {
            MessageTb.Text = message;
            if (messageTimer == null)
            {
                messageTimer = new DispatcherTimer();
                messageTimer.Interval = new TimeSpan(0, 0, secs);
                EventHandler tickHandler = null;
                tickHandler = (s, e) => { MessageTb.Text = ""; messageTimer.Tick -= tickHandler; messageTimer.Stop(); messageTimer = null; };
                messageTimer.Tick += tickHandler;
                messageTimer.Start();
            }
            else
            {
                messageTimer.Stop();
                messageTimer.Start();
            }
        }
        DispatcherTimer messageTimer;
        private bool IsAngled(Tuple<int,int> tup)
        {
            int abs1 = Math.Abs(tup.Item1);
            int abs2 = Math.Abs(tup.Item2);
            if (abs1 == 1 && abs2==1)
                return true;
            return false;
        }
        private Tuple<int, int> GetStupidDirectionFromVector(Vector vect)
        {
            float border = 0.3f;
            if (vect.X > border)
            {
                vect.X = 1;
            }
            else if (vect.X < -border)
            {
                vect.X = -1;
            }
            else
            {
                vect.X = 0;
            }

            if (vect.Y > border)
            {
                vect.Y = 1;
            }
            else if (vect.Y < -border)
            {
                vect.Y = -1;
            }
            else
            {
                vect.Y = 0;
            }
            return new Tuple<int, int>((int)vect.X,-(int)vect.Y);
        }
        private void ClearAll()
        {
            foreach(MyDrawable draw in drawables)
            {
                if(MainCanvas.Children.Contains(draw.CanvasObject))
                {
                    MainCanvas.Children.Remove(draw.CanvasObject);
                }
            }
        }
        List<Line> gridLines = new List<Line>();
        private void DrawGrid()
        {
            foreach(Line lin in gridLines)
            {
                if(MainCanvas.Children.Contains(lin))
                    MainCanvas.Children.Remove(lin);
            }
            gridLines.Clear();
            bool swi = false;
            for (int i = 0; i < CanvasImagWidth;i+=5)
            {
                Line gridLine = new Line();
                gridLine.Y1 = 0;
                gridLine.Y2 = ImaginaryToWindow(CanvasImagHeigth);
                gridLine.X1 = ImaginaryToWindow(i) + (Scaling / 2);
                gridLine.X2 = ImaginaryToWindow(i) + (Scaling / 2);
                gridLine.StrokeThickness = 0.5;
                gridLine.Stroke = currentGridBrush;
                gridLine.Tag = 42;
                MainCanvas.Children.Add(gridLine);
                gridLines.Add(gridLine);
            }
            for (int i = 0; i < CanvasImagHeigth; i += 5)
            {
                Line gridLine = new Line();
                gridLine.X1 = 0;
                gridLine.X2 = ImaginaryToWindow(CanvasImagWidth);
                gridLine.Y1 = ImaginaryToWindow(CanvasImagHeigth- i) + (Scaling / 2);
                gridLine.Y2 = ImaginaryToWindow(CanvasImagHeigth-i) + (Scaling / 2);
                gridLine.StrokeThickness = 0.5;
                gridLine.Stroke = currentGridBrush;
                gridLine.Tag = 42;
                MainCanvas.Children.Add(gridLine);
                gridLines.Add(gridLine);
            }
        }

        bool deleting = false;
        private void MainCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            deleting = true;
            FocusRect.Fill = new SolidColorBrush(Colors.Purple);
            
        }

        private void MainCanvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            deleting = false;
            FocusRect.Fill = new SolidColorBrush(Colors.Red);
        }
    }
}
