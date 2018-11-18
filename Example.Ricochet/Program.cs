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

            //Copy all properties from the dbModel to the webModel ignoring the case of the property name
            KC.Ricochet.Util.CopyProps(fromT: dbModel, toU: webModel, ignoreCase: true, copyNullProperties: true);

            //Copy all properties from another webModel to the dbModel, ignoring case, and not copying any properties which are set to null.
            var userModifiedWebModel = new WebModel() {
                id = 1,
                name = "Bob Belcher"
            };

            KC.Ricochet.Util.CopyProps(fromT: userModifiedWebModel, toU: dbModel, ignoreCase: true, copyNullProperties: true);
            //You could then save the dbModel, knowing only user specified items were changed.

            //You can implement your own functions like the above. This is how the CopyProps function was implemented:
            void CopyProps<T, U>(T fromT, U toU, bool ignoreCase = true, bool copyNullProperties = false) where T : class where U : class {
                var fromProps = Util.GetProps<T>();
                var toProps = Util.GetProps<U>();

                foreach (var toProp in toProps.ValueAndStringProperties) {
                    var fromProp = fromProps.ValueAndStringProperties.FirstOrDefault(x => string.Compare(x.Name, toProp.Name, true) == 0);
                    if (fromProp == null) {
                        continue;
                    }

                    var fromValue = fromProp.GetVal(fromT);
                    if (!copyNullProperties && object.Equals(fromValue, null)) {
                        continue;
                    }

                    toProp.SetVal(toU, fromValue);
                }
            }

            //You can also get properties which you've marked:
            var nameProperties = KC.Ricochet.Util.GetProps<WebModel>()
                .AllProperties.Where(x => x.Markers.Contains("IsAName")); //See the class definition below.

            //This allows you to implement special logic with marked properties.
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
