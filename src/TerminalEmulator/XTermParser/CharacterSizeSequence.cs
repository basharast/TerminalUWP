using TerminalEmulator.VirtualTerminal.Enums;

namespace TerminalEmulator.XTermParser
{
    public class CharacterSizeSequence : TerminalSequence
    {
        public ECharacterSize Size { get; set; }
        public override string ToString()
        {
            return "Character size - " + Size.ToString();
        }
    }
}
