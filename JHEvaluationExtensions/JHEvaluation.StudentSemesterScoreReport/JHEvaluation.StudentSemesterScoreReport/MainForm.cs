﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FISCA.Presentation.Controls;
using Campus.Report;
using JHEvaluation.StudentSemesterScoreReport.Forms;
using JHSchool.Data;

namespace JHEvaluation.StudentSemesterScoreReport
{
    public partial class MainForm : BaseForm
    {
        /// <summary>
        /// 使用學生或班級
        /// </summary>
        public enum PrintSource {學生,班級}

        /// <summary>
        /// 列印來源是學生或班級
        /// </summary>
        public static PrintSource _PrintSource;
        
        public static DialogResult Run()
        {
            MainForm form = new MainForm();
            return form.ShowDialog();
        }

        private Options _options;
        private ReportConfiguration _config;

        public MainForm()
        {            
            InitializeComponent();
            InitSemester();
            Init();       
        }

        private void Init()
        {
            this.Text = Global.ReportName;
            this.MinimumSize = this.Size;
            this.MaximumSize = this.Size;

            _config = new ReportConfiguration(Global.ReportName);
            _options = new Options();
        }

        private void InitSemester()
        {
            try
            {
                int count = 2;
                int schoolYear = int.Parse(K12.Data.School.DefaultSchoolYear);
                int semester = int.Parse(K12.Data.School.DefaultSemester);

                for (int i = count * -1; i <= count; i++)
                    cboSchoolYear.Items.Add(schoolYear + i);
                cboSemester.Items.Add(1);
                cboSemester.Items.Add(2);

                cboSchoolYear.SelectedIndex = count;
                cboSemester.SelectedIndex = semester - 1;
            }
            catch (Exception ex)
            {
                MsgBox.Show("學年度學期初始化失敗。");
                btnPrint.Enabled = false;
            }
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {
            _options.SchoolYear = (int)cboSchoolYear.SelectedItem;
            _options.Semester = (int)cboSemester.SelectedItem;
            List<JHStudentRecord> students = new List<JHStudentRecord>();

            if (_PrintSource == PrintSource.學生)
                students = JHStudent.SelectByIDs(K12.Presentation.NLDPanels.Student.SelectedSource);
            else  // 一般或輟學
                students = (from data in JHStudent.SelectByClassIDs(K12.Presentation.NLDPanels.Class.SelectedSource) where data.Status == K12.Data.StudentRecord.StudentStatus.一般 || data.Status == K12.Data.StudentRecord.StudentStatus.輟學 select data).ToList();

            //排序學生
            students.Sort(SortStudentByClassSeatNo);
            _options.Students = students;
            ReportController controller = new ReportController();
            controller.Generate(_options);
        }

        private int SortStudentByClassSeatNo(JHStudentRecord x, JHStudentRecord y)
        {
            JHClassRecord c1 = x.Class;
            JHClassRecord c2 = y.Class;
            if (c1.ID == c2.ID)
            {
                int seatNo1 = x.SeatNo.HasValue ? x.SeatNo.Value : int.MinValue;
                int seatNo2 = y.SeatNo.HasValue ? y.SeatNo.Value : int.MinValue;

                if (seatNo1 == seatNo2)
                    return x.StudentNumber.CompareTo(y.StudentNumber);
                else
                    return seatNo1.CompareTo(seatNo2);
            }
            else
            {
                if (c1 == null)
                    return -1;
                else if (c2 == null)
                    return 1;
                return c1.Name.CompareTo(c2.Name);
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void lnConfig_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            PrintConfigForm form = new PrintConfigForm();
            form.ShowDialog();
        }

        private void lnAbsenceType_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            SelectTypeForm form = new SelectTypeForm(Global.ReportName);
            form.ShowDialog();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // 取得日常生活表現名稱
            Global.DLBehaviorConfigNameDict = Global.GetDLBehaviorConfigNameDict();
        }
    }
}
