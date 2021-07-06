using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;

namespace MonoMusicMaker
{
    public abstract class MidiBase 
    {
        public const int MIDI_PARAM_RANGE = 127;
        public const int MID_VELOCITY = (int) (MIDI_PARAM_RANGE *0.5f);
        public const int LOW_VELOCITY = (int)(MIDI_PARAM_RANGE * 0.25f);
        public const int HIGH_VELOCITY = (int)(MIDI_PARAM_RANGE * 0.75f);

        public const int VELOCITY_1 = (int)(MIDI_PARAM_RANGE * 0.1f);
        public const int VELOCITY_2 = (int)(MIDI_PARAM_RANGE * 0.2f);
        public const int VELOCITY_3 = (int)(MIDI_PARAM_RANGE * 0.3f);
        public const int VELOCITY_4 = (int)(MIDI_PARAM_RANGE * 0.4f);
        public const int VELOCITY_5 = (int)(MIDI_PARAM_RANGE * 0.5f);
        public const int VELOCITY_6 = (int)(MIDI_PARAM_RANGE * 0.6f);
        public const int VELOCITY_7 = (int)(MIDI_PARAM_RANGE * 0.7f);
        public const int VELOCITY_8 = (int)(MIDI_PARAM_RANGE * 0.8f);
        public const int VELOCITY_9 = (int)(MIDI_PARAM_RANGE * 0.9f);
        public const int VELOCITY_10 = (int)(MIDI_PARAM_RANGE * 1f);

        protected string mSaveName = "";
        Vector2 mScreenPos = Vector2.Zero;
        Button mSaveNameDisplay;
        ButtonGrid.GridEntryData mSaveNameGED;
        public bool mbRecordEnabled = false;

        public MelodyEditorInterface.MEIState mState; //To allow sending midi notes around from keyboard and control from UI

        public void SetState(MelodyEditorInterface.MEIState state)
        {
            mState = state;
        }
        public void MidiInNoteOn(int channel, int note, int velocity)
        {
            if (velocity != 0)
                mState.mMidiInNote = note;

            if (mState.mMidiThrough)
            {
                PlayArea pa = mState.mMeledInterf.GetCurrentPlayArea();
                //Override channel to record on teh current visible are with its current instrument
                channel = pa.mChannel;

                note = pa.GetConvertedNoteNumForCurrentKeyScale(note, true, mState.mbKeyRangePlayAreaMode);

                if (note == -1)
                {
                    return;
                }

                if (velocity != 0)
                {
                    NoteOn(channel, note, velocity);

                    if(mState.Playing && mbRecordEnabled) //TODO change to record flag?
                    {
                        pa.RecordNoteOn(note, velocity);
                    }
                }
                else
                {
                    NoteOff(channel, note);
                    pa.RecordNoteOff(note);
                }
            }
        }

        public void SetPitchBend(int pitchBend)
        {
            mState.mPitchBend = pitchBend;
            mState.mDebugPanel.SetFloat(pitchBend, "CONTROL PITCH");
        }

        public void SetControllerBend(int controllerBend)
        {
            mState.mControllerBend = controllerBend;
            mState.mDebugPanel.SetFloat(controllerBend, "CONTROL BEND");
        }

        public void MidiInNoteOff(int channel, int note)
        {
            PlayArea pa = mState.mMeledInterf.GetCurrentPlayArea();
            note = pa.GetConvertedNoteNumForCurrentKeyScale(note, false, mState.mbKeyRangePlayAreaMode);

            if (note == -1)
            {
                return;
            }

            //Override channel to record on teh current visible are with its current instrument
            channel = pa.mChannel;
            NoteOff(channel, note);
            pa.RecordNoteOff(note);
        }

        public String mMidiKeyboardName = "None";

        public bool EnterSaveNameMode
        {
            get => mTextEntry.EnterSaveNameMode;
            set
            {
                mTextEntry.EnterSaveNameMode = value;
            }

        }
        public bool SaveNameEntered
        {
            get => mTextEntry.SaveNameEntered;
            set
            {
                mTextEntry.SaveNameEntered = value;
            }

        } 

        TextEntryBase mTextEntry = null;

        bool USE_TEXT_ENTRY_ONLY = true;

        public void StartKB()
        {
            mTextEntry.StartKB();

            //mSaveName = "";
            SaveNameEntered = false;
            EnterSaveNameMode = true;
            mSaveNameDisplay.ButtonText = mSaveName;
        }

        public void ExitKB()
        {
            mTextEntry.ExitKB();

            EnterSaveNameMode = false;
        }
        public string GetSaveName()
        {
            if(USE_TEXT_ENTRY_ONLY)
            {
                return mTextEntry.GetTextEntryText();
            }

            return mSaveName;
        }

        const int TE_X = 400;
        const int TE_Y = 300;

        public void SetTextEntry(TextEntryBase te)
        {
            mTextEntry = te;
            mTextEntry.BaseInit(TE_X, TE_Y);
            mTextEntry.AddTextEntryNameString("SAVE NAME");
           //TODO mTextEntry.SetClickCB(tuib.mClickCB);
        }

        //Init sysex here
        byte[] mSysexReverb1 = new byte[11];
        byte[] mXGModeOn = new byte[9];
        byte[] mXGReverb = new byte[9];
        byte[] mXGChorus = new byte[9];


        public void TestSysex()
        {
            SetBuffer(mXGReverb);
        }

        public void SetUpSysEx()
        {
            //https://web.archive.org/web/20170116163927/https://kode54.net/bassmididrv/BASSMIDI_Driver_MIDI_Implementation_Chart.htm#reverb_effect_parameters
            //F0 43 10 4C 02 01 00 01 00 F7	
            //TODO TEMP TRY REVERB!
            mSysexReverb1[0] = (byte)0xf0;
            mSysexReverb1[1] = (byte)0x43;
            mSysexReverb1[2] = (byte)0x10;
            mSysexReverb1[3] = (byte)0x4C;
            mSysexReverb1[4] = (byte)0x02;
            mSysexReverb1[5] = (byte)0x01;
            mSysexReverb1[6] = (byte)0x00;
            mSysexReverb1[7] = (byte)0x01;
            mSysexReverb1[8] = (byte)0x0C;
            mSysexReverb1[9] = (byte)0x0f;
            mSysexReverb1[10] = (byte)0xf7;

            //XG Mode On
            //	F0 43 10 4C 00 00 7E 00 F7
            mXGModeOn[0] = (byte)0xf0;
            mXGModeOn[1] = (byte)0x43;
            mXGModeOn[2] = (byte)0x10;
            mXGModeOn[3] = (byte)0x4C;
            mXGModeOn[4] = (byte)0x00;
            mXGModeOn[5] = (byte)0x00;
            mXGModeOn[6] = (byte)0x7e;
            mXGModeOn[7] = (byte)0x00;
            mXGModeOn[8] = (byte)0xF7;

            mXGReverb[0] = (byte)0xf0;
            mXGReverb[1] = (byte)0x43;
            mXGReverb[2] = (byte)0x10;
            mXGReverb[3] = (byte)0x4C;
            mXGReverb[4] = (byte)0x02;
            mXGReverb[5] = (byte)0x01;
            mXGReverb[6] = (byte)0x0c;
            mXGReverb[7] = (byte)0x70;
            mXGReverb[8] = (byte)0xF7;

            mXGChorus[0] = (byte)0xf0;
            mXGChorus[1] = (byte)0x43;
            mXGChorus[2] = (byte)0x10;
            mXGChorus[3] = (byte)0x4C;
            mXGChorus[4] = (byte)0x02;
            mXGChorus[5] = (byte)0x01;
            mXGChorus[6] = (byte)0x2c;
            mXGChorus[7] = (byte)0x70;
            mXGChorus[8] = (byte)0xF7;

            //https://github.com/naudio/NAudio/issues/715#issue-758076608
            //var buffer = new byte[]
            //                     {
            //                         0xF0, // MIDI excl start
            //                         0x47, // Manu ID
            //                         0x7F, // DevID
            //                         0x73, // Prod Model ID
            //                         0x60, // Msg Type ID (0x60=Init?)
            //                         0x00, // Num Data Bytes (most sign.)
            //                         0x04, // Num Data Bytes (least sign.)
            //                         0x42, // Device Mode (0x40=unset, 0x41=Ableton, 0x42=Ableton with full ctrl)
            //                         0x01, // PC Ver Major (?)
            //                         0x01, // PC Ver Minor (?)
            //                         0x01, // PC Bug Fix Lvl (?)
            //                         0xF7, // MIDI excl end
            //                     };
            //midiOut.SendBuffer(buffer);

            //https://coolsoft.altervista.org/en/forum/thread/776
            //(GS mode)
            //reverb: (F0, 41, 10, 42, 12, 40, 01, 33, xx, yy, F7)
            //chorus: (F0, 41, 10, 42, 12, 40, 01, 3A, xx, yy, F7 )

            //(XG mode)
            //reverb: (F0, 43, 10, 4C, 02, 01, 0C, xx, F7 )
            //chorus: (F0, 43, 10, 4C, 02, 01, 2C, xx, F7 )

            //SetBuffer(mSysexReverb1);
            //SetBuffer(mXGModeOn);
            //SetBuffer(mXGReverb);
            //SetBuffer(new byte[] { 0xF0, 0x43, 0x10, 0x4C, 0x02, 0x01, 0x20, 0x43, 0x08, 0xF7 });
        }

        public void BaseInit()
        {
            mSaveNameGED.mClickOn = null;
            mSaveNameGED.mValue = ButtonGrid.GridEntryData.TEXT_TYPE;
            mSaveNameGED.mOnString = mSaveName;

            mScreenPos.X = TE_X;
            mScreenPos.Y = TE_Y;
            
            mSaveNameDisplay = new Button(mScreenPos, "PlainWhite", mSaveNameGED);
            //mSaveNameDisplay.Wide = true;
            mSaveNameDisplay.mType = Button.Type.Wide;
            mSaveNameDisplay.ColOn = Color.Azure;
            mSaveNameDisplay.ColOff = Color.Azure;
            mSaveNameDisplay.ButtonTextColour = Color.Black;
            mSaveNameDisplay.ButtonText = mSaveName;

            InitSDR();
        }

        public void LoadContent(ContentManager contentMan, SpriteFont font, SpriteFont textEntryFont)
        {
            {
                mSaveNameDisplay.mTexture = Button.mStaticDefaultTexture;
                mSaveNameDisplay.mFont = font;
            }
            mTextEntry.LoadContent(Button.mStaticDefaultTexture, textEntryFont); 
        }

        struct stuKey
        {
            public Keys mCurrentWinKeyDown; //set to some non expected one
            public bool mKeyShift;
        };

        stuKey mLastKey;

        public const int NUM_NOTES = 128;
        public abstract void Start();
        public abstract void InitSDR();
        public abstract void Write(byte[] msg);
        public abstract void SendMidi(int m, int n, int v);
        public abstract void NoteOn(int channel, int note, int velocity);
        public abstract void NoteOff(int channel, int note);
        public abstract void SetInstrument(int instrument, int channel);
        //public abstract void ClearAllNotes(int channel);
        public abstract void ShowKeyboard();
        public abstract void HideKeyboard();
        public abstract void SetMidiController(int channel, int ccType, int ccParam);
        public abstract void SetBuffer(byte[] buffer);
        //public abstract void AddCharacter(char nextChar, bool bUpper);

        public void StopAllNotes(int exceptChannel = -1)
        {
            for (int chan = 1; chan <= MidiTrackSelect.NUM_MIDI_TRACKS; chan++)
            {
                if(chan != exceptChannel)
                {
                    ClearAllNotes(chan);
                }
            }
        }

        //Was in interface requiring override for some reason but had same functionality in all derived so can be moved here into base
        public void ClearAllNotes(int channel)
        {
            for (int note = 0; note < MidiBase.NUM_NOTES; note++)
            {
                NoteOff(channel, note);
            }
        }

        public string GetFileName()
        {
            return mSaveName;
        }

        public bool AddCharacter(char nextChar, bool bUpper)
        {
#if  ANDROID
            mTextEntry.AddCharacter(nextChar, bUpper);
# endif
            if (nextChar == '\n')
            {
                //do save stuff with name
                HideKeyboard();
                SaveNameEntered = true;
                return true;
            }

            if (nextChar == 127)
            {
                //Backspace delete
                if (mSaveName.Length > 0)
                {
                    mSaveName = mSaveName.Remove(mSaveName.Length - 1, 1);
#if WINDOWS || ANDROID
                    Console.WriteLine(" AddCharacter BACKSPACE {0} mSaveName {1}", nextChar, mSaveName);
#endif
                    mSaveNameDisplay.ButtonText = mSaveName;
                }
                return false;
            }

            bool bCharOk = false;

            if (nextChar >= '0' && nextChar <= '9')
            {
                bCharOk = true;
            }
            else if (nextChar >= 'A' && nextChar <= 'Z')
            {
                bCharOk = true;
            }
            else if (nextChar == ' ')
            {
                bCharOk = true;
            }

            if (!bCharOk)
            {
#if WINDOWS || ANDROID
                Console.WriteLine(" AddCharacter BAD CHAR! {0} mSaveName {1}", nextChar, mSaveName);
#endif
                return false;
            }

            string nextCharStr = nextChar.ToString();

            if (bUpper)
            {
                nextCharStr = nextCharStr.ToUpper();
            }
            else
            {
                nextCharStr = nextCharStr.ToLower();
            }

            mSaveName += nextCharStr;
            mSaveNameDisplay.ButtonText = mSaveName;

#if WINDOWS || ANDROID
            Console.WriteLine(" AddCharacter OnKeyPress {0} mSaveName {1}", nextChar, mSaveName);
#endif
            return false;
        }

        public bool UpdateKeyboard(MelodyEditorInterface melEdInterf)
        {
            if(!EnterSaveNameMode)
            {
                return false;
            }

            if(USE_TEXT_ENTRY_ONLY)
            {
                return mTextEntry.UpdateInput(melEdInterf.mState);
            }

#if ANDROID
#else
            // Poll for current keyboard state
            KeyboardState state = Keyboard.GetState();

            // If they hit esc, exit
            if (state.IsKeyDown(Keys.Escape))
                return false;

            Keys[] keys = state.GetPressedKeys();

            bool bShift = false;
            foreach (Keys key in keys)
            {
                if (key == Keys.LeftShift || key == Keys.RightShift)
                {
                    bShift = true;
                }
                else
                {
                    mLastKey.mCurrentWinKeyDown = key;
                }
            }

            int lengthActualKeys = keys.Length;

            if(bShift)
            {
                lengthActualKeys -=1;
            }

            if (lengthActualKeys > 0)
            {
                mLastKey.mKeyShift = bShift;
            }
            else if(mLastKey.mCurrentWinKeyDown != Keys.Home)
            {
                char charKey = (char)mLastKey.mCurrentWinKeyDown;
                if (state.IsKeyUp(mLastKey.mCurrentWinKeyDown))
                {
                    if (mLastKey.mCurrentWinKeyDown == Keys.Back)
                    {
                        charKey = (char)127;
                    }
                    if (mLastKey.mCurrentWinKeyDown == Keys.Enter)
                    {
                        charKey = '\n';
                    }
                    AddCharacter(charKey, mLastKey.mKeyShift);
                    
                    mLastKey.mCurrentWinKeyDown = Keys.Home;
                }
            }

            //foreach (Keys key in keys)
            //{
            //    char charKey = (char)key;
            //   if (state.IsKeyUp(key))
            //    {
            //        if (key == Keys.Back)
            //        {
            //            charKey = (char)127;
            //        }
            //        AddCharacter(charKey, false);
            //    }
            //}
#endif

            return true;
        }

        public void Draw(SpriteBatch sb)
        {
            if(EnterSaveNameMode)
            {
                if(!USE_TEXT_ENTRY_ONLY)
                {
                    mSaveNameDisplay.Draw(sb);
                }
                mTextEntry.Draw(sb);
            }
        }
    }
}
