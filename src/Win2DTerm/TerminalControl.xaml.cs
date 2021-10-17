using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.IO;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TerminalEmulator.VirtualTerminal;
using TerminalEmulator.VirtualTerminal.Model;
using TerminalEmulator.XTermParser;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;


// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Win2DTerm
{
    public sealed partial class TerminalControl : UserControl
    {
        public VirtualTerminalController Terminal { get; set; } = new VirtualTerminalController();
        public DataConsumer Consumer { get; set; }

        public int ViewTop { get; set; } = 0;

        public TerminalControl()
        {
            InitializeComponent();

            Consumer = new DataConsumer(Terminal);

            Terminal.SendData += OnSendData;
        }

        private void OnSendData(object sender, SendDataEventArgs e)
        {
            Task.Run(() =>
            {
                try
                {
                    _stream.Write(e.Data, 0, e.Data.Length);
                    _stream.Flush();
                }
                catch (Exception ex)
                {

                }
            });
        }

        private void CoreWindow_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
        {
            try
            {
                if (!Connected)
                    return;

                var ch = char.ConvertFromUtf32((int)args.KeyCode);

                var toSend = System.Text.Encoding.UTF8.GetBytes(ch.ToString());
                _stream.Write(toSend, 0, toSend.Length);
                _stream.Flush();

                //System.Diagnostics.Debug.WriteLine(ch.ToString());
                args.Handled = true;
            }
            catch (Exception ex)
            {

            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            canvas.RemoveFromVisualTree();
            canvas = null;
        }

        public double CharacterWidth = -1;
        public double CharacterHeight = -1;
        public int Columns = -1;
        public int Rows = -1;

        AuthenticationMethod _authenticationMethod;
        ConnectionInfo _connectionInfo;
        public SshClient _client;
        ShellStream _stream;

        public bool Connected
        {
            get
            {
                return _stream != null && _client.IsConnected && _stream.CanWrite;
            }
        }

        string InputBuffer { get; set; } = "";
        public bool ConnectToSsh(string hostname, string username, string password)
        {
            _authenticationMethod = new PasswordAuthenticationMethod(username, password);

            _connectionInfo = new ConnectionInfo(hostname, username, _authenticationMethod);
            _client = new SshClient(_connectionInfo);
            _client.Connect();
            _stream = _client.CreateShellStream("xterm", (uint)Columns, (uint)Rows, 800, 300, 16384);
            _stream.DataReceived += _OnDataReceived;

            return true;
        }
        public bool ConnectToSsh(string hostname, int port, string username, string password)
        {
            _authenticationMethod = new PasswordAuthenticationMethod(username, password);

            _connectionInfo = new ConnectionInfo(hostname, port, username, _authenticationMethod);
            _client = new SshClient(_connectionInfo);
            _client.Connect();
            _stream = _client.CreateShellStream("xterm", (uint)Columns, (uint)Rows, 800, 300, 16384);
            _stream.DataReceived += _OnDataReceived;

            return true;
        }
        public async Task<bool> ConnectToSsh(string hostname, int port, string username, StorageFile keyFile)
        {
            using(var randomAccessStream = await keyFile.OpenAsync(FileAccessMode.Read))
            {
                Stream stream = randomAccessStream.AsStreamForRead();
                PrivateKeyFile privateKeyFile = new PrivateKeyFile(stream);
                _authenticationMethod = new PrivateKeyAuthenticationMethod(username, privateKeyFile);
            }

            _connectionInfo = new ConnectionInfo(hostname, port, username, _authenticationMethod);
            _client = new SshClient(_connectionInfo);
            _client.Connect();
            _stream = _client.CreateShellStream("xterm", (uint)Columns, (uint)Rows, 800, 300, 16384);
            _stream.DataReceived += _OnDataReceived;

            return true;
        }
        public void Disconnect()
        {
            _client.Disconnect();
        }
        public string RunCommand(string cmd)
        {
            var result = _client.RunCommand(cmd);
            return result.Result;
        }

        private void _OnDataReceived(object sender, ShellDataEventArgs e)
        {
            try
            {

                //System.Diagnostics.Debug.WriteLine(e.Data.Length.ToString() + " received");

                lock (Terminal)
                {
                    int oldTopRow = Terminal.ViewPort.TopRow;

                    Consumer.Push(e.Data);

                    if (Terminal.Changed)
                    {
                        Terminal.ClearChanges();

                        if (oldTopRow != Terminal.ViewPort.TopRow && oldTopRow >= ViewTop)
                            ViewTop = Terminal.ViewPort.TopRow;

                        canvas.Invalidate();
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
        public void PushNewData(string data)
        {
            try
            {
                lock (Terminal)
                {
                    int oldTopRow = Terminal.ViewPort.TopRow;
                    var bData = Encoding.UTF8.GetBytes(data);
                    Consumer.Push(bData);

                    if (Terminal.Changed)
                    {
                        Terminal.ClearChanges();

                        if (oldTopRow != Terminal.ViewPort.TopRow && oldTopRow >= ViewTop)
                            ViewTop = Terminal.ViewPort.TopRow;

                        canvas.Invalidate();
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        bool ViewDebugging = false;
        private void OnCanvasDraw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            try
            {
                CanvasDrawingSession drawingSession = args.DrawingSession;

                CanvasTextFormat format =
                    new CanvasTextFormat
                    {
                        FontSize = Convert.ToSingle(canvas.FontSize),
                        FontFamily = canvas.FontFamily.Source,
                        FontWeight = canvas.FontWeight,
                        WordWrapping = CanvasWordWrapping.NoWrap
                    };

                ProcessTextFormat(drawingSession, format);

                drawingSession.FillRectangle(new Rect(0, 0, canvas.RenderSize.Width, canvas.RenderSize.Height), GetBackgroundColor(Terminal.CursorState.Attribute, false));

                lock (Terminal)
                {
                    int row = ViewTop;
                    float verticalOffset = -row * (float)CharacterHeight;

                    var lines = Terminal.ViewPort.GetLines(ViewTop, Rows);

                    var defaultTransform = drawingSession.Transform;
                    foreach (var line in lines)
                    {
                        if (line == null)
                        {
                            row++;
                            continue;
                        }

                        int column = 0;

                        drawingSession.Transform = Matrix3x2.CreateScale(
                            (float)(line.DoubleWidth ? 2.0 : 1.0),
                            (float)(line.DoubleHeightBottom | line.DoubleHeightTop ? 2.0 : 1.0)
                        );

                        foreach (var character in line)
                        {
                            bool selected = TextSelection == null ? false : TextSelection.Within(column, row);

                            var rect = new Rect(
                                column * CharacterWidth,
                                ((row - (line.DoubleHeightBottom ? 1 : 0)) * CharacterHeight + verticalOffset) * (line.DoubleHeightBottom | line.DoubleHeightTop ? 0.5 : 1.0),
                                CharacterWidth + 0.9,
                                CharacterHeight + 0.9
                            );

                            var textLayout = new CanvasTextLayout(drawingSession, character.Char.ToString(), format, 0.0f, 0.0f);
                            var backgroundColor = GetBackgroundColor(character.Attributes, selected);
                            var foregroundColor = GetForegroundColor(character.Attributes, selected);
                            drawingSession.FillRectangle(rect, backgroundColor);

                            drawingSession.DrawTextLayout(
                                textLayout,
                                (float)rect.Left,
                                (float)rect.Top,
                                foregroundColor
                            );

                            if (character.Attributes.Underscore)
                            {
                                drawingSession.DrawLine(
                                    new Vector2(
                                        (float)rect.Left,
                                        (float)rect.Bottom
                                    ),
                                    new Vector2(
                                        (float)rect.Right,
                                        (float)rect.Bottom
                                    ),
                                    foregroundColor
                                );
                            }

                            column++;
                        }
                        row++;
                    }
                    drawingSession.Transform = defaultTransform;

                    if (Terminal.CursorState.ShowCursor)
                    {
                        var cursorY = Terminal.ViewPort.TopRow - ViewTop + Terminal.CursorState.CurrentRow;
                        var cursorRect = new Rect(
                            Terminal.CursorState.CurrentColumn * CharacterWidth,
                            cursorY * CharacterHeight,
                            CharacterWidth + 0.9,
                            CharacterHeight + 0.9
                        );

                        drawingSession.DrawRectangle(cursorRect, GetForegroundColor(Terminal.CursorState.Attribute, false));
                    }
                }

                if (ViewDebugging)
                    AnnotateView(drawingSession);
            }
            catch (Exception ex)
            {

            }
        }

        private void AnnotateView(CanvasDrawingSession drawingSession)
        {
            try
            {
                CanvasTextFormat lineNumberFormat =
                                new CanvasTextFormat
                                {
                                    FontSize = Convert.ToSingle(canvas.FontSize / 2),
                                    FontFamily = canvas.FontFamily.Source,
                                    FontWeight = canvas.FontWeight,
                                    WordWrapping = CanvasWordWrapping.NoWrap
                                };

                for (var i = 0; i < Rows; i++)
                {
                    string s = i.ToString();
                    var textLayout = new CanvasTextLayout(drawingSession, s.ToString(), lineNumberFormat, 0.0f, 0.0f);
                    float y = i * (float)CharacterHeight;
                    drawingSession.DrawLine(0, y, (float)canvas.RenderSize.Width, y, Colors.Beige);
                    drawingSession.DrawTextLayout(textLayout, (float)(canvas.RenderSize.Width - (CharacterWidth / 2 * s.Length)), y, Colors.Yellow);

                    s = (i + 1).ToString();
                    textLayout = new CanvasTextLayout(drawingSession, s.ToString(), lineNumberFormat, 0.0f, 0.0f);
                    drawingSession.DrawTextLayout(textLayout, (float)(canvas.RenderSize.Width - (CharacterWidth / 2 * (s.Length + 3))), y, Colors.Green);
                }
            }
            catch (Exception ex)
            {

            }
        }

        private static Color[] AttributeColors =
        {
            Color.FromArgb(255,0,0,0),        // Black
            Color.FromArgb(255,187,0,0),      // Red
            Color.FromArgb(255,0,187,0),      // Green
            Color.FromArgb(255,187,187,0),    // Yellow
            Color.FromArgb(255,0,0,187),      // Blue
            Color.FromArgb(255,187,0,187),    // Magenta
            Color.FromArgb(255,0,187,187),    // Cyan
            Color.FromArgb(255,187,187,187),  // White
            Color.FromArgb(255,85,85,85),     // Bright black
            Color.FromArgb(255,255,85,85),    // Bright red
            Color.FromArgb(255,85,255,85),    // Bright green
            Color.FromArgb(255,255,255,85),   // Bright yellow
            Color.FromArgb(255,85,85,255),    // Bright blue
            Color.FromArgb(255,255,85,255),   // Bright Magenta
            Color.FromArgb(255,85,255,255),   // Bright cyan
            Color.FromArgb(255,255,255,255),  // Bright white
        };

        private static SolidColorBrush[] AttributeBrushes =
        {
            new SolidColorBrush(AttributeColors[0]),
            new SolidColorBrush(AttributeColors[1]),
            new SolidColorBrush(AttributeColors[2]),
            new SolidColorBrush(AttributeColors[3]),
            new SolidColorBrush(AttributeColors[4]),
            new SolidColorBrush(AttributeColors[5]),
            new SolidColorBrush(AttributeColors[6]),
            new SolidColorBrush(AttributeColors[7]),
            new SolidColorBrush(AttributeColors[8]),
            new SolidColorBrush(AttributeColors[9]),
            new SolidColorBrush(AttributeColors[10]),
            new SolidColorBrush(AttributeColors[11]),
            new SolidColorBrush(AttributeColors[12]),
            new SolidColorBrush(AttributeColors[13]),
            new SolidColorBrush(AttributeColors[14]),
            new SolidColorBrush(AttributeColors[15]),
        };

        private Color GetBackgroundColor(TerminalAttribute attribute, bool invert)
        {
            try
            {
                var flip = Terminal.CursorState.ReverseVideoMode ^ attribute.Reverse ^ invert;

                if (flip)
                {
                    if (attribute.Bright)
                        return AttributeColors[(int)attribute.ForegroundColor + 8];

                    return AttributeColors[(int)attribute.ForegroundColor];
                }

                return AttributeColors[(int)attribute.BackgroundColor];
            }
            catch (Exception ex)
            {

            }
            return Colors.Black;
        }

        private Color GetForegroundColor(TerminalAttribute attribute, bool invert)
        {
            try
            {
                var flip = Terminal.CursorState.ReverseVideoMode ^ attribute.Reverse ^ invert;

                if (flip)
                    return AttributeColors[(int)attribute.BackgroundColor];

                if (attribute.Bright)
                    return AttributeColors[(int)attribute.ForegroundColor + 8];

                return AttributeColors[(int)attribute.ForegroundColor];
            }
            catch (Exception ex)
            {

            }
            return Colors.Green;
        }

        private void ProcessTextFormat(CanvasDrawingSession drawingSession, CanvasTextFormat format)
        {
            try
            {
                CanvasTextLayout textLayout = new CanvasTextLayout(drawingSession, "Q", format, 0.0f, 0.0f);
                if (CharacterWidth != textLayout.DrawBounds.Width || CharacterHeight != textLayout.DrawBounds.Height)
                {
                    CharacterWidth = textLayout.DrawBounds.Right;
                    CharacterHeight = textLayout.DrawBounds.Bottom;
                }

                int columns = Convert.ToInt32(Math.Floor(canvas.RenderSize.Width / CharacterWidth));
                int rows = Convert.ToInt32(Math.Floor(canvas.RenderSize.Height / CharacterHeight));
                if (Columns != columns || Rows != rows)
                {
                    Columns = columns;
                    Rows = rows;
                    ResizeTerminal();

                    if (_stream != null && _stream.CanWrite)
                        _stream.SendWindowChangeRequest((uint)columns, (uint)rows, 800, 600);
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void ResizeTerminal()
        {
            //System.Diagnostics.Debug.WriteLine("ResizeTerminal()");
            //System.Diagnostics.Debug.WriteLine("  Character size " + CharacterWidth.ToString() + "," + CharacterHeight.ToString());
            //System.Diagnostics.Debug.WriteLine("  Terminal size " + Columns.ToString() + "," + Rows.ToString());

            try
            {
                Terminal.ResizeView(Columns, Rows);
            }
            catch (Exception ex)
            {

            }
        }

        private void TerminalKeyDown(object sender, KeyRoutedEventArgs e)
        {
            try
            {
                if (!Connected)
                    return;

                var controlPressed = (Window.Current.CoreWindow.GetKeyState(Windows.System.VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down));
                var shiftPressed = (Window.Current.CoreWindow.GetKeyState(Windows.System.VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down));

                switch (e.Key)
                {
                    case Windows.System.VirtualKey.Shift:
                    case Windows.System.VirtualKey.Control:
                        return;

                    default:
                        break;
                }

                if (controlPressed && e.Key == Windows.System.VirtualKey.F12)
                    Terminal.Debugging = !Terminal.Debugging;

                if (controlPressed && e.Key == Windows.System.VirtualKey.F10)
                    Consumer.SequenceDebugging = !Consumer.SequenceDebugging;

                if (controlPressed && e.Key == Windows.System.VirtualKey.F11)
                {
                    ViewDebugging = !ViewDebugging;
                    canvas.Invalidate();
                }

                var code = Terminal.GetKeySequence((controlPressed ? "Ctrl+" : "") + (shiftPressed ? "Shift+" : "") + e.Key.ToString());
                if (code != null)
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            _stream.Write(code, 0, code.Length);
                            _stream.Flush();
                        }
                        catch (Exception ex)
                        {

                        }
                    });

                    e.Handled = true;

                    if (ViewTop != Terminal.ViewPort.TopRow)
                    {
                        Terminal.ViewPort.SetTopLine(ViewTop);
                        canvas.Invalidate();
                    }
                }
            }
            catch (Exception ex)
            {

            }

            //System.Diagnostics.Debug.WriteLine(e.Key.ToString() + ",S" + (shiftPressed ? "1" : "0") + ",C" + (controlPressed ? "1" : "0"));
        }

        private void TerminalTapped(object sender, TappedRoutedEventArgs e)
        {
            this.Focus(FocusState.Pointer);
        }
        public void SetFocus()
        {
            this.Focus(FocusState.Pointer);
        }
        private void TerminalGotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                Window.Current.CoreWindow.CharacterReceived += CoreWindow_CharacterReceived;
            }
            catch (Exception ex)
            {

            }
        }

        private void TerminalLostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                Window.Current.CoreWindow.CharacterReceived -= CoreWindow_CharacterReceived;
            }
            catch (Exception ex)
            {

            }
        }

        public bool ScrollModeState = false;
        private void TerminalWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                var pointer = e.GetCurrentPoint(canvas);

                int oldViewTop = ViewTop;

                ViewTop -= pointer.Properties.MouseWheelDelta / 40;
                if (ViewTop < 0)
                    ViewTop = 0;
                else if (ViewTop > Terminal.ViewPort.TopRow)
                    ViewTop = Terminal.ViewPort.TopRow;

                if (oldViewTop != ViewTop)
                    canvas.Invalidate();
            }
            catch (Exception ex)
            {

            }
        }
        public void TerminalWheelChanged(int Delta)
        {
            try
            {
                int oldViewTop = ViewTop;

                ViewTop -= Delta / 40;
                if (ViewTop < 0)
                    ViewTop = 0;
                else if (ViewTop > Terminal.ViewPort.TopRow)
                    ViewTop = Terminal.ViewPort.TopRow;

                if (oldViewTop != ViewTop)
                    canvas.Invalidate();
            }
            catch (Exception ex)
            {

            }
        }
        TextPosition MouseOver { get; set; } = new TextPosition();

        TextRange TextSelection { get; set; }
        bool Selecting = false;

        private TextPosition ToPosition(Point point)
        {
            try
            {
                int overColumn = (int)Math.Floor(point.X / CharacterWidth);
                if (overColumn >= Columns)
                    overColumn = Columns - 1;

                int overRow = (int)Math.Floor(point.Y / CharacterHeight);
                if (overRow >= Rows)
                    overRow = Rows - 1;

                return new TextPosition { Column = overColumn, Row = overRow };
            }
            catch (Exception ex)
            {

            }
            return new TextPosition { Column = 0, Row = 0 };
        }

        Point PreviousPoint;
        private void TerminalPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            {
                try
                {
                    var pointer = e.GetCurrentPoint(canvas);
                    var position = ToPosition(pointer.Position);

                    if (MouseOver != null && MouseOver == position)
                        return;

                    MouseOver = position;
                    if (ScrollModeState && pointer.Properties.IsLeftButtonPressed)
                    {
                        try
                        {
                            int oldViewTop = ViewTop;
                            double yDistance = pointer.Position.Y - PreviousPoint.Y;
                            PreviousPoint = pointer.Position;
                            ViewTop -= (int)Math.Round(yDistance) / 40;
                            if (ViewTop < 0)
                                ViewTop = 0;
                            else if (ViewTop > Terminal.ViewPort.TopRow)
                                ViewTop = Terminal.ViewPort.TopRow;

                            if (oldViewTop != ViewTop)
                                canvas.Invalidate();
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                    else
                   if (pointer.Properties.IsLeftButtonPressed)
                    {
                        TextRange newSelection;

                        var textPosition = position.OffsetBy(0, ViewTop);
                        if (MousePressedAt != textPosition)
                        {
                            if (MousePressedAt <= textPosition)
                            {
                                newSelection = new TextRange
                                {
                                    Start = MousePressedAt,
                                    End = textPosition.OffsetBy(-1, 0)
                                };
                            }
                            else
                            {
                                newSelection = new TextRange
                                {
                                    Start = textPosition,
                                    End = MousePressedAt
                                };
                            }
                            Selecting = true;

                            if (TextSelection != newSelection)
                            {
                                TextSelection = newSelection;

                                System.Diagnostics.Debug.WriteLine(
                                    "Selection: " + TextSelection.ToString()
                                );

                                canvas.Invalidate();
                            }
                        }
                    }

                    //System.Diagnostics.Debug.WriteLine("Pointer Moved (c" + MouseOverColumn.ToString() + ",r=" + MouseOverRow.ToString() + ")");
                }
                catch (Exception ex)
                {

                }
            }
        }
        public void copySelectedText()
        {
            if (TextSelection == null)
            {
                return;
            }
            try
            {
                var captured = Terminal.GetText(TextSelection.Start.Column, TextSelection.Start.Row, TextSelection.End.Column, TextSelection.End.Row);
                DataPackage dataPackage = new DataPackage();
                dataPackage.RequestedOperation = DataPackageOperation.Copy;
                dataPackage.SetText(captured);
                Clipboard.SetContent(dataPackage);
            }
            catch (Exception ex)
            {

            }
        }
        public string getAllOutput()
        {
            try
            {
                var captured = Terminal.GetScreenText();
                return captured;
            }
            catch (Exception ex)
            {

            }
            return "";
        }
        public void Clean()
        {
            try
            {
                Terminal.EraseAbove();
                Terminal.FullReset();
                canvas.Invalidate();
            }
            catch(Exception ex)
            {

            }
        }
        private void TerminalPointerExited(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                MouseOver = null;

                System.Diagnostics.Debug.WriteLine("TerminalPointerExited()");
                canvas.Invalidate();
            }
            catch (Exception ex)
            {

            }
        }

        public TextPosition MousePressedAt { get; set; }

        private void TerminalPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            {
                try
                {
                    var pointer = e.GetCurrentPoint(canvas);
                    var position = ToPosition(pointer.Position);

                    if (pointer.Properties.IsLeftButtonPressed)
                        MousePressedAt = position.OffsetBy(0, ViewTop);
                    else if (pointer.Properties.IsRightButtonPressed)
                        PasteClipboard();
                }
                catch (Exception ex)
                {

                }
            }
        }

        private void TerminalPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (!ScrollModeState)
            {
                try
                {
                    var pointer = e.GetCurrentPoint(canvas);
                    if (!pointer.Properties.IsLeftButtonPressed)
                    {
                        if (Selecting)
                        {
                            MousePressedAt = null;
                            Selecting = false;

                            //System.Diagnostics.Debug.WriteLine("Captured : " + Terminal.GetText(TextSelection.Start.Column, TextSelection.Start.Row, TextSelection.End.Column, TextSelection.End.Row);;

                            var captured = Terminal.GetText(TextSelection.Start.Column, TextSelection.Start.Row, TextSelection.End.Column, TextSelection.End.Row);

                            var dataPackage = new DataPackage();
                            dataPackage.SetText(captured);
                            dataPackage.Properties.EnterpriseId = "Terminal";
                            Clipboard.SetContent(dataPackage);
                        }
                        else
                        {
                            TextSelection = null;
                            canvas.Invalidate();
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }
            else
            {
                MousePressedAt = null;
                Selecting = false;
            }
        }

        public void PasteText(string text)
        {
            try
            {
                Task.Run(() =>
                {
                    try
                    {
                        var buffer = Encoding.UTF8.GetBytes(text);
                        _stream.Write(buffer, 0, buffer.Length);
                        _stream.Flush();
                    }
                    catch (Exception ex)
                    {

                    }
                });
            }
            catch (Exception ex)
            {

            }
        }

        public void PasteClipboard()
        {
            try
            {
                var package = Clipboard.GetContent();

                Task.Run(async () =>
                {
                    try
                    {
                        string text = await package.GetTextAsync();
                        if (!string.IsNullOrEmpty(text))
                            PasteText(text);
                    }
                    catch (Exception ex)
                    {

                    }
                });
            }
            catch (Exception ex)
            {

            }
        }
    }
}
