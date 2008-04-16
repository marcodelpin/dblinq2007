#region MIT license
// 
// Copyright (c) 2007-2008 Jiri Moudry
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
#endregion
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using DbLinq.Sqlite.Schema;

namespace DbLinq.Sqlite.Schema
{
    /// <summary>
    /// represents one row from MySQL's information_schema.`Key_Column_Usage` table
    /// </summary>
    public class KeyColumnUsage
    {
        public string constraint_schema;
        public string constraint_name;
        public string table_schema;
        public string table_name;
        public string column_name;
        public string referenced_table_schema;
        public string referenced_table_name;
        public string referenced_column_name;

        public override string ToString()
        {
            string detail = constraint_name == "PRIMARY"
                                ? table_name + " PK"
                                : constraint_name;
            return "KeyColUsage " + detail;
        }
    }

    /// <summary>
    /// class for reading from "information_schema.`Key_Column_Usage`"
    /// </summary>
    class KeyColumnUsageSql
    {
        enum fk_index
        {
            id = 0,
            seq = 1,
            table = 2,
            from = 3,
            to = 4
        }

        KeyColumnUsage fromRow(IDataReader rdr, string table)
        {
            KeyColumnUsage t = new KeyColumnUsage();
            const int K_ID = 0;
            //const int K_SEQ = 1;
            const int K_TABLE = 2;
            const int K_FROM = 3;
            const int K_TO = 4;
            
            t.constraint_schema = "main";
            t.table_schema = "main";
            t.referenced_table_schema = "main";
            
            t.constraint_name = "fk_" + table + "_" + rdr.GetInt32(K_ID).ToString();
            t.table_name = table;
            t.column_name = rdr.GetString(K_FROM);
            
            t.referenced_table_name = rdr.GetString(K_TABLE);
            t.referenced_column_name = rdr.GetString(K_TO);
            return t;

        }

        public List<KeyColumnUsage> getConstraints(IDbConnection conn, string db)
        {
            //Could perhaps use conn.GetSchema() instead 
            //Warning... Sqlite doesnt enforce constraints unless you define some triggers

            string sql = @" SELECT tbl_name FROM sqlite_master WHERE type='table' order by tbl_name";
            string sqlPragma = @"PRAGMA foreign_key_list('{0}');";

            return DataCommand.Find<KeyColumnUsage>(conn, sql, sqlPragma, fromRow);
        }
    }
}