using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;

namespace MonoMusicMaker
{
    public class TextEntryWin : TextEntryBase
    {
        public override void HideKeyboard()
        {
            ExitKB();
        }

        public override void ShowKeyboard()
        {
            StartKB();
        }
    }
}
