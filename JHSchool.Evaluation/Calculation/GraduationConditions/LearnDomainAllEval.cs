﻿using System.Collections.Generic;
using System.Xml;
using JHSchool.Data;
using System;

namespace JHSchool.Evaluation.Calculation.GraduationConditions
{
    /// <summary>
    /// 學習領域成績是否符所有學期條件
    /// </summary>
    internal class LearnDomainAllEval : IEvaluative
    {
        private EvaluationResult _result;
        private int _domain_count = 0;
        private decimal _score = 0;

        /// <summary>
        /// XML參數建構式
        /// <![CDATA[ 
        /// <條件 Checked="True" Type="LearnDomainEach" 學習領域="2" 等第="丙"/>
        /// ]]>
        /// </summary>
        /// <param name="element"></param>
        public LearnDomainAllEval(XmlElement element)
        {
            _result = new EvaluationResult();

            _domain_count = int.Parse(element.GetAttribute("學習領域"));
            string degree = element.GetAttribute("等第");

            //ConfigData cd = School.Configuration["等第對照表"];
            //if (!string.IsNullOrEmpty(cd["xml"]))
            //{
            //    XmlElement xml = XmlHelper.LoadXml(cd["xml"]);
            //    XmlElement scoreMapping = (XmlElement)xml.SelectSingleNode("ScoreMapping[@Name=\"" + degree + "\"]");
            //    decimal d;
            //    if (scoreMapping != null && decimal.TryParse(scoreMapping.GetAttribute("Score"), out d))
            //        _score = d;
            //}

            JHSchool.Evaluation.Mapping.DegreeMapper mapper = new JHSchool.Evaluation.Mapping.DegreeMapper();
            decimal? d = mapper.GetScoreByDegree(degree);
            if (d.HasValue) _score = d.Value;

            //<條件 Checked="True" Type="LearnDomainEach" 學習領域="2" 等第="丙"/>
        }

        #region IEvaluative 成員

        public Dictionary<string, bool> Evaluate(IEnumerable<StudentRecord> list)
        {
            _result.Clear();

            Dictionary<string, bool> passList = new Dictionary<string, bool>();

            //Dictionary<string, SemesterHistoryUtility> shList = new Dictionary<string, SemesterHistoryUtility>();
            //foreach (Data.JHSemesterHistoryRecord shRecord in Data.JHSemesterHistory.SelectByStudentIDs(list.AsKeyList().ToArray()))
            //{
            //    if (!shList.ContainsKey(shRecord.RefStudentID))
            //        shList.Add(shRecord.RefStudentID, new SemesterHistoryUtility(shRecord));
            //}

            //list.SyncSemesterScoreCache();
            Dictionary<string, List<Data.JHSemesterScoreRecord>> studentSemesterScoreCache = new Dictionary<string, List<JHSchool.Data.JHSemesterScoreRecord>>();
            foreach (Data.JHSemesterScoreRecord record in Data.JHSemesterScore.SelectByStudentIDs(list.AsKeyList()))
            {
                if (!studentSemesterScoreCache.ContainsKey(record.RefStudentID))
                    studentSemesterScoreCache.Add(record.RefStudentID, new List<JHSchool.Data.JHSemesterScoreRecord>());
                studentSemesterScoreCache[record.RefStudentID].Add(record);
            }

            foreach (StudentRecord each in list)
            {
                List<ResultDetail> resultList = new List<ResultDetail>();
                JHSemesterHistoryRecord shRec = new JHSemesterHistoryRecord();
                if (UIConfig._StudentSHistoryRecDict.ContainsKey(each.ID))
                    shRec = UIConfig._StudentSHistoryRecDict[each.ID];

                //領域成績字典
                Dictionary<string, List<decimal>> DomainScoreDic = new Dictionary<string, List<decimal>>();

                //領域畢業平均成績
                Dictionary<string, decimal> Averages = new Dictionary<string, decimal>();

                // 有成績學年度學期
                List<string> hasSemsScoreSchoolYearSemester = new List<string>();

                // 有學期成績
                if (studentSemesterScoreCache.ContainsKey(each.ID))
                {
                    #region 取得各學期領域成績
                    foreach (Data.JHSemesterScoreRecord record in studentSemesterScoreCache[each.ID])
                    {
                        hasSemsScoreSchoolYearSemester.Add(record.SchoolYear.ToString() + record.Semester.ToString());

                        foreach (K12.Data.DomainScore domain in record.Domains.Values)
                        {
                            //若該領域有成績
                            if(domain.Score.HasValue)
                            {
                                string key = domain.Domain;
                                if (key == "國語文" || key == "英語") key = "語文";

                                //建立該領域
                                if (!DomainScoreDic.ContainsKey(key)) DomainScoreDic.Add(key, new List<decimal>());
                                //填入該領域成績
                                DomainScoreDic[key].Add(domain.Score.Value);
                            }
                        }
                    }
                    #endregion
                }

                #region 計算各領域平均成績
                //巡迴各領域
                foreach (KeyValuePair<string, List<decimal>> kvp in DomainScoreDic)
                {
                    //領域名稱
                    string domain = kvp.Key;
                    //各領域初值為0
                    if (!Averages.ContainsKey(domain)) Averages.Add(domain, 0);

                    //巡迴該領域的所有成績
                    foreach (decimal score in kvp.Value)
                    {
                        //加總該領域成績
                        Averages[domain] += score;
                    }

                    //計算該領域平均
                    Averages[domain] /= kvp.Value.Count;
                    //四捨五入
                    Averages[domain] = Math.Round(Averages[domain], 2, MidpointRounding.AwayFromZero);
                }
                #endregion

                int count = 0;
                foreach(string key in Averages.Keys)
                {
                    if(Averages[key] >= _score)
                    {
                        count++;
                    }
                }

                if(count < _domain_count)
                {
                    ResultDetail rd = new ResultDetail(each.ID, "0", "0");
                    rd.AddMessage("領域平均成績不符合畢業規範");
                    rd.AddDetail("領域平均成績不符合畢業規範");
                    resultList.Add(rd);
                }

                // 檢查有學期歷程沒有成績
                //foreach (K12.Data.SemesterHistoryItem shi in shRec.SemesterHistoryItems)
                //{
                //    //if (shi.RefStudentID != each.ID) continue;
                //    if (!hasSemsScoreSchoolYearSemester.Contains(shi.SchoolYear.ToString() + shi.Semester.ToString()))
                //    {
                //        ResultDetail rd = new ResultDetail(each.ID, shi.GradeYear.ToString(), shi.Semester.ToString());
                //        rd.AddMessage("學期領域成績資料缺漏");
                //        rd.AddDetail("學期領域成績資料缺漏");
                //        resultList.Add(rd);
                //    }
                //}

                if (resultList.Count > 0)
                {
                    _result.Add(each.ID, resultList);
                    passList.Add(each.ID, false);
                }
                else
                    passList.Add(each.ID, true);
            }

            return passList;
        }

        public EvaluationResult Result
        {
            get { return _result; }
        }

        #endregion
    }
}