#region MIT license
////////////////////////////////////////////////////////////////////
// MIT license:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
//
// Authors:
//        Jiri George Moudry
////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using SqlMetal.schema;
using SqlMetal.util;

namespace SqlMetal.codeGen
{
    /// <summary>
    /// generates a c# class representing database.
    /// calls into CodeGenClass and CodeGenField.
    /// </summary>
    public class CodeGenAll
    {
        const string NL = "\r\n";
        const string NLNL = "\r\n\r\n";

        CodeGenClass codeGenClass = new CodeGenClass();

        public string generateAll(DlinqSchema.Database dbSchema, string vendorName)
        {
            if (dbSchema == null || dbSchema.Tables == null)
            {
                Console.WriteLine("CodeGenAll ERROR: incomplete dbSchema, cannot start generating code");
                return null;
            }

            string prolog = @"//#########################################################################
//generated by $vendorMetal on $date - extracted from $db.
//#########################################################################

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Linq;
//using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Reflection;
using DBLinq.Linq;
using DBLinq.Linq.Mapping;
";

            List<string> classBodies = new List<string>();


            foreach (DlinqSchema.Table tbl in dbSchema.Tables)
            {
                string classBody = codeGenClass.generateClass(dbSchema, tbl);
                classBodies.Add(classBody);
            }

            string opt_namespace = @"
namespace $ns
{
    $classes
}
";
            string prolog1 = prolog.Replace("$date", DateTime.Now.ToString("yyyy-MMM-dd"));
            prolog1 = prolog1.Replace("$vendor", vendorName);
            string source = mmConfig.server != null ? "server " + mmConfig.server : "file " + mmConfig.schemaXmlFile;
            //prolog1 = prolog1.Replace("$db", mmConfig.server);
            prolog1 = prolog1.Replace("$db", source);
            string classesConcat = string.Join(NLNL, classBodies.ToArray());
            classesConcat = generateDbClass(dbSchema, vendorName) + NLNL + classesConcat;
            string fileBody;
            if (mmConfig.@namespace == null || mmConfig.@namespace == "")
            {
                fileBody = prolog1 + classesConcat;
            }
            else
            {
                string body1 = opt_namespace;
                body1 = body1.Replace("$ns", mmConfig.@namespace);
                classesConcat = classesConcat.Replace(NL, NL + "\t"); //move text one tab to the right
                body1 = body1.Replace("$classes", classesConcat);
                fileBody = prolog1 + body1;
            }
            return fileBody;
        }

        string generateDbClass(DlinqSchema.Database dbSchema, string vendorName)
        {
            #region generateDbClass()
            //if (tables.Count==0)
            //    return "//L69 no tables found";
            if (dbSchema.Tables.Count == 0)
                return "//L69 no tables found";

            const string dbClassStr = @"
/// <summary>
/// This class represents $vendor database $dbname.
/// </summary>
public partial class $dbname : DataContext
{
    public $dbname(string connStr) : base(connStr)
    {
    }
    public $dbname(System.Data.IDbConnection connection) : base(connection)
    {
    }

    //these fields represent tables in database and are
    //ordered - parent tables first, child tables next. Do not change the order.
    $fieldDecl

    $storedProcs
}
";
            string dbName = dbSchema.Class;
            if (dbName == null)
            {
                dbName = dbSchema.Name;
            }

            List<string> dbFieldDecls = new List<string>();
            List<string> dbFieldInits = new List<string>();
            foreach (DlinqSchema.Table tbl in dbSchema.Tables)
            {
                string className = tbl.Type.Name;

                string fldDecl = "public Table<$1> $0 { get { return base.GetTable<$1>(\"$0\"); } }"
                    .Replace("$0", tbl.Member)
                    .Replace("$1", className); //cannot use string.Format() - there are curly brackets
                dbFieldDecls.Add(fldDecl);

                string fldInit = string.Format("{0} = new Table<{1}>(this);"
                    , tbl.Member, className);
                dbFieldInits.Add(fldInit);
            }

            //obsolete - we cannot declare a field for each table - it's a problem for large DBs
            //string dbFieldInitStr = string.Join(NL + "\t\t", dbFieldInits.ToArray());
            string dbFieldDeclStr = string.Join(NL + "\t", dbFieldDecls.ToArray());

            List<string> storedProcList = new List<string>();
            foreach (DlinqSchema.Function storedProc in dbSchema.Functions)
            {
                string s = CodeGenStoredProc.FormatProc(storedProc);
                storedProcList.Add(s);
            }
            
            if (storedProcList.Count > 0)
            {
                storedProcList.Insert(0, "#region stored procs");
                storedProcList.Add("#endregion");
            }

            string storedProcsStr = string.Join(NLNL, storedProcList.ToArray());

            string dbs = dbClassStr;
            dbs = dbs.Replace("    ", "\t"); //for spaces mean a tab
            dbs = dbs.Replace("$vendor", vendorName);
            dbs = dbs.Replace("$dbname", dbName);
            //dbs = dbs.Replace("$fieldInit", dbFieldInitStr); //no more tables as fields
            dbs = dbs.Replace("$fieldDecl", dbFieldDeclStr);
            dbs = dbs.Replace("$storedProcs", storedProcsStr);
            return dbs;
            #endregion
        }

    }
}
