using System;
using System.Text;
using TerminalEmulator.XTermParser;

namespace TerminalEmulator.Exceptions
{
    public class EscapeSequenceException : Exception
    {
        public byte[] Buffer { get; set; }
        public TerminalSequence Sequence { get; set; }

        public string BufferTextUTF8
        {
            get
            {
                return Encoding.UTF8.GetString(Buffer);
            }
        }

        public EscapeSequenceException(string message, byte[] buffer) :
            base(message)
        {
            Buffer = buffer;
        }

        public EscapeSequenceException(string message, TerminalSequence sequence) :
            base(message)
        {
            Sequence = sequence;
        }
    }
}
