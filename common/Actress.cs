using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WpfMovieManager2.common
{
    public class Actress
    {
        /// <summary>
        /// タグに設定された複数の女優名をParseして、複数の女優の配列としてリターン
        /// </summary>
        /// <returns></returns>
        public static string[] ParseTag(string myTag)
        {
            string[] arrData;
            if (myTag.IndexOf(",") < 0)
            {
                arrData = new string[1];
                arrData[0] = GetActressName(myTag);

                return arrData;
            }

            List<string> listActress = new List<string>();
            arrData = myTag.Split(',');
            foreach (string data in arrData)
            {
                // 「松ゆきの(2人目)」のような場合に括弧内を消す
                string act = GetActressName(data);
                if (act.Trim().Length > 0)
                    listActress.Add(act);
            }

            return listActress.ToArray();
        }

        public static string GetActressName(string myActressInfo)
        {
            string name = myActressInfo;

            if (myActressInfo.IndexOf("(") >= 0)
            {
                if (myActressInfo.IndexOf("(仮)") >= 0)
                    return name;

                Regex re = new Regex(String.Format("{0}.*{1}", Regex.Escape("("), ".*", Regex.Escape(")")));
                if (myActressInfo.IndexOf("(") >= 0)
                {
                    Match m = re.Match(myActressInfo);

                    if (m.Success)
                        name = myActressInfo.Replace(m.Groups[0].ToString(), "");
                }
            }
            return name;
        }

        public static List<string> AppendMatch(string myNames, List<string> myExistList)
        {
            List<string> listTarget = new List<string>();

            char splitChar = Actress.GetSplitChar(myNames);
            string[] arrLabel = myNames.Split(splitChar);

            foreach (string label in arrLabel)
                if (label.Length > 0)
                {
                    if (!myExistList.Exists(x => x == label))
                        listTarget.Add(label);
                }

            return listTarget;
        }

        public static char GetSplitChar(string myTag)
        {
            char splitChar = (char)0;

            if (myTag.IndexOf("／") >= 0)
                splitChar = '／';
            else if (myTag.IndexOf(",") >= 0)
                splitChar = ',';
            else if (myTag.IndexOf(" ") >= 0)
                splitChar = ' ';

            return splitChar;
        }

        public static bool IsNullChar(char myChar)
        {
            if ((int)myChar == 0)
                return false;

            return true;
        }

    }
}
