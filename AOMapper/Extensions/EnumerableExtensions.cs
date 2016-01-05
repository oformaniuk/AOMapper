using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace AOMapper.Extensions
{
    internal static class EnumerableExtensions
    {
        public static BlockExpression ForEach<T>(this IEnumerable<T> source, string collectionName, string itemName)
        {
            var item = Expression.Variable(typeof(T), itemName);

            var enumerator = Expression.Variable(typeof(IEnumerator<T>), "enumerator");

            var param = Expression.Parameter(typeof(IEnumerable<T>), collectionName);

            var doMoveNext = Expression.Call(enumerator, typeof(IEnumerator).GetMethod("MoveNext"));

            var assignToEnum = Expression.Assign(enumerator, Expression.Call(param, typeof(IEnumerable<T>).GetMethod("GetEnumerator")));

            var assignCurrent = Expression.Assign(item, Expression.Property(enumerator, "Current"));

            var @break = Expression.Label();

            var @foreach = Expression.Block(
                assignToEnum,
                Expression.Loop(
                    Expression.IfThenElse(
                    Expression.NotEqual(doMoveNext, Expression.Constant(false)),
                        assignCurrent
                    , Expression.Break(@break))
                , @break)
            );
            return @foreach;

        }

        public static Expression For(this Expression collection, ParameterExpression loopVar, Expression loopContent, Expression initValue = null, Expression condition = null, Expression increment = null)
        {
            if (increment == null)
                increment = Expression.PostIncrementAssign(loopVar);

            if(initValue == null)
                initValue = Expression.Constant(0);

            if(condition == null)
                condition = Expression.LessThan(loopVar, Expression.ArrayLength(collection));            

            var initAssign = Expression.Assign(loopVar, initValue);

            var breakLabel = Expression.Label("LoopBreak");

            var loop = Expression.Block(new[] { loopVar },
                initAssign,
                Expression.Loop(
                    Expression.IfThenElse(
                        condition,
                        Expression.Block(
                            loopContent,
                            increment
                        ),
                        Expression.Break(breakLabel)
                    ),
                breakLabel)
            );

            return loop;
        }
    }
}