using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonoMusicMaker
{
    public class StaticHelpers
    {
        public static bool mFoundInString = true;

        public static uint CRCString(string stringInput)
        {
            uint CRCResult = 0x0;
            string lowerStringInput = stringInput.ToLower();
                            
            foreach(char c in lowerStringInput) 
            {
                uint numChar = (uint)c;            
        
                CRCResult = CRCResult + numChar;
                
                CRCResult  = (CRCResult & 0xffffffff);
                
                CRCResult = CRCResult + (CRCResult<<10);
                CRCResult  = (CRCResult & 0xffffffff);
                CRCResult = CRCResult ^ (CRCResult>>6 );
                CRCResult  = (CRCResult & 0xffffffff);
            }
                   
            CRCResult = CRCResult + (CRCResult<<3);
            CRCResult = (CRCResult & 0xffffffff);
        
            //print 'Before ^ 0x%08x' % (CRCResult & 0xffffffff);
        
            CRCResult = CRCResult ^ (CRCResult>>11 );
            //print 'After ^ 0x%08x' % (CRCResult & 0xffffffff);
            CRCResult  = (CRCResult & 0xffffffff);
            CRCResult = CRCResult + (CRCResult<<15);
            CRCResult  = (CRCResult & 0xffffffff);

            if (CRCResult < 2)
            {
                CRCResult = CRCResult + 2;
            }

            return CRCResult;
        }
        public static string SubtractString(string sourceString, string removeString)
        {
            mFoundInString = true;
            int index = sourceString.IndexOf(removeString);
            if(index<0)
            {
                mFoundInString = false;
                System.Diagnostics.Debug.WriteLine(string.Format("SubtractString cant find {0} in {1}", removeString, sourceString));
                return sourceString;
            }
            int length = removeString.Length;
            String startOfString = sourceString.Substring(0, index);
            String endOfString = sourceString.Substring(index + length);
            String cleanPath = startOfString + endOfString;

            return cleanPath;
        }
    }
}
