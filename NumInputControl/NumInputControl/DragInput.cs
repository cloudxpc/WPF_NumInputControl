using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NumInputControl
{
    public class DragInput : ContentControl
    {
        public static DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(object), typeof(DragInput),
                new PropertyMetadata(0.0, ValuePropertyChangedCallback, ValueCoerceValueCallback));
        public static DependencyProperty MaxValueProperty =
            DependencyProperty.Register(nameof(MaxValue), typeof(double), typeof(DragInput),
                new PropertyMetadata(10000.0, MaxValuePropertyChangedCallback, MaxValueCoerceValueCallback));
        public static DependencyProperty MinValueProperty =
            DependencyProperty.Register(nameof(MinValue), typeof(double), typeof(DragInput),
                new PropertyMetadata(-10000.0, MinValuePropertyChangedCallback, MinValueCoerceValueCallback));
        public static DependencyProperty StepProperty = DependencyProperty.Register(nameof(Step), typeof(double), typeof(DragInput), new PropertyMetadata(1.0));
        public static DependencyProperty PrecisionProperty = DependencyProperty.Register(nameof(Precision), typeof(int), typeof(DragInput), new PropertyMetadata(2));

        public static void ValuePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        public static object ValueCoerceValueCallback(DependencyObject d, object baseValue)
        {
            DragInput dragInput = d as DragInput;
            double val;

            if (double.TryParse(baseValue.ToString(), out val))
            {
                val = val > dragInput.MaxValue ? dragInput.MaxValue : (val < dragInput.MinValue ? dragInput.MinValue : val);
            }

            return string.Format("{0:F" + dragInput.Precision + "}", val);
        }

        public static void MaxValuePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(ValueProperty);
        }

        public static object MaxValueCoerceValueCallback(DependencyObject d, object baseValue)
        {
            DragInput dragInput = d as DragInput;
            double max = (double)baseValue;
            return max < dragInput.MinValue ? dragInput.MinValue : max;
        }

        public static void MinValuePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(ValueProperty);
        }

        public static object MinValueCoerceValueCallback(DependencyObject d, object baseValue)
        {
            DragInput dragInput = d as DragInput;
            double min = (double)baseValue;
            return min > dragInput.MaxValue ? dragInput.MaxValue : min;
        }

        private const int MouseMoveMaxY = 300;
        private const int IX = 50;
        private const int IPrimaryThickness = 5;
        private const int ISecondThickness = 3;
        private const int IMaxLines = 5;

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

        private FrameworkElement _element;

        public object Value
        {
            get => this.GetValue(ValueProperty);
            set => this.SetValue(ValueProperty, value);
        }

        public double NumValue
        {
            get
            {
                double.TryParse((Value ?? "0").ToString(), out double val);
                return val;
            }
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

        public override void OnApplyTemplate()
        {
            if (Content is FrameworkElement ele)
            {
                _element = ele;
                _element.PreviewMouseRightButtonDown += Content_PreviewMouseButtonDown;
                _element.PreviewMouseRightButtonUp += Content_PreviewMouseButtonUp;
                _element.PreviewMouseMove += Content_PreviewMouseMove;
            }
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

        private Label CreateLabel(double val, double left, double top)
        {
            Label label = new Label()
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
                Height = ILabelHeight,
                Content = string.Format("{0:F" + Precision + "}", val)
            };

            Canvas.SetLeft(label, left);
            Canvas.SetTop(label, top);

            return label;
        }

        private double CalculateValuePos()
        {
            double pct = 1 - (NumValue - MinValue) / interval;
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

            //Max
            canvas.Children.Add(CreateLabel(MaxValue, 0, 0));
            canvas.Children.Add(new Line()
            {
                X1 = marginLR,
                X2 = marginLR + IX,
                Y1 = marginTB,
                Y2 = marginTB,
                Stroke = LineBrush,
                StrokeThickness = IPrimaryThickness
            });

            double lineValue = Math.Abs(MaxValue - MinValue) / IMaxLines;
            int lineY = MouseMoveMaxY / IMaxLines;
            for (int i = 1; i < IMaxLines; i++)
            {
                double v = MaxValue - lineValue * i;
                double y = marginTB + lineY * i - ISecondThickness / 2;

                canvas.Children.Add(CreateLabel(v, 0, y - ILabelHeight / 2));
                canvas.Children.Add(new Line()
                {
                    X1 = secondMargin,
                    X2 = secondMargin + secondWidth,
                    Y1 = y,
                    Y2 = y,
                    Stroke = LineBrush,
                    StrokeThickness = ISecondThickness
                });
            }

            //Min
            canvas.Children.Add(CreateLabel(MinValue, 0, MouseMoveMaxY));
            canvas.Children.Add(new Line()
            {
                X1 = marginLR,
                X2 = marginLR + IX,
                Y1 = marginTB + MouseMoveMaxY,
                Y2 = marginTB + MouseMoveMaxY,
                Stroke = LineBrush,
                StrokeThickness = IPrimaryThickness
            });

            //Middle
            canvas.Children.Add(new Line()
            {
                X1 = middleLineX,
                X2 = middleLineX,
                Y1 = marginTB,
                Y2 = marginTB + MouseMoveMaxY,
                Stroke = LineBrush,
                StrokeThickness = ISecondThickness
            });

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
            IWin = new Window
            {
                Width = wndWidth,
                Height = wndHeight,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                Owner = Application.Current.MainWindow,
                AllowsTransparency = true,
                Background = new SolidColorBrush { Opacity = 0 },
                Content = canvas
            };

            double ypos = CalculateValuePos();
            IWin.Top = startMousePos.Y - ypos;
            MouseLine.Y1 = ypos;
            MouseLine.Y2 = ypos;
            relativeStartMouseY = ypos;
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

        private void Content_PreviewMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_element == null)
                return;

            e.Handled = true;
            _element.Cursor = Cursors.ScrollNS;
            startMousePos = _element.PointToScreen(e.GetPosition(this));
            originalValue = NumValue;
            interval = Math.Abs(MaxValue - MinValue);

            InitIWin();

            _element.CaptureMouse();
        }

        private void Content_PreviewMouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_element == null)
                return;

            _element.ReleaseMouseCapture();

            ReleaseIWin();

            _element.Cursor = null;
        }

        private void Content_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_element != null && _element.IsMouseCaptured && e.RightButton == MouseButtonState.Pressed)
            {
                Point currentMousePos = _element.PointToScreen(e.GetPosition(this));

                //Console.WriteLine(string.Format("Previous: {0} Current:{1}", previousMousePos, currentMousePos));

                double moveY = startMousePos.Y - currentMousePos.Y;
                double ratio = moveY / MouseMoveMaxY;
                double delta = ratio * interval;
                double val = Math.Round(delta / Step) * Step;

                this.Value = originalValue + val;

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
