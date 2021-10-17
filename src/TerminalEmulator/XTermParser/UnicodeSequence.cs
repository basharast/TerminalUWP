namespace TerminalEmulator.XTermParser
{
    public class UnicodeSequence : EscapeSequence
    {
        public override string ToString()
        {
            return "Unicode - " + base.ToString();
        }
    }
}
