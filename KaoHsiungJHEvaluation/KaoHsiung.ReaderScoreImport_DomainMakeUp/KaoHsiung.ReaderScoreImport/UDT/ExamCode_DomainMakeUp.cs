﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FISCA.UDT;

namespace KaoHsiung.ReaderScoreImport_DomainMakeUp.UDT
{
    [TableName("ReaderScoreImport.ExamCode_DomainMakeUp")]
    public class ExamCode_DomainMakeUp : ActiveRecord
    {
        [Field(Field = "ExamName", Indexed = true)]
        public string ExamName { get; set; }

        [Field(Field = "Code", Indexed = false)]
        public string Code { get; set; }
    }
}
