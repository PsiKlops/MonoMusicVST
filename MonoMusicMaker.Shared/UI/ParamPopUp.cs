using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
//using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace MonoMusicMaker
{
    public class ParamPopUp
    {
        const int XPOS = 100;
        const int YPOS = 100;
        const int WIDTH = 1600;
        const int HEIGHT = 800;
        const int BUTTON_GAP = 20;
        const int EXIT_BUTT_X = XPOS + (WIDTH / 2) - (Button.mWidth / 2);
        const int EXIT_BUTT_Y = YPOS + BUTTON_GAP;
        const int DELETE_BUTT_Y = YPOS + HEIGHT - (Button.mBarHeight + BUTTON_GAP);

        const int MAIN_BEAT_ACCENT_Y = EXIT_BUTT_Y + (Button.mTickBoxSize);
        const int BASS_BEAT_ACCENT_Y = EXIT_BUTT_Y + (2 * Button.mTickBoxSize + 2 * BUTTON_GAP);
        const int FEEL_GOOD_BOOLS_Y = EXIT_BUTT_Y + (6 * Button.mTickBoxSize + 2 * BUTTON_GAP);

        const int UP_DOWN_NOTE_X = XPOS + WIDTH - BUTTON_GAP - Button.mTickBoxSize;

        const int START_STOP_NOTE_X = XPOS + WIDTH - 2 * (BUTTON_GAP + Button.mTickBoxSize);


        const int PARAM_BAR_YPOS = YPOS + HEIGHT / 2 - (Button.mBarHeight / 2);
        const int PARAM_BAR_XPOS_START = 40;
        const int PARAM_BAR_XPOS_GAP = 40;

        const int TEXT_INFO_X = XPOS + BUTTON_GAP;
        const int TEXT_INFO_Y = EXIT_BUTT_Y;
        const int TEXT_CHORD_INFO_Y = TEXT_INFO_Y + 40;

        const int TEXT_RECT_BACK_X = TEXT_INFO_X;
        const int TEXT_RECT_BACK_Y = TEXT_INFO_Y;
        const int TEXT_RECT_BACK_H = 70;
        const int TEXT_RECT_BACK_W = 600;

        const int NUM_FEEL_GOOD_BOOLS = 5; // Add second note, regular, 1st inversion, 2nd inversion
        string[] feelGoodTabNames = new string[NUM_FEEL_GOOD_BOOLS] { "Add 2nd", "Regular", "1ST INV", "2ND INV", "4 Beat" };

        Button mExitButton;
        Button mDeleteButton;

        Button mUpNote;
        Button mDownNote;
        Button mStartStop;
        Button mShiftLeft;

        Button mMidiIn;

        List<Button> mAllButtons = new List<Button>();

        public enum UIButtonMask
        {
            BIT_EMPTY = 0,
            BIT0 = 1 << 0,
            BIT1 = 1 << 1,
            BIT2 = 1 << 2,
            BIT3 = 1 << 3,
            BIT4 = 1 << 4,
            BIT5 = 1 << 5,
            BIT6 = 1 << 6,
            BIT7 = 1 << 7,
            BIT8 = 1 << 8,
            BIT9 = 1 << 9,
            BIT10 = 1 << 10,
            BIT11 = 1 << 11,
            BIT12 = 1 << 12,
        }


        List<Button> mAccentBeatTabs = new List<Button>();
        List<Button> mBassAccentBeatTabs = new List<Button>();
        List<Button> mFeelGoodBoolsTabs = new List<Button>();

        public bool Active { get; set; } = false;
        Vector2 mScreenPos;   //top left
        Rectangle mPopUpRect = new Rectangle(XPOS, YPOS, WIDTH, HEIGHT);
        SpriteFont mFont;

        Rectangle mTextBackRect;

        AIChordRegion mCurrentRegion; //used to get data and set up pop up
        String mRegionInfoText; //bar range covered mostly I think
        String mCurrentChordInfoText; // describes chord e.g. C# Maj 1st Inv. or F# Feel Good, 2nd note, 2nd Inv.

        List<ParamButtonBar> mParamBars = new List<ParamButtonBar>();

        int CacheLoopStart = 0;
        int CacheLoopEnd = 0;


        public void Init()
        {
            mUpNote = new Button(new Vector2(UP_DOWN_NOTE_X, MAIN_BEAT_ACCENT_Y), "UpArrow");
            mUpNote.mType = Button.Type.Tick;
            mUpNote.ColOff = Color.White * 0.6f;
            mUpNote.ColOn = Color.White * 0.6f;
            mAllButtons.Add(mUpNote);

            mDownNote = new Button(new Vector2(UP_DOWN_NOTE_X, BASS_BEAT_ACCENT_Y), "DownArrow");
            mDownNote.mType = Button.Type.Tick;
            mDownNote.ColOff = Color.White * 0.6f;
            mDownNote.ColOn = Color.White * 0.6f;
            mAllButtons.Add(mDownNote);

            mMidiIn = new Button(new Vector2(START_STOP_NOTE_X, FEEL_GOOD_BOOLS_Y), "PlainWhite");
            mMidiIn.ButtonText = "MIDI IN";
            mMidiIn.ButtonTextColour = Color.Black;
            mMidiIn.mType = Button.Type.Tick;
            mMidiIn.ColOff = Color.DarkGray * 0.6f;
            mMidiIn.ColOn = Color.DarkGreen * 0.6f;
            mAllButtons.Add(mMidiIn);

            mStartStop = new Button(new Vector2(START_STOP_NOTE_X, BASS_BEAT_ACCENT_Y), "PlainWhite");
            mStartStop.mType = Button.Type.Tick;
            mStartStop.ColOff = Color.Red * 0.6f;
            mStartStop.ColOn = Color.Green * 0.6f;
            mAllButtons.Add(mStartStop);

            mShiftLeft = new Button(new Vector2(START_STOP_NOTE_X, MAIN_BEAT_ACCENT_Y), "LeftArrow");
            mShiftLeft.mType = Button.Type.Tick;
            mShiftLeft.ColOff = Color.Yellow * 0.6f;
            mShiftLeft.ColOn = Color.Yellow * 0.6f;
            mAllButtons.Add(mShiftLeft);

            mExitButton = new Button(new Vector2(EXIT_BUTT_X, EXIT_BUTT_Y), "PlainWhite");
            mExitButton.ButtonText = "Exit";
            mExitButton.mMask = MelodyEditorInterface.UIButtonMask.UIBM_Popup;
            mExitButton.mType = Button.Type.Bar;
            mExitButton.ColOn = Color.BlanchedAlmond;
            mExitButton.ColOff = Color.BlanchedAlmond;
            mExitButton.ButtonTextColour = Color.Black;
            mAllButtons.Add(mExitButton);

            mDeleteButton = new Button(new Vector2(EXIT_BUTT_X, DELETE_BUTT_Y), "PlainWhite");
            mDeleteButton.ButtonText = "Delete";
            mDeleteButton.mMask = MelodyEditorInterface.UIButtonMask.UIBM_Popup;
            mDeleteButton.mType = Button.Type.Bar;
            mDeleteButton.ColOn = Color.BlanchedAlmond;
            mDeleteButton.ColOff = Color.BlanchedAlmond;
            mDeleteButton.ButtonTextColour = Color.Black;
            mAllButtons.Add(mDeleteButton);

            mTextBackRect = new Rectangle(TEXT_RECT_BACK_X, TEXT_RECT_BACK_Y, TEXT_RECT_BACK_W, TEXT_RECT_BACK_H);

            const int numBeatAccents = 8;

            int startBeatAccentX = XPOS + PARAM_BAR_XPOS_START;
            int breakBeatsIntoGroupOfSize = 4;
            int startX = startBeatAccentX;
            for (int i = 0; i < numBeatAccents; i++)
            {
                int beatXGap = (i + 1) % breakBeatsIntoGroupOfSize == 0 ? 100 : 50;

                Button beatTick;

                beatTick = new Button(new Vector2(startX, MAIN_BEAT_ACCENT_Y), "PlainWhite"); ;
                beatTick.mType = Button.Type.Tick;
                beatTick.ColOff = Color.DarkGray * 0.6f;
                beatTick.ColOn = Color.DarkGreen * 0.6f;
                //beatTick.mMask = MelodyEditorInterface.UIButtonMask.UIBM_Tick;

                startX += Button.mTickBoxSize + beatXGap;

                mAccentBeatTabs.Add(beatTick);
            }

            startX = startBeatAccentX;
            for (int i = 0; i < numBeatAccents; i++)
            {
                int beatXGap = (i + 1) % breakBeatsIntoGroupOfSize == 0 ? 100 : 50;

                Button beatTick;

                beatTick = new Button(new Vector2(startX, BASS_BEAT_ACCENT_Y), "PlainWhite"); ;
                beatTick.mType = Button.Type.Tick;
                beatTick.ColOff = Color.DarkGray * 0.4f;
                beatTick.ColOn = Color.DarkGreen * 0.4f;
                //beatTick.mMask = MelodyEditorInterface.UIButtonMask.UIBM_Tick;

                startX += Button.mTickBoxSize + beatXGap;

                mBassAccentBeatTabs.Add(beatTick);
            }

            startX = startBeatAccentX;
            int feelGoodTabGap = 140;
            for (int i = 0; i < NUM_FEEL_GOOD_BOOLS; i++)
            {
                Button beatTick;

                beatTick = new Button(new Vector2(startX, FEEL_GOOD_BOOLS_Y), "PlainWhite"); ;
                beatTick.mType = Button.Type.Tick;
                beatTick.ColOff = Color.DarkGray * 0.4f;
                beatTick.ColOn = Color.DarkGreen * 0.4f;
                beatTick.ButtonTextColour = Color.Black;
                beatTick.ButtonText = feelGoodTabNames[i];
                //beatTick.mMask = MelodyEditorInterface.UIButtonMask.UIBM_Tick;

                startX += Button.mTickBoxSize + feelGoodTabGap;

                mFeelGoodBoolsTabs.Add(beatTick);
            }

            //set up the mutual buttons so they operate as a button panel where only one is on at a time

            mFeelGoodBoolsTabs[1].AddMutualToggle(mFeelGoodBoolsTabs[2]);
            mFeelGoodBoolsTabs[1].AddMutualToggle(mFeelGoodBoolsTabs[3]);

            mFeelGoodBoolsTabs[2].AddMutualToggle(mFeelGoodBoolsTabs[1]);
            mFeelGoodBoolsTabs[2].AddMutualToggle(mFeelGoodBoolsTabs[3]);

            mFeelGoodBoolsTabs[3].AddMutualToggle(mFeelGoodBoolsTabs[2]);
            mFeelGoodBoolsTabs[3].AddMutualToggle(mFeelGoodBoolsTabs[1]);
            mFeelGoodBoolsTabs[1].mbOn = true; //set this on first
            mFeelGoodBoolsTabs[4].mbOn = true; //set this on first
        }

        public void AddParamBar(ParamButtonBar pbb)
        {
            int numBars = mParamBars.Count;
            Vector2 vPos = new Vector2();

            vPos.Y = PARAM_BAR_YPOS;
            vPos.X = XPOS + PARAM_BAR_XPOS_START + numBars * (PARAM_BAR_XPOS_GAP + Button.mWidth);
            pbb.Init(vPos);

            mParamBars.Add(pbb);
        }

        bool ShiftCurrentRegionLeft()
        {
            if (mCurrentRegion != null)
            {
                return mCurrentRegion.ShiftCurrentRegionLeft();
            }

            return false;
        }

        public void LoadContent(ContentManager contentMan, SpriteFont font)
        {
            mFont = font;
            //mDeleteButton.LoadContent(contentMan, font);
            //mExitButton.LoadContent(contentMan, font);
            //mUpNote.LoadContent(contentMan, font);
            //mDownNote.LoadContent(contentMan, font);
            //mStartStop.LoadContent(contentMan, font);

            //mShiftLeft.LoadContent(contentMan, font);

            foreach(Button b in mAllButtons)
            {
                b.LoadContent(contentMan, font);
            }

            foreach (Button ab in mAccentBeatTabs)
            {
                ab.LoadContent(contentMan, font);
            }
            foreach (Button ab in mBassAccentBeatTabs)
            {
                ab.LoadContent(contentMan, font);
            }
            foreach (Button ab in mFeelGoodBoolsTabs)
            {
                ab.LoadContent(contentMan, font);
            }

            foreach (ParamButtonBar pbb in mParamBars)
            {
                pbb.LoadContent(contentMan, font);
            }
        }

        void SetUpChordTextInfo()
        {
            mCurrentChordInfoText = mCurrentRegion.GetChordTypeName();
        }

        public void Show(AIChordRegion region, MelodyEditorInterface.MEIState state)
        {
            Active = true;
            mCurrentRegion = region;
            state.mCurrentButtonUpdate = MelodyEditorInterface.UIButtonMask.UIBM_Popup; //blank out all other buttons

            PlayArea pa = state.mMeledInterf.GetCurrentPlayArea();
            CacheLoopStart = pa.StartBar;
            CacheLoopEnd = pa.EndBar;
            mCurrentRegion.SetPlayAreaLoopRange();
            state.PlayAreaChanged(true); //so square cursor is updated to show loop region correctly back to how it was

            RefreshPopupToRegion();
        }

        public void RefreshPopupToRegion()
        {
            if(mCurrentRegion==null)
            {
                System.Diagnostics.Debug.Assert(false, string.Format("mCurrentRegion==null"));
                return;
            }

            mRegionInfoText = string.Format("Bar {0} to {1}", mCurrentRegion.BeatPos, mCurrentRegion.BeatEndPos);

            SetUpChordTextInfo();

            int count = 0;
            //Set up param bars to this loaded region - the data should overly in an expected way
            foreach (ParamButtonBar pbb in mParamBars)
            {
                pbb.SetSelected(mCurrentRegion.GetDataValue(count));
                count++;//TODO!
            }

            SetTabArrayFromValue(mAccentBeatTabs, mCurrentRegion.GetDataValue(count++));
            SetTabArrayFromValue(mBassAccentBeatTabs, mCurrentRegion.GetDataValue(count++));
            SetTabArrayFromValue(mFeelGoodBoolsTabs, mCurrentRegion.GetDataValue(count++));
        }

        public bool Update(MelodyEditorInterface.MEIState state)
        {
            bool bValueChanged = false;
            int count = 0;

            ParamButtonBar beenSetInBarMode = null;
            foreach (ParamButtonBar pbb in mParamBars)
            {
                if (pbb.Update(state))
                {
                    beenSetInBarMode = pbb; //Exit since this has been set 
                }
            }

            if (beenSetInBarMode != null)
            {
                foreach (ParamButtonBar pbb in mParamBars)
                {
                    if (pbb != beenSetInBarMode)
                    {
                        pbb.ShowSelected(state);
                    }
                }
            }
  
            //Keep updating this so any edits can be changed in playing music immediately
            count = 0;
            foreach (ParamButtonBar pbb in mParamBars)
            {
                bValueChanged |= mCurrentRegion.SetDataValue(count, pbb.GetSelected());
                count++; //TODO!
            }

            //Click off to close param bar
            if (bValueChanged == false && beenSetInBarMode == null)
            {
                if (state.input.LeftUp)
                {
                    foreach (ParamButtonBar pbb in mParamBars)
                    {
                        pbb.ShowSelected(state);
                    }
                }
            }

            //BUTTON UPDATES
            if (mExitButton.Update(state))
            {
                PlayArea pa = state.mMeledInterf.GetCurrentPlayArea();
                pa.StartBar = CacheLoopStart;
                pa.EndBar = CacheLoopEnd;
                state.PlayAreaChanged(true); //so square cursor is updated to show loop region correctly back to how it was

                count = 0;
                foreach (ParamButtonBar pbb in mParamBars)
                {
                    pbb.Exit();
                    count++; //TODO!
                }
                Active = false;
                mCurrentRegion = null;
            }
            if (mDeleteButton.Update(state))
            {
                //DO SOMETHING!
                if (mCurrentRegion != null)
                {
                    mCurrentRegion.GetParentLine().DeleteRegion(mCurrentRegion);
                    state.PlayAreaChanged(true); //so square cursor is updated
                    Active = false;
                    mCurrentRegion = null;
                }
            }

            if (mUpNote.Update(state))
            {
                if(mCurrentRegion.IncrementDecrementRootNote(true))
                {
                    state.PlayAreaChanged(true);//so square cursor is updated
                    bValueChanged = true;
                }
                mUpNote.mbOn = false;
            }
            if (mDownNote.Update(state))
            {
                if(mCurrentRegion.IncrementDecrementRootNote(false))
                {
                    state.PlayAreaChanged(true); //so square cursor is updated
                    bValueChanged = true;
                }
                mDownNote.mbOn = false;
            }

            if (mMidiIn.Update(state))
            {
                state.mMidiThrough = !mMidiIn.mbOn; //stop regular midi keyoard sounds goiung through while controlling the note in the region
            }

            if(mMidiIn.mbOn && mCurrentRegion != null)
            {
                if(mCurrentRegion.SetToNote(state.mMidiInNote))
                {
                    state.PlayAreaChanged(true); //so square cursor is updated
                    bValueChanged = true;
                }
            }

            if (mStartStop.Update(state))
            {
                state.mMaskToSetButton = MelodyEditorInterface.UIButtonMask.UIBM_Start;
            }

            if (mShiftLeft.Update(state))
            {
                //if (mCurrentRegion.IncrementDecrementRootNote(false))
                //{
                //    state..PlayAreaChanged(true); //so square cursor is updated
                //    bValueChanged = true;
                //}
                if (ShiftCurrentRegionLeft())
                {
                    state.PlayAreaChanged(true);
                    bValueChanged = true;
                }
                mShiftLeft.mbOn = false;
            }

            foreach (Button ab in mAccentBeatTabs)
            {
                if (ab.Update(state))
                {
                    bValueChanged = true;
                }
            }
            foreach (Button ab in mBassAccentBeatTabs)
            {
                if (ab.Update(state))
                {
                    bValueChanged = true;
                }
            }
            foreach (Button ab in mFeelGoodBoolsTabs)
            {
                if (ab.Update(state))
                {
                    bValueChanged = true;
                }
            }

            if (bValueChanged)
            {
                int value = GetValueFromTabArray(mAccentBeatTabs);
                mCurrentRegion.SetDataValue(count++, value);
                value = GetValueFromTabArray(mBassAccentBeatTabs);
                mCurrentRegion.SetDataValue(count++, value);
                value = GetValueFromTabArray(mFeelGoodBoolsTabs);
                mCurrentRegion.SetDataValue(count++, value);
                SetUpChordTextInfo();
            }

            if (Active)
            {
                state.mCurrentButtonUpdate = MelodyEditorInterface.UIButtonMask.UIBM_Popup; //blank out buttons
            }
            else
            {
                state.mCurrentButtonUpdate = MelodyEditorInterface.UIButtonMask.UIBM_ALL; //blank out buttons
            }
            state.mbRegionRefreshToEdit = bValueChanged;
            return bValueChanged;
        }

        int   GetValueFromTabArray(List<Button> buttons)
        {
            int value = 0;
            int index = 0;
            foreach(Button but in buttons)
            {
                if(but.mbOn)
                {
                    value |= (1 << index);
                }
                index++;
            }

            return value;
        }

        void SetTabArrayFromValue(List<Button> buttons, int value)
        {
            int index = 0;
            foreach (Button but in buttons)
            {
                int bitValue = (1 << index);

                if ((value & bitValue) == bitValue)
                {
                    but.mbOn = true;
                }
                else
                {
                    but.mbOn = false;
                }
                index++;
            }
        }

        public void Draw(SpriteBatch sb)
        {
            if (!Active)
            {
                return;
            }
            sb.Begin();
            sb.FillRectangle(mPopUpRect,Color.SteelBlue * 0.5f );
            sb.DrawRectangle(mPopUpRect, Color.Black, 4);
            sb.FillRectangle(mTextBackRect, Color.White * 0.5f );
            sb.DrawString(mFont, mRegionInfoText, new Vector2(TEXT_INFO_X, TEXT_INFO_Y), Color.Black);
            sb.DrawString(mFont, mCurrentChordInfoText, new Vector2(TEXT_INFO_X, TEXT_CHORD_INFO_Y), Color.Black);
            sb.End();

            mExitButton.Draw(sb);
            mDeleteButton.Draw(sb);

            mUpNote.Draw(sb);
            mDownNote.Draw(sb);
            mStartStop.Draw(sb);

            mShiftLeft.Draw(sb);

            mMidiIn.Draw(sb);

            foreach (Button ab in mAccentBeatTabs)
            {
                ab.Draw(sb);
            }
            foreach (Button ab in mBassAccentBeatTabs)
            {
                ab.Draw(sb);
            }
            foreach (Button ab in mFeelGoodBoolsTabs)
            {
                ab.Draw(sb);
            }


            sb.Begin();
            sb.DrawRectangle(mExitButton.Rectangle, Color.Black);
            sb.DrawRectangle(mDeleteButton.Rectangle, Color.Black);

            sb.DrawRectangle(mUpNote.Rectangle, Color.Black);
            sb.DrawRectangle(mDownNote.Rectangle, Color.Black);
            sb.DrawRectangle(mMidiIn.Rectangle, Color.Black);

            foreach (Button ab in mAccentBeatTabs)
            {
                sb.DrawRectangle(ab.Rectangle, Color.Black);
            }
            foreach (Button ab in mBassAccentBeatTabs)
            {
                sb.DrawRectangle(ab.Rectangle, Color.Black);
            }
            foreach (Button ab in mFeelGoodBoolsTabs)
            {
                sb.DrawRectangle(ab.Rectangle, Color.Black);
            }
            sb.End();

            foreach (ParamButtonBar pbb in mParamBars)
            {
                pbb.Draw(sb);

            }
        }
    }
}
