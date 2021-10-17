namespace TerminalEmulator.XTermParser
{
    public class EscapeSequence : TerminalSequence
    {
        public override string ToString()
        {
            return "ESC - " + base.ToString();
        }
    }
}
