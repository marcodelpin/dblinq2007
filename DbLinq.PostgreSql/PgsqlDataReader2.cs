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

// picrap: excluded from build (but kept in SVN)

using System;
using System.Data;
using System.Collections.Generic;
using System.Text;
using DbLinq.Vendor.Implementation;
using Npgsql;

namespace DbLinq.PostgreSql
{
    /// <summary>
    /// This class wraps NpgsqlDataReader.
    /// It logs exceptions and provides methods to retrieve nullable types.
    /// 
    /// When we have a workaround for FatalExecutionEngineError on nullables, 
    /// this can go away.
    /// </summary>
    public class PgsqlDataReader2 : DataReader2
    {
        protected NpgsqlDataReader Reader { get { return _reader as NpgsqlDataReader; } }

        public PgsqlDataReader2(IDataReader rdr)
            : base(rdr)
        {
            if (Reader == null)
                throw new ArgumentException("rdr");
        }
        
        public override short? GetInt16N(int index)
        {
            try
            {
                if(_reader.IsDBNull(index))
                    return null;
                return _reader.GetInt16(index);
            } 
            catch(Exception ex)
            {
                Logger.Write("GetInt16N failed: "+ex);
                return null;
            }
        }

        public override char? GetCharN(int index)
        {
            try
            {
                if(_reader.IsDBNull(index))
                    return null;
                return _reader.GetChar(index);
            } 
            catch(Exception ex)
            {
                Logger.Write("GetCharN failed: "+ex);
                return null;
            }
        }

        public override bool? GetBooleanN(int index)
        {
            try
            {
                if(_reader.IsDBNull(index))
                    return null;
                return _reader.GetBoolean(index);
            } 
            catch(Exception ex)
            {
                Logger.Write("GetBooleanN failed: "+ex);
                return null;
            }
        }

        public override int GetInt32(int index)
        {
            try
            {
                DbType dbType1 = Reader.GetFieldDbType(index);
                NpgsqlTypes.NpgsqlDbType dbType2 = Reader.GetFieldNpgsqlDbType(index);
                if(dbType2==NpgsqlTypes.NpgsqlDbType.Bigint)
                {
                    return (int)_reader.GetInt64(index);
                }
                else if(dbType2==NpgsqlTypes.NpgsqlDbType.Integer)
                {
                    //ok
                }
                else
                {
                    //there is a problem (we're getting text)
                    Logger.Write("DataReader2.GetInt32: got unexpected type:"+dbType2);
                    object val = _reader.GetValue(index);
                    Logger.Write("DataReader2.GetInt32: got unexpected type:"+dbType2+"  val="+val);
                }

                //PgSql seems to give us Int64
                return (int)_reader.GetInt32(index);
            } 
            catch(Exception ex)
            {
                Logger.Write("GetInt32 failed: "+ex);
                try {
                    object obj = _reader.GetValue(index);
                    Logger.Write("GetInt32 failed, offending val: "+obj);
                } catch(Exception)
                {
                }
                return 0;
            }
        }

        public override int? GetInt32N(int index)
        {
            try
            {
                if (_reader.IsDBNull(index))
                    return null;
                return _reader.GetInt32(index);
            }
            catch (Exception ex)
            {
                Logger.Write("GetInt32N(" + index + ") failed: " + ex);
                return 0;
            }
        }

        public override uint GetUInt32(int index)
        {
            try
            {
                return (uint)_reader.GetInt32(index);
            } 
            catch(Exception ex)
            {
                Logger.Write("GetUInt32("+index+") failed: "+ex);
                try {
                    object obj = _reader.GetValue(index);
                    Logger.Write("GetUInt32 failed, offending val: "+obj);
                } catch(Exception){}
                return 0;
            }
        }

        public override uint? GetUInt32N(int index)
        {
            try
            {
                if(_reader.IsDBNull(index))
                    return null;
                return (uint)_reader.GetInt32(index);
            } 
            catch(Exception ex)
            {
                Logger.Write("GetUInt32 failed: "+ex);
                return null;
            }
        }

        public override float GetFloat(int index)
        {
            try
            {
                return _reader.GetFloat(index);
            } 
            catch(Exception ex)
            {
                Logger.Write("GetInt32 failed: "+ex);
                return 0;
            }
        }

        public override float? GetFloatN(int index)
        {
            try
            {
                if (_reader.IsDBNull(index))
                    return null;
                return _reader.GetFloat(index);
            }
            catch (Exception ex)
            {
                Logger.Write("GetFloatN failed: " + ex);
                return 0;
            }
        }

        public override double GetDouble(int index)
        {
            try
            {
                if (_reader.GetFieldType(index) == typeof(decimal))
                {
                    //occurs in: "SELECT AVG(ProductID) FROM Products"
                    return (double)_reader.GetDecimal(index);
                }

                return _reader.GetDouble(index);
            } 
            catch(Exception ex)
            {
                Logger.Write("GetDouble failed: "+ex);
                return 0;
            }
        }

        public override double? GetDoubleN(int index)
        {
            try
            {
                if (_reader.IsDBNull(index))
                    return null;
                return _reader.GetDouble(index);
            }
            catch (Exception ex)
            {
                Logger.Write("GetInt32 failed: " + ex);
                return 0;
            }
        }

        public override decimal GetDecimal(int index)
        {
            try
            {
                return _reader.GetDecimal(index);
            } 
            catch(Exception ex)
            {
                Logger.Write("GetInt32 failed: "+ex);
                return 0;
            }
        }
        public override decimal? GetDecimalN(int index)
        {
            try
            {
                if (_reader.IsDBNull(index))
                    return null;
                return _reader.GetDecimal(index);
            }
            catch (Exception ex)
            {
                Logger.Write("GetDecimal(" + index + ") failed: " + ex);
                return 0;
            }
        }
        public override DateTime GetDateTime(int index)
        {
            try
            {
                return _reader.GetDateTime(index);
            } 
            catch(Exception ex)
            {
                Logger.Write("GetInt32 failed: "+ex);
                return new DateTime();
            }
        }
        public override DateTime? GetDateTimeN(int index)
        {
            try
            {
                if(_reader.IsDBNull(index))
                    return null;
                return _reader.GetDateTime(index);
            } 
            catch(Exception ex)
            {
                Logger.Write("GetInt32 failed: "+ex);
                return new DateTime();
            }
        }

        public override long GetInt64(int index)
        {
            try
            {
                if(_reader.IsDBNull(index))
                    return -1;
                return _reader.GetInt64(index);
            } 
            catch(Exception ex)
            {
                Logger.Write("GetInt64N failed: "+ex);
                return 0;
            }
        }
        public override long? GetInt64N(int index)
        {
            try
            {
                if(_reader.IsDBNull(index))
                    return null;
                return _reader.GetInt64(index);
            } 
            catch(Exception ex)
            {
                Logger.Write("GetInt64N failed: "+ex);
                return 0;
            }
        }

        public override string GetString(int index)
        {
            try
            {
                if(_reader.IsDBNull(index))
                    return null;
                return _reader.GetString(index);
            } 
            catch(Exception ex)
            {
                Logger.Write("GetString("+index+") failed: "+ex);
                return null;
            }
        }

        public override byte[] GetBytes(int index)
        {
            try
            {
                //System.Data.SqlClient.SqlDataReader rdr2;
                //rdr2.GetSqlBinary(); //SqlBinary does not seem to exist on Npgsql
                object obj = _reader.GetValue(index);
                if(obj==null)
                    return null; //nullable blob?
                byte[] bytes = obj as byte[];
                if(bytes!=null)
                    return bytes; //works for BLOB field
                Logger.Write("GetBytes: received unexpected type:"+obj);
                //return _rdr.GetInt32(index);
                return new byte[0];
            } 
            catch(Exception ex)
            {
                Logger.Write("GetBytes failed: "+ex);
                return null;
            }
        }
    }
}