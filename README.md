# Ricochet

This is a library for a few simple fast reflection operations.
It's largely used for automagically copying properties and fields between
similar types (ie copying from EntityFramework models to Web Controller models).
There is also a bit of functionality for marking types with attributes, or ignoring them.


### Installing

* .Net Framework: Install-Package KC.Ricochet
* .Net Core: dotnet add package KC.Ricochet

## Getting Started

```C#
using System;
using System.Linq;
using KC.Ricochet;

namespace Example.Ricochet
{
    class Program
    {
        static void Main(string[] args)
        {
            var dbModel = new DataBaseModel() {
                Id = 1,
                Name = "Bob",
                Date = DateTime.UtcNow
            };
            var webModel = new WebModel();

            //Copy all public properties from the dbModel to the webModel ignoring the case of the property name
            KC.Ricochet.Util.CopyPublicValueProps(fromT: dbModel, toU: webModel, ignoreCase: true, copyNullMembers: true);

            //Copy all public properties from another webModel to the dbModel, ignoring case, and not copying any properties which are set to null.
            var userModifiedWebModel = new WebModel() {
                id = 1,
                name = "Bob Belcher"
            };

            KC.Ricochet.Util.CopyPublicValueProps(fromT: userModifiedWebModel, toU: dbModel, ignoreCase: true, copyNullMembers: false);
            //You could then save the dbModel, knowing only user specified (non-null) items were changed.

            //You can implement your own functions like the above. This is how the Copy function was implemented:
            void CopyPublicValueProps<T, U>(T fromT, U toU, bool ignoreCase = true, bool copyNullMembers = false) where T : class where U : class {
                Copy(fromT, toU, x => x.IsPublic && x.IsValueOrString && x.IsProperty, ignoreCase, copyNullMembers);
            }

            void Copy<T, U>(T fromT, U toU, Func<PropertyAndFieldAccessor, bool> predicate = null, bool ignoreCase = true, bool copyNullMembers = false) where T : class where U : class {
                var fromProps = Util.GetPropsAndFields<T>();
                var toProps = Util.GetPropsAndFields<U>();
                if (predicate != null) {
                    fromProps = fromProps.Where(predicate);
                    toProps = toProps.Where(predicate);
                }

                foreach (var toProp in toProps) {
                    var fromProp = fromProps.FirstOrDefault(x => string.Compare(x.Name, toProp.Name, ignoreCase) == 0);
                    if (fromProp == null) {
                        continue;
                    }

                    var fromValue = fromProp.GetVal(fromT);
                    if (!copyNullMembers && object.Equals(fromValue, null)) {
                        continue;
                    }

                    toProp.SetVal(toU, fromValue);
                }
            }

            //You can also get members which you've marked:
            var nameMembers = KC.Ricochet.Util.GetPropsAndFields<WebModel>(x => x.Markers.Contains("IsAName"));
            //See class definition below with RicochetMark attribute.
            //This allows you to implement special logic with marked members.
        }
    }

    class DataBaseModel {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime Date { get; set; }
        public string SecretData { get; set; }
    }

    class WebModel {
        public int id { get; set; }
        [RicochetMark("IsAName")]
        public string name { get; set; }
        public DateTime? date { get; set; }
        [RicochetIgnore] //Ensure that this secret data must be manually copied into a WebModel.
        public string secretData { get; set; }
    }
}
```
