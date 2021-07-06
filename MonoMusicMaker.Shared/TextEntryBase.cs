using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;

namespace MonoMusicMaker
{
    public abstract class TextEntryBase
    {
        public const int CURSOR_WIDTH = 20;
        public const int CURSOR_HEIGHT = 40;

        public const int CURSOR_FLASH_TIME = 800;
        public const int CURSOR_FLASH_HALF_TIME = 400;

        public const int MIDI_PARAM_RANGE = 127;
        public const int MID_VELOCITY = (int)(MIDI_PARAM_RANGE * 0.5f);
        public const int LOW_VELOCITY = (int)(MIDI_PARAM_RANGE * 0.25f);
        public const int HIGH_VELOCITY = (int)(MIDI_PARAM_RANGE * 0.75f);

        protected string mTextEntryText = "";
        int mCharCount = 0;
        int mCursorPosition = 0;
        Vector2 mScreenPos = Vector2.Zero;
        Button mSaveNameDisplay;
        Button mSaveNameClearButton;
        Button mSaveNamePasteButton;

        ButtonGrid.GridEntryData mSaveNameGED;
        public Rectangle mCursorRect;
        Rectangle mTextRect;
        int mCharWidth; //set up this early on for mouse select calcs

        bool mbTextChanged = false;
        bool mbCursorOn = false;
        bool mbDebugRectOn = false;
        public bool mbClickedInside = false;

        public bool EnterSaveNameMode { get; set; } = false;
        public bool SaveNameEntered { get; set; } = false;

        public MainMusicGame mParent;


        List<char> mNonAlphNumeral = new List<char> { '=','-','[',']', '#', '\'', ';','/','.',',','\\'};

        public void AddTextEntryNameString(string nameString)
        {
            mSaveNameDisplay.BackgroundIdentity = nameString;
        }

        public void StartKB()
        {
            //mSaveName = "";
            SaveNameEntered = false;
            EnterSaveNameMode = true;
            mSaveNameDisplay.ButtonText = mTextEntryText;
        }

        public void ExitKB()
        {
            EnterSaveNameMode = false;
        }
        public string GetTextEntryText()
        {
            return mTextEntryText;
        }

        public void BaseInit(int posX, int posY)
        {
            mSaveNameGED.mClickOn = null;
            mSaveNameGED.mValue = ButtonGrid.GridEntryData.TEXT_TYPE;
            mSaveNameGED.mOnString = mTextEntryText;

            mScreenPos.X = posX;
            mScreenPos.Y = posY;

            mSaveNameDisplay = new Button(mScreenPos, "PlainWhite", mSaveNameGED);
            //mSaveNameDisplay.Wide = true;
            mSaveNameDisplay.mType = Button.Type.Wide;
            mSaveNameDisplay.ColOn = Color.Azure;
            mSaveNameDisplay.ColOff = Color.Azure;
            mSaveNameDisplay.ButtonTextColour = Color.Black;
            mSaveNameDisplay.ButtonText = mTextEntryText;

            Vector2 pasteBuutonPos = new Vector2(mScreenPos.X - Button.mTickBoxSize, mScreenPos.Y);
            mSaveNamePasteButton = new Button(pasteBuutonPos, "PlainWhite");
            mSaveNamePasteButton.mType = Button.Type.Tick;
            mSaveNamePasteButton.ColOn = Color.White;
            mSaveNamePasteButton.ColOff = Color.Yellow;

            Vector2 clearBuutonPos = new Vector2(mScreenPos.X + mSaveNameDisplay.Rectangle.Width, mScreenPos.Y);
            mSaveNameClearButton = new Button(clearBuutonPos, "PlainWhite");
            mSaveNameClearButton.mType = Button.Type.Tick;
            mSaveNameClearButton.ColOn = Color.White;
            mSaveNameClearButton.ColOff = Color.DarkOrchid;

            int cursorOffsetY = mSaveNameDisplay.Rectangle.Height / 2 - CURSOR_HEIGHT/2;
            mCursorRect = new Rectangle(posX, posY+ cursorOffsetY, CURSOR_WIDTH, CURSOR_HEIGHT);

            mTextRect = new Rectangle(posX, posY, CURSOR_WIDTH, mSaveNameDisplay.Rectangle.Height);

        }

        void Reset()
        {
            mSaveNamePasteButton.mbOn = false;
            mSaveNameClearButton.mbOn = false;
            mSaveNameDisplay.ButtonText = "";
            mTextEntryText = "";
            mCursorPosition = 0;
            UpdateCursor(mTextEntryText);
        }

        public void LoadContent(Texture2D texture, SpriteFont font)
        {
            {
                mSaveNameDisplay.mTexture = texture;
                mSaveNameDisplay.mFont = font;
                mSaveNameClearButton.mTexture = texture;
                mSaveNameClearButton.mFont = font;
                mSaveNamePasteButton.mTexture = texture;
                mSaveNamePasteButton.mFont = font;
            }

            mCharWidth = (int)font.MeasureString("X").X;

            //TODO Best place for this reset?
            Reset();
        }

        struct stuKey
        {
            public Keys mCurrentWinKeyDown; //set to some non expected one
            public bool mKeyShift;
        };


        public void SetClickCB(Button.ButtonClick cb)
        {
            mSaveNameDisplay.ClickCB = cb;
        }

        stuKey mLastKey;

        //////////////////////////////////////////////////////////////////////
        //ABSTRACT FUNCTIONS FOR BASE 
        //////////////////////////////////////////////////////////////////////
        public abstract void ShowKeyboard();
        public abstract void HideKeyboard();
 

        public string GetFileName()
        {
            return mTextEntryText;
        }

        public bool AddCharacter(char nextChar, bool bUpper)
        {
            if (nextChar == '\n')
            {
                //do save stuff with name
                HideKeyboard();
                SaveNameEntered = true;
                return true;
            }

            if (nextChar == 127)
            {
                RemoveChar();
                mSaveNameDisplay.ButtonText = mTextEntryText;
                return false;
            }

            bool bLeftClick = false;
            bool bRightClick = false;

            bool bCharOk = true;
            //Check if a left right cursor moove happened
            switch (nextChar)
            {
                case (char)37:
                    bLeftClick = true;
                    break;
                case (char)39:
                    bRightClick = true;
                    break;
                default:
                    bCharOk = false;
                    break;
            }


            if (nextChar >= '0' && nextChar <= '9')
            {
                bCharOk = true;

                if (bUpper)
                {
                    switch(nextChar)
                    {
                        case '0':
                            nextChar = ')';
                            break;
                        case '1':
                            nextChar = '!';
                            break;
                        case '2':
                            nextChar = '"';
                            break;
                        case '3':
                            nextChar = '£';
                            break;
                        case '4':
                            nextChar = '$';
                            break;
                        case '5':
                            nextChar = '%';
                            break;
                        case '6':
                            nextChar = '^';
                            break;
                        case '7':
                            nextChar = '&';
                            break;
                        case '8':
                            nextChar = '*';
                            break;
                        case '9':
                            nextChar = '(';
                            break;
                    }
                }
            }
            else if (nextChar >= 'A' && nextChar <= 'Z')
            {
                bCharOk = true;
            }
            else if (nextChar == ' ')
            {
                bCharOk = true;
            }
            else if(!bCharOk)
            {
                bCharOk = true;
                switch (nextChar)
                {
                    case (char)222:
                        if (bUpper)
                        {
                            nextChar = '~';
                        }
                        else
                        {
                            nextChar = '#';
                        }
                        break;
                    case (char)187:
                        if (bUpper)
                        {
                            nextChar = '+';
                        }
                        else
                        {
                            nextChar = '=';
                        }
                        break;
                    case (char)189:
                        if (bUpper)
                        {
                            nextChar = '_';
                        }
                        else
                        {
                            nextChar = '-';
                        }
                        break;
                    case (char)221:
                        if (bUpper)
                        {
                            nextChar = '}';
                        }
                        else
                        {
                            nextChar = ']';
                        }
                        break;
                    case (char)219:
                        if (bUpper)
                        {
                            nextChar = '{';
                        }
                        else
                        {
                            nextChar = '[';
                        }
                        break;
                    case (char)192:
                        if (bUpper)
                        {
                            nextChar = '@';
                        }
                        else
                        {
                            nextChar = '\'';
                        }
                        break;
                    case (char)186:
                        if (bUpper)
                        {
                            nextChar = ':';
                        }
                        else
                        {
                            nextChar = ';';
                        }
                        break;
                    case (char)191:
                        if (bUpper)
                        {
                            nextChar = '?';
                        }
                        else
                        {
                            nextChar = '/';
                        }
                        break;
                    case (char)190:
                        if (bUpper)
                        {
                            nextChar = '>';
                        }
                        else
                        {
                            nextChar = '.';
                        }
                        break;
                    case (char)188:
                        if (bUpper)
                        {
                            nextChar = '<';
                        }
                        else
                        {
                            nextChar = ',';
                        }
                        break;
                    case (char)220:
                        if (bUpper)
                        {
                            nextChar = '|';
                        }
                        else
                        {
                            nextChar = '\\';
                        }
                        break;
                    default:
                        bCharOk = false;
                        break;
                }
            }

#if ANDROID
            if (!bCharOk)
            {
                bCharOk = AndroidCharOK(nextChar, true);
            }
#endif

                if (!bCharOk)
            {
#if WINDOWS || ANDROID
                Console.WriteLine(" AddCharacter BAD CHAR! {0} mSaveName {1}", nextChar, mTextEntryText);
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

            AddChar(nextCharStr, bLeftClick, bRightClick);
            mSaveNameDisplay.ButtonText = mTextEntryText;

#if WINDOWS || ANDROID
            Console.WriteLine(" AddCharacter OnKeyPress {0} mSaveName {1}", nextChar, mTextEntryText);
#endif
            return false;
        }

        bool AndroidCharOK(char nextChar, bool bUseForSaveName)
        {
            bool bCharOk = true;
            switch (nextChar)
            {
                case '#':
                case '~':

                case '+':
                case '=':

                case '_':
                case '-':

                case '}':
                case ']':

                case '{':
                case '[':

                case '@':
                case '\'':

                case ';':

                case '?':
                case '/':

                case '>':
                case '.':

                case '<':
                case ',':

                case '|':
                case '\\':

                case ':':
                    {
                        if (bUseForSaveName)
                        {
                            char[] invalidPathChars = Path.GetInvalidPathChars();

                            foreach(char c in invalidPathChars)
                            {
                                if(c==nextChar)
                                {
                                    bCharOk = false;
                                }
                            }
                        }
                    }
                    break;

                default:
                    bCharOk = false;
                    break;
            }

            return bCharOk;
        }

        void RemoveChar()
        {
            if(mCursorPosition==0)
            {
                return;
            }

            string firstPart = mTextEntryText;
            string secondPart = "";
            if (mCursorPosition < mTextEntryText.Length)
            {
                int diff = mTextEntryText.Length - mCursorPosition;
                firstPart = mTextEntryText.Substring(0, mCursorPosition);
 
                firstPart = firstPart.Remove(firstPart.Length - 1, 1);
                secondPart = mTextEntryText.Substring(mCursorPosition);
                mTextEntryText = firstPart + secondPart;
            }
            else
            {
                mTextEntryText = mTextEntryText.Remove(mTextEntryText.Length - 1, 1);
                mCharCount = mTextEntryText.Length;
                firstPart = mTextEntryText;
            }

            UpdateCursor(firstPart);

        }

        void AddChar(string addChar, bool leftCursor, bool rightCursor)
        {
            string firstPart = mTextEntryText;
            string secondPart = "";

            if(leftCursor)
            {
                if (mCursorPosition > 0)
                {
                    mCursorPosition--;
                }
            }
            if (rightCursor)
            {
                if (mCursorPosition < mTextEntryText.Length)
                {
                    mCursorPosition++;
                }
            }

            bool bAddChar = !leftCursor && !rightCursor;

            if (mCursorPosition < mTextEntryText.Length)
            {
                int diff = mTextEntryText.Length - mCursorPosition;
                firstPart = mTextEntryText.Substring(0, mCursorPosition);
                if (bAddChar)
                {
                    //addchar on first part
                    firstPart += addChar;
                    secondPart = mTextEntryText.Substring(mCursorPosition);
                    mTextEntryText = firstPart + secondPart;
                }
            }
            else
            {
                if (bAddChar)
                {
                    mTextEntryText += addChar;
                }
                mCharCount = mTextEntryText.Length;
                firstPart = mTextEntryText;
            }

            UpdateCursor(firstPart);
         }

        void UpdateCursor(string firstPart)
        {
            mCursorPosition = firstPart.Length;
            int startText = (int)mSaveNameDisplay.GetXposEndStringCentred(mTextEntryText);
            mCursorRect.X = startText + (int)mSaveNameDisplay.mFont.MeasureString(firstPart).X;
            mTextRect.X = startText;
            mTextRect.Width = (int)mSaveNameDisplay.mFont.MeasureString(mTextEntryText).X;

            mbTextChanged = true; //to allow calling call back
        }

        public bool UpdateInput(MelodyEditorInterface.MEIState mainstate)
        {
            mbClickedInside = false;

            if(mbTextChanged)
            {
                if(mSaveNameDisplay.ClickCB != null)
                {
                    mSaveNameDisplay.ClickCB(mainstate);
                }
                mbTextChanged = false;
            }

            if ( mSaveNameClearButton.Update(mainstate))
            {
                Reset();
            }

            if (mSaveNamePasteButton.Update(mainstate))
            {
                if(mParent.mGetClipBoardHelper!=null)
                {
                    mSaveNamePasteButton.mbOn = false;
                    mTextEntryText = mParent.mGetClipBoardHelper.GetClipboard();
                    mSaveNameDisplay.ButtonText = mTextEntryText;
                    UpdateCursor(mTextEntryText);
                    return true;
                }

                return false;
            }

            if (mSaveNameDisplay.Update(mainstate))
            {
                mbClickedInside = true;
            }

            if (mbClickedInside)
            {
                if (mainstate.input.mRectangle.Intersects(mTextRect))
                {
                    //Set the cursor appropriately to mouse click
                    int posAlongX = mainstate.input.mRectangle.X - mTextRect.X;
                    mCursorPosition = posAlongX / mCharWidth;
                    string firstPart = mTextEntryText.Substring(0, mCursorPosition);
                    UpdateCursor(firstPart);
                    return true;
                }
            }

            if (!EnterSaveNameMode)
            {
                return false;
            }

            int timeMS = (int)mainstate.gameTime.TotalGameTime.TotalMilliseconds % CURSOR_FLASH_TIME;

            if(timeMS > CURSOR_FLASH_HALF_TIME)
            {
                mbCursorOn = true;
            }
            else
            {
                mbCursorOn = false;
            }

            //mCursorRect.X = mSaveNameDisplay.mRightTextPos;

            //mainstate.mSetDownTime
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

            if (bShift)
            {
                lengthActualKeys -= 1;
            }

            if (lengthActualKeys > 0)
            {
                mLastKey.mKeyShift = bShift;
            }
            else if (mLastKey.mCurrentWinKeyDown != Keys.Home)
            {
                char charKey = (char)mLastKey.mCurrentWinKeyDown;

                string strTest = mLastKey.ToString();
                Console.WriteLine(" AddCharacter strTest {0} ", strTest);


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
#endif

            return true;
        }

        public void Draw(SpriteBatch sb)
        {
            mSaveNameDisplay.Draw(sb);
            mSaveNameClearButton.Draw(sb);
            mSaveNamePasteButton.Draw(sb);

            if (EnterSaveNameMode)
            {
                if (mbCursorOn)
                {
                    sb.Begin();

                    sb.FillRectangle(mCursorRect, Color.DarkBlue * 0.3f);

                    if(mbDebugRectOn)
                    {
                        sb.DrawRectangle(mTextRect, Color.Black * 1.0f);
                    }

                    sb.End();
                }
            }
        }
    }
}
