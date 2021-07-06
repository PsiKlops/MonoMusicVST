using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using NAudio.Midi;
using System.Threading;
using System.Threading.Tasks;

namespace MonoMusicMaker
{
    public class PresetManager
    {
        MelodyEditorInterface mMEI = null;

        // to map to chordTypeNames array below
        public enum eRiffTypeNum
        {
            None,
            LOAD,
            SAVE,
            Jazz,
            Funk,
            Riff3,
            CopyNotes,
            Riff5,
            num_riffNums,
        }

        public string[] riffNames = new string[(int)eRiffTypeNum.num_riffNums] { "None", "LOAD", "SAVE", "Jazz", "Funk", "Riff3", "Copy Notes", "TBD" }; //TODO ADD EDITABLE

        eRiffTypeNum mCurrentSelected = eRiffTypeNum.None;

        public void Init(MelodyEditorInterface mei)
        {
            mMEI = mei;
        }

        public int GetSelected()
        {
            return (int)mCurrentSelected;
        }

        public bool SetSelected(int val)
        {
            if ((eRiffTypeNum)val == eRiffTypeNum.LOAD) //Dont set selected to load, we just trigger this function
            {
                DoLoad();
                return true;
            }
            else if ((eRiffTypeNum)val == eRiffTypeNum.SAVE) //Dont set selected to save, we just trigger this function
            {
                DoSave();
                return true;
            }
            else
            {
                mCurrentSelected = (eRiffTypeNum)val;
            }
            return false;
        }

        bool DoSave()
        {
#if WINDOWS
            mMEI.GetSaveLoad().SaveDialogFile(mMEI.mState, true);
#endif
            return true;
        }

        bool DoLoad()
        {
#if WINDOWS
            mMEI.GetSaveLoad().LoadDialogFile(mMEI.mState, true);
#endif
            return true;
        }

    }
}
