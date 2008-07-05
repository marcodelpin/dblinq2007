﻿#region MIT license
// 
// MIT license
//
// Copyright (c) 2007-2008 Jiri Moudry, Pascal Craponne
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
using System.Data.Linq.Mapping;
using System.Reflection;

namespace DbLinq.Util
{
    internal static class TypeExtensions
    {
        /// <summary>
        /// Categorize type - this will determine further processing of retrieved types
        /// </summary>
        public static TypeCategory GetCategory(this Type t)
        {
            if (IsPrimitive(t))
                return TypeCategory.Primitive;
            if (IsTable(t))
                return TypeCategory.Column;
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                //make 'double?' also primitive
                Type tInner = t.GetGenericArguments()[0];
                if (IsPrimitive(tInner))
                    return TypeCategory.Primitive;
            }
            return TypeCategory.Other;
        }

        /// <summary>
        /// if T is string or int or DateTime? or friends, return true.
        /// </summary>
        public static bool IsPrimitive(this Type t)
        {
            if (t.IsGenericType)
            {
                Type genericType = t.GetGenericTypeDefinition();
                if (genericType == typeof(Nullable<>))
                {
                    Type[] genericArgs = t.GetGenericArguments();
                    bool ret = IsPrimitive(genericArgs[0]);
                    return ret;
                }
                else
                {
                    return false;
                }
            }

            #region IsBuiltinType
            bool isBuiltinType = t == typeof(string)
                || t == typeof(short)
                || t == typeof(ushort)
                || t == typeof(int)
                || t == typeof(uint)
                || t == typeof(long)
                || t == typeof(ulong)
                || t == typeof(float)
                || t == typeof(double)
                || t == typeof(decimal)
                || t == typeof(char)
                || t == typeof(byte)
                || t == typeof(bool)
                || t == typeof(DateTime) //DateTime: not strictly a primitive time
                || t == typeof(Guid); //Guid: not strictly a primitive time
            return isBuiltinType;
            #endregion
        }

        /// <summary>
        /// a projection has only a default ctor and some fields.
        /// A projected class is generated by the compiler and has a name like $__proj4.
        /// 
        /// Update for Beta2: f__AnonymousType is also a proj, has one ctor, >0 params
        /// </summary>
        public static bool IsProjection(this Type t)
        {
            ConstructorInfo[] cinfo = t.GetConstructors();
            if (cinfo.Length == 0 && t.IsValueType)
                return true; //allow projection into user-defined structs
            if (cinfo.Length < 1)
                return false;
            if (t.Name.Contains("<>f__AnonymousType"))
                return true; //Beta2 logic

            //ParameterInfo[] ctor0_params = cinfo[0].GetParameters();
            //if (ctor0_params.Length != 0)
            //    return false;
            return true;
        }

        /// <summary>
        /// is this a type IQueryable{DynamicClass1} ?
        /// where DynamicClass1 descends from DynamicClass 
        /// (see Dynamic Linq and test DL4_DynamicAssociationProperty)
        /// </summary>
        public static bool IsDynamicClass(this Type t, out Type dynamicClassType)
        {
            dynamicClassType = null;
            if (!t.IsGenericType)
                return false;

            Type genericDef = t.GetGenericTypeDefinition();
            if (genericDef != typeof(System.Linq.IQueryable<>))
                return false;

            Type[] genericArg = t.GetGenericArguments();
            if (genericArg.Length == 1 && genericArg[0].Name.StartsWith("DynamicClass"))
            {
                //occurs for DynamicType, see test DL4_DynamicAssociationProperty
                dynamicClassType = genericArg[0];
                return true;
            }
            return false;
        }

        /// <summary>
        /// is this a type with a [Table] attribute?
        /// (Alternatively, you could check if it implements IModified)
        /// </summary>
        public static bool IsTable(this Type t)
        {
            TableAttribute tableAttribute = AttribHelper.GetTableAttrib(t);
            return (tableAttribute != null);
        }

        /// <summary>
        /// Determines if a given type can have a null value
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool CanBeNull(this Type t)
        {
            return IsNullable(t) || !t.IsValueType;
        }

        /// <summary>
        /// Returns a unique MemberInfo
        /// </summary>
        /// <param name="t">The declaring type</param>
        /// <param name="name">The member name</param>
        /// <returns>A MemberInfo or null</returns>
        public static MemberInfo GetSingleMember(this Type t, string name)
        {
            return GetSingleMember(t, name, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
        }

        /// <summary>
        /// Returns a unique MemberInfo
        /// </summary>
        /// <param name="t">The declaring type</param>
        /// <param name="name">The member name</param>
        /// <param name="bindingFlags">Binding flags</param>
        /// <returns>A MemberInfo or null</returns>
        public static MemberInfo GetSingleMember(this Type t, string name, BindingFlags bindingFlags)
        {
            var members = t.GetMember(name, bindingFlags);
            if (members.Length > 0)
                return members[0];
            return null;
        }

        /// <summary>
        /// Determines if a Type is specified as nullable
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool IsNullable(this Type t)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        /// <summary>
        /// If the type is nullable, returns the underlying type
        /// Undefined behavior otherwise (it's user responsibility to check for Nullable first)
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Type GetNullableType(this Type t)
        {
            return Nullable.GetUnderlyingType(t);
        }

        /// <summary>
        /// Returns default value for provided type
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static object GetDefault(this Type t)
        {
            return TypeConvert.GetDefault(t);
        }
    }
}
