using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WpfMovieManager2.data;
using WpfMovieManager2.service;
using WpfMovieManager2Mysql;

namespace WpfMovieManager2.common
{
    public class Actress
    {
        private static Regex regFav = new Regex("Fav[1-9]{0,2}");

        /// <summary>
        /// タグに設定された複数の女優名をParseして、複数の女優の配列としてリターン
        /// </summary>
        /// <returns></returns>
        public static Tuple<string, string[]> ParseTag(string myTag)
        {
            string[] arrData;
            if (myTag.IndexOf(",") < 0)
            {
                arrData = new string[1];
                arrData[0] = GetActressName(myTag);

                return Tuple.Create("", arrData);
            }

            string favInfo = "";
            List<string> listActress = new List<string>();
            arrData = myTag.Split(',');
            foreach (string data in arrData)
            {
                Match mFav = regFav.Match(data);
                if (mFav.Success)
                {
                    favInfo = mFav.Groups[0].Value;
                    continue;
                }

                // 「松ゆきの(2人目)」のような場合に括弧内を消す
                string act = GetActressName(data);
                if (act.Trim().Length > 0)
                    listActress.Add(act);
            }

            return Tuple.Create(favInfo, listActress.ToArray());
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
        internal static string GetEvaluation(string myFavInfo, string[] myArrayActress, AvContentsService contentsService, MySqlDbConnection dockerMysqlConn, int myMode)
        {
            // myMode 1: Favでの選択時、1人で別名モード 2: 複数含むモード
            string favInfo = "";
            string resultEvaluation = "", evaluation = "";
            int maxFav = 0;
            string maxActress = "";

            int totalTaget = 0;
            int totalTagetLike = 0;
            int totalSumEvaluate = 0;
            int totalUnEvaluate = 0;
            int totalMaxEvaluate = 0;

            if (myFavInfo.Length > 0)
                favInfo = "(" + myFavInfo + ")";

            foreach (string actress in myArrayActress)
            {
                if (actress.Trim().Length <= 0)
                    continue;

                string[] arrFavActress = contentsService.GetFavoriteActresses(actress, dockerMysqlConn);

                List<AvContentsData> avContentsList = new List<AvContentsData>();
                List<AvContentsData> avContentsFilenameLikeList = new List<AvContentsData>();
                if (arrFavActress.Length >= 1 && myMode != 1)
                {
                    favInfo = "Fav";
                    foreach (string favActress in arrFavActress)
                    {
                        List<AvContentsData> list = contentsService.GetActressList(actress, dockerMysqlConn);

                        foreach (AvContentsData data in list)
                        {
                            if (!avContentsList.Exists(x => x.Id == data.Id))
                                avContentsList.Add(data);
                        }
                    }
                }
                else
                {
                    avContentsList = contentsService.GetActressList("%" + actress + "%", dockerMysqlConn);
                    avContentsFilenameLikeList = contentsService.GetActressLikeFilenameList("%" + actress + "%", dockerMysqlConn, avContentsList);
                }

                int sumEvaluate = 0, unEvaluate = 0, maxEvaluate = 0;

                if (avContentsList.Count > 0)
                {
                    sumEvaluate = avContentsList.Sum(x => x.Rating);
                    unEvaluate = avContentsList.Where(x => x.Rating == 0).Count();
                    maxEvaluate = avContentsList.Max(x => x.Rating);
                }

                if (myArrayActress.Length > 1)
                {
                    if (maxFav < maxEvaluate)
                    {
                        maxFav = maxEvaluate;
                        maxActress = actress;
                    }
                }

                if (myMode == 1)
                {
                    totalTagetLike += avContentsFilenameLikeList.Count;
                    totalTaget += avContentsList.Count;
                    totalSumEvaluate += sumEvaluate;
                    totalUnEvaluate += unEvaluate;
                    if (totalMaxEvaluate < maxEvaluate)
                        totalMaxEvaluate = maxEvaluate;
                }
                else
                {
                    if (sumEvaluate <= 0 || avContentsList.Count - unEvaluate <= 0)
                        evaluation = String.Format("全未評価 {0} ({1})", avContentsList.Count, avContentsFilenameLikeList.Count);
                    else
                        evaluation = String.Format("未 {0}/全 {1} Max {2} Avg {3} ({4})", unEvaluate, avContentsList.Count, maxEvaluate, sumEvaluate / (avContentsList.Count - unEvaluate), avContentsFilenameLikeList.Count);

                    if (myArrayActress.Length > 1)
                        resultEvaluation = String.Format("{0} {1} {2}", resultEvaluation, actress, evaluation);
                    else
                        resultEvaluation = evaluation;
                }
            }

            if (myMode == 1)
            {
                if (totalSumEvaluate <= 0 || totalTaget - totalUnEvaluate <= 0)
                    resultEvaluation = String.Format("全未評価 {0} ({1})", totalTaget, totalTagetLike);
                else
                    resultEvaluation = String.Format("未 {0}/全 {1} Max {2} Avg {3} ({4})", totalUnEvaluate, totalTaget, totalMaxEvaluate, totalSumEvaluate / (totalTaget - totalUnEvaluate), totalTagetLike);
            }
            else
            {
                if (myArrayActress.Length > 1)
                    resultEvaluation = String.Format("【{0} Max{1}】{2}", maxActress, maxFav, resultEvaluation);

                if (favInfo.Length > 0)
                    resultEvaluation = favInfo + " " + resultEvaluation;
            }

            return resultEvaluation;

        }

        internal static string GetEvaluation(string myTag, AvContentsService contentsService, MySqlDbConnection dockerMysqlConn, int myMode)
        {
            Tuple<string, string[]> tupleResult = common.Actress.ParseTag(myTag);

            return Actress.GetEvaluation(tupleResult.Item1, tupleResult.Item2, contentsService, dockerMysqlConn, myMode);
        }
    }
}
