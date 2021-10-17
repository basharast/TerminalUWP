namespace TerminalEmulator.VirtualTerminal.Model
{
    public class TerminalCharacter
    {
        public char Char { get; set; } = ' ';
        public TerminalAttribute Attributes { get; set; } = new TerminalAttribute();

        public TerminalCharacter Clone()
        {
            return new TerminalCharacter
            {
                Char = Char,
                Attributes = Attributes.Clone()
            };
        }
    }
}
