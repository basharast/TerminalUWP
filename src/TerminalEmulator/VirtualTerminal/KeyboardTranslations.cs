using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerminalEmulator.VirtualTerminal
{
    public static class KeyboardTranslations
    {
        public static byte[] GetKeySequence(string key, bool applicationMode)
        {
            if (KeyTranslations.TryGetValue(key, out KeyboardTranslation translation))
                return applicationMode ? translation.ApplicationMode : translation.NormalMode;

            return null;
        }

        private static readonly Dictionary<string, KeyboardTranslation> KeyTranslations = new Dictionary<string, KeyboardTranslation>
        {
            { "Ctrl+Tab",       new KeyboardTranslation { NormalMode = RAW('\t'),   ApplicationMode = RAW('\t')   } },
            { "ESC",            new KeyboardTranslation { NormalMode = RAW('\x1B'), ApplicationMode = RAW('\x1B') } },

            { "F1",             new KeyboardTranslation { NormalMode = SS3("P"),    ApplicationMode = SS3("P")    } },
            { "F2",             new KeyboardTranslation { NormalMode = SS3("Q"),    ApplicationMode = SS3("Q")    } },
            { "F3",             new KeyboardTranslation { NormalMode = SS3("R"),    ApplicationMode = SS3("R")    } },
            { "F4",             new KeyboardTranslation { NormalMode = SS3("S"),    ApplicationMode = SS3("S")    } },
            { "F5",             new KeyboardTranslation { NormalMode = CSI("15~"),  ApplicationMode = CSI("15~")  } },
            { "F6",             new KeyboardTranslation { NormalMode = CSI("17~"),  ApplicationMode = CSI("17~")  } },
            { "F7",             new KeyboardTranslation { NormalMode = CSI("18~"),  ApplicationMode = CSI("18~")  } },
            { "F8",             new KeyboardTranslation { NormalMode = CSI("19~"),  ApplicationMode = CSI("19~")  } },
            { "F9",             new KeyboardTranslation { NormalMode = CSI("20~"),  ApplicationMode = CSI("20~")  } },
            { "F10",            new KeyboardTranslation { NormalMode = CSI("21~"),  ApplicationMode = CSI("21~")  } },
            { "F11",            new KeyboardTranslation { NormalMode = CSI("23~"),  ApplicationMode = CSI("23~")  } },
            { "F12",            new KeyboardTranslation { NormalMode = CSI("24~"),  ApplicationMode = CSI("24~")  } },

            { "Up",             new KeyboardTranslation { NormalMode = CSI("A"),    ApplicationMode = ESC("OA")    } },
            { "Down",           new KeyboardTranslation { NormalMode = CSI("B"),    ApplicationMode = ESC("OB")    } },
            { "Right",          new KeyboardTranslation { NormalMode = CSI("C"),    ApplicationMode = ESC("OC")    } },
            { "Left",           new KeyboardTranslation { NormalMode = CSI("D"),    ApplicationMode = ESC("OD")    } },

            { "Home",           new KeyboardTranslation { NormalMode = CSI("1~"),   ApplicationMode = CSI("1~")    } },
            { "Insert",         new KeyboardTranslation { NormalMode = CSI("2~"),   ApplicationMode = CSI("2~")    } },
            { "Delete",         new KeyboardTranslation { NormalMode = CSI("3~"),   ApplicationMode = CSI("3~")    } },
            { "End",            new KeyboardTranslation { NormalMode = CSI("4~"),   ApplicationMode = CSI("4~")    } },
            { "PageUp",         new KeyboardTranslation { NormalMode = CSI("5~"),   ApplicationMode = CSI("5~")    } },
            { "PageDown",       new KeyboardTranslation { NormalMode = CSI("6~"),   ApplicationMode = CSI("6~")    } },
        };

        private static byte[] ESC(string command)
        {
            return (new byte[] { 0x1B }).Concat(Encoding.ASCII.GetBytes(command)).ToArray();
        }
        private static byte[] CSI(string command)
        {
            return (new byte[] { 0x1B, (byte)'[' }).Concat(Encoding.ASCII.GetBytes(command)).ToArray();
        }
        private static byte[] SS3(string command)
        {
            return (new byte[] { 0x8F }).Concat(Encoding.ASCII.GetBytes(command)).ToArray();
        }
        private static byte[] RAW(char ch)
        {
            return new byte[] { (byte)ch };
        }

    }
}
