using TerminalEmulator.VirtualTerminal.Enums;

namespace TerminalEmulator.VirtualTerminal
{
    public interface IVirtualTerminalController
    {
        void ClearChanges();

        void MoveCursorRelative(int x, int y);
        void SetCursorPosition(int column, int row);
        void CarriageReturn();
        void NewLine();
        void PutChar(char character);
        void SetWindowTitle(string title);
        void SetCharacterAttribute(int parameter);

        // https://www.vt100.net/docs/vt510-rm/DECSC.html
        void SaveCursor();
        void RestoreCursor();
        void EnableAlternateBuffer();
        void EnableNormalBuffer();
        void ClearScrollingRegion();
        void SetScrollingRegion(int top, int bottom);
        void EraseToEndOfLine();
        void EraseToStartOfLine();
        void EraseLine();
        void Backspace();
        void Bell();
        void DeleteCharacter(int count);
        void EraseBelow();
        void EraseAbove();
        void EraseAll();
        void ShiftIn();
        void ShiftOut();
        void UseHighlightMouseTracking(bool enable);
        void UseCellMotionMouseTracking(bool enable);
        void SetCharacterSet(ECharacterSet characterSet);
        void VerticalTab();
        void EnableSgrMouseMode(bool enable);
        void FormFeed();
        void SaveEnableNormalBuffer();
        void SaveUseHighlightMouseTracking();
        void SaveUseCellMotionMouseTracking();
        void SaveEnableSgrMouseMode();
        void RestoreEnableNormalBuffer();
        void RestoreUseHighlightMouseTracking();
        void RestoreUseCellMotionMouseTracking();
        void RestoreEnableSgrMouseMode();
        void SetBracketedPasteMode(bool enable);
        void SaveBracketedPasteMode();
        void RestoreBracketedPasteMode();
        void SetInsertReplaceMode(EInsertReplaceMode mode);
        void SetAutomaticNewLine(bool enable);
        void EnableApplicationCursorKeys(bool enable);
        void SaveCursorKeys();
        void RestoreCursorKeys();
        void SetKeypadType(EKeypadType type);
        void DeleteLines(int count);
        void FullReset();
        void SendDeviceAttributes();
        void SendDeviceAttributesSecondary();
        void TabSet();
        void Tab();
        void ClearTabs();
        void ClearTab();
        void Enable132ColumnMode(bool enable);
        void EnableSmoothScrollMode(bool enable);
        void EnableReverseVideoMode(bool enable);
        void EnableOriginMode(bool enable);
        void EnableWrapAroundMode(bool enable);
        void EnableAutoRepeatKeys(bool enable);
        void Enable80132Mode(bool enable);
        void EnableReverseWrapAroundMode(bool enable);
        void ReverseIndex();
        void SetCharacterSize(ECharacterSize size);
        void SetLatin1();
        void SetUTF8();
        void InsertBlanks(int count);
        void EnableBlinkingCursor(bool enable);
        void ShowCursor(bool show);
        void DeviceStatusReport();
        void ReportCursorPosition();
        void InsertLines(int count);
    }
}
