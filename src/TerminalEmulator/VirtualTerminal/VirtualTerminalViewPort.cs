﻿namespace TerminalEmulator.VirtualTerminal
{
    using System;
    using TerminalEmulator.VirtualTerminal.Model;

    public class VirtualTerminalViewPort
    {
        public VirtualTerminalController Parent { get; private set; }

        public int TopRow { get { return Parent.TopRow; } }

        internal VirtualTerminalViewPort(VirtualTerminalController controller)
        {
            Parent = controller;
        }

        public TerminalLine [] GetLines(int from, int count)
        {
            var result = new TerminalLine[count];

            for(int i=0; i<count; i++)
                result[i] = ((from + i) >= Parent.Buffer.Count) ? null : Parent.Buffer[from + i];

            return result;
        }

        public void SetTopLine(int top)
        {
            Parent.TopRow = top;
        }
    }
}
