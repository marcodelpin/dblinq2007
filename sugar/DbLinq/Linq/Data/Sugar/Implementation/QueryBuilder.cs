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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using DbLinq.Factory;
using DbLinq.Linq.Data.Sugar.Expressions;
using DbLinq.Logging;
using DbLinq.Util;

namespace DbLinq.Linq.Data.Sugar.Implementation
{
    /// <summary>
    /// Full query builder, with cache management
    /// 1. Parses Linq Expression
    /// 2. Generates SQL
    /// </summary>
    public class QueryBuilder : IQueryBuilder
    {
        public IExpressionLanguageParser ExpressionLanguageParser { get; set; }
        public IExpressionOptimizer ExpressionOptimizer { get; set; }
        public IExpressionDispatcher ExpressionDispatcher { get; set; }

        protected virtual ExpressionQuery BuildExpressionQuery(ExpressionChain expressions, QueryContext queryContext)
        {
            var builderContext = new BuilderContext(queryContext);
            BuildExpressionQuery(expressions, builderContext);
            CheckTablesAlias(builderContext);
            return builderContext.PiecesQuery;
        }

        protected virtual IList<Expression> FindPiecesByName(string name, BuilderContext builderContext)
        {
            var expressions = new List<Expression>();
            expressions.AddRange(from t in builderContext.EnumerateTables() where t.Alias == name select (Expression)t);
            expressions.AddRange(from c in builderContext.EnumerateColumns() where c.Alias == name select (Expression)c);
            return expressions;
        }

        protected virtual string GetAnonymousTableName(string aliasBase, int anonymousIndex, BuilderContext builderContext)
        {
            if (string.IsNullOrEmpty(aliasBase))
                aliasBase = "t";
            return string.Format("{0}{1}", aliasBase, anonymousIndex);
        }

        /// <summary>
        /// Give all non-aliased tables a name
        /// </summary>
        /// <param name="builderContext"></param>
        protected virtual void CheckTablesAlias(BuilderContext builderContext)
        {
            int anonymousIndex = 0;
            foreach (TableExpression tableExpression in builderContext.EnumerateTables())
            {
                // if no alias, or duplicate alias
                if (string.IsNullOrEmpty(tableExpression.Alias) ||
                    FindPiecesByName(tableExpression.Alias, builderContext).Count > 1)
                {
                    var aliasBase = tableExpression.Alias;
                    // we try to assign one until we have a unique alias
                    do
                    {
                        tableExpression.Alias = GetAnonymousTableName(aliasBase, ++anonymousIndex, builderContext);
                    } while (FindPiecesByName(tableExpression.Alias, builderContext).Count != 1);
                }
            }
            // TODO: move down this to IVendor
            foreach (TableExpression tablePiece in builderContext.EnumerateTables())
            {
                tablePiece.Alias += "$";
            }
        }

        protected virtual void BuildExpressionQuery(ExpressionChain expressions, BuilderContext builderContext)
        {
            var previousExpression = ExpressionDispatcher.RegisterTable(expressions.Expressions[0], builderContext);
            foreach (var expression in expressions)
            {
                builderContext.QueryContext.DataContext.Logger.WriteExpression(Level.Debug, expression);
                // Convert linq Expressions to QueryOperationExpressions and QueryConstantExpressions 
                // Query expressions language identification and optimization
                var currentExpression = ExpressionLanguageParser.Parse(expression, builderContext);
                currentExpression = ExpressionOptimizer.Optimize(currentExpression, builderContext);
                // Query expressions query identification 
                previousExpression = ExpressionDispatcher.Analyze(currentExpression, previousExpression, builderContext);
            }
            // the last return value becomes the select, with CurrentScope
            builderContext.CurrentScope.Operands.Add(previousExpression);
            builderContext.PiecesQuery.Select = builderContext.CurrentScope;
        }

        protected virtual Query BuildSqlQuery(ExpressionQuery expressionQuery, QueryContext queryContext)
        {
            var sql = "";
            var sqlQuery = new Query(sql, expressionQuery.Parameters);
            return sqlQuery;
        }

        [DbLinqToDo]
        protected virtual Query GetFromCache(ExpressionChain expressions)
        {
            return null;
        }

        [DbLinqToDo]
        protected virtual void SetInCache(ExpressionChain expressions, Query sqlQuery)
        {
        }

        public Query GetQuery(ExpressionChain expressions, QueryContext queryContext)
        {
            var query = GetFromCache(expressions);
            if (query == null)
            {
                var expressionsQuery = BuildExpressionQuery(expressions, queryContext);
                query = BuildSqlQuery(expressionsQuery, queryContext);
                SetInCache(expressions, query);
            }
            throw new Exception("Can't go further anyway...");
            return query;
        }

        public QueryBuilder()
        {
            ExpressionLanguageParser = ObjectFactory.Get<IExpressionLanguageParser>();
            ExpressionOptimizer = ObjectFactory.Get<IExpressionOptimizer>();
            ExpressionDispatcher = ObjectFactory.Get<IExpressionDispatcher>();
        }
    }
}
