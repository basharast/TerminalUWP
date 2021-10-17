namespace TerminalEmulator.XTermParser
{
    public class OscSequence : TerminalSequence
    {
        public override string ToString()
        {
            return "OSC - " + base.ToString();
        }
    }
}
