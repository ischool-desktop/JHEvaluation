﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using FISCA.Presentation.Controls;
using JHSchool.Data;
using FISCA.Presentation;
using FISCA.LogAgent;

namespace JHEvaluation.ScoreCalculation
{
    public partial class DomainScoreCalculate : BaseForm, IStatusReporter
    {
        private List<string> StudentIDs;
        private int SchoolYear { get; set; }
        private int Semester { get; set; }

        public DomainScoreCalculate(List<string> studentIds)
        {
            InitializeComponent();
            StudentIDs = studentIds;
        }

        private void DomainScoreCalculate_Load(object sender, EventArgs e)
        {
            Util.SetSemesterDefaultItems(intSchoolYear, intSemester);
            StudentScore.SetClassMapping();
        }

        private bool RecalculateAll = false;

        private void btnCalc_Click(object sender, EventArgs e)
        {
            DialogResult dr = MsgBox.Show("您確定要計算學生學期領域成績？", MessageBoxButtons.YesNo);

            if (dr == DialogResult.No) return;

            if (Control.ModifierKeys == Keys.Shift)
            {
                string msg = "您確定要刪掉本學期所有領域成績重算？";
                if (MsgBox.Show(msg, "密技", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    RecalculateAll = true;
            }
            else
                RecalculateAll = false;

            SchoolYear = intSchoolYear.Value;
            Semester = intSemester.Value;

            btnCalc.Enabled = false;
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.RunWorkerAsync();
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            List<StudentScore> Students = StudentIDs.ToStudentScore();

            List<StudentScore> noValid = Students.ReadCalculationRule(this); //沒有計算規則的學生。

            if (noValid.Count > 0)
                throw new CalculationException(noValid, "下列學生沒有計算規則，無法計算成績。");

            Students.ReadSemesterScore(SchoolYear, Semester, this);

            if (RecalculateAll) Students.ClearDomainScore(SemesterData.Empty);

            Students.CalcuateDomainSemesterScore(new string[] { });
            Students.SaveSemesterScore(this);

            //儲存 Log。
            try
            {
                LogSaver logger = FISCA.LogAgent.ApplicationLog.CreateLogSaverInstance();
                logger.Log("學生學期領域成績計算(學生)", "計算學生學期領域成績", "計算學生數：" + Students.Count);
                //logger.BatchLogCompleted += delegate(object sender1, EventArgs e1) { };
                //logger.BatchLogFailure += delegate(object sender1, LogErrorEventArgs e1) { };
                foreach (StudentScore each in Students)
                {
                    StringBuilder description = new StringBuilder();
                    description.AppendLine(string.Format("學生：{0}({1})", each.Name, each.StudentNumber));
                    description.AppendLine(string.Format("學年度：{0} 學期：{1}", SchoolYear, Semester));

                    foreach (LogData log in each.SemestersScore[SemesterData.Empty].Domain.Log)
                        description.Append(log.ToString());

                    description.Append(each.SemestersScore[SemesterData.Empty].LearningLog.ToString());
                    description.Append(each.SemestersScore[SemesterData.Empty].CourseLog.ToString());

                    logger.AddBatch("學生學期領域成績計算(學生)", "計算學生學期領域成績", "student", each.Id, description.ToString());
                }
                logger.LogBatch();
            }
            catch { }

            Feedback("", 0);
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            btnCalc.Enabled = true;
            MotherForm.SetStatusBarMessage("", 0);

            if (e.Error != null)
            {
                if (e.Error is CalculationException)
                {
                    CalculationException ce = e.Error as CalculationException;
                    new StudentsView<StudentScore>(ce.Students, ce.Message).ShowDialog();
                }
                else
                    MsgBox.Show(e.Error.Message);

                return;
            }

            MsgBox.Show("成績計算完成。");
            Close();
        }

        #region IStatusReporter 成員

        public void Feedback(string message, int percentage)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string, int>(Feedback), new object[] { message, percentage });
            }
            else
            {
                MotherForm.SetStatusBarMessage(message, percentage);
                Application.DoEvents();
            }
        }

        #endregion
    }
}
