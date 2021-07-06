using System;
using System.Collections.Generic;
using System.Text;

namespace MonoMusicMaker
{
    public class DrumPresetManager
    {
        MelodyEditorInterface mMEI = null;

        // to map to chordTypeNames array below
        public enum eDrumBeatTypeNum
        {
            None,
            LOAD,
            SAVE,
            Police,
            Rock,
            Blues,
            Riff4,
            num_riffNums,
        }

        public string[] mDrumPresetNames = new string[(int)eDrumBeatTypeNum.num_riffNums] { "None", "LOAD", "SAVE", "Police", "Rock", "Blues", "Riff ?" }; //TODO ADD EDITABLE

        eDrumBeatTypeNum mCurrentSelected = eDrumBeatTypeNum.None;

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
            if ((eDrumBeatTypeNum)val == eDrumBeatTypeNum.LOAD) //Dont set selected to load, we just trigger this function
            {
                DoLoad();
                return true;
            }
            else if ((eDrumBeatTypeNum)val == eDrumBeatTypeNum.SAVE) //Dont set selected to save, we just trigger this function
            {
                DoSave();
                return true;
            }
            else
            {
                mCurrentSelected = (eDrumBeatTypeNum)val;
            }

            return false;
        }


        bool DoSave()
        {
            TopLineDrumAI tlda = GatherDrumData();

            if(tlda!=null)
            {
                if (mMEI.GetSaveLoad().SaveDrumLoop(tlda))
                {

                }
            }

            return true;
        }

        TopLineDrumAI GatherDrumData()
        {
            List<PlayArea> pas = mMEI.GetPlayAreas();

            PlayArea drumTrack = pas[MidiTrackSelect.DRUM_MACHINE_PLAY_AREA_INDEX];

            TopLineDrumAI tlda = drumTrack.GetTopLineDrumAI();

            return tlda;
            //if (tlda != null)
            //{
            //    List<AIDrumRegion.AIDrumSaveData> dsd = new List<AIDrumRegion.AIDrumSaveData>();  //TODO DRUM SAVE - -

            //    dsd = tlda.GetRegionSaveData(); //just the one drm save from track 9, channel 10 
            //    if(dsd != null)
            //    {

            //    }
            //}
        }

        bool DoLoad()
        {
            if (mMEI.GetSaveLoad().LoadDrumLoop(mMEI))
            {

            }
            return true;
        }

    }
}
