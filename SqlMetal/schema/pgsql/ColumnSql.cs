////////////////////////////////////////////////////////////////////
//Initial author: Jiri George Moudry, 2006.
//License: LGPL. (Visit http://www.gnu.org)
////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Text;
using Npgsql;

namespace SqlMetal.schema.pgsql
{
    /// <summary>
    /// represents one row from information_schema.`COLUMNS`
    /// </summary>
    public class Column
    {
        public string table_catalog;
        public string table_schema;
        public string table_name;
        public string column_name;
        public bool isNullable;

        /// <summary>
        /// eg 'int' or 'datetime'
        /// </summary>
        public string datatype;
        //public string extra;

        /// <summary>
        /// eg. 'int(10) unsigned'
        /// </summary>
        public string column_type;

        /// <summary>
        /// eg. for column called 'int' we use csharpName='int_'
        /// </summary>
        public string csharpFieldName;

        /// <summary>
        /// eg. "nextval('products_productid_seq'::regclass)"
        /// </summary>
        public string column_default;

        public override string ToString()
        {
            return "Column "+table_name+"."+column_name+"  "+datatype.Substring(0,4);
        }
    }

    /// <summary>
    /// class for sleecting from information_schema.`COLUMNS`
    /// </summary>
    class ColumnSql
    {
        Column fromRow(NpgsqlDataReader rdr)
        {
            Column t = new Column();
            int field = 0;
            t.table_catalog = rdr.GetString(field++);
            t.table_schema  = rdr.GetString(field++);
            t.table_name    = rdr.GetString(field++);
            t.column_name   = rdr.GetString(field++);
            string nullableStr = rdr.GetString(field++);
            t.isNullable    = nullableStr=="YES";
            t.datatype      = rdr.GetString(field++);
            t.column_default = rdr.IsDBNull(field++) ? null : rdr.GetString(field-1);
            //t.extra         = null; //rdr.GetString(field++);
            t.column_type   = null; //rdr.GetString(field++);
            //t.column_key    = null; //rdr.GetString(field++);
            return t;
        }

        public List<Column> getColumns(NpgsqlConnection conn, string db)
        {
            string sql = @"
SELECT table_catalog,table_schema,table_name,column_name
    ,is_nullable,data_type,column_default
FROM information_schema.COLUMNS
WHERE table_catalog=:db
AND table_schema NOT IN ('pg_catalog','information_schema')";

            using(NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.Add(":db", db);
                using(NpgsqlDataReader rdr = cmd.ExecuteReader())
                {
                    List<Column> list = new List<Column>();
                    while(rdr.Read())
                    {
                        list.Add( fromRow(rdr) );
                    }
                    return list;
                }
            }

        }

    }
}
