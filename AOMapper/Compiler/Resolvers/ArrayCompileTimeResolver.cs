using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AOMapper.Compiler.Helpers;
using AOMapper.Data;
using AOMapper.Extensions;
using AOMapper.Interfaces;
using AOMapper.Resolvers;

namespace AOMapper.Compiler.Resolvers
{
    public class ArrayCompileTimeResolver<TS, TD> :
        CompileTimeResolver
    {
        public ArrayCompileTimeResolver(IMap map, Resolver parent)
            : base(map, parent)
        {
            //if(!_parent.DestinationType.IsArray) throw new ArgumentException("Cannot resolve requested type");
        }

        [SuppressMessage("ReSharper", "RedundantAssignment")]
        public override Expression Resolve(MemberExpression destinationExpression, Expression sourceExpression,
            Expression destinationParameterExpression, Expression sourceParameterExpression)
        {
            ParameterExpression srcParameterExpression, destParameterExpression;

            var resolver = typeof (TS) != typeof (TD) ? Resolver.Create(typeof (TS), typeof (TD), _map) : null;
            IMap<TS, TD> map;
            map = resolver != null
                ? Mapper.Create<TS, TD>(resolver.CompileTimeResolver)
                : Mapper.Create<TS, TD>();

            var mapExpression = map
                .ConfigMap(o => _map.ConfigMap(x => o = x))
                .Auto()
                .CompileToExpression(out srcParameterExpression, out destParameterExpression);          

            var collType = typeof (ICollection<TS>); //.MakeGenericType(_parent.SouceType);
            var collection = Expression.Convert(sourceExpression, collType);
            var count = Expression.Property(collection, "Count");
            //var lstType = typeof (IList);
            //collection = Expression.Convert(sourceExpression, lstType);
            var isFixed = Expression.Property(collection, "IsReadOnly");
            var isArray = destinationExpression.Type.IsArray;

            collType = typeof (IList<TS>); //.MakeGenericType(_parent.SouceType);
            collection = Expression.Convert(sourceExpression, collType);

            Expression<Action<IList<TD>, TD>> addMethodExpression = (ds, d) => ds.Add(d);
            Expression<Action<TD, IList<TD>, int>> setItemMethodExpression =
                (d, ds, arg3) => SetItemByIndex(d, ds, arg3);
            Expression<Func<IList<TS>, int, TS>> getItemMethodExpression = (ss, i1) => ss[i1];//GetItemByIndex(ss, i1);

            var newCollection = Expression.Assign(
                destinationExpression,
                Expression.New(_parent.DestinationType.GetConstructor(new[] {typeof (int)}), count)
                );

            var i = Expression.Parameter(typeof (int), "i");
            var expressionRewriter = new ExpressionRewriter();
            BlockExpression @for;
            if (!isArray)
            {                               
                @for = Expression.Block(
                    newCollection,
                    collection.For(i, Expression.Block(new[] {srcParameterExpression, destParameterExpression},
                        Expression.IfThenElse(
                            Expression.Equal(isFixed, Expression.Constant(true)),
                            Expression.Block(
                                Expression.Assign(srcParameterExpression,
                                    expressionRewriter.AutoInline(Expression.Invoke(getItemMethodExpression, collection,
                                        i))),
                                expressionRewriter.AutoInline(Expression.Invoke(setItemMethodExpression, mapExpression,
                                    destinationExpression, i))
                                ),
                            Expression.Block(
                                Expression.Assign(srcParameterExpression,
                                    Expression.Invoke(getItemMethodExpression, collection, i)),
                                expressionRewriter.AutoInline(Expression.Invoke(addMethodExpression,
                                    destinationExpression, mapExpression))
                                )
                            )
                        ),
                        condition: Expression.LessThan(i, count)
                        )
                    );
            }
            else
            {                
                @for = Expression.Block(
                    newCollection,
                    collection.For(i, Expression.Block(new[] {srcParameterExpression, destParameterExpression},
                        Expression.Block(
                            Expression.Assign(srcParameterExpression,
                                expressionRewriter.AutoInline(Expression.Invoke(getItemMethodExpression, collection, i))),
                            expressionRewriter.AutoInline(Expression.Invoke(setItemMethodExpression, mapExpression,
                                destinationExpression, i))
                            )
                        ),
                        condition: Expression.LessThan(i, count)
                        )
                    );
            }

            return @for;
        }

        public static void SetItemByIndex(TD item, IList<TD> list, int index)
        {
            list[index] = item;
        }

        public static TS GetItemByIndex(IList<TS> list, int index)
        {
            return list[index];
        }
    }
}