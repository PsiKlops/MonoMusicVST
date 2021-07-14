using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;


namespace MonoMusicMaker //.Shared.MEI
{
    using ListGridEntries = List<ButtonGrid.GridEntryData>;
    public class MidiTrackSelect
    {
        public bool Active { get; set; } = false;
        public const int NUM_MIDI_TRACKS = 16;
        public const int DRUM_MACHINE_PLAY_AREA_INDEX = 9;

        ParamButtonBar mTrackSelectBar;

        string[] mTrackNames;
        ButtonGrid mMidiTrackBG;

        ListGridEntries mMidiTrackGED = new List<ButtonGrid.GridEntryData>();
        ButtonGrid.GridEntryData mMidiTrackExitGED;

        Button mCurrentMidiTrackButton = null;
        ButtonGrid mLastMidiTrackGrid = null;
        ButtonGrid mActiveGrid = null;
        public void SetActive()
        {
            mActiveGrid = mMidiTrackBG;
            Active = true;
        }
        public void SetInactive()
        {
            mActiveGrid = null; //leave altogether
            Active = false; //close screen
        }

        public MidiTrackSelect(ParamButtonBar trackSelectBar)
        {
            mTrackSelectBar =  trackSelectBar;
            Init();
        }

        public void SetTrackSelectBar(ParamButtonBar trackSelectBar)
        {
            mTrackSelectBar = trackSelectBar;
        }

        void Init()
        {
            mTrackNames = new string[16] {    "Track 1", "Track 2", "Track 3", "Track 4", "Track 5", "Track 6", "Track 7", "Track 8",
                                                    "Track 9", "DRUMS", "Track 11", "Track 12", "Track 13", "Track 14", "Track 15", "Track 16"  };

            int count = 0;
            int patchCount = 0;

            mMidiTrackExitGED.mClickOn = ExitToPlayArea;
            mMidiTrackExitGED.mValue = ButtonGrid.GridEntryData.EXIT_TYPE;
            mMidiTrackExitGED.mOnString = "EXIT";

            mMidiTrackGED.Add(mMidiTrackExitGED);

            foreach (string str in mTrackNames)
            {
                ButtonGrid.GridEntryData midiTrackEntry;

                midiTrackEntry.mOnString = str;
                midiTrackEntry.mValue = count;
                midiTrackEntry.mClickOn = SetMidiTrack;
                count++;
                mMidiTrackGED.Add(midiTrackEntry);
            }


            mMidiTrackBG = new ButtonGrid(mMidiTrackGED);

        }

        public void SetButtonTextOld(int buttNum, string text, bool setCol = false)
        {
            if(buttNum==10)
            {
                text = "DRUMS";
            }

            mMidiTrackBG.mButtons[buttNum].ButtonText = text;

            if (setCol)
            {
                mMidiTrackBG.mButtons[buttNum].ButtonTextColour = Color.Red;
            }
            else
            {
                mMidiTrackBG.mButtons[buttNum].ButtonTextColour = ButtonGrid.DefaultTextCol;
            }
        }

        public void SetButtonText(int buttNum, string text, bool setCol = false)
        {
            if(mTrackSelectBar==null)
            {
                return;
            }

            if (buttNum == 10)
            {
                text = "DRUMS";
            }

            buttNum -= 1; //Old grid sustem has exit button we need to subtract to get the same range for the param bar - bit of a hack I know!
            Button button = mTrackSelectBar.mButtons[buttNum];
 
            button.ButtonText = string.Format("{0}:{1}", buttNum+1, text ) ;

            if (setCol)
            {
                button.ButtonTextColour = Color.LightPink;
            }
            else
            {
                button.ButtonTextColour = ButtonGrid.DefaultTextCol;
            }
        }

        public void ExitToPlayArea(Button button, MelodyEditorInterface.MEIState state)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("Midi Track ExitToPlayArea{0}", button.mGED.mValue));
            SetInactive();
        }

        public void SetMidiTrack(Button button, MelodyEditorInterface.MEIState state)
        {
            mActiveGrid.SetOnAndClearAllOthers(button);
            System.Diagnostics.Debug.WriteLine(string.Format("Midi Track SetMidiTrack{0}", button.mGED.mValue));

            state.StopAllNotes(); //TODO Blatt out a clear to stop notes being stuck on 
          
            if (state.mCurrentTrackPlayArea != button.mGED.mValue)
            {
                state.mSoloCurrent = false; //always turn off when we move away a track to another one
                state.mCurrentTrackPlayArea = button.mGED.mValue;
                state.PlayAreaChanged(true, false, false);
                state.mMeledInterf.SetCurrentPlayArea(button.mGED.mValue);
            }

            mActiveGrid = null; //leave altogether
            Active = false; //close screen
        }

        public void LoadContent(ContentManager contentMan, SpriteFont font)
        {
            mMidiTrackBG.LoadContent(contentMan, font);
        }

        public bool Update(MelodyEditorInterface.MEIState state)
        {
            if (mActiveGrid != null)
            {
                mActiveGrid.Update(state);
                return true;
            }
            return false;
        }

        public void Draw(SpriteBatch sb)
        {
            if (mActiveGrid != null)
            {
                mActiveGrid.Draw(sb);
            }
        }
    }
}