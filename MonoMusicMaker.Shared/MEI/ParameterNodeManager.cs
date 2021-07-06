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

namespace MonoMusicMaker
{
    public class ParameterNodeManager
    {

        public class ChannelParamType
        {
            public string name;
            public int command;
            public int lastDataSent;
            public float defaultRatio;

            public ChannelParamType(string nm, int com)
            {
                name = nm;
                command = com;
                lastDataSent = -1;
                defaultRatio = ParameterNodeRegion.DEFAULT_PARAM_RATIO;
            }
        }

        List<ChannelParamType> mParamTypesForChannel = new List<ChannelParamType>();

        void SetChannelParams()
        {
            ChannelParamType vol = new ChannelParamType("Volume", (int)MidiController.MainVolume);
            mParamTypesForChannel.Add(vol);

            ChannelParamType pan = new ChannelParamType("Pan", (int)MidiController.Pan);
            mParamTypesForChannel.Add(pan);

            ChannelParamType reverb = new ChannelParamType("Reverb Send", 91);
            mParamTypesForChannel.Add(reverb);

            ChannelParamType chorus = new ChannelParamType("Chorus Send", 93);
            mParamTypesForChannel.Add(chorus);

            ChannelParamType filter = new ChannelParamType("Filter Cutoff", 74);
            mParamTypesForChannel.Add(filter);

            ChannelParamType resonance = new ChannelParamType("Resonance", 71);
            mParamTypesForChannel.Add(resonance);

            ChannelParamType Phaser = new ChannelParamType("Phaser", 95);
            mParamTypesForChannel.Add(Phaser);

            ChannelParamType Detune = new ChannelParamType("Detune", 94);
            mParamTypesForChannel.Add(Detune);

            ChannelParamType Variation = new ChannelParamType("Variation", 94);
            mParamTypesForChannel.Add(Variation);
        }

        void SetChannelParamFromExampleRegionData(ParameterNodeRegion.ParameterNodeRegionSaveData r )
        {
            //List<PNR_CommandData> mCommands
            mParamTypesForChannel = new List<ChannelParamType>();

            foreach(ParameterNodeRegion.PNR_CommandData cmd in r.mCommands)
            {
                ChannelParamType param = new ChannelParamType(cmd.mName, cmd.mMidiCommand);
                mParamTypesForChannel.Add(param);
            }
        }

        public const float FOUR_BEAT_REGION_RANGE = 4.0f;
        const int mBeatLength = PlayArea.NUM_DRAW_BEATS; //num beats in track
        public const int SPACE_FOR_BANK_SELECT_BUTTONS = 100;

        const int BANK_SWITCH_BACK_GAP = 10;
        const int BANK_SWITCH_BACK_HEIGHT = Button.mTickBoxSize+2* BANK_SWITCH_BACK_GAP;

        Rectangle mBankSwitchRect;

        const int NUM_BANKS = 5;
        List<Button> mBankButtons = new List<Button>();

        public Rectangle mRect;
        public PlayArea mParent;
        public SpriteFont mFont;
        List<ParameterNodeRegion> mRegions;

        public List<ParameterNodeRegion> GetRegions()
        {
            return mRegions;
        }

        public static int SortRegionsByBeatStart(ParameterNodeRegion x, ParameterNodeRegion y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    // If x is null and y is null, they're
                    // equal. 
                    return 0;
                }
                else
                {
                    // If x is null and y is not null, y
                    // is greater. 
                    return -1;
                }
            }
            else
            {
                // If x is not null...
                //
                if (y == null)
                // ...and y is null, x is greater.
                {
                    return 1;
                }
                else
                {
                    if (x.BeatPos < y.BeatPos)
                    {
                        return -1;
                    }
                    else if (x.BeatPos > y.BeatPos)
                    {
                        return 1;
                    }

                    return 0;
                }
            }
        }

        //List<ParameterNodeRegion.PNRSaveData> mDeletedRegions; //when undoing or clicking again on space where old region was and saved here

        public static int mTrackWidth;
        public int mStartX;
        public int mStartY;
        bool mbIsDrumChannel = false;
        public bool mSelected = false;

        public int mParamBank = 0;

        //Wrong put in airegion itself List<Note> mRootNotes; //set up from the top single note along the tracks below to give the root note for chords arpeggios etc

        ParameterNodeRegion mLastRegion = null;

        public ParameterNodeManager()
        {
            //TEST WHN LOAD
        }


        public ParameterNodeManager(PlayArea parent)
        {
            mParent = parent;

            if (mParent.mChannel == 10)
            {
                mbIsDrumChannel = true;
            }

            SetChannelParams();
        }

        public void SetFont(SpriteFont font)
        {
            mFont = font;
            SetBankButtons();
        }

        void SetBankButtons()
        {
            int bankBackY = mParent.mAreaRectangle.Bottom - BANK_SWITCH_BACK_HEIGHT;

            mBankButtons = new List<Button>();

            int buttonWidthRequirement = (Button.mTickBoxSize + BANK_SWITCH_BACK_GAP);

            int buttonStartX = mTrackWidth / 2 - (buttonWidthRequirement * NUM_BANKS) / 2;
            int buttonY = bankBackY;
            int butX = buttonStartX;

            for (int i = 0; i < NUM_BANKS; i++)
            {
                Button but = new Button(new Vector2(butX, bankBackY + BANK_SWITCH_BACK_GAP), "PlainWhite");
                but.mType = Button.Type.Tick;
                but.ColOff = Color.Red * 0.6f;
                but.ColOn = Color.Green * 0.6f;
                but.TextMid = "Bank\n" + i.ToString();
                but.ButtonTextOff = "Bank\n" + i.ToString(); ;
                but.ButtonTextColour = Color.Black;

                butX += buttonWidthRequirement;
                but.SetFontDefaultTexture(mFont);
                mBankButtons.Add(but);
            }

            mBankButtons[0].mbOn = true;

            int startBackRectY = bankBackY ;
            int startBackRect = mBankButtons[0].Rectangle.X - BANK_SWITCH_BACK_GAP;
            int widthBackRect = mBankButtons[mBankButtons.Count-1].Rectangle.X+ buttonWidthRequirement - startBackRect;

            mBankSwitchRect = new Rectangle(startBackRect, startBackRectY, widthBackRect, BANK_SWITCH_BACK_HEIGHT);

        }

        public void Init()
        {
            mStartX = MelodyEditorInterface.TRACK_START_X;
            mStartY = MelodyEditorInterface.TRACK_START_Y - NoteLine.TRACK_HEIGHT;
            mTrackWidth = (int)mBeatLength * (int)MelodyEditorInterface.BEAT_PIXEL_WIDTH;
            mRect = new Rectangle(mStartX, mStartY, mTrackWidth, NoteLine.TRACK_HEIGHT);

            mRegions = new List<ParameterNodeRegion>(); //empty set
            //mDeletedRegions = new List<AIChordRegion.AIRegionSaveData>();
        }

        public void ToggleRegions(bool bEnable)
        {
            bool bAnyOn = false;
            bool bAnyOff = false;

            foreach (ParameterNodeRegion r in mRegions)
            {
                bAnyOff |= !r.Active;
                bAnyOn |= r.Active;
            }

            if(!bAnyOn)
            {
                foreach (ParameterNodeRegion r in mRegions)
                {
                    r.Active = true;
                }
            }
            //if (!bAnyOff)
            else
            {
                foreach (ParameterNodeRegion r in mRegions)
                {
                    r.Active = false;
                }
            }
        }

        // // // // // // // // // // // // //
        //LOAD SAVE
        public List<ParameterNodeRegion.ParameterNodeRegionSaveData> GetRegionSaveData()
        {
            List<ParameterNodeRegion.ParameterNodeRegionSaveData> saveRegionData = new List<ParameterNodeRegion.ParameterNodeRegionSaveData>();

            foreach (ParameterNodeRegion r in mRegions)
            {
                ParameterNodeRegion.ParameterNodeRegionSaveData sd = r.GetSaveData();
                saveRegionData.Add(sd);
            }

            return saveRegionData;
        }

        public void SetRegionSaveData(List<ParameterNodeRegion.ParameterNodeRegionSaveData> loadRegionData)
        {
            mRegions = new List<ParameterNodeRegion>(); //empty set
            mParamBank = 0;

            //Create new param list from save data in case it had different data/size
            SetChannelParamFromExampleRegionData(loadRegionData[0]);

            foreach (ParameterNodeRegion.ParameterNodeRegionSaveData ld in loadRegionData)
            {
                ParameterNodeRegion possibleRegion = new ParameterNodeRegion(this, mFont);
                possibleRegion.LoadSaveData(ld, mParamTypesForChannel);
                mRegions.Add(possibleRegion);
            }

            mRegions.Sort(SortRegionsByBeatStart);
        }

        ParameterNodeRegion TouchInRegion(Rectangle touchRect)
        {
            foreach (ParameterNodeRegion r in mRegions)
            {
                if (r.Overlaps(touchRect))
                {
                    return r;
                }
            }
            return null;
        }
        //Input allow setting 2 regions per 4 beat bar, fixed at placing at start or beat 2 pos for now
        //once set the touch will pass through to the region and allow setting AI states on it
        //eg. what chord type maj, 7th etc, note pattern ballad, arpeggio etc
        bool UpdateTopBarInput(MelodyEditorInterface.MEIState state)
        {
            if (state.input.LeftUp)
            {
                if (state.input.mRectangle.Intersects(mRect) && state.mbPlayAreaUpdate)
                {
                    //System.Diagnostics.Debug.WriteLine(string.Format(" HELD - - Track InputUpdate NoteNum: {0}", NoteNum));

                    ParameterNodeRegion r = TouchInRegion(state.input.mRectangle);

                    if (r == null)
                    {
                        //Note UI
                        int pixelOffsetX = state.input.mRectangle.X - mStartX;
                        float drawPosBeatTouch = (float)pixelOffsetX / MelodyEditorInterface.BEAT_PIXEL_WIDTH;
                        float actualbeatTouch = drawPosBeatTouch + mParent.StartBeat; //correct for draw offset

                        float fCorrectToRangePlacePosOffset = actualbeatTouch % FOUR_BEAT_REGION_RANGE;  //for now each new region will be 2 bars long and only place exactly at start of 4 bars or at middle of 4 bar
                        actualbeatTouch -= fCorrectToRangePlacePosOffset;


                        ParameterNodeRegion possibleRegion = new ParameterNodeRegion(this, mFont);

                        state.PlayAreaChanged(true); //So can see in square cursor TODO put in region?

                        float barWidthToUse = FOUR_BEAT_REGION_RANGE; //No longer support this two bar example -> TWO_BEAT_REGION_RANGE;
                        float barStartToUse = actualbeatTouch;

                        mLastRegion = possibleRegion;
                        possibleRegion.InitParamRegion(barStartToUse, barWidthToUse, mParamTypesForChannel); //redundant if getting old/last data above?
                        mRegions.Add(possibleRegion);
                        mRegions.Sort(SortRegionsByBeatStart);
                        mLastRegion.Active = true;
                        return true;
                    }
                    else
                    {
                        r.Active = !r.Active;
                        //mRegions.Remove(r);
                    }
                }
            }
            return false;
        }

        //LOAD SAVE
        // // // // // // // // // // // // //
        public void RefreshDrawRectForOffset(int offset)
        {
            //Refresh existing notes
            foreach (ParameterNodeRegion r in mRegions)
            {
                r.RefreshRectToData();
            }
        }
        public bool QuarterNoteHere(float pos)
        {
            foreach (ParameterNodeRegion r in mRegions)
            {
                if (r.IsNoteInRegion(pos, 0.25f))
                {
                    return true;
                }
            }

            return false;
        }

        public List<ParameterNodeRegion>  GetNormalisedRegionsForRange(float startBeat, float endBeat)
        {
            List < ParameterNodeRegion >  normaliseList = new List<ParameterNodeRegion>();

            foreach (ParameterNodeRegion r in mRegions)
            {
                if(r.BeatPos>=startBeat && r.BeatEndPos<=endBeat )
                {
                    ParameterNodeRegion newRegion = r.GetShiftedCopy(-startBeat);
                    normaliseList.Add(newRegion);
                }
            }

            return normaliseList;
        }

        ParameterNodeRegion mLastRegionPlaying = null;
        //public bool mDefaultParams = true; //if we are in a region with no param area we have default params which would be global volume and pan and the reset default
        public bool UpdatePlaying(MelodyEditorInterface.MEIState state )
        {
            foreach (ParameterNodeRegion r in mRegions)
            {
                if (r.UpdatePlaying(state))
                {
                    mLastRegionPlaying = r;
                    return true;
                }
            }

            if(mLastRegionPlaying !=null)
            {
                //Do some default param stuff
                mLastRegionPlaying.ResetMidiToDefault(state);
                mLastRegionPlaying = null;
            }

            return false;
        }

        ParameterNodeRegion mLastRegionInput = null;
        public bool UpdateInput(MelodyEditorInterface.MEIState state, bool bAllowInputPlayArea)
        {
            if (state.input.SecondHeld)
            {
                mLastRegionInput = null; //Reset this on second touch
            }

            if (mLastRegionInput!=null)
            {
                if(!mLastRegionInput.UpdateInput(state))
                {
                    mLastRegionInput = null;
                }
            }

            if (mLastRegionInput == null)
            {
                //Dont we need to just update while the song is passing through like a note?
                foreach (ParameterNodeRegion r in mRegions)
                {
                    if(r.UpdateInput(state))
                    {
                        mLastRegionInput = r;
                        break;
                    }
                }
            }

            bool bToggleTopTrack = false;
            if(mLastRegionInput==null)
            {
                if (mParent.HasCurrentView)
                {
                    if (state.mbAllowInputPlayArea && bAllowInputPlayArea) //TODO rationalise
                    {
                        bToggleTopTrack = UpdateTopBarInput(state);
                    }
                }
            }

            bool butUpdate = false;
            bool butHeld = false;
            int bankSelected = -1;
            int bankIndex = 0;
            foreach(Button but in mBankButtons)
            {
                if(but.Update(state))
                {
                    butUpdate = true;
                    bankSelected = bankIndex;
                }
                butHeld |= but.Held;
                bankIndex++;
            }

            if(butUpdate)
            {
                foreach (Button but in mBankButtons)
                {
                    if(but != mBankButtons[bankSelected])
                    {
                        but.mbOn = false; //turn all theother buttons off
                    }
                }
                mBankButtons[bankSelected].mbOn = true;
            }

            if (bankSelected !=-1 && bankSelected !=mParamBank)
            {
                mParamBank = bankSelected;

                foreach (ParameterNodeRegion r in mRegions)
                {
                    r.SetCurrentBankCommands(mParamBank);
                }
            }

            return mLastRegionInput != null || bToggleTopTrack || butUpdate || butHeld;
        }

        public void Draw(SpriteBatch sb)
        {
            Color col = Color.LightCyan;
            if (mSelected)
            {
                col = Color.Yellow;
            }
            sb.FillRectangle(mRect, col * .2f);
            sb.FillRectangle(mBankSwitchRect, Color.Gray * .9f);

            foreach (ParameterNodeRegion r in mRegions)
            {
                if(mLastRegionInput!=r)
                {
                    r.Draw(sb);
                }         
            }
            if(mLastRegionInput!=null)
            {
                mLastRegionInput.Draw(sb);
            }
            foreach (Button but in mBankButtons)
            {
                but.DrawExtBeginEnd(sb);
            }
        }
    }
}
