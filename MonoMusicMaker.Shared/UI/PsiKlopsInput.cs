using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Content;
using Android.Hardware;
#endif
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

//using Android.Content;
//using Android.Hardware;
//using Xamarin.Essentials;

//This takes care of both touch and mouse and returns stuff like the mouse state 
namespace MonoMusicMaker
{
    public class PsiKlopsInput
    {
        enum eInputType
        {
            PKI_None,
            PKI_Touch,
            PKI_Mouse,
            PKI_Pad
        };

        const bool PC_SIMULATE_SECOND_TOUCH = false; // true; //use on builds where right button on mouse is not needed and can be used for this instead
        const float MIN_PINCH_SCALE = 0.2f;
        const float MAX_PINCH_SCALE = 6f;
        public const int DOUBLE_CLICK_TIME_MS = 270;

        eInputType m_InputType = eInputType.PKI_None;

#if ANDROID
        //Accelerometer mtestAccel;
        static SensorManager sensorManager;
#endif

        KeyboardState mLastKeyboardState;
        MouseState mLastState;
        TouchCollection mLastTouchCollection;
        GamePadState mLastGamePadState;


        public PsiKlopsInput(MouseState ms)
        {
            System.Diagnostics.Debug.Assert(m_InputType == eInputType.PKI_None);
            mMouseState = ms;
            m_InputType = eInputType.PKI_Mouse;
            UpdateFromMouseState(ms,null);
            mRectangle = new Rectangle(0, 0, 1, 1);
            mSecondRectangle = new Rectangle(0, 0, 1, 1);
        }

        public PsiKlopsInput(TouchCollection tc)
        {
            System.Diagnostics.Debug.Assert(m_InputType == eInputType.PKI_None);
            mTouchCollection = tc;
            UpdateFromTouchCollection(tc, null);
            m_InputType = eInputType.PKI_Touch;
            mRectangle = new Rectangle(0, 0, 1, 1);
            mSecondRectangle = new Rectangle(0, 0, 1, 1);
        }

        MouseState mMouseState;
        TouchCollection mTouchCollection;
        ButtonState m_LeftButton;
        ButtonState m_LastLeftButtonSeen;

        ButtonState m_SecondTouch;
        ButtonState m_LastSecondTouch;
        public Rectangle mRectangle;
        public Rectangle mSecondRectangle;


        ButtonState m_RightButton;
        ButtonState m_LastRightButtonSeen;
        bool mbLastRightButtonReleased = false;

        bool mbLastLeftButtonReleased = false;
        bool mbLeftDown = false;
        bool mbRightDown = false;
        public bool LeftUp { get; set; } = false;
        public bool RightUp { get; set; } = false;
        public bool MiddleUp { get; set; } = false;
        bool mbHeldPressedAndMoving = false;

        bool mbSecondTouchDown = false;
        bool mbSecondTouchUp = false;
        bool mbSecondHeld = false;

        //PINCH
        float mfStartPinchDistance = 1f;
        float mfTouchScale = 1f; // Value will track the current pinch scale 1 when first touched then scale based on distance between first to second
        float mfLastTouchScale = 1f;

        float mfLRVel = 0f;
        float mfUDVel = 0f;
        float mfMovedDistance = 0f;
        const float MOVE_UPDOWN_DETECT = 4f; //if start and end pos more than this in Y it is denoted as moved - usfeful to ignore button press
        const float MOVE_LEFTRIGHT_DETECT = 4f; //if start and end pos more than this in X it is denoted as moved - usfeful to ignore button press
        bool mbLRMOved = false;
        bool mbUDMOved = false;
        public int mDeltaY = 0;
        public int mDeltaX = 0;

        const int TAP_Q_CAPACITY = 2;
        Queue<Vector2> mRepeatTaps = new Queue<Vector2>(TAP_Q_CAPACITY); //keep two for now
        int mLastTapTimeMs = 0; //Time of the last down tap

        void InitReleaseData()
        {
            mbLRMOved = false;
            mbUDMOved = false;
            mfMovedDistance = 0f;
            mDeltaY = 0;
            mDeltaX = 0;
            mfLRVel = 0f;
            mfUDVel = 0f;
            mi_LastY = mi_Y;
            mi_LastX = mi_X;

            InitSecondReleaseData();
        }

        void InitSecondReleaseData()
        {
            mDeltaSecondTouchY = 0;
            mDeltaSecondTouchX = 0;
            mi_LastSecondTouchY = mi_SecondTouchY;
            mi_LastSecondTouchX = mi_SecondTouchX;
        }

        //DOUBLE TAP
        bool UpdateTapQueue(int x, int y, GameTime gameTime, bool bTapped)
        {
            int currentTime = (int)gameTime.TotalGameTime.TotalMilliseconds;

            if(currentTime - mLastTapTimeMs > DOUBLE_CLICK_TIME_MS && mRepeatTaps.Count() > 0)
            {
                mRepeatTaps.Clear();
            }

            bool bDoubleTapped = false;  

            if(bTapped)
            {
                bDoubleTapped = AddTap(x, y, gameTime);
                mLastTapTimeMs = currentTime;
            }


            return bDoubleTapped;
        }

        bool AddTap(int x, int y, GameTime gameTime)
        {
            if (mRepeatTaps.Count() == TAP_Q_CAPACITY)
            {
                mRepeatTaps.Dequeue();
            }

            Vector2 vTap = new Vector2(x, y);
            mRepeatTaps.Enqueue(vTap);
            return mRepeatTaps.Count() == TAP_Q_CAPACITY;
        }

        public bool DoubleTapInRect(Rectangle rect, bool bConsumeInfo = false)
        {
            if(mRepeatTaps.Count()>=2)
            {
                foreach(Vector2 v in mRepeatTaps)
                {
                    if(!rect.Contains(v))
                    {
                        return false;
                    }
                }
                if(bConsumeInfo)
                {
                    Held = false;
                    mRepeatTaps.Clear(); //we have the info now so clear it so not seen again in following frames
                }
                return true;
            }

            return false;
        }

        void UpdateReleaseData()
        {
            Vector2 vEnd = new Vector2(mi_X, mi_Y);
            Vector2 vStart = new Vector2(mi_InitX, mi_InitY); 
            mfMovedDistance = GetDistanceBetweenVecs(vStart, vEnd);

            mbLRMOved = false;
            mbUDMOved = false;
            if (Math.Abs(mi_InitX - mi_X) > MOVE_LEFTRIGHT_DETECT)
            {
                mbLRMOved = true;
            }
            if (Math.Abs(mi_InitY - mi_Y) > MOVE_UPDOWN_DETECT)
            {
                mbUDMOved = true;
            }
        }

        ButtonState m_LastMiddleButtonSeen;
        ButtonState m_MiddleButton;
        private int mi_ScrollWheel;
        private int mi_X;
        private int mi_Y;
        private int mi_Z;
        private int mi_PC_X;
        private int mi_PC_Y;

        int mi_InitX; //position when first pressed
        int mi_InitY;

        private int mi_LastX;
        private int mi_LastY;

        private int mi_LastSecondTouchX;
        private int mi_LastSecondTouchY;
        public int mDeltaSecondTouchY = 0;
        public int mDeltaSecondTouchX = 0;

        private int mi_SecondTouchX;
        private int mi_SecondTouchY;

        public bool RightReleased()
        {
            return mbLastRightButtonReleased;
        }

        public bool LeftReleased()
        {
            return mbLastLeftButtonReleased;
        }

        public bool LeftDown()
        {
            return mbLeftDown;
        }

        public bool HeldPressedAndMoving()
        {
            return mbHeldPressedAndMoving;
        }

        public bool Held { get; set; } = false;
        public bool SecondHeld { get; set; } = false;

        public bool SecondTouchDown()
        {
            return mbSecondTouchDown;
        }
        public bool SecondTouchUp()
        {
            return mbSecondTouchUp;
        }

        public bool SecondTouchHeld()
        {
            return mbSecondHeld;
        }


        public float GetPinchScale()
        {
            return mfTouchScale;
        }
        public float GetLastPinchScale()
        {
            return mfLastTouchScale;
        }

        public void UpdateFromMouseState(MouseState ms, GameTime gameTime)
        {
            System.Diagnostics.Debug.Assert(m_InputType == eInputType.PKI_Mouse);

            m_LeftButton = ms.LeftButton;
            m_RightButton = ms.RightButton;

            if(PC_SIMULATE_SECOND_TOUCH)
            {
                if(m_LeftButton==ButtonState.Released && m_RightButton == ButtonState.Pressed)
                {
                    m_LeftButton = m_RightButton;
                }
            }

            m_MiddleButton = ms.MiddleButton;
            m_SecondTouch = ButtonState.Released;

            mi_ScrollWheel = ms.ScrollWheelValue;
            mi_Z = 0; //? ms.Z;
            mRectangle.X = mi_X;
            mRectangle.Y = mi_Y;

            mSecondRectangle.X = mi_SecondTouchX;
            mSecondRectangle.Y = mi_SecondTouchY;

            //Simulate second touch from right button
            if (m_RightButton == ButtonState.Pressed)
            {
                mi_SecondTouchX = ms.X;
                mi_SecondTouchY = ms.Y;

                //mi_Y = mi_SecondTouchY - 30; //PUT START POSITION OFFSET FROM SECOND POS -  HACK TO TEST PINCH SCALING ON PC
                m_SecondTouch = ButtonState.Pressed;
            }
            else
            {
                mi_X = ms.X;
                mi_Y = ms.Y;
            }

            UpdateCommonAndDebounce( gameTime);
        }

        public void UpdateFromTouchCollection(TouchCollection tc, GameTime gameTime)
        {
            System.Diagnostics.Debug.Assert(m_InputType == eInputType.PKI_Touch);

            m_LeftButton = ButtonState.Released;
            m_RightButton = ButtonState.Released; //TODO Why not use Right touch as second touch?
            m_MiddleButton = ButtonState.Released;
            m_SecondTouch = ButtonState.Released;

            if (tc.Count > 0)
            {
                //foreach (TouchLocation tl in tc)
                //{
                //    //
                //    mi_X = (int)tl.Position.X;
                //    mi_Y = (int)tl.Position.Y;
                //    mi_Z = 0; //? ms.Z;

                //    m_LeftButton = ButtonState.Pressed;

                //    mi_ScrollWheel = 0
                //}


                TouchLocation tl = tc[0];
                {
                    //
                    mi_X = (int)tl.Position.X;
                    mi_Y = (int)tl.Position.Y;
                    mi_Z = 0; //? ms.Z;

                    m_LeftButton = ButtonState.Pressed;

                    mi_ScrollWheel = 0;
                }

                if (tc.Count > 1)
                {
                    mi_SecondTouchX = (int)tc[1].Position.X;
                    mi_SecondTouchY = (int)tc[1].Position.Y;
                    m_SecondTouch = ButtonState.Pressed;
                    m_RightButton = ButtonState.Pressed;
                }
            }
            mRectangle.X = mi_X;
            mRectangle.Y = mi_Y;
            mSecondRectangle.X = mi_SecondTouchX;
            mSecondRectangle.Y = mi_SecondTouchY;

            UpdateCommonAndDebounce( gameTime);
        }

        void InitPinchDistance()
        {
            Vector2 vEnd = new Vector2(mi_SecondTouchX, mi_SecondTouchY);
            Vector2 vStart = new Vector2(mi_X, mi_Y);

#if !ANDROID
            //PC DEBUGGING NOT USED IN ANDROID
            mi_PC_X = MelodyEditorInterface.TRACK_START_Y;
            mi_PC_Y = MelodyEditorInterface.TRACK_START_X;
            vStart = new Vector2(mi_PC_X, mi_PC_Y);
#endif
            mfStartPinchDistance = GetDistanceBetweenVecs(vStart, vEnd);    
            
            if(mfStartPinchDistance == 0f)
            {
                mfStartPinchDistance = 1f;
            }
        }

        float GetDistanceBetweenVecs(Vector2 vStart, Vector2 vEnd)
        {
            Vector2 vDiff = vStart - vEnd;
            float fDiff = vDiff.Length();
            return fDiff;
        }

        float GetScaleOfPinch()
        {
            float fScalePinch = 1f;

            Vector2 vEnd = new Vector2(mi_SecondTouchX, mi_SecondTouchY);
#if ANDROID
            Vector2 vStart = new Vector2(mi_X, mi_Y);
#else
            Vector2 vStart = new Vector2(mi_PC_X, mi_PC_Y);
#endif
            float fDiff = GetDistanceBetweenVecs( vStart,  vEnd);

            fScalePinch = fDiff / mfStartPinchDistance;

            if(fScalePinch < MIN_PINCH_SCALE)
            {
                fScalePinch = MIN_PINCH_SCALE;
            }
            else if (fScalePinch > MAX_PINCH_SCALE)
            {
                fScalePinch = MAX_PINCH_SCALE;
            }

            System.Diagnostics.Debug.WriteLine(string.Format(" GetScaleOfPinch {0}", fScalePinch));

            return fScalePinch;
        }

        void UpdateCommonAndDebounce(GameTime gameTime)
        {
            //Debounce first
            /////////////////////////////////
            ///  RELEASED
            if (m_LastLeftButtonSeen == ButtonState.Pressed && m_LeftButton != ButtonState.Pressed)
            {
                mbLastLeftButtonReleased = true;
            }
            else
            {
                mbLastLeftButtonReleased = false;
            }

            if (m_LastRightButtonSeen == ButtonState.Pressed && m_RightButton != ButtonState.Pressed)
            {
                mbLastRightButtonReleased = true;
            }
            else
            {
                mbLastLeftButtonReleased = false;
            }

            /////////////////////////////////
            ///  DOWN
            if (m_LastRightButtonSeen == ButtonState.Released && m_RightButton != ButtonState.Released)
            {
                mbRightDown = true;
                InitSecondReleaseData();
            }
            else
            {
                mbRightDown = false;
            }

            if (m_LastLeftButtonSeen == ButtonState.Released && m_LeftButton != ButtonState.Released)
            {
                mi_InitX = mi_X;
                mi_InitY = mi_Y;
                InitReleaseData();

                mbLeftDown = true;
            }
            else
            {
                mbLeftDown = false;
            }

            /////////////////////////////////
            /// RIGHT UP
            if (m_LastRightButtonSeen != ButtonState.Released && m_RightButton == ButtonState.Released)
            {
                RightUp = true;
            }
            else
            {
                RightUp = false;
            }

            /////////////////////////////////
            /// MIDDLE UP
            if (m_LastMiddleButtonSeen != ButtonState.Released && m_MiddleButton == ButtonState.Released)
            {
                MiddleUp = true;
            }
            else
            {
                MiddleUp = false;
            }

            if (m_LastLeftButtonSeen != ButtonState.Released && m_LeftButton == ButtonState.Released)
            {
                //mi_InitX = mi_X;
                //mi_InitY = mi_Y;
                UpdateReleaseData();

                LeftUp = true;
            }
            else
            {
                LeftUp = false;
            }

            if (gameTime != null)
            {
                UpdateTapQueue(mi_X, mi_Y, gameTime, LeftUp);
            }

            //Debounce second
            //Debounce
            if (m_LastSecondTouch == ButtonState.Pressed && m_SecondTouch != ButtonState.Pressed)
            {
                mbSecondTouchUp = true;
            }
            else
            {
                mbSecondTouchUp = false;
            }

            if (m_LastSecondTouch == ButtonState.Released && m_SecondTouch != ButtonState.Released)
            {
                InitPinchDistance();
                mbSecondTouchDown = true;
            }
            else
            {
                mbSecondTouchDown = false;
            }

            mbHeldPressedAndMoving = false;
            Held = m_LeftButton == ButtonState.Pressed;
            SecondHeld = m_SecondTouch == ButtonState.Pressed;

            if (Held)
            {
                Vector2 vOldPos = new Vector2(mi_LastX, mi_LastY);
                Vector2 vNewPos = new Vector2(mi_X, mi_Y);
                Vector2 vDiff = vOldPos - vNewPos;
                mDeltaY = mi_Y - mi_LastY;
                mDeltaX = mi_X - mi_LastX;

                float xDiff = mDeltaX;
                float yDiff = mDeltaY;

                int timeDiffms = gameTime.ElapsedGameTime.Milliseconds;

                mfUDVel = yDiff / (float)timeDiffms;
                mfLRVel = xDiff / (float)timeDiffms;

                if (vDiff.Length() > 0.25f)
                {
                    mbHeldPressedAndMoving = true;
                }
            }

            if (SecondHeld)
            {
                mDeltaSecondTouchY = mi_SecondTouchY - mi_LastSecondTouchY;
                mDeltaSecondTouchX = mi_SecondTouchX - mi_LastSecondTouchX;
            }

            if (m_LastLeftButtonSeen == ButtonState.Pressed)
            {
                //System.Diagnostics.Debug.WriteLine(string.Format(" - m_LastLeftButtonSeen: {0}", m_LastLeftButtonSeen));
            }
            if (m_LeftButton == ButtonState.Pressed)
            {
                //System.Diagnostics.Debug.WriteLine(string.Format(" + m_LeftButton: {0}", m_LeftButton));
            }

            mi_LastX = mi_X;
            mi_LastY = mi_Y;
            mi_LastSecondTouchX = mi_SecondTouchX;
            mi_LastSecondTouchY = mi_SecondTouchY;

            m_LastSecondTouch = m_SecondTouch;
            m_LastLeftButtonSeen = m_LeftButton;
            m_LastRightButtonSeen = m_RightButton;

            m_LastMiddleButtonSeen = m_MiddleButton;

            //While two touches held
            if (m_SecondTouch == ButtonState.Pressed)
            {
                mbSecondHeld = true;
                mfTouchScale = GetScaleOfPinch();      
            }
            else if(mbSecondTouchUp)
            {
                mfLastTouchScale = mfTouchScale;
                mbSecondHeld = false;
                mfTouchScale = 1f;        
            }
        }

        public int ScrollWheelValue
        {
            get { return mi_ScrollWheel; }
            //set { mi_ScrollWheel = value; }
        }
        public int X
        {
            get { return mi_X; }
            //set { mi_X = value; }
        }
        public int Y
        {
            get { return mi_Y; }
            //set { mi_Y = value; }
        }
        public int Z
        {
            get { return mi_Z; }
            //set { mi_Y = value; }
        }


        /// <summary>
        /// SECOND TOUCH
        /// </summary>
        /// 
        public int X2
        {
            get { return mi_SecondTouchX; }
            //set { mi_X = value; }
        }
        public int Y2
        {
            get { return mi_SecondTouchY; }
            //set { mi_Y = value; }
        }
 

        public ButtonState LeftButton
        {
            get { return m_LeftButton; }
        }
        public ButtonState RightButton
        {
            get { return m_RightButton; }
        }
        public ButtonState MiddleButton
        {
            get { return m_MiddleButton; }
        }

    }
}