using System.Collections.Generic;
using System.Linq;
using TerminalEmulator.VirtualTerminal.Enums;
using TerminalEmulator.VirtualTerminal.Model;

namespace TerminalEmulator.VirtualTerminal
{
    public class TerminalCursorState
    {
        public int CurrentColumn { get; set; } = 0;
        public int CurrentRow { get; set; } = 0;
        public bool ApplicationCursorKeysMode { get; set; } = false;
        public TerminalAttribute Attribute { get; set; } = new TerminalAttribute();
        public bool ShowCursor { get; set; } = true;
        public bool BlinkingCursor { get; set; } = false;

        public List<int> TabStops = new List<int>
        {
            8, 16, 24, 32, 40, 48, 56, 64, 72, 80
        };
        public bool WordWrap = true;
        public bool ReverseVideoMode = false;
        public int ScrollTop = 0;
        public int ScrollBottom = -1;
        public bool OriginMode = false;
        public EInsertReplaceMode InsertMode = EInsertReplaceMode.Replace;

        public TerminalCursorState Clone()
        {
            return new TerminalCursorState
            {
                CurrentColumn = CurrentColumn,
                CurrentRow = CurrentRow,
                ApplicationCursorKeysMode = ApplicationCursorKeysMode,
                Attribute = Attribute.Clone(),
                TabStops = TabStops.ToList(),
                WordWrap = WordWrap,
                ReverseVideoMode = ReverseVideoMode,
                ScrollTop = ScrollTop,
                ScrollBottom = ScrollBottom,
                OriginMode = OriginMode,
                InsertMode = InsertMode,
                ShowCursor = ShowCursor,
                BlinkingCursor = BlinkingCursor
            };
        }
    }
}
