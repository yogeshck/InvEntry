﻿using DevExpress.Data.Filtering.Helpers;
using DevExpress.Data.Filtering;
using DevExpress.DataAccess.DataFederation;
using DevExpress.Xpf.Map;
using InvEntry.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using DevExpress.CodeParser;

namespace InvEntry.Extension
{
    public static class ExpressionExtensions
    {
        private static IDictionary<Type, EvaluatorContextDescriptorDefault> _lookup = new Dictionary<Type, EvaluatorContextDescriptorDefault>();
        public static string GetMemberName<TSource, TProperty>(this Expression<Func<TSource, TProperty>> lamdaExpression)
        {
            MemberExpression body = lamdaExpression.Body as MemberExpression;

            if (body == null)
            {
                UnaryExpression ubody = (UnaryExpression)lamdaExpression.Body;
                body = ubody.Operand as MemberExpression;
            }

            return body.Member.Name;
        }

        public static TProperty? Evaluate<TSource,TProperty>(this Formula formula, TSource source)
            where TSource : class
            where TProperty : struct
        {
            try
            {
                CriteriaOperator op = CriteriaOperator.Parse(formula.Expression);

                EvaluatorContextDescriptorDefault? descriptor;

                if (!_lookup.TryGetValue(formula.Type, out descriptor))
                {
                    descriptor = new(formula.Type);
                    _lookup[formula.Type] = descriptor;
                }

                ExpressionEvaluator evaluator = new ExpressionEvaluator(descriptor, op);
                return (TProperty)evaluator.Evaluate(source);
            }
            catch(Exception ex)
            {
                return null;
            }
        }
    }
}
