﻿using System;
using System.Text;
using TerminalEmulator.VirtualTerminal.Enums;
using TerminalEmulator.VirtualTerminal.Model;
using TerminalEmulator.XTermParser;

// https://github.com/joejulian/xterm/blob/defc6dd5684a12dc8e56cb6973ef973e7a32caa3/ctlseqs.txt

namespace TerminalEmulator.VirtualTerminal
{
    public class VirtualTerminalController : IVirtualTerminalController
    {
        private TerminalLines alternativeBuffer = new TerminalLines();
        private TerminalLines normalBuffer = new TerminalLines();

        public int alternativeBufferTopRow = 0;
        public int normalBufferTopRow = 0;

        internal TerminalLines Buffer { get; set; }
        private EActiveBuffer ActiveBuffer { get; set; } = EActiveBuffer.Normal;

        internal int TopRow { get; set; } = 0;

        public int Columns { get; set; } = 80;
        public int Rows { get; set; } = 24;

        public int VisibleColumns { get; set; } = 0;
        public int VisibleRows { get; set; } = 0;

        private TerminalCursorState SavedCursorState { get; set; } = null;
        public TerminalCursorState CursorState { get; set; } = new TerminalCursorState();

        public bool HighlightMouseTracking { get; set; }
        public bool CellMotionMouseTracking { get; set; }
        public bool SgrMouseMode { get; set; }

        public bool Debugging { get; set; }

        public VirtualTerminalController()
        {
            Buffer = normalBuffer;
            ViewPort = new VirtualTerminalViewPort(this);
        }

        public EventHandler<SendDataEventArgs> SendData;
        public int ChangeCount { get; private set; }

        public VirtualTerminalViewPort ViewPort { get; private set; }

        public void ClearChanges()
        {
            ChangeCount = 0;
        }

        public bool Changed { get { return ChangeCount > 0;  } }

        public char GetVisibleChar(int x, int y)
        {
            if ((TopRow + y) >= Buffer.Count)
                return ' ';

            var line = Buffer[TopRow + y];
            if (line.Count <= x)
                return ' ';

            return line[x].Char;
        }

        public string GetVisibleChars(int x, int y, int count)
        {
            string result = "";

            for (var i = 0; i < count; i++)
                result += GetVisibleChar(x + i, y);

            return result;
        }

        public string GetScreenText()
        {
            string result = "";

            for (var y = 0; y < Rows; y++)
            {
                for (var x = 0; x < Columns; x++)
                    result += GetVisibleChar(x, y);

                if (y < (Rows - 1))
                    result += '\n';
            }

            return result;
        }

        public string GetText(int startColumn, int startRow, int endColumn, int endRow)
        {
            if (startRow > endRow || (startRow == endRow && startColumn > endColumn))
            {
                var holder = startColumn;
                startColumn = endColumn;
                endColumn = holder;

                holder = startRow;
                startRow = endRow;
                endRow = holder;
            }

            string result = "";

            if (startRow >= Buffer.Count)
                return result;

            var line = Buffer[startRow];

            if (startRow == endRow)
            {
                for (int i = startColumn; i <= endColumn && i < line.Count; i++)
                    result += line[i].Char;

                return result;
            }

            for (int i = startColumn; i < line.Count; i++)
                result += line[i].Char;

            for (int y=startRow + 1; y < endRow; y++)
            {
                result += '\n';

                line = Buffer[y];
                for (int i = 0; i < line.Count; i++)
                    result += line[i].Char;
            }

            result += '\n';

            line = Buffer[endRow];
            for (int i = 0; i <= endColumn && i < line.Count; i++)
                result += line[i].Char;

            return result;
        }

        public void FullReset()
        {
            alternativeBufferTopRow = alternativeBuffer.Count;
            normalBufferTopRow = normalBuffer.Count;

            ActiveBuffer = EActiveBuffer.Normal;

            SavedCursorState = null;
            CursorState = new TerminalCursorState();

            Columns = VisibleColumns;
            Rows = VisibleRows;

            ChangeCount++;
        }

        private void Log(string message)
        {
            if (Debugging)
                System.Diagnostics.Debug.WriteLine("Terminal: " + message);
        }

        private void LogController(string message)
        {
            if (Debugging)
                System.Diagnostics.Debug.WriteLine("Controller: " + message);
        }

        private void LogExtreme(string message)
        {
            if (Debugging)
                System.Diagnostics.Debug.WriteLine("Terminal: (c=" + CursorState.CurrentColumn.ToString() + ",r=" + CursorState.CurrentRow.ToString() + ")" + message);
        }

        public void SetCharacterSet(ECharacterSet characterSet)
        {
            LogController("Unimplemented: SetCharacterSet(characterSet:" + characterSet.ToString() + ")");
        }

        public void TabSet()
        {
            var stop = CursorState.CurrentColumn + 1;
            LogController("TabSet() [cursorX=" + stop.ToString() + "]");

            var tabStops = CursorState.TabStops;
            int index = 0;
            while (index < tabStops.Count && tabStops[index] < stop)
                index++;

            if (index >= tabStops.Count)
                tabStops.Add(stop);
            else if (tabStops[index] != stop)
                tabStops.Insert(index, stop);
        }

        public void Tab()
        {
            var current = CursorState.CurrentColumn + 1;
            LogController("Tab() [cursorX=" + current.ToString() + "]");

            var tabStops = CursorState.TabStops;
            int index = 0;
            while (index < tabStops.Count && tabStops[index] <= current)
                index++;

            if (index < tabStops.Count)
                SetCursorPosition(tabStops[index], CursorState.CurrentRow + 1);
        }

        public void ClearTabs()
        {
            LogController("ClearTabs()");

            CursorState.TabStops.Clear();
        }

        public void ClearTab()
        {
            var stop = CursorState.CurrentColumn + 1;

            LogController("ClearTab() [cursorX=" + stop.ToString() + "]");

            var tabStops = CursorState.TabStops;
            int index = 0;
            while (index < tabStops.Count && tabStops[index] < stop)
                index++;

            if (index < tabStops.Count && tabStops[index] == stop)
                tabStops.RemoveAt(index);
        }

        public void CarriageReturn()
        {
            LogExtreme("Carriage return");

            CursorState.CurrentColumn = 0;
            ChangeCount++;
        }

        public void NewLine()
        {
            LogExtreme("NewLine()");

            CursorState.CurrentRow++;

            if (CursorState.ScrollBottom == -1 && CursorState.CurrentRow >= VisibleRows)
            {
                LogController("Scroll all (before:" + TopRow.ToString() + ",after:" + (TopRow + 1).ToString() + ")");
                TopRow++;
                CursorState.CurrentRow--;
            }
            else if (CursorState.ScrollBottom >= 0 && CursorState.CurrentRow > CursorState.ScrollBottom)
            {
                LogController("Scroll region");

                if (Buffer.Count > (CursorState.ScrollBottom + TopRow))
                    Buffer.Insert(CursorState.ScrollBottom + TopRow + 1, new TerminalLine());

                Buffer.RemoveAt(CursorState.ScrollTop + TopRow);

                CursorState.CurrentRow--;
            }

            ChangeCount++;
        }

        public void VerticalTab()
        {
            LogController("VerticalTab()");
            MoveCursorRelative(0, 1);
        }

        public void FormFeed()
        {
            LogController("FormFeed()");
            MoveCursorRelative(0, 1);
        }

        public void ReverseIndex()
        {
            LogController("ReverseIndex()");

            CursorState.CurrentRow--;
            if (CursorState.CurrentRow < CursorState.ScrollTop)
            {
                var scrollBottom = 0;
                if (CursorState.ScrollBottom == -1)
                    scrollBottom = TopRow + VisibleRows - 1;
                else
                    scrollBottom = TopRow + CursorState.ScrollBottom;

                if (Buffer.Count > scrollBottom)
                    Buffer.RemoveAt(scrollBottom);

                Buffer.Insert(TopRow + CursorState.ScrollTop, new TerminalLine());

                CursorState.CurrentRow++;
            }
        }

        public void Backspace()
        {
            LogExtreme("Backspace");

            if (CursorState.CurrentColumn > 0)
            {
                CursorState.CurrentColumn--;

                ChangeCount++;
            }
        }

        public void Bell()
        {
            LogExtreme("Unimplemented: Bell()");
        }

        public void MoveCursorRelative(int x, int y)
        {
            LogController("MoveCursorRelative(x:" + x.ToString() + ",y:" + y.ToString() + ",vis:[" + VisibleColumns.ToString() + "," + VisibleRows.ToString() + "]" + ")");

            CursorState.CurrentColumn += x;
            if (CursorState.CurrentColumn < 0)
                CursorState.CurrentColumn = 0;
            if (CursorState.CurrentColumn >= Columns)
                CursorState.CurrentColumn = Columns - 1;

            CursorState.CurrentRow += y;
            if (CursorState.CurrentRow < CursorState.ScrollTop)
                CursorState.CurrentRow = CursorState.ScrollTop;

            var scrollBottom = (CursorState.ScrollBottom == -1) ? Rows - 1 : CursorState.ScrollBottom;
            if (CursorState.CurrentRow > scrollBottom)
                CursorState.CurrentRow = scrollBottom;

            ChangeCount++;
        }

        public void SetCursorPosition(int column, int row)
        {
            LogController("SetCursorPosition(column:" + column.ToString() + ",row:" + row.ToString() + ")");

            CursorState.CurrentColumn = column - 1;
            CursorState.CurrentRow = row - 1 + (CursorState.OriginMode ? CursorState.ScrollTop : 0);
            if (CursorState.ScrollBottom > -1 && CursorState.CurrentRow > CursorState.ScrollBottom)
                CursorState.CurrentRow = CursorState.ScrollBottom;

            ChangeCount++;
        }

        public void InsertBlanks(int count)
        {
            LogExtreme("InsertBlank()");

            while (Buffer.Count <= (TopRow + CursorState.CurrentRow))
                Buffer.Add(new TerminalLine());

            var line = Buffer[TopRow + CursorState.CurrentRow];
            while (line.Count < CursorState.CurrentColumn)
                line.Add(new TerminalCharacter());

            while ((count--) > 0)
                line.Insert(CursorState.CurrentColumn, new TerminalCharacter());

            while (line.Count > Columns)
                line.RemoveAt(line.Count - 1);
        }

        public void PutChar(char character)
        {
            LogExtreme("PutChar(ch:'" + character + "'=" + ((int)character).ToString() + ")");

            if (CursorState.InsertMode == EInsertReplaceMode.Insert)
            {
                while (Buffer.Count <= (TopRow + CursorState.CurrentRow))
                    Buffer.Add(new TerminalLine());

                var line = Buffer[TopRow + CursorState.CurrentRow];
                while (line.Count < CursorState.CurrentColumn)
                    line.Add(new TerminalCharacter());

                line.Insert(CursorState.CurrentColumn, new TerminalCharacter());
            }

            if (CursorState.WordWrap)
            {
                if (CursorState.CurrentColumn >= Columns)
                {
                    CursorState.CurrentColumn = 0;
                    NewLine();
                }
            }

            SetCharacter(CursorState.CurrentColumn, CursorState.CurrentRow, character, CursorState.Attribute);
            CursorState.CurrentColumn++;

            var lineToClip = Buffer[TopRow + CursorState.CurrentRow];
            while (lineToClip.Count > Columns)
                lineToClip.RemoveAt(lineToClip.Count - 1);

            ChangeCount++;
        }

        public void SetWindowTitle(string title)
        {
            LogController("SetWindowTitle(t:'" + title + "')");
        }

        public void ShiftIn()
        {
            LogController("Unimplemented: ShiftIn()");
        }

        public void ShiftOut()
        {
            LogController("Unimplemented: ShiftOut()");
        }

        public void SetCharacterAttribute(int parameter)
        {
            switch (parameter)
            {
                case 0:
                    LogController("SetCharacterAttribute(reset)");
                    CursorState.Attribute.ForegroundColor = ETerminalColor.White;
                    CursorState.Attribute.BackgroundColor = ETerminalColor.Black;
                    CursorState.Attribute.Bright = false;
                    CursorState.Attribute.Standout = false;
                    CursorState.Attribute.Underscore = false;
                    CursorState.Attribute.Blink = false;
                    CursorState.Attribute.Reverse = false;
                    CursorState.Attribute.Hidden = false;
                    break;

                case 1:
                    LogController("SetCharacterAttribute(bright)");
                    CursorState.Attribute.Bright = true;
                    break;

                case 2:
                    LogController("SetCharacterAttribute(dim)");
                    CursorState.Attribute.Bright = false;
                    break;

                case 3:
                    LogController("SetCharacterAttribute(standout)");
                    CursorState.Attribute.Standout = true;
                    break;

                case 4:
                    LogController("SetCharacterAttribute(underscore)");
                    CursorState.Attribute.Underscore = true;
                    break;

                case 5:
                    LogController("SetCharacterAttribute(blink)");
                    CursorState.Attribute.Blink = true;
                    break;

                case 7:
                    LogController("SetCharacterAttribute(reverse)");
                    CursorState.Attribute.Reverse = true;
                    break;

                case 8:
                    LogController("SetCharacterAttribute(hidden)");
                    CursorState.Attribute.Hidden = true;
                    break;

                case 22:
                    LogController("SetCharacterAttribute(not bright)");
                    CursorState.Attribute.Bright = false;
                    break;

                case 24:
                    LogController("SetCharacterAttribute(not underlined)");
                    CursorState.Attribute.Underscore = false;
                    break;

                case 25:
                    LogController("SetCharacterAttribute(steady)");
                    CursorState.Attribute.Blink = false;
                    break;

                case 27:
                    LogController("SetCharacterAttribute(not reverse)");
                    CursorState.Attribute.Reverse = false;
                    break;

                case 28:
                    LogController("SetCharacterAttribute(not hidden)");
                    CursorState.Attribute.Hidden = false;
                    break;

                case 30:
                case 31:
                case 32:
                case 33:
                case 34:
                case 35:
                case 36:
                case 37:
                case 38:
                    CursorState.Attribute.ForegroundColor = (ETerminalColor)(parameter - 30);
                    LogController("SetCharacterAttribute(foreground:" + CursorState.Attribute.ForegroundColor.ToString() + ")");
                    break;
                case 39:
                    CursorState.Attribute.ForegroundColor = ETerminalColor.White;
                    LogController("SetCharacterAttribute(foreground:default)");
                    break;
                case 40:
                case 41:
                case 42:
                case 43:
                case 44:
                case 45:
                case 46:
                case 47:
                case 48:
                    CursorState.Attribute.BackgroundColor = (ETerminalColor)(parameter - 40);
                    LogController("SetCharacterAttribute(background:" + CursorState.Attribute.BackgroundColor.ToString() + ")");
                    break;
                case 49:
                    CursorState.Attribute.BackgroundColor = ETerminalColor.Black;
                    LogController("SetCharacterAttribute(background:default)");
                    break;

                default:
                    LogController("SetCharacterAttribute(parameter:" + parameter + ")");
                    break;
            }
        }

        public void SetCharacterSize(ECharacterSize size)
        {
            LogController("SetCharacterSize(size:" + size.ToString() + ")");

            while ((CursorState.CurrentRow + TopRow) >= Buffer.Count)
                Buffer.Add(new TerminalLine());
            var currentLine = Buffer[CursorState.CurrentRow + TopRow];

            switch (size)
            {
                default:
                case ECharacterSize.SingleWidthLine:
                    currentLine.DoubleWidth = false;
                    currentLine.DoubleHeightTop = false;
                    currentLine.DoubleHeightBottom = false;
                    break;
                case ECharacterSize.DoubleHeightLineTop:
                    currentLine.DoubleWidth = true;
                    currentLine.DoubleHeightBottom = false;
                    currentLine.DoubleHeightTop = true;
                    break;
                case ECharacterSize.DoubleHeightLineBottom:
                    currentLine.DoubleWidth = true;
                    currentLine.DoubleHeightTop = false;
                    currentLine.DoubleHeightBottom = true;
                    break;
                case ECharacterSize.DoubleWidthLine:
                    currentLine.DoubleHeightTop = false;
                    currentLine.DoubleHeightBottom = false;
                    currentLine.DoubleWidth = true;
                    break;
                case ECharacterSize.ScreenAlignmentTest:
                    ScreenAlignmentTest();
                    break;
            }
        }

        public void ScreenAlignmentTest()
        {
            for (var y = 0; y < VisibleRows; y++)
                for (var x = 0; x < VisibleColumns; x++)
                    SetCharacter(x, y, 'E', CursorState.Attribute);
        }

        public void SaveCursor()
        {
            LogController("SaveCursor()");

            SavedCursorState = CursorState.Clone();

            LogController("     C=" + CursorState.CurrentColumn.ToString() + ",R=" + CursorState.CurrentRow.ToString());
        }

        public void RestoreCursor()
        {
            LogController("RestoreCursor()");

            if (SavedCursorState != null)
                CursorState = SavedCursorState.Clone();

            LogController("     C=" + CursorState.CurrentColumn.ToString() + ",R=" + CursorState.CurrentRow.ToString());
        }

        public void EnableNormalBuffer()
        {
            LogController("EnableNormalBuffer()");

            if (ActiveBuffer == EActiveBuffer.Normal)
                return;

            ActiveBuffer = EActiveBuffer.Normal;
            Buffer = normalBuffer;

            alternativeBufferTopRow = TopRow;
            TopRow = normalBufferTopRow;

            ChangeCount++;
        }

        public void EnableAlternateBuffer()
        {
            LogController("EnableAlternateBuffer()");

            if (ActiveBuffer == EActiveBuffer.Alternative)
                return;

            ActiveBuffer = EActiveBuffer.Alternative;
            Buffer = alternativeBuffer;

            normalBufferTopRow = TopRow;
            TopRow = alternativeBufferTopRow;

            ChangeCount++;
        }

        public void UseHighlightMouseTracking(bool enable)
        {
            LogController("Unimplemented: UseHighlightMouseTracking(enable:" + enable.ToString() + ")");
            HighlightMouseTracking = enable;
            ChangeCount++;
        }

        public void UseCellMotionMouseTracking(bool enable)
        {
            LogController("Unimplemented: UseCellMotionMouseTracking(enable:" + enable.ToString() + ")");
            CellMotionMouseTracking = enable;
            ChangeCount++;
        }

        public void EnableSgrMouseMode(bool enable)
        {
            LogController("Unimplemented: EnableSgrMouseMode(enable:" + enable.ToString() + ")");
            SgrMouseMode = enable;
            ChangeCount++;
        }

        public void SaveEnableNormalBuffer()
        {
            LogController("Unimplemented: SaveEnableNormalBuffer()");
        }

        public void RestoreEnableNormalBuffer()
        {
            LogController("Unimplemented: RestoreEnableNormalBuffer()");
        }

        public void SaveUseHighlightMouseTracking()
        {
            LogController("Unimplemented: SaveUseHighlightMouseTracking()");
        }

        public void RestoreUseHighlightMouseTracking()
        {
            LogController("Unimplemented: RestoreUseHighlightMouseTracking()");
        }

        public void SaveUseCellMotionMouseTracking()
        {
            LogController("Unimplemented: SaveUseCellMotionMouseTracking()");
        }

        public void RestoreUseCellMotionMouseTracking()
        {
            LogController("Unimplemented: RestoreUseCellMotionMouseTracking()");
        }

        public void SaveEnableSgrMouseMode()
        {
            LogController("Unimplemented: SaveEnableSgrMouseMode()");
        }

        public void RestoreEnableSgrMouseMode()
        {
            LogController("Unimplemented: RestoreEnableSgrMouseMode()");
        }

        public void SetBracketedPasteMode(bool enable)
        {
            LogController("Unimplemented: SetBracketedPasteMode(enable:" + enable.ToString() + ")");
        }

        public void SaveBracketedPasteMode()
        {
            LogController("Unimplemented: SaveBracketedPasteMode()");
        }

        public void RestoreBracketedPasteMode()
        {
            LogController("Unimplemented: RestoreBracketedPasteMode()");
        }

        public void SetInsertReplaceMode(EInsertReplaceMode mode)
        {
            LogController("Unimplemented: SetInsertReplaceMode(mode:" + mode.ToString() + ")");
            CursorState.InsertMode = mode;
        }

        public void ClearScrollingRegion()
        {
            LogController("ClearScrollingRegion()");
            CursorState.ScrollTop = 0;
            CursorState.ScrollBottom = -1;
        }

        public void SetAutomaticNewLine(bool enable)
        {
            LogController("Unimplemented: SetAutomaticNewLine(enable:" + enable.ToString() + ")");
        }

        public void EnableApplicationCursorKeys(bool enable)
        {
            LogController("EnableApplicationCursorKeys(enable:" + enable.ToString() + ")");
            CursorState.ApplicationCursorKeysMode = enable;
        }

        public void SaveCursorKeys()
        {
            LogController("Unimplemented: SaveCursorKeys()");
        }

        public void RestoreCursorKeys()
        {
            LogController("Unimplemented: RestoreCursorKeys()");
        }

        public void SetKeypadType(EKeypadType type)
        {
            LogController("Unimplemented: SetKeypadType(type:" + type.ToString() + ")");
        }

        public void SetScrollingRegion(int top, int bottom)
        {
            LogController("SetScrollingRegion(top:" + top.ToString() + ",bottom:" + bottom.ToString() + ")");

            if (top == 1 && bottom == VisibleRows)
                ClearScrollingRegion();
            else
            {
                CursorState.ScrollTop = top - 1;
                CursorState.ScrollBottom = bottom - 1;

                if (CursorState.OriginMode)
                    CursorState.CurrentRow = CursorState.ScrollTop;
            }
        }

        public void EraseLine()
        {
            LogController("EraseLine()");

            for (var i = 0; i < Columns; i++)
                SetCharacter(i, CursorState.CurrentRow, ' ', CursorState.Attribute);

            var line = Buffer[TopRow + CursorState.CurrentRow];
            while (line.Count > Columns)
                line.RemoveAt(line.Count - 1);

            ChangeCount++;
        }

        public void EraseToEndOfLine()
        {
            LogController("EraseToEndOfLine()");

            for (var i = CursorState.CurrentColumn; i < Columns; i++)
                SetCharacter(i, CursorState.CurrentRow, ' ', CursorState.Attribute);

            var line = Buffer[TopRow + CursorState.CurrentRow];
            while (line.Count > Columns)
                line.RemoveAt(line.Count - 1);

            ChangeCount++;
        }

        public void EraseToStartOfLine()
        {
            LogController("EraseToStartOfLine()");

            for (var i = 0; i < Columns && i <= CursorState.CurrentColumn; i++)
                SetCharacter(i, CursorState.CurrentRow, ' ', CursorState.Attribute);

            var line = Buffer[TopRow + CursorState.CurrentRow];
            while (line.Count > Columns)
                line.RemoveAt(line.Count - 1);

            ChangeCount++;
        }

        public void EraseBelow()
        {
            // TODO : Optimize
            LogController("EraseBelow()");

            for (var y = CursorState.CurrentRow + 1; y < VisibleRows; y++)
            {
                for (var x = 0; x < VisibleColumns; x++)
                    SetCharacter(x, y, ' ', CursorState.Attribute);

                var line = Buffer[TopRow + y];
                while (line.Count > Columns)
                    line.RemoveAt(line.Count - 1);
            }


            for (var x = CursorState.CurrentColumn; x < VisibleColumns; x++)
                SetCharacter(x, CursorState.CurrentRow, ' ', CursorState.Attribute);
        }

        public void EraseAbove()
        {
            // TODO : Optimize
            LogController("EraseAbove()");

            for (var y = CursorState.CurrentRow - 1; y >= 0; y--)
            {
                for (var x = 0; x < VisibleColumns; x++)
                    SetCharacter(x, y, ' ', CursorState.Attribute);

                var line = Buffer[TopRow + y];
                while (line.Count > Columns)
                    line.RemoveAt(line.Count - 1);
            }

            for (var x = 0; x <= CursorState.CurrentColumn; x++)
                SetCharacter(x, CursorState.CurrentRow, ' ', CursorState.Attribute);
        }

        public void DeleteLines(int count)
        {
            // TODO : Verify it works with scroll range
            LogController("Unimplemented: DeleteLines(count:" + count.ToString() + ")");

            if ((CursorState.CurrentRow + TopRow) >= Buffer.Count)
                return;

            while ((count > 0) && (CursorState.CurrentRow + TopRow) < Buffer.Count)
                Buffer.RemoveAt(CursorState.CurrentRow);

            ChangeCount++;
        }

        public void InsertLines(int count)
        {
            LogController("Unimplemented: InsertLines(count:" + count.ToString() + ")");

            if ((CursorState.CurrentRow + TopRow) >= Buffer.Count)
                return;

            while((count--) > 0)
                Buffer.Insert((CursorState.CurrentRow + TopRow), new TerminalLine());

            // TODO : Remove last line of the buffer so that scrolling works
        }

        public void EraseAll()
        {
            // TODO : Verify it works with scroll range
            LogController("Partial: EraseAll()");

            TopRow = Buffer.Count;

            SetCursorPosition(1, 1);
            Columns = VisibleColumns;
            Rows = VisibleRows;

            ChangeCount++;
        }

        public void DeleteCharacter(int count)
        {
            LogController("DeleteCharacter(count:" + count.ToString() + ")");

            if (CursorState.CurrentRow >= Buffer.Count)
                return;

            var line = Buffer[CursorState.CurrentRow];

            while (count > 0 && CursorState.CurrentColumn < line.Count)
            {
                line.RemoveAt(CursorState.CurrentColumn);
                count--;
            }

            ChangeCount++;
        }

        public void Enable132ColumnMode(bool enable)
        {
            LogController("Enable132ColumnMode(enable:" + enable.ToString() + ")");
            EraseAll();
            Columns = enable ? 132 : 80;
        }

        public void EnableSmoothScrollMode(bool enable)
        {
            LogController("Unimplemented: EnableSmoothScrollMode(enable:" + enable.ToString() + ")");
        }

        public void EnableReverseVideoMode(bool enable)
        {
            LogController("EnableReverseVideoMode(enable:" + enable.ToString() + ")");
            CursorState.ReverseVideoMode = enable;

            ChangeCount++;
        }

        public void EnableBlinkingCursor(bool enable)
        {
            LogController("EnableBlinkingCursor(enable:" + enable.ToString() + ")");
            CursorState.BlinkingCursor = enable;

            ChangeCount++;
        }

        public void ShowCursor(bool show)
        {
            LogController("ShowCursor(show:" + show.ToString() + ")");
            CursorState.ShowCursor = show;

            ChangeCount++;
        }

        public void EnableOriginMode(bool enable)
        {
            LogController("EnableOriginMode(enable:" + enable.ToString() + ")");
            CursorState.OriginMode = enable;
            SetCursorPosition(0, 0);
        }

        public void EnableWrapAroundMode(bool enable)
        {
            LogController("EnableWrapAroundMode(enable:" + enable.ToString() + ")");
            CursorState.WordWrap = enable;
        }

        public void EnableAutoRepeatKeys(bool enable)
        {
            LogController("Unimplemented: EnableAutoRepeatKeys(enable:" + enable.ToString() + ")");
        }

        public void Enable80132Mode(bool enable)
        {
            LogController("Unimplemented: Enable80132Mode(enable:" + enable.ToString() + ")");
            if (!enable)
                Columns = VisibleColumns;
        }

        public void EnableReverseWrapAroundMode(bool enable)
        {
            LogController("Unimplemented: EnableReverseWrapAroundMode(enable:" + enable.ToString() + ")");
        }

        public static readonly byte[] VT102DeviceAttributes = { 0x1B, (byte)'[', (byte)'?', (byte)'6', (byte)'C' };

        public void SendDeviceAttributes()
        {
            LogController("SendDeviceAttributes()");
            SendData.Invoke(this, new SendDataEventArgs { Data = VT102DeviceAttributes });
        }

        public static readonly byte[] XTermDeviceAttributesSecondary = { 0x1B, (byte)'[', (byte)'>', (byte)'0', (byte)';', (byte)'1', (byte)'3', (byte)'6', (byte)';', (byte)'0', (byte)'C' };

        public void SendDeviceAttributesSecondary()
        {
            LogController("SendDeviceAttributesSecondary()");
            SendData.Invoke(this, new SendDataEventArgs { Data = XTermDeviceAttributesSecondary });
        }

        public static readonly byte[] DsrOk= { 0x1B, (byte)'[', (byte)'0', (byte)'n' };

        public void DeviceStatusReport()
        {
            LogController("DeviceStatusReport()");
            SendData.Invoke(this, new SendDataEventArgs { Data = DsrOk });
        }

        public void ReportCursorPosition()
        {
            LogController("ReportCursorPosition()");

            var rcp = "\u001b[" + (CursorState.CurrentRow + 1).ToString() + ";" + (CursorState.CurrentColumn + 1).ToString() + "R";

            SendData.Invoke(this, new SendDataEventArgs { Data = Encoding.UTF8.GetBytes(rcp) });
        }

        public void SetLatin1()
        {
            LogController("Unimplemented: SetLatin1()");
        }

        public void SetUTF8()
        {
            LogController("Unimplemented: SetUTF8()");
        }

        public void ResizeView(int columns, int rows)
        {
            VisibleColumns = columns;
            VisibleRows = rows;
            Columns = columns;
            Rows = rows;

            if (CursorState.CurrentRow >= Rows)
            {
                var offset = CursorState.CurrentRow - Rows + 1;
                TopRow += offset;
                CursorState.CurrentRow -= offset;
            }
        }

        private void Send(byte[] v)
        {
            SendData.Invoke(this, new SendDataEventArgs { Data = v });
        }

        private void SetCharacter(int currentColumn, int currentRow, char ch, TerminalAttribute attribute)
        {
            while (Buffer.Count < (currentRow + TopRow + 1))
                Buffer.Add(new TerminalLine());

            var line = Buffer[currentRow + TopRow];
            while (line.Count < (currentColumn + 1))
                line.Add(new TerminalCharacter { Char = ' ', Attributes = CursorState.Attribute });

            var character = line[currentColumn];
            character.Char = ch;
            character.Attributes = attribute.Clone();
        }

        public byte [] GetKeySequence(string key)
        {
            return KeyboardTranslations.GetKeySequence(key, CursorState.ApplicationCursorKeysMode);
        }
    }
}
