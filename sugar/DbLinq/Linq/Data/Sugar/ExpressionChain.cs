﻿#region MIT license
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

using System.Collections;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace DbLinq.Linq.Data.Sugar
{
    public class ExpressionChain : IEnumerable<Expression>
    {
        public IList<Expression> Expressions { get; private set; }

        public ExpressionChain(IList<Expression> expressions)
        {
            Expressions = expressions;
        }

        public ExpressionChain(IEnumerable expressions)
        {
            Expressions = new List<Expression>();
            foreach (Expression e in expressions)
                Expressions.Add(e);
        }

        public override bool Equals(object obj)
        {
            var other = obj as ExpressionChain;
            if (other == null)
                return false;
            if (Expressions.Count != other.Expressions.Count)
                return false;
            for (int expressionIndex = 0; expressionIndex < Expressions.Count; expressionIndex++)
            {
                if (!ReferenceEquals(Expressions[expressionIndex], other.Expressions[expressionIndex]))
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            foreach (var expression in Expressions)
                hash ^= expression.GetHashCode();
            return hash;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<Expression>)this).GetEnumerator();
        }

        public IEnumerator<Expression> GetEnumerator()
        {
            return Expressions.GetEnumerator();
        }
    }
}
