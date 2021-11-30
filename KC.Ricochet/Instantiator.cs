using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace KC.Ricochet {
    public struct TypedInstantiator<T> {
        private Instantiator instantiator;
        internal TypedInstantiator(Instantiator i) {
            this.instantiator = i;
        }

        public T New(params object[] args) {
            return instantiator.New<T>(args);
        }

        public ConstructorInfo ConstructorInfo => instantiator.ConstructorInfo;
    }

    public class Instantiator {
        public ConstructorInfo ConstructorInfo { get; private set; }
        public Type[] ParameterTypes { get; private set; }
        public bool IsPublic => ConstructorInfo.IsPublic;

        private delegate object Creator(params object[] args);
        public object New(params object[] args) {
            return compiledLambda(args);
        }

        public T New<T>(params object[] args) {
            if (args?.Length != ParameterTypes.Length) {
                throw new ApplicationException($"Incorrect number of arguments. Expected {ParameterTypes.Length}");
            }
            return (T)compiledLambda(args);
        }

        private Creator compiledLambda;
        public Instantiator(ConstructorInfo ctorInfo) {
            this.ConstructorInfo = ctorInfo;
            this.ParameterTypes = ctorInfo.GetParameters().OrderBy(x => x.Position).Select(x => x.ParameterType).ToArray();
            var hasArgs = ParameterTypes.Length > 0;

            //http://mattgabriel.co.uk/2016/02/10/object-creation-using-lambda-expression/
            var paramsExpr = Expression.Parameter(typeof(object[]), "args");
            var argsExpressions = Enumerable.Range(0, ParameterTypes.Length)
                .Select(i => new { i, indexExpr = Expression.Constant(i) })
                .Select(x => new { x.i, paramAccessorExpr = Expression.ArrayIndex(paramsExpr, x.indexExpr) })
                .Select(x => Expression.Convert(x.paramAccessorExpr, ParameterTypes[x.i]))
                .ToArray();

            var newExpr = hasArgs? Expression.New(ConstructorInfo, argsExpressions) : Expression.New(ConstructorInfo);
            var lambda = Expression.Lambda(typeof(Creator), newExpr, paramsExpr);
            compiledLambda = (Creator)lambda.Compile();
        }

        public bool Matches(params Type[] args) {
            if (args == null || args.Length == 0) {
                return ParameterTypes.Length == 0;
            }
            if (args.Length != ParameterTypes.Length) {
                return false;
            }

            for (int i = 0; i < args.Length; i++) {
                if (args[i] != ParameterTypes[i]) {
                    return false;
                }
            }
            return true;
        }
    }
}
