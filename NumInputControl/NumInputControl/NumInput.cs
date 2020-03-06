using System;
using System.Collections.Generic;
using System.Globalization;
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

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
                                     //(used if a resource is not found in the page,
                                     // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
                                              //(used if a resource is not found in the page,
                                              // app, or any theme specific resource dictionaries)
)]

namespace NumInputControl
{
    public class NumInput : TextBox
    {
        public static DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(double), typeof(NumInput), new FrameworkPropertyMetadata(0.0, ValuePropertyChangedCallback));
        public static DependencyProperty StepProperty = DependencyProperty.Register(nameof(Step), typeof(double), typeof(NumInput), new PropertyMetadata(1.0));
        public static DependencyProperty PrecisionProperty = DependencyProperty.Register(nameof(Precision), typeof(int), typeof(NumInput), new PropertyMetadata(2));
        public static DependencyProperty MaxValueProperty = DependencyProperty.Register(nameof(MaxValue), typeof(double), typeof(NumInput), new PropertyMetadata(99999.99));
        public static DependencyProperty MinValueProperty = DependencyProperty.Register(nameof(MinValue), typeof(double), typeof(NumInput), new PropertyMetadata(-99999.99));

        public static void ValuePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            NumInput numInput = d as NumInput;
            numInput.Value = numInput.GetValidValue((double)e.NewValue);
        }

        static NumInput()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NumInput), new FrameworkPropertyMetadata(typeof(NumInput)));
        }

        private const int MouseMoveMaxY = 300;
        private const int IX = 50;
        private const int IPrimaryThickness = 5;
        private const int ISecondThickness = 3;
        private const int IMaxLines = 5;

        private Point currentMousePos;
        private Point startMousePos;
        private double originalValue;
        private double interval;
        private double relativeStartMouseY;

        private Window IWin;
        private Line MouseLine;
        private Brush LineBrush = new SolidColorBrush(Colors.Red);
        private Brush MouseLineBrush = new SolidColorBrush(Colors.Gray);
        private FontFamily ILabelFontFamily = new FontFamily("Arial");
        private FontStyle ILabelFontStyle = FontStyles.Normal;
        private FontWeight ILabelFontWeight = FontWeights.Bold;
        private FontStretch ILabelFontStretch = FontStretches.Normal;
        private double LabelFontSize = 14;
        private double ILabelWidth;
        private double ILabelHeight;

        public NumInput()
        {
            DataContext = this;
            VerticalContentAlignment = VerticalAlignment.Center;
            Padding = new Thickness(5);

            GotFocus += TextBox_GotFocus;
            LostFocus += TextBox_LostFocus;
            KeyDown += TextBox_KeyDown;
            PreviewTextInput += TextBox_PreviewTextInput;
            PreviewTextInput += TextBox_PreviewTextInput;
            DataObject.AddPastingHandler(this, TextBox_Pasting);
            PreviewMouseRightButtonDown += TextBox_PreviewMouseButtonDown;
            PreviewMouseRightButtonUp += TextBox_PreviewMouseButtonUp;
            PreviewMouseMove += TextBox_PreviewMouseMove;
            DataObject.AddCopyingHandler(this, (sender, e) => { if (e.IsDragDrop) e.CancelCommand(); });
        }

        public double Value
        {
            get => (double)this.GetValue(ValueProperty);
            set => this.SetValue(ValueProperty, value);
        }

        public double Step
        {
            get => (double)this.GetValue(StepProperty);
            set => this.SetValue(StepProperty, value);
        }

        public int Precision
        {
            get => (int)this.GetValue(PrecisionProperty);
            set => this.SetValue(PrecisionProperty, value);
        }

        public double MaxValue
        {
            get => (double)this.GetValue(MaxValueProperty);
            set => this.SetValue(MaxValueProperty, value);
        }

        public double MinValue
        {
            get => (double)this.GetValue(MinValueProperty);
            set => this.SetValue(MinValueProperty, value);
        }

        private void CalculateLabelSize()
        {
            double length = Math.Max(Math.Abs(MaxValue), Math.Abs(MinValue));
            string candidate = string.Format("-{0:F" + Precision + "}", length);
            var formattedText = new FormattedText(
                candidate,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(ILabelFontFamily, ILabelFontStyle, ILabelFontWeight, ILabelFontStretch),
                LabelFontSize,
                Brushes.Black,
                new NumberSubstitution(),
                1
            );

            ILabelWidth = formattedText.Width;
            ILabelHeight = formattedText.Height;
        }

        private Label CreateLabel()
        {
            return new Label()
            {
                FontFamily = ILabelFontFamily,
                FontStyle = ILabelFontStyle,
                FontWeight = ILabelFontWeight,
                FontStretch = ILabelFontStretch,
                FontSize = LabelFontSize,
                Padding = new Thickness(0),
                Foreground = LineBrush,
                //Background = new SolidColorBrush(Colors.Blue),
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Right,
                Width = ILabelWidth,
                Height = ILabelHeight
            };
        }

        private double CalculateValuePos()
        {
            double pct = 1 - (Value - MinValue) / interval;
            double ypos = MouseMoveMaxY * pct;
            return (ILabelHeight / 2 + ypos) - ISecondThickness / 2;
        }

        private void InitIWin()
        {
            ReleaseIWin();
            CalculateLabelSize();

            double marginLR = ILabelWidth + 5;
            double marginTB = ILabelHeight / 2;
            double secondWidth = IX / 2.0;
            double secondMargin = marginLR + (IX - secondWidth) / 2;
            double middleLineX = marginLR + IX / 2 - ISecondThickness / 2;
            double wndWidth = ILabelWidth + IX + 40;
            double wndHeight = marginTB * 2 + MouseMoveMaxY;
            Canvas canvas = new Canvas();
            Label label;
            Line line;

            //Max
            label = CreateLabel();
            label.Content = string.Format("{0:F" + Precision + "}", MaxValue);
            Canvas.SetLeft(label, 0);
            Canvas.SetTop(label, 0);
            canvas.Children.Add(label);
            line = new Line()
            {
                X1 = marginLR,
                X2 = marginLR + IX,
                Y1 = marginTB,
                Y2 = marginTB,
                Stroke = LineBrush,
                StrokeThickness = IPrimaryThickness
            };
            canvas.Children.Add(line);

            double lineValue = Math.Abs(MaxValue - MinValue) / IMaxLines;
            int lineY = MouseMoveMaxY / IMaxLines;
            for (int i = 1; i < IMaxLines; i++)
            {
                double v = MaxValue - lineValue * i;
                double y = marginTB + lineY * i - ISecondThickness / 2;

                label = CreateLabel();
                label.Content = string.Format("{0:F" + Precision + "}", v);
                Canvas.SetLeft(label, 0);
                Canvas.SetTop(label, y - ILabelHeight / 2);
                canvas.Children.Add(label);
                line = new Line()
                {
                    X1 = secondMargin,
                    X2 = secondMargin + secondWidth,
                    Y1 = y,
                    Y2 = y,
                    Stroke = LineBrush,
                    StrokeThickness = ISecondThickness
                };
                canvas.Children.Add(line);
            }

            //Min
            label = CreateLabel();
            label.Content = string.Format("{0:F" + Precision + "}", MinValue);
            Canvas.SetLeft(label, 0);
            Canvas.SetTop(label, MouseMoveMaxY);
            canvas.Children.Add(label);
            line = new Line()
            {
                X1 = marginLR,
                X2 = marginLR + IX,
                Y1 = marginTB + MouseMoveMaxY,
                Y2 = marginTB + MouseMoveMaxY,
                Stroke = LineBrush,
                StrokeThickness = IPrimaryThickness
            };
            canvas.Children.Add(line);

            //Middle
            line = new Line()
            {
                X1 = middleLineX,
                X2 = middleLineX,
                Y1 = marginTB,
                Y2 = marginTB + MouseMoveMaxY,
                Stroke = LineBrush,
                StrokeThickness = ISecondThickness
            };
            canvas.Children.Add(line);

            //Mouse Line
            MouseLine = new Line
            {
                Stroke = MouseLineBrush,
                StrokeThickness = ISecondThickness,
                X1 = secondMargin,
                X2 = wndWidth
            };
            canvas.Children.Add(MouseLine);

            //Window
            IWin = new Window();
            IWin.Width = wndWidth;
            IWin.Height = wndHeight;
            IWin.WindowStyle = WindowStyle.None;
            IWin.ResizeMode = ResizeMode.NoResize;
            IWin.Owner = Application.Current.MainWindow;
            IWin.AllowsTransparency = true;
            SolidColorBrush solidColorBrush = new SolidColorBrush();
            solidColorBrush.Opacity = 0;
            IWin.Background = solidColorBrush;
            IWin.Content = canvas;

            double ypos = CalculateValuePos();
            IWin.Top = currentMousePos.Y - ypos;
            MouseLine.Y1 = ypos;
            MouseLine.Y2 = ypos;
            relativeStartMouseY = startMousePos.Y - IWin.Top;
        }

        private void ReleaseIWin()
        {
            if (IWin != null)
            {
                IWin.Close();
                IWin = null;
                MouseLine = null;
            }
        }

        private double GetValidValue(double value)
        {
            return value > MaxValue ? MaxValue : (value < MinValue ? MinValue : value);
        }

        private bool ValidateInputValue(string value)
        {
            string text;
            if (this.SelectionLength == 0)
            {
                text = this.Text.Insert(this.SelectionStart, value);
            }
            else
            {
                string selection = this.Text.Remove(this.SelectionStart, this.SelectionLength);
                text = selection.Insert(this.SelectionStart, value);
            }

            if (text == "-")
                text += 0;

            return double.TryParse(text, out double val);
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            this.SelectAll();
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ReleaseIWin();
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !ValidateInputValue(e.Text);
        }

        private void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!ValidateInputValue(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void TextBox_PreviewMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            Cursor = Cursors.ScrollNS;
            startMousePos = currentMousePos = PointToScreen(e.GetPosition(this));
            originalValue = Value;
            interval = Math.Abs(MaxValue - MinValue);

            InitIWin();

            CaptureMouse();
        }

        private void TextBox_PreviewMouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            ReleaseMouseCapture();

            ReleaseIWin();

            Cursor = null;
        }

        private void TextBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (IsMouseCaptured && e.RightButton == MouseButtonState.Pressed)
            {
                Point previousMousePos = currentMousePos;
                currentMousePos = PointToScreen(e.GetPosition(this));

                //Console.WriteLine(string.Format("Previous: {0} Current:{1}", previousMousePos, currentMousePos));

                double moveY = startMousePos.Y - currentMousePos.Y;
                double ratio = moveY / MouseMoveMaxY;
                double delta = ratio * interval;
                double val = Math.Round(delta / Step) * Step;

                this.Value = GetValidValue(originalValue + val);

                //Console.WriteLine("MoveY: {0} Ratio: {1} Delta: {2} OriginalValue: {3} Value: {4}", moveY, ratio, delta, originalValue, Value);

                if (IWin != null)
                {
                    IWin.Left = currentMousePos.X - (ILabelWidth + IX + 20);

                    double ypos = CalculateValuePos();
                    MouseLine.Y1 = ypos;
                    MouseLine.Y2 = ypos;

                    if (currentMousePos.Y > (IWin.Top + IWin.Height))
                    {
                        IWin.Top = currentMousePos.Y - IWin.Height;
                        startMousePos.Y = IWin.Top + relativeStartMouseY;
                    }
                    else if (currentMousePos.Y < IWin.Top)
                    {
                        IWin.Top = currentMousePos.Y;
                        startMousePos.Y = IWin.Top + relativeStartMouseY;
                    }

                    IWin.Show();
                }
            }
        }
    }
}
