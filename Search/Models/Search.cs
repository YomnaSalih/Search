using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using mshtml;
using System.Data.SqlClient;
using System.Web;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Porter2Stemmer;
using System.Xml;
using MySqlConnector;

namespace Search.Models
{
    public struct my_Words
    {
        public int frequency;
        public string position;
        
    };

    public class Indexing
    {
        private static string conn = @"port =3306 ; server = localhost; user id = root; password='yomna_123'; persistsecurityinfo = False; database = mydb";
        static MySqlConnection connection = new MySqlConnection(conn);
        static string[] arrToCheck = File.ReadAllLines(@"E:\\ir\\Indexing\\Indexing\\stop_words_english.txt");

        public static List<string> my_Link = new List<string>();

        public static void RemovingStopwords(List<string> word)
        {

            for (int i = 0; i < word.Count; i++)
            {
                for (int j = 0; j < word[i].Length; j++)
                {
                    if (word[i][j] == '"')
                    {
                        word[i] = word[i].Replace('"', ' ').Trim();
                    }
                    else if (word[i][j] == '^')
                    {
                        word[i] = word[i].Replace('^', ' ').Trim();
                    }
                    else if (word[i][j] == '@')
                    {
                        word[i] = word[i].Replace('@', ' ').Trim();
                    }
                }

                word[i] = word[i].Trim();
            }
            for (int i = 0; i < word.Count; i++)
            {
                word[i] = Regex.Replace(word[i], @"[\d-]", string.Empty);
                if (string.IsNullOrEmpty(word[i]))
                {

                    word.RemoveAt(i);
                }
            }


            for (int i = 0; i < word.Count; i++)
            {
                for (int j = 0; j < word[i].Length; j++)
                {
                    if (char.IsDigit(word[i][j]))
                    {
                        word.RemoveAt(i);
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }

            }


            for (int i = 0; i < word.Count; i++)
            {
                if (arrToCheck.Contains(word[i]) || word[i].Length < 3)
                {

                    word.RemoveAt(i);
                }

            }

            for (int i = 0; i < word.Count; i++)
            {
                word[i] = Regex.Replace(word[i], @"[\d-]", string.Empty);
                if (string.IsNullOrEmpty(word[i]))
                {

                    word.RemoveAt(i);
                }
            }

            Stemming(word);
        }


        public static void GetSplitedWords(List<String> splited_words_list)
        {

            for (int i = 0; i < splited_words_list.Count; i++)
            {

                splited_words_list[i] = splited_words_list[i].ToLower();

            }

            RemovingStopwords(splited_words_list);
        }

        public static void Stemming(List<string> word)
        {

            var stem = new PorterStemmer();
            for (int i = 0; i < word.Count; i++)
            {
                //  stemmer.StemWord(token[i]);
                if (!string.IsNullOrEmpty(stem.StemWord(word[i])))
                {
                    word[i] = (stem.StemWord(word[i]));
                }
            }


            // Token_info(tokens, new_positions, docid);
        }


        public class PorterStemmer
        {

            // The passed in word turned into a char array. 
            // Quicker to use to rebuilding strings each time a change is made.
            private char[] wordArray;

            // Current index to the end of the word in the character array. This will
            // change as the end of the string gets modified.
            private int endIndex;

            // Index of the (potential) end of the stem word in the char array.
            private int stemIndex;


            /// <summary>
            /// Stem the passed in word.
            /// </summary>
            /// <param name="word">Word to evaluate</param>
            /// <returns></returns>
            public string StemWord(string word)
            {

                // Do nothing for empty strings or short words.
                if (string.IsNullOrWhiteSpace(word) || word.Length <= 2) return word;

                wordArray = word.ToCharArray();

                stemIndex = 0;
                endIndex = word.Length - 1;
                Step1();
                Step2();
                Step3();
                Step4();
                Step5();
                Step6();

                var length = endIndex + 1;
                return new String(wordArray, 0, length);
            }


            // Step1() gets rid of plurals and -ed or -ing.
            /* Examples:
                   caresses  ->  caress
                   ponies    ->  poni
                   ties      ->  ti
                   caress    ->  caress
                   cats      ->  cat

                   feed      ->  feed
                   agreed    ->  agree
                   disabled  ->  disable

                   matting   ->  mat
                   mating    ->  mate
                   meeting   ->  meet
                   milling   ->  mill
                   messing   ->  mess

                   meetings  ->  meet  		*/
            private void Step1()
            {
                // If the word ends with s take that off
                if (wordArray[endIndex] == 's')
                {
                    if (EndsWith("sses"))
                    {
                        endIndex -= 2;
                    }
                    else if (EndsWith("ies"))
                    {
                        SetEnd("i");
                    }
                    else if (wordArray[endIndex - 1] != 's')
                    {
                        endIndex--;
                    }
                }
                if (EndsWith("eed"))
                {
                    if (MeasureConsontantSequence() > 0)
                        endIndex--;
                }
                else if ((EndsWith("ed") || EndsWith("ing")) && VowelInStem())
                {
                    endIndex = stemIndex;
                    if (EndsWith("at"))
                        SetEnd("ate");
                    else if (EndsWith("bl"))
                        SetEnd("ble");
                    else if (EndsWith("iz"))
                        SetEnd("ize");
                    else if (IsDoubleConsontant(endIndex))
                    {
                        endIndex--;
                        int ch = wordArray[endIndex];
                        if (ch == 'l' || ch == 's' || ch == 'z')
                            endIndex++;
                    }
                    else if (MeasureConsontantSequence() == 1 && IsCVC(endIndex)) SetEnd("e");
                }
            }

            // Step2() turns terminal y to i when there is another vowel in the stem.
            private void Step2()
            {
                if (EndsWith("y") && VowelInStem())
                    wordArray[endIndex] = 'i';
            }

            // Step3() maps double suffices to single ones. so -ization ( = -ize plus
            // -ation) maps to -ize etc. note that the string before the suffix must give m() > 0. 
            private void Step3()
            {
                if (endIndex == 0) return;

                /* For Bug 1 */
                switch (wordArray[endIndex - 1])
                {
                    case 'a':
                        if (EndsWith("ational")) { ReplaceEnd("ate"); break; }
                        if (EndsWith("tional")) { ReplaceEnd("tion"); }
                        break;
                    case 'c':
                        if (EndsWith("enci")) { ReplaceEnd("ence"); break; }
                        if (EndsWith("anci")) { ReplaceEnd("ance"); }
                        break;
                    case 'e':
                        if (EndsWith("izer")) { ReplaceEnd("ize"); }
                        break;
                    case 'l':
                        if (EndsWith("bli")) { ReplaceEnd("ble"); break; }
                        if (EndsWith("alli")) { ReplaceEnd("al"); break; }
                        if (EndsWith("entli")) { ReplaceEnd("ent"); break; }
                        if (EndsWith("eli")) { ReplaceEnd("e"); break; }
                        if (EndsWith("ousli")) { ReplaceEnd("ous"); }
                        break;
                    case 'o':
                        if (EndsWith("ization")) { ReplaceEnd("ize"); break; }
                        if (EndsWith("ation")) { ReplaceEnd("ate"); break; }
                        if (EndsWith("ator")) { ReplaceEnd("ate"); }
                        break;
                    case 's':
                        if (EndsWith("alism")) { ReplaceEnd("al"); break; }
                        if (EndsWith("iveness")) { ReplaceEnd("ive"); break; }
                        if (EndsWith("fulness")) { ReplaceEnd("ful"); break; }
                        if (EndsWith("ousness")) { ReplaceEnd("ous"); }
                        break;
                    case 't':
                        if (EndsWith("aliti")) { ReplaceEnd("al"); break; }
                        if (EndsWith("iviti")) { ReplaceEnd("ive"); break; }
                        if (EndsWith("biliti")) { ReplaceEnd("ble"); }
                        break;
                    case 'g':
                        if (EndsWith("logi"))
                        {
                            ReplaceEnd("log");
                        }
                        break;
                }
            }

            /* step4() deals with -ic-, -full, -ness etc. similar strategy to step3. */
            private void Step4()
            {
                switch (wordArray[endIndex])
                {
                    case 'e':
                        if (EndsWith("icate")) { ReplaceEnd("ic"); break; }
                        if (EndsWith("ative")) { ReplaceEnd(""); break; }
                        if (EndsWith("alize")) { ReplaceEnd("al"); }
                        break;
                    case 'i':
                        if (EndsWith("iciti")) { ReplaceEnd("ic"); }
                        break;
                    case 'l':
                        if (EndsWith("ical")) { ReplaceEnd("ic"); break; }
                        if (EndsWith("ful")) { ReplaceEnd(""); }
                        break;
                    case 's':
                        if (EndsWith("ness")) { ReplaceEnd(""); }
                        break;
                }
            }

            /* step5() takes off -ant, -ence etc., in context <c>vcvc<v>. */
            private void Step5()
            {
                if (endIndex == 0) return;

                switch (wordArray[endIndex - 1])
                {
                    case 'a':
                        if (EndsWith("al")) break; return;
                    case 'c':
                        if (EndsWith("ance")) break;
                        if (EndsWith("ence")) break; return;
                    case 'e':
                        if (EndsWith("er")) break; return;
                    case 'i':
                        if (EndsWith("ic")) break; return;
                    case 'l':
                        if (EndsWith("able")) break;
                        if (EndsWith("ible")) break; return;
                    case 'n':
                        if (EndsWith("ant")) break;
                        if (EndsWith("ement")) break;
                        if (EndsWith("ment")) break;
                        /* element etc. not stripped before the m */
                        if (EndsWith("ent")) break; return;
                    case 'o':
                        if (EndsWith("ion") && stemIndex >= 0 && (wordArray[stemIndex] == 's' || wordArray[stemIndex] == 't')) break;
                        /* j >= 0 fixes Bug 2 */
                        if (EndsWith("ou")) break; return;
                    /* takes care of -ous */
                    case 's':
                        if (EndsWith("ism")) break; return;
                    case 't':
                        if (EndsWith("ate")) break;
                        if (EndsWith("iti")) break; return;
                    case 'u':
                        if (EndsWith("ous")) break; return;
                    case 'v':
                        if (EndsWith("ive")) break; return;
                    case 'z':
                        if (EndsWith("ize")) break; return;
                    default:
                        return;
                }
                if (MeasureConsontantSequence() > 1)
                    endIndex = stemIndex;
            }

            /* step6() removes a final -e if m() > 1. */
            private void Step6()
            {
                stemIndex = endIndex;

                if (wordArray[endIndex] == 'e')
                {
                    var a = MeasureConsontantSequence();
                    if (a > 1 || a == 1 && !IsCVC(endIndex - 1))
                        endIndex--;
                }
                if (wordArray[endIndex] == 'l' && IsDoubleConsontant(endIndex) && MeasureConsontantSequence() > 1)
                    endIndex--;
            }

            // Returns true if the character at the specified index is a consonant.
            // With special handling for 'y'.
            private bool IsConsonant(int index)
            {
                var c = wordArray[index];
                if (c == 'a' || c == 'e' || c == 'i' || c == 'o' || c == 'u') return false;
                return c != 'y' || (index == 0 || !IsConsonant(index - 1));
            }

            /* m() measures the number of consonant sequences between 0 and j. if c is
               a consonant sequence and v a vowel sequence, and <..> indicates arbitrary
               presence,

                  <c><v>       gives 0
                  <c>vc<v>     gives 1
                  <c>vcvc<v>   gives 2
                  <c>vcvcvc<v> gives 3
                  ....		*/
            private int MeasureConsontantSequence()
            {
                var n = 0;
                var index = 0;
                while (true)
                {
                    if (index > stemIndex) return n;
                    if (!IsConsonant(index)) break; index++;
                }
                index++;
                while (true)
                {
                    while (true)
                    {
                        if (index > stemIndex) return n;
                        if (IsConsonant(index)) break;
                        index++;
                    }
                    index++;
                    n++;
                    while (true)
                    {
                        if (index > stemIndex) return n;
                        if (!IsConsonant(index)) break;
                        index++;
                    }
                    index++;
                }
            }

            // Return true if there is a vowel in the current stem (0 ... stemIndex)
            private bool VowelInStem()
            {
                int i;
                for (i = 0; i <= stemIndex; i++)
                {
                    if (!IsConsonant(i)) return true;
                }
                return false;
            }

            // Returns true if the char at the specified index and the one preceeding it are the same consonants.
            private bool IsDoubleConsontant(int index)
            {
                if (index < 1) return false;
                return wordArray[index] == wordArray[index - 1] && IsConsonant(index);
            }

            /* cvc(i) is true <=> i-2,i-1,i has the form consonant - vowel - consonant
               and also if the second c is not w,x or y. this is used when trying to
               restore an e at the end of a short word. e.g.

                  cav(e), lov(e), hop(e), crim(e), but
                  snow, box, tray.		*/
            private bool IsCVC(int index)
            {
                if (index < 2 || !IsConsonant(index) || IsConsonant(index - 1) || !IsConsonant(index - 2)) return false;
                var c = wordArray[index];
                return c != 'w' && c != 'x' && c != 'y';
            }

            // Does the current word array end with the specified string.
            private bool EndsWith(string s)
            {
                var length = s.Length;
                var index = endIndex - length + 1;
                if (index < 0) return false;

                for (var i = 0; i < length; i++)
                {
                    if (wordArray[index + i] != s[i]) return false;
                }
                stemIndex = endIndex - length;
                return true;
            }

            // Set the end of the word to s.
            // Starting at the current stem pointer and readjusting the end pointer.
            private void SetEnd(string s)
            {
                var length = s.Length;
                var index = stemIndex + 1;
                for (var i = 0; i < length; i++)
                {
                    wordArray[index + i] = s[i];
                }
                // Set the end pointer to the new end of the word.
                endIndex = stemIndex + length;
            }

            // Conditionally replace the end of the word
            private void ReplaceEnd(string s)
            {
                if (MeasureConsontantSequence() > 0) SetEnd(s);
            }
        }

    }

    public class search
    {
        private static string conn = @"port =3306 ; server = localhost; user id = root; password='yomna_123'; persistsecurityinfo = False; database = mydb";
        static MySqlConnection connection = new MySqlConnection(conn);
        static string[] arrToCheck = File.ReadAllLines(@"E:\\ir\\Indexing\\Indexing\\stop_words_english.txt");

        Indexing Ind = new Indexing();

        public static List<string> my_Link = new List<string>();
        public static string paragraph { get; set; }

        public search(string paragraph)
        {
            search.paragraph = paragraph;
        }

        public static void Retrive_Pages()
        {
            connection.Open();
            MySqlCommand Select_from_table = new MySqlCommand("SELECT * from inverted_index;", connection);
            var Reader = Select_from_table.ExecuteReader();
            IndexingParg(Reader);
            connection.Close();
        }

        private static void IndexingParg(MySqlDataReader Reader)
        {
            Dictionary<string, Dictionary<int, my_Words>> dic = new Dictionary<string, Dictionary<int, my_Words>>();
            //[Term ,Dictionary<document Id,token>
            // token -> pos freq


            while (Reader.Read())
            {
                string name = Reader.GetString(1);
                int docID = (Reader.GetInt32(2));
                int freq = (Reader.GetInt32(3));
                string pos = (Reader.GetString(4));


                my_Words words = new my_Words();
                words.frequency = freq;
                words.position = pos;

                if (dic.ContainsKey(name))
                {
                    Dictionary<int, my_Words> Dic_var = dic[name];
                    if (!Dic_var.ContainsKey(docID))
                        dic[name].Add(docID, words);
                }
                else
                {
                    Dictionary<int, my_Words> Dic_var = new Dictionary<int, my_Words>();
                    Dic_var.Add(docID, words);
                    dic.Add(name, Dic_var);

                }
            }

            //Split the Query with space and ","
            List<String> splited_words_list = new List<String>();
            splited_words_list = paragraph.Split(new char[] { ' ', ',' }).ToList();

            List<string> splited_words = splited_words_list.ToList();

            GetSplitedWords(splited_words_list);

            my_Link.Clear();

            if (paragraph.Contains(@""""))
            {
                // remove double quotes
                String readyParag = paragraph.Replace("\"", "");

                List<string> all_documents = new List<string>();
                List<string> all_links = new List<string>();

                // get All documents 

                connection.Open(); MySqlCommand Select_from_table = new MySqlCommand("SELECT * from documents ;", connection);
                Reader = Select_from_table.ExecuteReader();

                while (Reader.Read())
                {
                    all_links.Add(Reader.GetString(1));
                    all_documents.Add(Reader.GetString(2));
                }

                connection.Close();


                Dictionary<String, int> linkFreq = new Dictionary<String, int>();
                int freq = 0;

                for (int i = 0; i < all_documents.Count; i++)
                {
                    if (all_documents[i].Contains(readyParag))
                    {
                        freq = countFrequency(readyParag, all_documents[i]);

                        if (!linkFreq.ContainsKey(all_links[i]))
                            linkFreq.Add(all_links[i], freq);
                        freq = 0;
                    }
                }

                var tmp = linkFreq.OrderByDescending(z => z.Value).ToDictionary(z => z.Key, z => z.Value); ;

                foreach (var entry in linkFreq)
                {
                    all_links.Add(entry.Key);
                }
                foreach (var entry in linkFreq)
                {
                    my_Link.Add(entry.Key);
                }
            }

            else
            {

                List<int> WordDocs = dic[splited_words_list[0]].Keys.ToList();
                for (int i = 1; i < splited_words_list.Count; i++)
                {
                    if (dic.ContainsKey(splited_words_list[i]))
                    {
                        List<int> WordDocs2 = dic[splited_words_list[i]].Keys.ToList();
                        WordDocs = (WordDocs2.Intersect(WordDocs2.Select(y => y))).ToList();
                    }
                }
            
                //Get matched urls from database
                
                for (int i = 0; i < WordDocs.Count; i++)
                {
                    connection.Open();
                    int id = WordDocs[i];
                    MySqlCommand Select_from_table = new MySqlCommand("SELECT url from documents where ID = " + id+ ";", connection);
                    Reader = Select_from_table.ExecuteReader();

                    while (Reader.Read())
                    {
                        my_Link.Add(Reader.GetString(0));
                    }

                    connection.Close();
                }

                Dictionary<int, int> ProximityScoring = new Dictionary<int, int>();

                for (int j = 0; j < WordDocs.Count; j++)
                {
                    int counter = 0;
                    int current = 0;

                    for (int i = 0; i < splited_words_list.Count; i++)
                    {
                        
                        string[] positionsString = dic[splited_words_list[i]][WordDocs[j]].position.Split(',');
                        List<string> positionsList = positionsString.ToList();
                        positionsList.RemoveAt(positionsList.Count - 1);
                        List<int> y = positionsList.Select(int.Parse).ToList();

                        if (i != 0)
                            counter += Math.Abs(y[0] - current);

                        current = y[0];

                    }

                    ProximityScoring.Add(WordDocs[j], counter);

                }

                ProximityScoring.OrderBy(y => y.Value).ToDictionary(y => y.Key, y => y.Value);

            }
        }

        public static void RemovingStopwords(List<string> word)
        {

            for (int i = 0; i < word.Count; i++)
            {
                for (int j = 0; j < word[i].Length; j++)
                {
                    if (word[i][j] == '"')
                    {
                        word[i] = word[i].Replace('"', ' ').Trim();
                    }
                    else if (word[i][j] == '^')
                    {
                        word[i] = word[i].Replace('^', ' ').Trim();
                    }
                    else if (word[i][j] == '@')
                    {
                        word[i] = word[i].Replace('@', ' ').Trim();
                    }
                }

                word[i] = word[i].Trim();
            }
            for (int i = 0; i < word.Count; i++)
            {
                word[i] = Regex.Replace(word[i], @"[\d-]", string.Empty);
                if (string.IsNullOrEmpty(word[i]))
                {

                    word.RemoveAt(i);
                }
            }


            for (int i = 0; i < word.Count; i++)
            {
                for (int j = 0; j < word[i].Length; j++)
                {
                    if (char.IsDigit(word[i][j]))
                    {
                        word.RemoveAt(i);
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }

            }


            for (int i = 0; i < word.Count; i++)
            {
                if (arrToCheck.Contains(word[i]) || word[i].Length < 3)
                {

                    word.RemoveAt(i);
                }

            }

            for (int i = 0; i < word.Count; i++)
            {
                word[i] = Regex.Replace(word[i], @"[\d-]", string.Empty);
                if (string.IsNullOrEmpty(word[i]))
                {

                    word.RemoveAt(i);
                }
            }

            Stemming(word);
        }


        public static void GetSplitedWords(List<String> splited_words_list)
        {

            for (int i = 0; i < splited_words_list.Count; i++)
            {

                splited_words_list[i] = splited_words_list[i].ToLower();

            }

            RemovingStopwords(splited_words_list);
        }


        public static void Stemming(List<string> word)
        {

            var stem = new PorterStemmer();
            for (int i = 0; i < word.Count; i++)
            {
                //  stemmer.StemWord(token[i]);
                if (!string.IsNullOrEmpty(stem.StemWord(word[i])))
                {
                    word[i] = (stem.StemWord(word[i]));
                }
            }


            // Token_info(tokens, new_positions, docid);
        }

        public static int countFrequency(String pattern, String txt)
        {
            int X = pattern.Length;
            int Y = txt.Length;
            int res = 0;

            /* A loop to slide pattern[] one by one */
            for (int i = 0; i <= Y - X; i++)
            {
                /* For current index i, check for
            pattern match */
                int j;
                for (j = 0; j < X; j++)
                {
                    if (txt[i + j] != pattern[j])
                    {
                        break;
                    }
                }

                // if pat[0...X-1] = txt[i, i+1, ...i+X-1]
                if (j == X)
                {
                    res++;
                    j = 0;
                }
            }
            return res;
        }

        

        public class PorterStemmer
        {

            // The passed in word turned into a char array. 
            // Quicker to use to rebuilding strings each time a change is made.
            private char[] wordArray;

            // Current index to the end of the word in the character array. This will
            // change as the end of the string gets modified.
            private int endIndex;

            // Index of the (potential) end of the stem word in the char array.
            private int stemIndex;


            /// <summary>
            /// Stem the passed in word.
            /// </summary>
            /// <param name="word">Word to evaluate</param>
            /// <returns></returns>
            public string StemWord(string word)
            {

                // Do nothing for empty strings or short words.
                if (string.IsNullOrWhiteSpace(word) || word.Length <= 2) return word;

                wordArray = word.ToCharArray();

                stemIndex = 0;
                endIndex = word.Length - 1;
                Step1();
                Step2();
                Step3();
                Step4();
                Step5();
                Step6();

                var length = endIndex + 1;
                return new String(wordArray, 0, length);
            }


            // Step1() gets rid of plurals and -ed or -ing.
            /* Examples:
                   caresses  ->  caress
                   ponies    ->  poni
                   ties      ->  ti
                   caress    ->  caress
                   cats      ->  cat

                   feed      ->  feed
                   agreed    ->  agree
                   disabled  ->  disable

                   matting   ->  mat
                   mating    ->  mate
                   meeting   ->  meet
                   milling   ->  mill
                   messing   ->  mess

                   meetings  ->  meet  		*/
            private void Step1()
            {
                // If the word ends with s take that off
                if (wordArray[endIndex] == 's')
                {
                    if (EndsWith("sses"))
                    {
                        endIndex -= 2;
                    }
                    else if (EndsWith("ies"))
                    {
                        SetEnd("i");
                    }
                    else if (wordArray[endIndex - 1] != 's')
                    {
                        endIndex--;
                    }
                }
                if (EndsWith("eed"))
                {
                    if (MeasureConsontantSequence() > 0)
                        endIndex--;
                }
                else if ((EndsWith("ed") || EndsWith("ing")) && VowelInStem())
                {
                    endIndex = stemIndex;
                    if (EndsWith("at"))
                        SetEnd("ate");
                    else if (EndsWith("bl"))
                        SetEnd("ble");
                    else if (EndsWith("iz"))
                        SetEnd("ize");
                    else if (IsDoubleConsontant(endIndex))
                    {
                        endIndex--;
                        int ch = wordArray[endIndex];
                        if (ch == 'l' || ch == 's' || ch == 'z')
                            endIndex++;
                    }
                    else if (MeasureConsontantSequence() == 1 && IsCVC(endIndex)) SetEnd("e");
                }
            }

            // Step2() turns terminal y to i when there is another vowel in the stem.
            private void Step2()
            {
                if (EndsWith("y") && VowelInStem())
                    wordArray[endIndex] = 'i';
            }

            // Step3() maps double suffices to single ones. so -ization ( = -ize plus
            // -ation) maps to -ize etc. note that the string before the suffix must give m() > 0. 
            private void Step3()
            {
                if (endIndex == 0) return;

                /* For Bug 1 */
                switch (wordArray[endIndex - 1])
                {
                    case 'a':
                        if (EndsWith("ational")) { ReplaceEnd("ate"); break; }
                        if (EndsWith("tional")) { ReplaceEnd("tion"); }
                        break;
                    case 'c':
                        if (EndsWith("enci")) { ReplaceEnd("ence"); break; }
                        if (EndsWith("anci")) { ReplaceEnd("ance"); }
                        break;
                    case 'e':
                        if (EndsWith("izer")) { ReplaceEnd("ize"); }
                        break;
                    case 'l':
                        if (EndsWith("bli")) { ReplaceEnd("ble"); break; }
                        if (EndsWith("alli")) { ReplaceEnd("al"); break; }
                        if (EndsWith("entli")) { ReplaceEnd("ent"); break; }
                        if (EndsWith("eli")) { ReplaceEnd("e"); break; }
                        if (EndsWith("ousli")) { ReplaceEnd("ous"); }
                        break;
                    case 'o':
                        if (EndsWith("ization")) { ReplaceEnd("ize"); break; }
                        if (EndsWith("ation")) { ReplaceEnd("ate"); break; }
                        if (EndsWith("ator")) { ReplaceEnd("ate"); }
                        break;
                    case 's':
                        if (EndsWith("alism")) { ReplaceEnd("al"); break; }
                        if (EndsWith("iveness")) { ReplaceEnd("ive"); break; }
                        if (EndsWith("fulness")) { ReplaceEnd("ful"); break; }
                        if (EndsWith("ousness")) { ReplaceEnd("ous"); }
                        break;
                    case 't':
                        if (EndsWith("aliti")) { ReplaceEnd("al"); break; }
                        if (EndsWith("iviti")) { ReplaceEnd("ive"); break; }
                        if (EndsWith("biliti")) { ReplaceEnd("ble"); }
                        break;
                    case 'g':
                        if (EndsWith("logi"))
                        {
                            ReplaceEnd("log");
                        }
                        break;
                }
            }

            /* step4() deals with -ic-, -full, -ness etc. similar strategy to step3. */
            private void Step4()
            {
                switch (wordArray[endIndex])
                {
                    case 'e':
                        if (EndsWith("icate")) { ReplaceEnd("ic"); break; }
                        if (EndsWith("ative")) { ReplaceEnd(""); break; }
                        if (EndsWith("alize")) { ReplaceEnd("al"); }
                        break;
                    case 'i':
                        if (EndsWith("iciti")) { ReplaceEnd("ic"); }
                        break;
                    case 'l':
                        if (EndsWith("ical")) { ReplaceEnd("ic"); break; }
                        if (EndsWith("ful")) { ReplaceEnd(""); }
                        break;
                    case 's':
                        if (EndsWith("ness")) { ReplaceEnd(""); }
                        break;
                }
            }

            /* step5() takes off -ant, -ence etc., in context <c>vcvc<v>. */
            private void Step5()
            {
                if (endIndex == 0) return;

                switch (wordArray[endIndex - 1])
                {
                    case 'a':
                        if (EndsWith("al")) break; return;
                    case 'c':
                        if (EndsWith("ance")) break;
                        if (EndsWith("ence")) break; return;
                    case 'e':
                        if (EndsWith("er")) break; return;
                    case 'i':
                        if (EndsWith("ic")) break; return;
                    case 'l':
                        if (EndsWith("able")) break;
                        if (EndsWith("ible")) break; return;
                    case 'n':
                        if (EndsWith("ant")) break;
                        if (EndsWith("ement")) break;
                        if (EndsWith("ment")) break;
                        /* element etc. not stripped before the m */
                        if (EndsWith("ent")) break; return;
                    case 'o':
                        if (EndsWith("ion") && stemIndex >= 0 && (wordArray[stemIndex] == 's' || wordArray[stemIndex] == 't')) break;
                        /* j >= 0 fixes Bug 2 */
                        if (EndsWith("ou")) break; return;
                    /* takes care of -ous */
                    case 's':
                        if (EndsWith("ism")) break; return;
                    case 't':
                        if (EndsWith("ate")) break;
                        if (EndsWith("iti")) break; return;
                    case 'u':
                        if (EndsWith("ous")) break; return;
                    case 'v':
                        if (EndsWith("ive")) break; return;
                    case 'z':
                        if (EndsWith("ize")) break; return;
                    default:
                        return;
                }
                if (MeasureConsontantSequence() > 1)
                    endIndex = stemIndex;
            }

            /* step6() removes a final -e if m() > 1. */
            private void Step6()
            {
                stemIndex = endIndex;

                if (wordArray[endIndex] == 'e')
                {
                    var a = MeasureConsontantSequence();
                    if (a > 1 || a == 1 && !IsCVC(endIndex - 1))
                        endIndex--;
                }
                if (wordArray[endIndex] == 'l' && IsDoubleConsontant(endIndex) && MeasureConsontantSequence() > 1)
                    endIndex--;
            }

            // Returns true if the character at the specified index is a consonant.
            // With special handling for 'y'.
            private bool IsConsonant(int index)
            {
                var c = wordArray[index];
                if (c == 'a' || c == 'e' || c == 'i' || c == 'o' || c == 'u') return false;
                return c != 'y' || (index == 0 || !IsConsonant(index - 1));
            }

            /* m() measures the number of consonant sequences between 0 and j. if c is
               a consonant sequence and v a vowel sequence, and <..> indicates arbitrary
               presence,

                  <c><v>       gives 0
                  <c>vc<v>     gives 1
                  <c>vcvc<v>   gives 2
                  <c>vcvcvc<v> gives 3
                  ....		*/
            private int MeasureConsontantSequence()
            {
                var n = 0;
                var index = 0;
                while (true)
                {
                    if (index > stemIndex) return n;
                    if (!IsConsonant(index)) break; index++;
                }
                index++;
                while (true)
                {
                    while (true)
                    {
                        if (index > stemIndex) return n;
                        if (IsConsonant(index)) break;
                        index++;
                    }
                    index++;
                    n++;
                    while (true)
                    {
                        if (index > stemIndex) return n;
                        if (!IsConsonant(index)) break;
                        index++;
                    }
                    index++;
                }
            }

            // Return true if there is a vowel in the current stem (0 ... stemIndex)
            private bool VowelInStem()
            {
                int i;
                for (i = 0; i <= stemIndex; i++)
                {
                    if (!IsConsonant(i)) return true;
                }
                return false;
            }

            // Returns true if the char at the specified index and the one preceeding it are the same consonants.
            private bool IsDoubleConsontant(int index)
            {
                if (index < 1) return false;
                return wordArray[index] == wordArray[index - 1] && IsConsonant(index);
            }

            /* cvc(i) is true <=> i-2,i-1,i has the form consonant - vowel - consonant
               and also if the second c is not w,x or y. this is used when trying to
               restore an e at the end of a short word. e.g.

                  cav(e), lov(e), hop(e), crim(e), but
                  snow, box, tray.		*/
            private bool IsCVC(int index)
            {
                if (index < 2 || !IsConsonant(index) || IsConsonant(index - 1) || !IsConsonant(index - 2)) return false;
                var c = wordArray[index];
                return c != 'w' && c != 'x' && c != 'y';
            }

            // Does the current word array end with the specified string.
            private bool EndsWith(string s)
            {
                var length = s.Length;
                var index = endIndex - length + 1;
                if (index < 0) return false;

                for (var i = 0; i < length; i++)
                {
                    if (wordArray[index + i] != s[i]) return false;
                }
                stemIndex = endIndex - length;
                return true;
            }

            // Set the end of the word to s.
            // Starting at the current stem pointer and readjusting the end pointer.
            private void SetEnd(string s)
            {
                var length = s.Length;
                var index = stemIndex + 1;
                for (var i = 0; i < length; i++)
                {
                    wordArray[index + i] = s[i];
                }
                // Set the end pointer to the new end of the word.
                endIndex = stemIndex + length;
            }

            // Conditionally replace the end of the word
            private void ReplaceEnd(string s)
            {
                if (MeasureConsontantSequence() > 0) SetEnd(s);
            }
        }



    }
   
}
