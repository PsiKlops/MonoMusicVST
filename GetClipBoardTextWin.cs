using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms; //!!!! NEED TO REMEMBER TO ADD REFERENCE FOR THIS TO GET CLIPBOARD!!!!! -> https://stackoverflow.com/questions/9646684/cant-use-system-windows-forms

namespace MonoMusicMaker
{
    public class GetClipBoardTextWin : GetClipBoardBase
    {
        public override string GetClipboard()
        {
            return Clipboard.GetText(TextDataFormat.Text);
        }
    }
}
