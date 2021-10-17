using System.Collections.Generic;

namespace TerminalEmulator.VirtualTerminal.Model
{
    public class TerminalLine : List<TerminalCharacter>
    {
        public bool DoubleWidth { get; set; } = false;
        public bool DoubleHeightTop { get; set; } = false;
        public bool DoubleHeightBottom { get; set; } = false;
    }
}
