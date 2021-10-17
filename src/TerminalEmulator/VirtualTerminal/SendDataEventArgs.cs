using System;

namespace TerminalEmulator.VirtualTerminal
{
    public class SendDataEventArgs : EventArgs
    {
        public byte [] Data { get; set; }
    }
}
