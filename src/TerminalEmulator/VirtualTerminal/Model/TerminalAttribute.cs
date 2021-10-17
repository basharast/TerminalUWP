using TerminalEmulator.VirtualTerminal.Enums;

namespace TerminalEmulator.VirtualTerminal.Model
{
    public class TerminalAttribute
    {
        public ETerminalColor ForegroundColor { get; set; } = ETerminalColor.White;
        public ETerminalColor BackgroundColor { get; set; } = ETerminalColor.Black;
        public bool Bright { get; set; } = false;
        public bool Standout { get; set; } = false;
        public bool Underscore { get; set; } = false;
        public bool Blink { get; set; } = false;
        public bool Reverse { get; set; } = false;
        public bool Hidden { get; set; } = false;

        public TerminalAttribute Clone()
        {
            return new TerminalAttribute
            {
                ForegroundColor = ForegroundColor,
                BackgroundColor = BackgroundColor,
                Bright = Bright,
                Standout = Standout,
                Underscore = Underscore,
                Blink = Blink,
                Reverse = Reverse,
                Hidden = Hidden
            };
        }
    }
}
