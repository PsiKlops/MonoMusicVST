using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoMusicMaker
{
    public class SaveSwitchingManager
    {
        MelodyEditorInterface mMEI = null;

        ParamButtonBar mpbb;

        // to map to chordTypeNames array below
        public enum eSaveSlots
        {
            Main1,
            Main2,
            Main3,
            Slot1,
            Slot2,
            Slot3,
            Slot4,
            Slot5,
            Slot6,
            Slot7,
            Slot8,
            Slot9,
            Slot10,
            num_saveSlots,
        }

        List<SaveLoad.SaveSongData> mSaveSongDataList = new List<SaveLoad.SaveSongData>();

        public string[] mSaveSlotNames = new string[(int)eSaveSlots.num_saveSlots] { "Main1", "Main2", "Main3", "Slot1", "Slot2", "Slot3", "Slot4", "Slot5", "Slot6", "Slot7", "Slot8", "Slot9", "Slot10" }; //TODO ADD EDITABLE

        eSaveSlots mCurrentSelected = eSaveSlots.Main1;
        Button mCurrentButtonSelected = null;

        public SaveSwitchingManager()
        {
            ResetSlots();
        }

        void ResetSlots()
        {
            for (int i = 0; i < (int)eSaveSlots.num_saveSlots; i++)
            {
                SaveLoad.SaveSongData ssd = null;

                if (i < (int)eSaveSlots.Slot1)
                {
                    ssd = new SaveLoad.SaveSongData();
                }
                mSaveSongDataList.Add(ssd);
            }
            System.Diagnostics.Debug.WriteLine(string.Format("Size array {0}", mSaveSongDataList.Count));
        }

        void ResetSlot(int index)
        {
            if(index < mSaveSongDataList.Count)
            {
                SaveLoad.SaveSongData ssd = null;

                if (index < (int)eSaveSlots.Slot1)
                {
                    ssd = new SaveLoad.SaveSongData();
                }
                mSaveSongDataList[index] = ssd;
                System.Diagnostics.Debug.WriteLine(string.Format("Reset index {0} Size array {1}", index, mSaveSongDataList.Count));
            }
        }

        public int GetSelected()
        {
            return (int)mCurrentSelected;
        }

        public bool AddTag(Button selectedButton, Button currentKeyButton = null, Button currentModeButton = null)
        {
            if (currentModeButton != null && currentKeyButton != null)
            {
                Color colScaleType = currentModeButton.GetTopTagCol();
                string strKey = currentKeyButton.ButtonText;

                List<int> data = new List<int>();

                data.Add(currentModeButton.mGED.mValue);
                data.Add(currentKeyButton.mGED.mValue);

                if (!selectedButton.DataExists(data))
                {
                    if (strKey.Length > 1)
                    {
                        strKey = strKey.Substring(0, 2);
                    }
                    selectedButton.AddTag(colScaleType, strKey, data);
                    return true;
                }
            }

            return false;
        }

        public void SetSelected(Button selectedButton, Button currentKeyButton = null, Button currentModeButton = null)
        {
            int val = selectedButton.mGED.mValue;

            bool switchToSlot = false;
            if(val >= (int)eSaveSlots.Slot1)
            {
                switchToSlot = true;
            }

            if (mCurrentSelected == (eSaveSlots)val)
            {
                return;
            }

            if(!mMEI.mbNotEmpty && mSaveSongDataList[val] == null) //TODO: Stops taking up slots by setting them with empty current info
            {
                return;
            }

            if (mSaveSongDataList[val]==null)
            {
                //Load song data
                mSaveSongDataList[val] = mMEI.GetSaveLoad().GetCurrentSongData(mMEI, switchToSlot);

                if(mCurrentButtonSelected != null)
                {
                    selectedButton.CopyButtonTags(mCurrentButtonSelected);
                }

                AddTag( selectedButton,  currentKeyButton ,  currentModeButton );
                mMEI.GetSaveLoad().SetToSaveData(mSaveSongDataList[val], mMEI); // if we have edited a song to a new key or mode when going through the GetCurrentSongData above we need to load it back in to the current play area to reflect that

                SetButtonSaveSlotName(val);
            }
            else
            {
                //We're moving away from a slot/main entry check if we need to update and tag it to current set key/mode as we move away
                bool currentIsASlot = mCurrentSelected >= eSaveSlots.Slot1;

                UpdateCurrentSlotToEdits(currentIsASlot); //we're moving away from this slot so ensure it is up to data with song key and mode - todo add tag for this last song
                if(currentIsASlot)
                {
                    AddTag(mCurrentButtonSelected, currentKeyButton, currentModeButton);
                }

                mMEI.GetSaveLoad().SetToSaveData(mSaveSongDataList[val], mMEI); // dont add tag for this new one though!
            }

            mCurrentSelected = (eSaveSlots)val;
            mCurrentButtonSelected = selectedButton; //Type check!
        }

        void UpdateCurrentSlotToEdits(bool switchToSlot)
        {
            //Load song data
            mSaveSongDataList[(int)mCurrentSelected] = mMEI.GetSaveLoad().GetCurrentSongData(mMEI, switchToSlot);
        }

        void SetButtonSaveSlotName(int val)
        {
            const int maxLenName = 17;
            string newButtonText = mSaveSongDataList[val].mSongName;
            if (newButtonText == "") //this would be the case if we have not loaded the song in and have just edited what's there and have no name in this 
            {
                newButtonText = "Emp:";
            }
            else if (newButtonText.Length > maxLenName)
            {
                newButtonText = newButtonText.Substring(0, maxLenName);
            }

            newButtonText += " ";
            newButtonText += mSaveSlotNames[val];

            newButtonText = WordHelper.GetWords(newButtonText, 14);  //

            mSaveSongDataList[val].mSongName = newButtonText;
            mpbb.SetButtonText(val, newButtonText);
        }

        public bool FreeCurrentSaveSlot()
        {
            if(mCurrentSelected > eSaveSlots.Main3)
            {
                ResetSlot((int)mCurrentSelected);
                mpbb.ResetButtonToGED((int)mCurrentSelected);
                mpbb.SetSelected(0);
                mCurrentSelected = eSaveSlots.Main1;
                mCurrentButtonSelected = null;

                mMEI.GetSaveLoad().SetToSaveData(mSaveSongDataList[(int)mCurrentSelected], mMEI); // dont add tag for this new one though!
                return true;
            }

            return false;
        }

        public void NukeAll()
        {
            mCurrentSelected = eSaveSlots.Main1;
            mCurrentButtonSelected = null;
            mSaveSongDataList = new List<SaveLoad.SaveSongData>();
            mpbb.SetSelected(0);
            ResetSlots();
        }

        //Wehn we load a song from a file we put it in the first main slot (TODO: maybe cycle through mains slots) and overwrite whats there
        //main slots dont get their notes pared back by filtering through the key/mode lines
        public void LoadInSong(string songName)
        {
            mCurrentSelected = eSaveSlots.Main1;
            //Song should be loaded into melEdinterf by here so get it into our main slot
            mSaveSongDataList[(int)mCurrentSelected] = mMEI.GetSaveLoad().GetCurrentSongData(mMEI, false);
            mCurrentButtonSelected = null;
            SetButtonSaveSlotName((int)mCurrentSelected);
            mpbb.SetSelected(0);
        }

        public void Init(MelodyEditorInterface mei, ParamButtonBar pbb)
        {
            mpbb = pbb;
            mMEI = mei;
        }
    }
}
