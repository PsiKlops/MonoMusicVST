using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonoMusicMaker
{
    class WordHelper
    {
        const int LEN_CUME_WORD = 10;

        public static string GetWords(string input, int lenCumeWord = LEN_CUME_WORD)
        {     
            string[] words = input.Split(' ');
            
            string multiLine2 = "";
            string cumulativeWord = "";
            foreach (string word in words)
            {
                if ((cumulativeWord.Length + word.Length + 1) > lenCumeWord)
                {
                    if (multiLine2 != "")
                    {
                        multiLine2 += "\n";
                    }
                    multiLine2 += cumulativeWord;
                    cumulativeWord = word;
                }
                else
                {
                    if (cumulativeWord != "")
                    {
                        cumulativeWord += " " + word;
                    }
                    else
                    {
                        cumulativeWord = word;
                    }
                }
            }

            if (cumulativeWord != "")
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

            //        if((cumulativeWord.Length + word.Length)> lenCumeWord)
            //        {

            //        }
            //        multiLine2 += "\n";
            //    }
            //    multiLine2 += word;
            //}

            return multiLine2;
        }
    }
}