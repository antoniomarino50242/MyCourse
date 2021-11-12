// Generated by https://quicktype.io

namespace MyCourse.Models.Options
{

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public partial class CoursesOptions
    {
        public long PerPage { get; set; }

        public Order Order { get; set; }
    }

    public partial class Order
    {
        public string By { get; set; }

        public bool Ascending { get; set; }

        public string[] Allow { get; set; }
    }
}
