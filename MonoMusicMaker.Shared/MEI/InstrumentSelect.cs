using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
//using Commons.Music.Midi;
//using NAudio.Midi;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MonoMusicMaker //.MEI
{
    using ListGridEntries = List<ButtonGrid.GridEntryData>;
    class InstrumentSelect
    {
        public bool Active
        { get; set; } = false;

        const int NUM_INST_IN_GROUP = 8;
        string[] mInstFamilyNames;

        ButtonGrid mInstFamilySelectBG;
        List<ButtonGrid> mListInstGroupSelectBG = new List<ButtonGrid>();

        ListGridEntries mInstFamilies = new List<ButtonGrid.GridEntryData>();
        List<ListGridEntries> mInstGroups = new List<ListGridEntries>();
        ButtonGrid.GridEntryData mInstGroupBackGED;
        //ButtonGrid.GridEntryData mInstGroupExitGED;

        Button mCurrentGroupButton = null;
        Button mLastTriedGroupButton = null;
        ButtonGrid mActiveGrid = null;
        ButtonGrid mLastInstrumentGrid = null;

        public InstrumentSelect()
        {
            Init();
        }

        public void SetActive()
        {
            mActiveGrid = mInstFamilySelectBG;
            Active = true;
        }
        public void SetInActive()
        {
            mActiveGrid = null;
            Active = false;
        }

        void Init()
        {
            mInstFamilyNames =  new string[16] {    "Piano", "Chrom Perc", "Organ", "Guitar", "Bass", "Strings", "Ensemble", "Brass",
                                                    "Reed", "Pipe", "Syn Lead", "Syn Pad", "Syn FX", "Ethnic", "Perc", "Sound FX"  };
            int count = 0;
            int patchCount = 0;
            mInstGroupBackGED.mClickOn = BackToFamilySelectGrid;
            mInstGroupBackGED.mValue = ButtonGrid.GridEntryData.BACK_TYPE;
            mInstGroupBackGED.mOnString = "BACK";

            //mInstGroupExitGED.mClickOn = ExitToPlayArea;
            //mInstGroupExitGED.mValue = ButtonGrid.GridEntryData.EXIT_TYPE;
            //mInstGroupExitGED.mOnString = "EXIT";

            //mInstFamilies.Add(mInstGroupExitGED);

            foreach (string str in mInstFamilyNames)
            {
                ButtonGrid.GridEntryData instFamEntry;

                instFamEntry.mOnString = str;
                instFamEntry.mValue = count;
                instFamEntry.mClickOn = SetInstrumentFamily;
                count++;
                mInstFamilies.Add(instFamEntry);
                patchCount = CreateInstrumentGroup(patchCount);
            }

            mInstFamilies.Add(mInstGroupBackGED);

            mInstFamilySelectBG = new ButtonGrid(mInstFamilies);

        }

        int CreateInstrumentGroup(int patchStart)
        {
            const int MAX_STR_LEN = 12;
            ListGridEntries lge = new ListGridEntries();
            //lge.Add(mInstGroupExitGED);
            lge.Add(mInstGroupBackGED);
            int currentPatch = patchStart;
            for (int i= 0; i< NUM_INST_IN_GROUP;i++)
            {
                currentPatch = patchStart + i;
                ButtonGrid.GridEntryData instEntry;
                string instName = NAudio.Midi.PatchChangeEvent.GetPatchName(currentPatch);

                string multiLine = instName.Replace(' ', '\n');

                string multiLine2 = GetWords(instName);
                var firstChars = instName.Length <= MAX_STR_LEN ? instName : instName.Substring(0, MAX_STR_LEN);
                instEntry.mValue = currentPatch;
                instEntry.mOnString = multiLine2; // firstChars;
                instEntry.mClickOn = SetInstrument;

                lge.Add(instEntry);

            }

            ButtonGrid instSelectBG = new ButtonGrid(lge);
            mListInstGroupSelectBG.Add(instSelectBG);

            return patchStart + NUM_INST_IN_GROUP;
        }

        public List<ButtonGrid.GridEntryData> GetGridEntries()
        {
            return mInstFamilies;
        }

        public void LoadContent(ContentManager contentMan, SpriteFont font)
        {
            mInstFamilySelectBG.LoadContent(contentMan,font);
            foreach (ButtonGrid bg in mListInstGroupSelectBG)
            {
                bg.LoadContent(contentMan, font);
            }
        }

        public void SetInstrumentFamily(Button button, MelodyEditorInterface.MEIState state)
        {
            mActiveGrid.SetOnAndClearAllOthers(mCurrentGroupButton);
            mLastTriedGroupButton = button;
            System.Diagnostics.Debug.WriteLine(string.Format("Set Instrument Family {0}", button.mGED.mValue));
            mActiveGrid = mListInstGroupSelectBG[button.mGED.mValue];
        }
        public void SetInstrument(Button button, MelodyEditorInterface.MEIState state)
        {
            button.mbOn = true;

            if(mLastTriedGroupButton!=null)
            {
                if(mCurrentGroupButton!=null)
                    mCurrentGroupButton.mbOn = false;
                mCurrentGroupButton = mLastTriedGroupButton;
                mCurrentGroupButton.mbOn = true;
            }
            mActiveGrid.ClearAllOnStateExcept(button);

            if (mLastInstrumentGrid!=mActiveGrid)
            {
                if(mLastInstrumentGrid!=null)
                {
                    mLastInstrumentGrid.ClearAllOnStateExcept(null);
                }
                mLastInstrumentGrid = mActiveGrid;
            }

            System.Diagnostics.Debug.WriteLine(string.Format("Set Instrument {0}", button.mGED.mValue));
            state.mMeledInterf.SetInstrument(button.mGED.mValue);
            //mActiveGrid = mInstFamilySelectBG;
        }
        public void BackToFamilySelectGrid(Button button, MelodyEditorInterface.MEIState state)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("BackToFamilySelectGrid{0}", button.mGED.mValue));
            if(mActiveGrid == mInstFamilySelectBG)
            {
                mActiveGrid = null; //leave altogether
                Active = false;
            }
            else
            {
                mActiveGrid = mInstFamilySelectBG; //go back to family grid
            }
        }
        public void ExitToPlayArea(Button button, MelodyEditorInterface.MEIState state)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("ExitToPlayArea{0}", button.mGED.mValue));
            mActiveGrid = null; //leave altogether
            Active = false;
        }

        public bool Update(MelodyEditorInterface.MEIState state)
        {
            if (mActiveGrid != null)
            {
                mActiveGrid.Update(state);
                //if(mActiveGrid == mInstFamilySelectBG)
                //{
                //    Button but = mActiveGrid.Update(state);
                //    if (but != null)
                //    {
                //        int groupSelected = but.mGED.mValue;
                //        if(mActiveGrid.AreAllOthersOff(but))
                //        {
                //            //pushed again so exit
                //            if(!but.mbOn)
                //            {
                //                but.mbOn = true;
                //                mActiveGrid = null;
                //                return false;
                //            }
                //        }
                //        mActiveGrid.ClearAllOnStateExcept(but);
                //        mActiveGrid = mListInstGroupSelectBG[groupSelected];
                //    }
                //}
                //else
                //{
                //    Button but = mActiveGrid.Update(state);
                //    if (but != null)
                //    {
                //        int instrumentSelected = but.mGED.mValue;
                //        //state.mMidiMgr.SetInstrument(instrumentSelected);
                //        mActiveGrid.ClearAllOnStateExcept(but);
                //        mActiveGrid = mInstFamilySelectBG;
                //    }
                //}

                return true;
            }

            return false;
        }

        static public string GetPatchString(int instrument)
        {
            string instName = NAudio.Midi.PatchChangeEvent.GetPatchName(instrument);
            string multiLine2 = GetWords(instName);
            return multiLine2;
        }

        static string GetWords(string input)
        {
            const int LEN_CUME_WORD = 10;
            string[] words = input.Split(' ');

            string multiLine2 = "";
            string cumulativeWord = "";
            foreach (string word in words)
            {
                if ((cumulativeWord.Length + word.Length + 1) > LEN_CUME_WORD)
                {
                    if(multiLine2!="")
                    {
                        multiLine2 += "\n";
                    }
                    multiLine2 += cumulativeWord;
                    cumulativeWord = word;
                }
                else
                {
                    if(cumulativeWord!="")
                    {
                        cumulativeWord += " " + word;
                    }
                    else
                    {
                        cumulativeWord = word;
                    }
                }
            }

            if(cumulativeWord!="")
            {
                if (multiLine2 != "")
                {
                    multiLine2 += "\n";
                }
                multiLine2 += cumulativeWord;
            }

            //foreach (string word in words)
            //{
            //    if(multiLine2!="")
            //    {
            //        cumulativeWord += word;

            //        if((cumulativeWord.Length + word.Length)> LEN_CUME_WORD)
            //        {

            //        }
            //        multiLine2 += "\n";
            //    }
            //    multiLine2 += word;
            //}

            return multiLine2;
        }

        public void Draw(SpriteBatch sb)
        {
            if(mActiveGrid!=null)
            {
                mActiveGrid.Draw(sb);
            }
         }
    }
}
