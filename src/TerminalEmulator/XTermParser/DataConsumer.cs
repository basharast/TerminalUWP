using System;
using System.Collections.Generic;
using System.Text;
using TerminalEmulator.VirtualTerminal;

namespace TerminalEmulator.XTermParser
{
    public class DataConsumer
    {
        public bool SequenceDebugging { get; set; }

        private XTermInputBuffer InputBuffer { get; set; } = new XTermInputBuffer();

        private bool ResumingStarvedBuffer { get; set; }

        public IVirtualTerminalController Controller { get; private set; }

        public DataConsumer(IVirtualTerminalController controller)
        {
            Controller = controller;
        }

        public void Consume(byte[] data)
        {
            InputBuffer.Add(data);

            Controller.ClearChanges();
            while (!InputBuffer.AtEnd)
            {
                try
                {
                    if (SequenceDebugging && ResumingStarvedBuffer)
                    {
                        System.Diagnostics.Debug.WriteLine("Resuming from starved buffer [" + Encoding.UTF8.GetString(InputBuffer.Buffer).Replace("\u001B", "<esc>") + "]");
                        ResumingStarvedBuffer = false;
                    }

                    var sequence = XTermSequenceReader.ConsumeNextSequence(InputBuffer);

                    // Handle poorly injected sequences
                    if (sequence.ProcessFirst != null)
                    {
                        foreach (var item in sequence.ProcessFirst)
                        {
                            if (SequenceDebugging)
                                System.Diagnostics.Debug.WriteLine(item.ToString());

                            XTermSequenceHandlers.ProcessSequence(item, Controller);
                        }
                    }

                    if (SequenceDebugging)
                        System.Diagnostics.Debug.WriteLine(sequence.ToString());

                    XTermSequenceHandlers.ProcessSequence(sequence, Controller);
                }
                catch (IndexOutOfRangeException)
                {
                    ResumingStarvedBuffer = true;
                    InputBuffer.PopAllStates();
                    break;
                }
                catch (ArgumentException)
                {
                    // We've reached an invalid state of the stream.
                    InputBuffer.ReadRaw();
                    InputBuffer.Commit();
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("Unknown exception " + e.Message);
                }
            }

            InputBuffer.Flush();
        }

        public void Push(byte[] data)
        {
            Consume(data);
        }
    }
}
