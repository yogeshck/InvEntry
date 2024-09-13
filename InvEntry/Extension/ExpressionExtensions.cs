using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Extension
{
    public static class ExpressionExtensions
    {
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
    }
}
