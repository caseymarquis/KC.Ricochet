using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections;
using System.Runtime.CompilerServices;

namespace KC.Ricochet {

    public class InstantiatorCache {
        private static object lockCaches = new object();
        private static Dictionary<Type, InstantiatorCache> caches = new Dictionary<Type, InstantiatorCache>();

        public IEnumerable<Instantiator> Instantiators => m_Instantiators;
        private Instantiator[] m_Instantiators;

        public static IEnumerable<TypedInstantiator<T>> GetAll<T>() {
            return GetAll(typeof(T)).Select(x => new TypedInstantiator<T>(x));
        }

        public static IEnumerable<Instantiator> GetAll(Type classType) {
            InstantiatorCache cache = null;
            lock (lockCaches) {
                if (!caches.TryGetValue(classType, out cache)) {
                    cache = new InstantiatorCache(classType);
                    caches[classType] = cache;
                }
            }
            return cache.Instantiators;
        }

        public static TypedInstantiator<T> Get<T>(params Type[] parameterTypes) {
            return new TypedInstantiator<T>(Get(typeof(T), parameterTypes));
        }

        public static Instantiator Get(Type classType, params Type[] parameterTypes) {
            var instantiators = GetAll(classType);
            var ret = instantiators.FirstOrDefault(x => {
                if (x.ParameterTypes.Length != parameterTypes.Length) {
                    return false;
                }
                for (int i = 0; i < parameterTypes.Length; i++) {
                    if (x.ParameterTypes[i] != parameterTypes[i]) {
                        return false;
                    }
                }
                return true;
            });
            if (ret == null) {
                throw new ApplicationException("There is no constructor which matches the given types.");
            }
            return ret;
        }

        public InstantiatorCache(Type classType) {
            var typeInfo = classType.GetTypeInfo();
            var constructors = typeInfo.GetAllConstructors(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic)
                .ToArray();
            m_Instantiators = constructors.Select(x => new Instantiator(x)).ToArray();
        }

    }
}
