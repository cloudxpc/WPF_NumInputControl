using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NumInputControl
{
    public class DragInput : ContentControl, INotifyPropertyChanged
    {
        public static DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(object), typeof(DragInput),
                new PropertyMetadata(0.0, ValuePropertyChangedCallback));
        public static DependencyProperty MaxValueProperty =
            DependencyProperty.Register(nameof(MaxValue), typeof(double), typeof(DragInput),
                new PropertyMetadata(10000.0, MaxValuePropertyChangedCallback, MaxValueCoerceValueCallback));
        public static DependencyProperty MinValueProperty =
            DependencyProperty.Register(nameof(MinValue), typeof(double), typeof(DragInput),
                new PropertyMetadata(-10000.0, MinValuePropertyChangedCallback, MinValueCoerceValueCallback));
        public static DependencyProperty StepProperty = DependencyProperty.Register(nameof(Step), typeof(double), typeof(DragInput), new PropertyMetadata(1.0));
        public static DependencyProperty PrecisionProperty = DependencyProperty.Register(nameof(Precision), typeof(int), typeof(DragInput), new PropertyMetadata(2));
        public static DependencyProperty MaxLinesProperty = DependencyProperty.Register(nameof(MaxLines), typeof(int), typeof(DragInput), new PropertyMetadata(5));
        public static DependencyProperty PrimaryThicknessProperty = DependencyProperty.Register(nameof(PrimaryThickness), typeof(double), typeof(DragInput), new PropertyMetadata(5.0));
        public static DependencyProperty SecondThicknessProperty = DependencyProperty.Register(nameof(SecondThickness), typeof(double), typeof(DragInput), new PropertyMetadata(3.0));
        public static DependencyProperty ControlHeightProperty = DependencyProperty.Register(nameof(ControlHeight), typeof(double), typeof(DragInput), new PropertyMetadata(300.0));
        public static DependencyProperty ControlWidthProperty = DependencyProperty.Register(nameof(ControlWidth), typeof(double), typeof(DragInput), new PropertyMetadata(50.0));
        public static DependencyProperty ColorProperty = DependencyProperty.Register(nameof(Color), typeof(Brush), typeof(DragInput), new PropertyMetadata(new SolidColorBrush(Colors.Red)));
        public static DependencyProperty MouseLineColorProperty = DependencyProperty.Register(nameof(MouseLineColor), typeof(Brush), typeof(DragInput), new PropertyMetadata(new SolidColorBrush(Colors.Gray)));

        public static void ValuePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DragInput dragInput = d as DragInput;
            double val;

            string strVal = e.NewValue == null ? "0" : e.NewValue.ToString();

            if (double.TryParse(strVal, out val))
                val = val > dragInput.MaxValue ? dragInput.MaxValue : val < dragInput.MinValue ? dragInput.MinValue : val;

            string formattedVal = string.Format("{0:F" + dragInput.Precision + "}", val);

            if (strVal != formattedVal)
                dragInput.Value = formattedVal;
        }

        //Not using this way because coerced value does not update the source of binding
        public static object ValueCoerceValueCallback(DependencyObject d, object baseValue)
        {
            DragInput dragInput = d as DragInput;
            double val;

            if (double.TryParse(baseValue == null ? "0" : baseValue.ToString(), out val))
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

        private Point startMousePos;
        private double originalValue;
        private double interval;
        private double relativeStartMouseY;
        private double mouseY;

        private Window IWin;
        private Label MouseLabel;
        private Line MouseLine;
        private FontFamily ILabelFontFamily = new FontFamily("Arial");
        private FontStyle ILabelFontStyle = FontStyles.Normal;
        private FontWeight ILabelFontWeight = FontWeights.Bold;
        private FontStretch ILabelFontStretch = FontStretches.Normal;
        private double LabelFontSize = 14;
        private double ILabelWidth;
        private double ILabelHeight;

        private FrameworkElement _element;

        public event PropertyChangedEventHandler PropertyChanged;

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

        public int MaxLines
        {
            get => (int)this.GetValue(MaxLinesProperty);
            set => this.SetValue(MaxLinesProperty, value);
        }

        public double PrimaryThickness
        {
            get => (double)this.GetValue(PrimaryThicknessProperty);
            set => this.SetValue(PrimaryThicknessProperty, value);
        }

        public double SecondThickness
        {
            get => (double)this.GetValue(SecondThicknessProperty);
            set => this.SetValue(SecondThicknessProperty, value);
        }

        public double ControlHeight
        {
            get => (double)this.GetValue(ControlHeightProperty);
            set => this.SetValue(ControlHeightProperty, value);
        }

        public double ControlWidth
        {
            get => (double)this.GetValue(ControlWidthProperty);
            set => this.SetValue(ControlWidthProperty, value);
        }

        public Brush Color
        {
            get => (Brush)this.GetValue(ColorProperty);
            set => this.SetValue(ColorProperty, value);
        }

        public Brush MouseLineColor
        {
            get => (Brush)this.GetValue(MouseLineColorProperty);
            set => this.SetValue(MouseLineColorProperty, value);
        }

        public double MouseY
        {
            get => mouseY;
            set
            {
                if (mouseY != value)
                {
                    mouseY = value;
                    OnPropertyChanged("MouseY");
                }
            }
        }

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
                Foreground = Color,
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
            double ypos = ControlHeight * pct;
            return (ILabelHeight / 2 + ypos) - SecondThickness / 2;
        }

        private void InitIWin()
        {
            ReleaseIWin();
            CalculateLabelSize();

            double marginLR = ILabelWidth + 5;
            double marginTB = ILabelHeight / 2;
            double secondWidth = ControlWidth / 2.0;
            double secondMargin = marginLR + (ControlWidth - secondWidth) / 2;
            double middleLineX = marginLR + ControlWidth / 2 - SecondThickness / 2;
            double wndWidth = ILabelWidth + ControlWidth + 40;
            double wndHeight = marginTB * 2 + ControlHeight;
            Canvas canvas = new Canvas();

            //Max
            canvas.Children.Add(CreateLabel(MaxValue, 0, 0));
            canvas.Children.Add(new Line()
            {
                X1 = marginLR,
                X2 = marginLR + ControlWidth,
                Y1 = marginTB,
                Y2 = marginTB,
                Stroke = Color,
                StrokeThickness = PrimaryThickness
            });

            double lineValue = Math.Abs(MaxValue - MinValue) / MaxLines;
            double lineY = ControlHeight / MaxLines;
            for (int i = 1; i < MaxLines; i++)
            {
                double v = MaxValue - lineValue * i;
                double y = marginTB + lineY * i - SecondThickness / 2;

                canvas.Children.Add(CreateLabel(v, 0, y - ILabelHeight / 2));
                canvas.Children.Add(new Line()
                {
                    X1 = secondMargin,
                    X2 = secondMargin + secondWidth,
                    Y1 = y,
                    Y2 = y,
                    Stroke = Color,
                    StrokeThickness = SecondThickness
                });
            }

            //Min
            canvas.Children.Add(CreateLabel(MinValue, 0, ControlHeight));
            canvas.Children.Add(new Line()
            {
                X1 = marginLR,
                X2 = marginLR + ControlWidth,
                Y1 = marginTB + ControlHeight,
                Y2 = marginTB + ControlHeight,
                Stroke = Color,
                StrokeThickness = PrimaryThickness
            });

            //Middle
            canvas.Children.Add(new Line()
            {
                X1 = middleLineX,
                X2 = middleLineX,
                Y1 = marginTB,
                Y2 = marginTB + ControlHeight,
                Stroke = Color,
                StrokeThickness = SecondThickness
            });

            //Mouse Line
            MouseLabel = CreateLabel(0, 0, 0);
            MouseLabel.Foreground = MouseLineColor;
            MouseLabel.Margin = new Thickness(0, -ILabelHeight / 2, 0, 0);
            BindingOperations.SetBinding(MouseLabel, Label.ContentProperty, new Binding("Value") { Source = this });
            BindingOperations.SetBinding(MouseLabel, Canvas.TopProperty, new Binding("MouseY") { Source = this });
            canvas.Children.Add(MouseLabel);
            MouseLine = new Line
            {
                Stroke = MouseLineColor,
                StrokeThickness = SecondThickness,
                X1 = secondMargin,
                X2 = wndWidth
            };
            BindingOperations.SetBinding(MouseLine, Line.Y1Property, new Binding("MouseY") { Source = this });
            BindingOperations.SetBinding(MouseLine, Line.Y2Property, new Binding("MouseY") { Source = this });
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
            relativeStartMouseY = ypos;
            MouseY = ypos;
        }

        private void ReleaseIWin()
        {
            if (IWin != null)
            {
                IWin.Close();
                IWin = null;
                BindingOperations.ClearAllBindings(MouseLine);
                MouseLine = null;
                BindingOperations.ClearAllBindings(MouseLabel);
                MouseLabel = null;
            }
        }

        private void Content_PreviewMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_element == null)
                return;

            e.Handled = true;
            _element.Cursor = Cursors.ScrollNS;
            startMousePos = _element.PointToScreen(e.GetPosition(_element));
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
                Point currentMousePos = _element.PointToScreen(e.GetPosition(_element));

                //Console.WriteLine(string.Format("Previous: {0} Current:{1}", previousMousePos, currentMousePos));

                double moveY = startMousePos.Y - currentMousePos.Y;
                double ratio = moveY / ControlHeight;
                double delta = ratio * interval;
                double val = Math.Round(delta / Step) * Step;

                this.Value = originalValue + val;

                //Console.WriteLine("MoveY: {0} Ratio: {1} Delta: {2} OriginalValue: {3} Value: {4}", moveY, ratio, delta, originalValue, Value);

                if (IWin != null)
                {
                    IWin.Left = currentMousePos.X - (ILabelWidth + ControlWidth + 20);

                    MouseY = CalculateValuePos();

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
